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
