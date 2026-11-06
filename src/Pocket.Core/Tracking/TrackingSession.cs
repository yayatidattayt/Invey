namespace Pocket.Core.Tracking;

public enum TrackingSource { Simulation, Camera, Recorded }
public enum TrackingState { Off, Starting, Ready, Observing, Lost, Stopping, Stopped, Unavailable, Stale, Partial }

public sealed record TrackingSnapshot(TrackingState State, Guid? SessionId, TrackingSource? Source,
    HandObservation? Observation, TimeSpan? HostObservationAge, TrackingFailure? Failure,
    bool CleanupUnconfirmed, long AcceptedObservations, long SupersededObservations);

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
    private bool _observedByConsumer;

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
                _accepted, _superseded);
        }
    }

    private bool AcceptObservation(HandObservation observation, long now)
    {
        if (!observation.IsValid || observation.Sequence <= _lastSequence ||
            observation.CaptureMonoMs < _lastCapture || observation.ResultMonoMs < _lastResult ||
            observation.ResultMonoMs - observation.CaptureMonoMs >= ObservationLifetime.TotalMilliseconds)
            return false;

        _lastSequence = observation.Sequence;
        _lastCapture = observation.CaptureMonoMs;
        _lastResult = observation.ResultMonoMs;
        _receivedAt = _lastContact = now;
        _accepted++;
        if (_latest is not null && !_observedByConsumer) _superseded++;
        _latest = observation.Tracked ? observation : null;
        _observedByConsumer = false;
        _state = observation.Partial ? TrackingState.Partial :
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
                _latest = null;
                _state = TrackingState.Stale;
            }
        }
    }

    private bool IsReceiving => _state is TrackingState.Ready or TrackingState.Observing or
        TrackingState.Lost or TrackingState.Stale or TrackingState.Partial;

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
