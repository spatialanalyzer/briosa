using Briosa.Worker.Control;

namespace Briosa.Server.Workers;

internal sealed class WorkerProcessLaunch
{
    public WorkerProcessLaunch(
        string fileName,
        IEnumerable<string>? arguments = null,
        string? workingDirectory = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        FileName = fileName;
        Arguments = arguments?.ToArray() ?? [];
        WorkingDirectory = workingDirectory;
    }

    public string FileName { get; }

    public IReadOnlyList<string> Arguments { get; }

    public string? WorkingDirectory { get; }
}

internal interface IWorkerProcessFactory
{
    ValueTask<IWorkerProcess> StartAsync(
        int generation,
        CancellationToken cancellationToken = default);
}

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
