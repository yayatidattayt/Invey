namespace Pocket.Core;

public enum ReplayAction { Show, Move, Press, Release, Hide }
public sealed record ReplayEvent(TimeSpan At, ReplayAction Action, DipPoint Position);
/// <summary>Caller supplies monotonic elapsed time. No timer, focus or OS input dependency.</summary>
public sealed class ReplaySession
{
    private readonly ReplayEvent[] _events;
    private int _index;
    private TimeSpan _last;
    public bool IsRunning { get; private set; }
    public long Epoch { get; private set; }
    public TimeSpan Duration => _events[^1].At;
    public ReplaySession(IEnumerable<ReplayEvent> events)
    {
        _events=events.ToArray();
        if (_events.Length==0 || _events.Length>10000) throw new ArgumentException("Replay requires 1..10000 events.");
        TimeSpan previous=TimeSpan.Zero;
        foreach(var item in _events)
        {
            if(item.At<previous || !item.Position.IsFinite || !Enum.IsDefined(item.Action)) throw new ArgumentException("Invalid replay.");
            previous=item.At;
        }
    }
    public bool Start()
    {
        if(IsRunning) return false;
        _index=0; _last=TimeSpan.Zero; Epoch++; IsRunning=true; return true;
    }
    public IReadOnlyList<ReplayEvent> Advance(long epoch, TimeSpan elapsed)
    {
        if(!IsRunning || epoch!=Epoch || elapsed<_last) return Array.Empty<ReplayEvent>();
        _last=elapsed; var due=new List<ReplayEvent>();
        while(_index<_events.Length && _events[_index].At<=elapsed) due.Add(_events[_index++]);
        if(_index==_events.Length) IsRunning=false;
        return due;
    }
    public void Stop() { IsRunning=false; Epoch++; }
    public static ReplaySession PanelRoute(DipRect panel, DipRect bounds, int cycles=20, int countdownMs=5000)
    {
        if(!panel.IsValid || !bounds.IsValid || cycles is <1 or >20 || countdownMs<0) throw new ArgumentOutOfRangeException(nameof(cycles));
        var events=new List<ReplayEvent>(); var start=new DipPoint(panel.X+panel.Width/2,panel.Y+panel.Height/2);
        double travel=Math.Min(100,Math.Max(0,bounds.Width-panel.X-panel.Width));
        for(int cycle=0;cycle<cycles;cycle++)
        {
            int at=countdownMs+cycle*1400;
            events.Add(new(TimeSpan.FromMilliseconds(at),ReplayAction.Show,start));
            events.Add(new(TimeSpan.FromMilliseconds(at),ReplayAction.Move,start));
            events.Add(new(TimeSpan.FromMilliseconds(at+50),ReplayAction.Press,start));
            for(int step=1;step<=40;step++)
            {
                double fraction=step<=20 ? step/20d : (40-step)/20d;
                events.Add(new(TimeSpan.FromMilliseconds(at+50+step*25),ReplayAction.Move,new(start.X+travel*fraction,start.Y)));
            }
            events.Add(new(TimeSpan.FromMilliseconds(at+1100),ReplayAction.Release,start));
            events.Add(new(TimeSpan.FromMilliseconds(at+1250),ReplayAction.Hide,start));
        }
        return new(events);
    }
}
