using Pocket.Core.Tracking;
using Xunit;

namespace Pocket.Core.Tests;
public class ObservationDeliveryTests
{
    [Theory]
    [InlineData(30)]
    [InlineData(60)]
    public void Fresh_delivery_does_not_wait_for_label_poll(int hz)
    {
        var queue = new Queue<Action>(); int current = 0, delivered = 0, labels = 0;
        using var wake = new LatestNotification(queue.Enqueue, () => { Assert.Equal(delivered + 1, current); delivered = current; });
        for (int i = 1; i <= hz * 3; i++)
        {
            current = i; wake.Signal(); Assert.Single(queue); queue.Dequeue()();
            if (i * 1000 / hz / 80 > labels) labels++;
        }
        Assert.Equal(hz * 3, delivered); Assert.True(delivered > labels); Assert.Equal(0, wake.Coalesced);
    }
    [Fact]
    public void Backpressure_reads_latest_stop_and_disposal_blocks_late_callback()
    {
        var queue = new Queue<Action>(); int current = 0, seen = -1;
        var wake = new LatestNotification(queue.Enqueue, () => seen = current);
        for (int i = 0; i < 1000; i++) { current = i; wake.Signal(); }
        current = -2; wake.Signal(); // Stop replaces all prior data before dispatch.
        Assert.Single(queue); queue.Dequeue()(); Assert.Equal(-2, seen); Assert.Equal(1000, wake.Coalesced);
        wake.Signal(); wake.Dispose(); queue.Dequeue()(); Assert.Equal(1, wake.Deliveries);
        wake.Signal(); Assert.Empty(queue);
    }
}
