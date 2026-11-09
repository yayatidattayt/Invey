namespace Pocket.Core.Tracking;

/// <summary>Visual interpolation between validated targets only. No extrapolation, observation
/// generation, or command authority. Source/loss/Stop reset it immediately.</summary>
public sealed class GhostPreviewMotion
{
    private Guid? _session;
    private long _sequence=-1,_at;
    private DipPoint _from,_to;
    public void Reset(){_session=null;_sequence=-1;}
    public DipPoint Present(Guid session,long sequence,DipPoint target,long now)
    {
        if(_session!=session){_session=session;_sequence=sequence;_from=_to=target;_at=now;return target;}
        if(sequence!=_sequence){_from=Sample(now);_to=target;_at=now;_sequence=sequence;}
        return Sample(now);
    }
    private DipPoint Sample(long now){double t=Math.Clamp((now-_at)/50d,0,1);return new(_from.X+(_to.X-_from.X)*t,_from.Y+(_to.Y-_from.Y)*t);}
}
