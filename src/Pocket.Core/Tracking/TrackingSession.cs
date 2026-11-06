namespace Pocket.Core.Tracking;

public enum TrackingSource { Simulation, Camera, Recorded }
public enum TrackingState { Off, Starting, Ready, Observing, Lost, Stopping, Stopped, Unavailable, Stale, Partial, Ambiguous }
public enum TrackingBreak { None, Loss, Partial, Ambiguous, Gap, HandChanged, Movement, Stale }
public sealed record TrackingCounters(long Hand, long Loss, long Partial, long Ambiguous,
    IReadOnlyDictionary<string, long> Breaks);

public sealed record TrackingSnapshot(TrackingState State, Guid? SessionId, TrackingSource? Source,
    HandObservation? Observation, TimeSpan? HostObservationAge, TrackingFailure? Failure,
    bool CleanupUnconfirmed, long AcceptedObservations, long SupersededObservations,
    long Continuity = 0, TrackingBreak LastBreak = TrackingBreak.None, long LastSequence = -1);

/// <summary>Owns a session and one latest observation. The caller owns the worker and resource teardown.</summary>
public sealed class TrackingSession(TimeProvider? clock = null)
{
    public static TimeSpan ObservationLifetime { get; } = TimeSpan.FromMilliseconds(250);
    public static TimeSpan StartupDeadline { get; } = TimeSpan.FromSeconds(5);
    public static TimeSpan HealthDeadline { get; } = TimeSpan.FromSeconds(3);
    public static TimeSpan StopDeadline { get; } = TimeSpan.FromSeconds(2);

    private readonly TimeProvider _clock = clock ?? TimeProvider.System;
    private readonly object _gate = new();
    private Guid? _session;
    private TrackingSource? _source;
    private TrackingState _state = TrackingState.Off;
    private TrackingFailure? _failure;
    private HandObservation? _latest;
    private long _stateSince, _lastContact, _receivedAt;
    private long _lastSequence = -1, _lastCapture, _lastResult;
    private long _accepted, _superseded;
    private long _continuity;
    private TrackingBreak _lastBreak;
    private bool _observedByConsumer;
    private readonly long[] _breakCounts = new long[Enum.GetValues<TrackingBreak>().Length];
    private long _hand, _loss, _partial, _ambiguous;

    public TrackingCounters ReadCounters()
    {
        lock (_gate)
            return new(_hand, _loss, _partial, _ambiguous,
                Enum.GetValues<TrackingBreak>().Where(reason => reason != TrackingBreak.None)
                    .ToDictionary(reason => reason.ToString(), reason => _breakCounts[(int)reason]));
    }

    public Guid? Enable(TrackingSource source)
    {
        if (!Enum.IsDefined(source)) throw new ArgumentOutOfRangeException(nameof(source));
        lock (_gate)
        {
            if (_session.HasValue) return null;
            _session = Guid.NewGuid();
            _source = source;
            _state = TrackingState.Starting;
            _stateSince = _lastContact = _clock.GetTimestamp();
            _failure = null;
            _lastSequence = -1;
            _lastCapture = _lastResult = _accepted = _superseded = 0;
            _continuity = 0;
            _lastBreak = TrackingBreak.None;
            _hand = _loss = _partial = _ambiguous = 0;
            Array.Clear(_breakCounts);
            return _session;
        }
    }

    public bool Accept(TrackerMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        lock (_gate)
        {
            long now = _clock.GetTimestamp();
            Advance(now);
            if (_session != message.SessionId) return false;
            if (_state == TrackingState.Stopping) return false;

            switch (message)
            {
                case TrackerReady when _state == TrackingState.Starting:
                    _state = TrackingState.Ready;
                    _receivedAt = _lastContact = now;
                    return true;
                case TrackerHealth when IsReceiving:
                    _lastContact = now;
                    return true;
                case HandObservation observation when IsReceiving:
                    return AcceptObservation(observation, now);
                case TrackerError error when Enum.IsDefined(error.Failure):
                    BeginStop(now, error.Failure);
                    return true;
                default:
                    return false;
            }
        }
    }

    public bool Stop() => RequestStop(null, null);

    public bool ReportProtocolFailure(Guid sessionId) => ReportFailure(sessionId, TrackingFailure.ProtocolFailure);

    public bool ReportFailure(Guid sessionId, TrackingFailure failure)
    {
        if (!Enum.IsDefined(failure)) throw new ArgumentOutOfRangeException(nameof(failure));
        return RequestStop(sessionId, failure);
    }

    public TrackingState Tick()
    {
        lock (_gate)
        {
            Advance(_clock.GetTimestamp());
            return _state;
        }
    }

