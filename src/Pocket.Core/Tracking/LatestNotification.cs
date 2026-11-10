namespace Pocket.Core.Tracking;

/// <summary>At most one queued dispatcher wakeup. The consumer reads current session state;
/// no observation or worker-owned object is retained by a queued callback.</summary>
public sealed class LatestNotification(Action<Action> schedule, Action consume) : IDisposable
{
    private readonly object _gate = new();
    private bool _pending, _disposed;
    public long Notifications { get; private set; }
    public long Coalesced { get; private set; }
    public long Deliveries { get; private set; }
    public void Signal()
    {
        lock (_gate)
        {
            if (_disposed) return;
            Notifications++;
            if (_pending) { Coalesced++; return; }
            _pending = true;
            try { schedule(Drain); }
            catch { _pending = false; throw; }
        }
    }
    private void Drain()
    {
        lock (_gate)
        {
            _pending = false;
            if (_disposed) return;
            Deliveries++;
        }
        consume();
    }
    public void Dispose() { lock (_gate) _disposed = true; }
}
