using System.Diagnostics;

namespace Briosa.Server.Services;

// The process reads that the platform makes. One system snapshot lists every running
// process with its ID and image name. Every other read opens the process, and reading
// its main module also reads its memory, so the platform makes those reads only for
// processes whose image name matches (#317).
internal interface ISpatialAnalyzerProcessSource
{
    IReadOnlyList<ISpatialAnalyzerProcessEntry> GetProcesses();
}

internal interface ISpatialAnalyzerProcessEntry : IDisposable
{
    // From the snapshot.
    int ProcessId { get; }

    // From the snapshot: the image file name without a final ".exe".
    string ProcessName { get; }

    string? ReadMainModulePath();

    long ReadStartTimeUtcTicks();

    bool ReadHasExited();
}

internal sealed class SystemSpatialAnalyzerProcessSource : ISpatialAnalyzerProcessSource
{
    private SystemSpatialAnalyzerProcessSource()
    {
    }

    public static SystemSpatialAnalyzerProcessSource Instance { get; } = new();

    public IReadOnlyList<ISpatialAnalyzerProcessEntry> GetProcesses() =>
        [.. Process.GetProcesses().Select(static process => new Entry(process))];

    private sealed class Entry(Process process) : ISpatialAnalyzerProcessEntry
    {
        private readonly Process _process = process;

        public int ProcessId => _process.Id;

        public string ProcessName => _process.ProcessName;

        public string? ReadMainModulePath() => _process.MainModule?.FileName;

        public long ReadStartTimeUtcTicks() => _process.StartTime.ToUniversalTime().Ticks;

        public bool ReadHasExited() => _process.HasExited;

        public void Dispose() => _process.Dispose();
    }
}
