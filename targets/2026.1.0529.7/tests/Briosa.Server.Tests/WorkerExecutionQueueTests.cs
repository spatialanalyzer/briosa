using Briosa.Server.Workers;

namespace Briosa.Server.Tests;

public sealed class WorkerExecutionQueueTests
{
    [Fact]
    public async Task ByteBudgetRemainsReservedAfterCountSlotIsReleased()
    {
        var queue = new WorkerExecutionQueue(generation: 1, capacity: 2, maxRetainedWorkBytes: 120);
        Assert.True(queue.TryReserve(100));
        Assert.False(queue.TryReserve(30));
        queue.ReleaseCountReservation();
        Assert.Equal(100, queue.ReservedBytes);
        Assert.False(queue.TryReserve(30));
        queue.ReleaseBytes(100);
        Assert.True(queue.TryReserve(30));
        queue.ReleaseReservation(30);
        Assert.Equal(0, queue.Reservations);
        Assert.Equal(0, queue.ReservedBytes);
        await queue.CloseAsync().ConfigureAwait(true);
    }
}
