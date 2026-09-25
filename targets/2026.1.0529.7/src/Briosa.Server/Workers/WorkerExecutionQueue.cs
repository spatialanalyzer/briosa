using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;

namespace Briosa.Server.Workers;

// A captured queue always belongs to one generation, including its reservations,
// cancellation, and consumer. A mapping still in progress cannot enter a successor.
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable",
    Justification = "Reserved mappers may outlive shutdown. Disposing their semaphore or cancellation source would race token checks/release. Neither uses a wait handle; GC reclaims them after the last mapper releases its queue reference.")]
internal sealed class WorkerExecutionQueue
{
    private readonly Channel<ExecutionWorkItem> _channel;
    private readonly SemaphoreSlim _slots;
    private readonly CancellationTokenSource _cancellation = new();
    private int _closed;
    private int _reservations;

    public WorkerExecutionQueue(int generation, int capacity)
    {
        Generation = generation;
        _slots = new SemaphoreSlim(capacity, capacity);
        _channel = Channel.CreateBounded<ExecutionWorkItem>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public int Generation { get; }
    public bool IsClosed => Volatile.Read(ref _closed) != 0;
    public int Reservations => Volatile.Read(ref _reservations);
    public ChannelReader<ExecutionWorkItem> Reader => _channel.Reader;
    public CancellationToken CancellationToken => _cancellation.Token;
    public Task Completion { get; set; } = Task.CompletedTask;

    public bool TryReserve()
    {
        if (IsClosed || !_slots.Wait(0)) return false;
        Interlocked.Increment(ref _reservations);
        if (!IsClosed) return true;
        ReleaseReservation();
        return false;
    }

    public bool TryWrite(ExecutionWorkItem item) => _channel.Writer.TryWrite(item);

    public void ReleaseReservation()
    {
        Interlocked.Decrement(ref _reservations);
        _slots.Release();
    }

    public async Task CloseAsync()
    {
        if (Interlocked.Exchange(ref _closed, 1) != 0) return;
        _channel.Writer.TryComplete();
        await _cancellation.CancelAsync().ConfigureAwait(false);
    }
}
