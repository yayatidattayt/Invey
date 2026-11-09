namespace Pocket.Core.Tracking;

/// <summary>Raw validated observation → calibration → compatible personal correction → one filter → physical desktop.</summary>
public sealed class GhostTrackingMapper
{
    private FingerPointer _eligibility = new(false);
    private readonly PrecisionPointFilter _filter=new();
    private LearningContext? _context;
    private Guid? _session, _model;
    private long _sequence=-1;
    private DipPoint? _position;
    public GhostProvenance Provenance { get; private set; }
    public void Reset(){_eligibility=new(false);_filter.Reset();_context=null;_session=null;_sequence=-1;_position=null;}
    public DipPoint? Map(TrackingSnapshot snapshot,LearningContext? context,PersonalModel? model)
    {
        if(context is not { IsValid:true } c || snapshot.SessionId is null || snapshot.Observation is not { } hand ||
            context.Source!=snapshot.Source?.ToString() || hand.FrameSize is not { } frame ||
            frame.Width!=context.FrameWidth || frame.Height!=context.FrameHeight) { Reset();return null; }
        var accepted=model is { IsValid:true } && model.Context==c ? model : null;
        if(c!=_context || snapshot.SessionId!=_session || accepted?.Id!=_model)
        { Reset();_context=c;_session=snapshot.SessionId;_model=accepted?.Id; }
        var sample=_eligibility.ObserveDesktopPreview(snapshot,c.Hand==1?HandSide.Left:HandSide.Right);
        if(sample.Position is null){_position=null;_filter.Reset();return null;}
        if(hand.Sequence==_sequence)return _position;
        _sequence=hand.Sequence;
        var tip=hand.Point(8) ?? (hand.Visual is { } v ? new ImageLandmark(v.X,v.Y,0) : (ImageLandmark?)null);
        if(tip is not { } point)return null;
        var mapping=new CalibrationMapping(c.Display,c.Inset/100d,c.Sensitivity/100d,c.Mirror);
        if(mapping.Map(new(point.X,point.Y)) is not { } logical){_filter.Reset();return _position=null;}
        var physical=mapping.ToPhysical(logical);
        var normalized=new DipPoint((physical.X-c.Display.X)/(c.Display.Width-1),(physical.Y-c.Display.Y)/(c.Display.Height-1));
        if(accepted is not null)normalized=accepted.Predict(normalized,c);
        var filtered=_filter.Apply(normalized,hand.CaptureMonoMs);
        if(_filter.Uncertain)return _position=null;
        Provenance=hand.Visual is not null ? GhostProvenance.FingerVisual : hand.Partial ? GhostProvenance.PartialHand : GhostProvenance.FullHand;
        return _position=new(c.Display.X+filtered.X*(c.Display.Width-1),c.Display.Y+filtered.Y*(c.Display.Height-1));
    }
}
