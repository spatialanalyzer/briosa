using System.ComponentModel;
using System.Diagnostics;

namespace Briosa.Server.Services;

internal sealed record SpatialAnalyzerProcessIdentity(int ProcessId, long StartTimeUtcTicks);

internal sealed record SpatialAnalyzerProcessObservation(
    SpatialAnalyzerProcessIdentity Identity);

internal interface ISpatialAnalyzerOwnedProcess : IDisposable
{
    SpatialAnalyzerProcessIdentity Identity { get; }

    bool HasExited { get; }

    bool IsApplicationWindowReady { get; }

    bool RequestClose();

    Task WaitForExitAsync(CancellationToken cancellationToken);

    void Refresh();
}

internal interface ISpatialAnalyzerProcessPlatform
{
    IReadOnlyList<SpatialAnalyzerProcessObservation> ObserveEligibleProcesses(
        string executablePath);

    ISpatialAnalyzerOwnedProcess Start(ProcessStartInfo startInfo);
}

internal sealed class WindowsSpatialAnalyzerProcessPlatform : ISpatialAnalyzerProcessPlatform
{
    public IReadOnlyList<SpatialAnalyzerProcessObservation> ObserveEligibleProcesses(
        string executablePath)
    {
        var expectedPath = Path.GetFullPath(executablePath);
        var observations = new List<SpatialAnalyzerProcessObservation>();
        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                try
                {
                    var actualPath = process.MainModule?.FileName;
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
                            process.Id,
                            process.StartTime.ToUniversalTime().Ticks)));
                }
                catch (Exception exception) when (
                    exception is InvalidOperationException or
                        Win32Exception or
                        NotSupportedException)
                {
                    // An inaccessible process is not safe to select or claim.
                }
            }
        }

        return observations;
    }

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
