namespace Pocket.Core.Tracking;

/// <summary>One explicitly creator-designated stationary interval; aggregates only, never a point series.</summary>
public sealed class PointerStillnessMeasurement
{
    private bool _designated;
    private Guid _session;
    private long _start,_sequence=-1,_count,_missing;
    private double _x,_y,_xx,_yy;
    public string Status {get;private set;}="Not measured";
    public bool Running {get;private set;}
    public void Begin(Guid session,long now)
    {
        if(session==Guid.Empty||now<0)throw new ArgumentException("Valid session/time required");
        _designated=true;_session=session;_start=now;_sequence=-1;_count=_missing=0;_x=_y=_xx=_yy=0;Running=true;Status="Creator-designated stationary interval running";
    }
    public void Tick(Guid? session,long now)
    {
        if(!Running)return;
        if(session!=_session||now<_start){Cancel();return;}
        if(now-_start>=10000){Running=false;Status="Completed · stationary intent, not verified physical ground truth";}
    }
    public void Observe(Guid session,long sequence,long now,DipPoint? point)
    {
        Tick(session,now);if(!Running||sequence<=_sequence)return;_sequence=sequence;
        if(point is not {} p||!double.IsFinite(p.X)||!double.IsFinite(p.Y)){_missing++;return;}
        _count++;double dx=p.X-_x,dy=p.Y-_y;_x+=dx/_count;_y+=dy/_count;_xx+=dx*(p.X-_x);_yy+=dy*(p.Y-_y);
    }
    public void Cancel(){if(Running){Running=false;Status="Incomplete · canceled or session changed";}}
    public object Read()=>new{Status,Running,ValidFinalPointerSamples=_count,FreshObservationsWithoutPosition=_missing,
        WindowMilliseconds=10000,UserDesignatedStationary=_designated,ContainsRawPoints=false,
        RmsPixels=_count>0?Math.Sqrt(Math.Max(0,(_xx+_yy)/_count)):(double?)null,
        StdXPixels=_count>0?Math.Sqrt(Math.Max(0,_xx/_count)):(double?)null,
        StdYPixels=_count>0?Math.Sqrt(Math.Max(0,_yy/_count)):(double?)null,
        Evidence="Fresh UI-observed final Ghost positions; not worker frames, camera ground truth or optical latency"};
}
