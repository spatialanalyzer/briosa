using Briosa.Server.Workers;
using Briosa.Worker.Control;

namespace Briosa.Server.Tests;

// Signals once a Stop request has been fully written to a real worker, so a
// virtual-time test can expire the shutdown deadline in the acknowledgement phase.
internal sealed class StopSendTrackingFactory(IWorkerProcessFactory inner) : IWorkerProcessFactory
{
    public TaskCompletionSource StopSent { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public async ValueTask<IWorkerProcess> StartAsync(int generation, CancellationToken cancellationToken = default) =>
        new TrackedProcess(await inner.StartAsync(generation, cancellationToken).ConfigureAwait(false), StopSent);

    private sealed class TrackedProcess(IWorkerProcess inner, TaskCompletionSource stopSent) : IWorkerProcess
    {
        public bool HasExited => inner.HasExited;
        public int? ExitCode => inner.ExitCode;

        public async ValueTask SendAsync(WorkerControlMessage message, CancellationToken cancellationToken = default)
        {
            await inner.SendAsync(message, cancellationToken).ConfigureAwait(false);
            if (message.Kind == WorkerControlMessageKind.Stop) stopSent.TrySetResult();
        }

        public ValueTask<WorkerControlMessage> ReceiveAsync(CancellationToken cancellationToken = default) =>
            inner.ReceiveAsync(cancellationToken);
        public Task WaitForExitAsync(CancellationToken cancellationToken = default) =>
            inner.WaitForExitAsync(cancellationToken);
        public ValueTask TerminateAsync(CancellationToken cancellationToken = default) =>
            inner.TerminateAsync(cancellationToken);
        public ValueTask DisposeAsync() => inner.DisposeAsync();
    }
}
