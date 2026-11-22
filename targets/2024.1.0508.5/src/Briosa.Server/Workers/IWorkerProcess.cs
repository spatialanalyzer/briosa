using Briosa.Worker.Control;

namespace Briosa.Server.Workers;

internal interface IWorkerProcess : IAsyncDisposable
{
    bool HasExited { get; }

    int? ExitCode { get; }

    // Completes once the launched process owns the control channel, so a start can
    // log that phase separately. ReceiveAsync still connects on first use; a process
    // without a separate connection step completes at once.
    ValueTask WaitForControlConnectionAsync(CancellationToken cancellationToken = default) =>
        ValueTask.CompletedTask;

    ValueTask SendAsync(
        WorkerControlMessage message,
        CancellationToken cancellationToken = default);

    ValueTask<WorkerControlMessage> ReceiveAsync(
        CancellationToken cancellationToken = default);

    Task WaitForExitAsync(CancellationToken cancellationToken = default);

    ValueTask TerminateAsync(CancellationToken cancellationToken = default);
}