    private bool RequestStop(Guid? sessionId, TrackingFailure? failure)
    {
        lock (_gate)
        {
            if (!_session.HasValue || _state == TrackingState.Stopping || (sessionId.HasValue && _session != sessionId))
                return false;
            BeginStop(_clock.GetTimestamp(), failure);
            return true;
        }
    }

    /// <summary>Call only after the process owning the source is independently observed to have exited.</summary>
    public bool ConfirmWorkerExit(Guid sessionId)
    {
        lock (_gate)
        {
            if (_session != sessionId) return false;
            if (_state != TrackingState.Stopping) _failure = TrackingFailure.WorkerExited;
            CompleteStop();
            return true;
        }
    }

    public TrackingSnapshot Read()
    {
        lock (_gate)
        {
            long now = _clock.GetTimestamp();
            Advance(now);
            _observedByConsumer = true;
            return new(_state, _session, _source, _latest,
                _latest is null ? null : _clock.GetElapsedTime(_receivedAt, now), _failure,
                _state == TrackingState.Stopping && _clock.GetElapsedTime(_stateSince, now) >= StopDeadline,
                _accepted, _superseded, _continuity, _lastBreak, _lastSequence);
        }
    }

    private bool AcceptObservation(HandObservation observation, long now)
    {
        if (!observation.IsValid || observation.Sequence <= _lastSequence ||
            observation.CaptureMonoMs < _lastCapture || observation.ResultMonoMs < _lastResult ||
            observation.ResultMonoMs - observation.CaptureMonoMs >= ObservationLifetime.TotalMilliseconds)
            return false;

        if (_latest is { } prior)
        {
            var reason = observation switch
            {
                { Ambiguous: true } => TrackingBreak.Ambiguous,
                { Partial: true } => TrackingBreak.Partial,
                { Tracked: false } => TrackingBreak.Loss,
                _ when observation.FrameSize != prior.FrameSize || observation.Handedness != prior.Handedness => TrackingBreak.HandChanged,
                _ when observation.CaptureMonoMs - prior.CaptureMonoMs is <= 0 or > 220 => TrackingBreak.Gap,
                _ when Jumped(prior, observation, 0) || Jumped(prior, observation, 8) => TrackingBreak.Movement,
                _ => TrackingBreak.None
            };
            if (reason != TrackingBreak.None) Break(reason);
        }
        if (_lastSequence >= 0 && !_observedByConsumer) _superseded++;
        _lastSequence = observation.Sequence;
        _lastCapture = observation.CaptureMonoMs;
        _lastResult = observation.ResultMonoMs;
        _receivedAt = _lastContact = now;
        _accepted++;
        if (observation.Tracked) _hand++;
        else if (observation.Ambiguous) _ambiguous++;
        else if (observation.Partial) _partial++;
        else _loss++;
        _latest = observation.Tracked ? observation : null;
        _observedByConsumer = false;
        _state = observation.Ambiguous ? TrackingState.Ambiguous : observation.Partial ? TrackingState.Partial :
            observation.Tracked ? TrackingState.Observing : TrackingState.Lost;
        return true;
    }

    private void Advance(long now)
    {
        if (_state == TrackingState.Starting && _clock.GetElapsedTime(_stateSince, now) >= StartupDeadline)
            BeginStop(now, TrackingFailure.Timeout);
        else if (IsReceiving)
        {
            if (_clock.GetElapsedTime(_lastContact, now) >= HealthDeadline)
                BeginStop(now, _source != TrackingSource.Camera ? TrackingFailure.Timeout :
                    _accepted == 0 ? TrackingFailure.NoFrames : TrackingFailure.StreamInterrupted);
            else if (_clock.GetElapsedTime(_receivedAt, now) >= ObservationLifetime)
            {
                if (_latest is not null) Break(TrackingBreak.Stale);
                _latest = null;
                _state = TrackingState.Stale;
            }
        }
    }

    private bool IsReceiving => _state is TrackingState.Ready or TrackingState.Observing or
        TrackingState.Lost or TrackingState.Stale or TrackingState.Partial or TrackingState.Ambiguous;

    private void Break(TrackingBreak reason)
    {
        _continuity++;
        _lastBreak = reason;
        _breakCounts[(int)reason]++;
    }

    private static bool Jumped(HandObservation prior, HandObservation current, int index) =>
        Math.Abs(prior.Landmarks[index].X - current.Landmarks[index].X) > .14 ||
        Math.Abs(prior.Landmarks[index].Y - current.Landmarks[index].Y) > .14;

    private void BeginStop(long now, TrackingFailure? failure)
    {
        _latest = null;
        _failure = failure;
        _state = TrackingState.Stopping;
        _stateSince = now;
    }

    private void CompleteStop()
    {
        _latest = null;
        _session = null;
        _source = null;
        _state = _failure.HasValue ? TrackingState.Unavailable : TrackingState.Stopped;
    }
}
