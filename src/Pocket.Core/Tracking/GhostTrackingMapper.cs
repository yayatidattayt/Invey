namespace Pocket.Core.Tracking;

/// <summary>Raw validated observation → calibration → compatible personal correction → one filter → physical desktop.</summary>
public sealed class GhostTrackingMapper(TimeProvider? clock = null)
{
    private FingerPointer _eligibility = new(false);
    private readonly PrecisionPointFilter _filter=new();
    private LearningContext? _context;
    private Guid? _session, _model;
    private long _sequence=-1;
    private DipPoint? _position;
    public GhostProvenance Provenance { get; private set; }
    private string _status="Pointer preview off";
    private readonly Dictionary<string,long> _rejectionPolls=[];
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;
    private readonly Dictionary<string,long> _reasonEntries = [];
    private readonly Dictionary<string,double> _reasonMilliseconds = [];
    private long? _reasonSince;
    public IReadOnlyDictionary<string,long> ReasonEntries => _reasonEntries;
    public IReadOnlyDictionary<string,double> ReasonMilliseconds
    {
        get
        {
            var result = new Dictionary<string,double>(_reasonMilliseconds);
            if (_reasonSince is { } start)
                result[_status] = result.GetValueOrDefault(_status) + _clock.GetElapsedTime(start).TotalMilliseconds;
            return result;
        }
    }
    /// <summary>UI-observed rejection polls, not worker frames; bounded reason vocabulary.</summary>
    public IReadOnlyDictionary<string,long> UiRejectionPolls=>_rejectionPolls;
    public string Status
    {
        get => _status;
        private set
        {
            if (!value.StartsWith("Pointer preview active",StringComparison.Ordinal))
                _rejectionPolls[value] = _rejectionPolls.GetValueOrDefault(value) + 1;
            if (value == _status && _reasonSince.HasValue) return;
            long now = _clock.GetTimestamp();
            if (_reasonSince is { } start)
                _reasonMilliseconds[_status] = _reasonMilliseconds.GetValueOrDefault(_status) + _clock.GetElapsedTime(start, now).TotalMilliseconds;
            _status = value; _reasonSince = now;
            _reasonEntries[value] = _reasonEntries.GetValueOrDefault(value) + 1;
        }
    }
    private void ResetState(){_eligibility=new(false);_filter.Reset();_context=null;_session=null;_sequence=-1;_position=null;}
    public void Reset(){ResetState();Status="Pointer preview off";_reasonSince=null;}
    public DipPoint? Map(TrackingSnapshot snapshot,LearningContext? context,PersonalModel? model)
    {
        if(context is not { IsValid:true } c || snapshot.SessionId is null || snapshot.Observation is not { } hand ||
            context.Source!=snapshot.Source?.ToString() || hand.FrameSize is not { } frame ||
            frame.Width!=context.FrameWidth || frame.Height!=context.FrameHeight)
        {
            ResetState();
            Status = context is null ? "Choose display and hand in Calibration" : snapshot.State switch {
                TrackingState.Lost => "No hand detected", TrackingState.Stale => "Tracking observation expired",
                TrackingState.Ambiguous => "Multiple hands detected", TrackingState.Stopping => "Tracking stopping",
                TrackingState.Stopped or TrackingState.Off or TrackingState.Unavailable => "Tracking off or unavailable",
                _ => "Finger evidence or calibration context unavailable" };
            return null;
        }
        var accepted=model is { IsValid:true } && model.Context==c ? model : null;
        if(c!=_context || snapshot.SessionId!=_session || accepted?.Id!=_model)
        { ResetState();_context=c;_session=snapshot.SessionId;_model=accepted?.Id; }
        var sample=_eligibility.ObserveDesktopPreview(snapshot,c.Hand==1?HandSide.Left:HandSide.Right);
        if(sample.Position is null){Status=sample.State==FingerPointerState.WrongHand?"Selected hand not detected":$"Finger preview · {sample.State}";_position=null;_filter.Reset();return null;}
        if(hand.Sequence==_sequence)return _position;
        _sequence=hand.Sequence;
        var tip=hand.Point(8) ?? (hand.Visual is { } v ? new ImageLandmark(v.X,v.Y,0) : (ImageLandmark?)null);
        if(tip is not { } point)return null;
        var mapping=new CalibrationMapping(c.Display,c.Inset/100d,c.Sensitivity/100d,c.Mirror);
        if(mapping.Map(new(point.X,point.Y)) is not { } logical){Status="Index outside calibrated working region";_filter.Reset();return _position=null;}
        var physical=mapping.ToPhysical(logical);
        var normalized=new DipPoint((physical.X-c.Display.X)/(c.Display.Width-1),(physical.Y-c.Display.Y)/(c.Display.Height-1));
        if(accepted is not null)normalized=accepted.Predict(normalized,c);
        var filtered=_filter.Apply(normalized,hand.CaptureMonoMs);
        if(_filter.Uncertain){Status="Large movement awaiting a consistent observation";return _position=null;}
        Status="Pointer preview active · Pocket actions disabled";
        Provenance=hand.Visual is not null ? GhostProvenance.FingerVisual : hand.Partial ? GhostProvenance.PartialHand : GhostProvenance.FullHand;
        return _position=new(c.Display.X+filtered.X*(c.Display.Width-1),c.Display.Y+filtered.Y*(c.Display.Height-1));
    }
}
