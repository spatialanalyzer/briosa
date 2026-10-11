using System.ComponentModel;
using System.Diagnostics;

namespace Briosa.Server.Services;

internal sealed class WindowsSpatialAnalyzerProcessPlatform : ISpatialAnalyzerProcessPlatform
{
    private readonly ISpatialAnalyzerProcessSource _source;

    public WindowsSpatialAnalyzerProcessPlatform()
        : this(SystemSpatialAnalyzerProcessSource.Instance)
    {
    }

    internal WindowsSpatialAnalyzerProcessPlatform(ISpatialAnalyzerProcessSource source) =>
        _source = source;

    // Candidates come from the snapshot's image names, and only candidates are opened
    // to confirm their exact executable path, so the cost follows the number of
    // processes with the SpatialAnalyzer image name, not the number of processes on
    // the machine (#317). The name is a necessary condition of the path check, so the
    // result is unchanged.
    public IReadOnlyList<SpatialAnalyzerProcessObservation> ObserveEligibleProcesses(
        string executablePath)
    {
        var expectedPath = Path.GetFullPath(executablePath);
        var expectedName = ImageNameOf(expectedPath);
        var observations = new List<SpatialAnalyzerProcessObservation>();
        foreach (var process in _source.GetProcesses())
        {
            using (process)
            {
                try
                {
                    if (!HasImageName(process, expectedName))
                    {
                        continue;
                    }

                    var actualPath = process.ReadMainModulePath();
                    if (actualPath is null ||
                        !string.Equals(
                            Path.GetFullPath(actualPath),
                            expectedPath,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    observations.Add(new SpatialAnalyzerProcessObservation(
                        new SpatialAnalyzerProcessIdentity(
                            process.ProcessId,
                            process.ReadStartTimeUtcTicks())));
                }
                catch (Exception exception) when (IsInaccessible(exception))
                {
                    // An inaccessible process is not safe to select or claim.
                }
            }
        }

        return observations;
    }

    // Reads one process ID from a fresh snapshot and opens only that process. An ID
    // that is unused, or used by a program with another image name, needs no further
    // read. The same image name and start time identify the same process, whose
    // executable cannot have changed since its path was confirmed, so its modules are
    // not read again.
    public SpatialAnalyzerProcessPresence ObserveProcess(
        string executablePath,
        SpatialAnalyzerProcessIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        var expectedName = ImageNameOf(executablePath);
        var presence = SpatialAnalyzerProcessPresence.Exited;
        foreach (var process in _source.GetProcesses())
        {
            using (process)
            {
                try
                {
                    if (process.ProcessId != identity.ProcessId ||
                        !HasImageName(process, expectedName))
                    {
                        continue;
                    }

                    presence =
                        process.ReadStartTimeUtcTicks() == identity.StartTimeUtcTicks &&
                        !process.ReadHasExited()
                            ? SpatialAnalyzerProcessPresence.Running
                            : SpatialAnalyzerProcessPresence.Exited;
                }
                catch (Exception exception) when (IsInaccessible(exception))
                {
                    presence = SpatialAnalyzerProcessPresence.Unknown;
                }
            }
        }

        return presence;
    }

    // The image name that a process snapshot reports is the executable's file name
    // without a final ".exe", the rule Process.ProcessName applies. Windows file
    // names, like the exact-path check, ignore case.
    private static string ImageNameOf(string executablePath)
    {
        var fileName = Path.GetFileName(executablePath);
        return fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? fileName[..^4]
            : fileName;
    }

    private static bool HasImageName(ISpatialAnalyzerProcessEntry process, string expectedName) =>
        string.Equals(process.ProcessName, expectedName, StringComparison.OrdinalIgnoreCase);

    private static bool IsInaccessible(Exception exception) =>
        exception is InvalidOperationException or Win32Exception or NotSupportedException;

    public ISpatialAnalyzerOwnedProcess Start(ProcessStartInfo startInfo)
    {
        ArgumentNullException.ThrowIfNull(startInfo);
        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException(
                "SpatialAnalyzer did not return a process handle.");
        return new WindowsSpatialAnalyzerOwnedProcess(process);
    }

    private sealed class WindowsSpatialAnalyzerOwnedProcess(
        Process process) : ISpatialAnalyzerOwnedProcess
    {
        private readonly Process _process = process;

        public SpatialAnalyzerProcessIdentity Identity { get; } = new(
            process.Id,
            process.StartTime.ToUniversalTime().Ticks);

        public bool HasExited => _process.HasExited;

        public bool IsApplicationWindowReady
        {
            get
            {
                if (_process.HasExited)
                {
                    return false;
                }

                var title = _process.MainWindowTitle;
                return _process.MainWindowHandle != IntPtr.Zero &&
                    !string.IsNullOrWhiteSpace(title) &&
                    !string.Equals(
                        title,
                        "SpatialAnalyzer License Notification Dialog",
                        StringComparison.Ordinal);
            }
        }

        public bool RequestClose() => !_process.HasExited && _process.CloseMainWindow();

        public Task WaitForExitAsync(CancellationToken cancellationToken) =>
            _process.WaitForExitAsync(cancellationToken);

        public void Refresh() => _process.Refresh();

        public void Dispose() => _process.Dispose();
    }
}
