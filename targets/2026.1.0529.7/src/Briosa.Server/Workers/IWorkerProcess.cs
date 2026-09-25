using Briosa.Worker.Control;

namespace Briosa.Server.Workers;

internal interface IWorkerProcess : IAsyncDisposable
{
    bool HasExited { get; }

    int? ExitCode { get; }

    ValueTask SendAsync(
        WorkerControlMessage message,
        CancellationToken cancellationToken = default);

    ValueTask<WorkerControlMessage> ReceiveAsync(
        CancellationToken cancellationToken = default);

    Task WaitForExitAsync(CancellationToken cancellationToken = default);

    ValueTask TerminateAsync(CancellationToken cancellationToken = default);
}
