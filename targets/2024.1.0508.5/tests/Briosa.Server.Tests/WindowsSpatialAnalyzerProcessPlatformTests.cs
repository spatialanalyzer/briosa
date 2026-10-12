using System.ComponentModel;
using System.Diagnostics;
using Briosa.Server.Services;

namespace Briosa.Server.Tests;

// #317: observing SpatialAnalyzer used to read the main module of every running
// process. The platform now selects candidates by image name from the process
// snapshot and opens only those, so its cost follows the number of candidates rather
// than the number of processes. These tests count reads instead of timing them.
public sealed class WindowsSpatialAnalyzerProcessPlatformTests
{
    private const string ExecutablePath =
        @"C:\Program Files (x86)\New River Kinematics\SpatialAnalyzer Target\x64\Spatial Analyzer64.exe";

    private const string OtherInstallationPath =
        @"C:\Program Files (x86)\New River Kinematics\SpatialAnalyzer Other\x64\Spatial Analyzer64.exe";

    [Theory]
    [InlineData(0)]
    [InlineData(500)]
    [InlineData(20_000)]
    public void ScanOpensOnlyProcessesWithTheSpatialAnalyzerImageName(int unrelatedProcesses)
    {
        var source = new FakeProcessSource();
        var target = source.Add(41, "Spatial Analyzer64", ExecutablePath, startTicks: 4100);
        var otherInstallation = source.Add(42, "Spatial Analyzer64", OtherInstallationPath, startTicks: 4200);
        // Windows compares image names without regard to case.
        var recased = source.Add(43, "SPATIAL ANALYZER64", ExecutablePath.ToUpperInvariant(), startTicks: 4300);
        var nearMisses = new[]
        {
            source.Add(44, "Spatial Analyzer64 Helper", @"C:\Tools\Spatial Analyzer64 Helper.exe", startTicks: 4400),
            source.Add(45, "Spatial Analyzer", @"C:\Tools\Spatial Analyzer.exe", startTicks: 4500),
            source.Add(46, "Spatial Analyzer64.exe", @"C:\Tools\Spatial Analyzer64.exe.exe", startTicks: 4600),
            source.Add(47, "SpatialAnalyzerSDK", @"C:\Tools\SpatialAnalyzerSDK.exe", startTicks: 4700)
        };
        var unrelated = Enumerable.Range(1_000, unrelatedProcesses)
            .Select(id => source.Add(id, $"unrelated-{id % 7}", @"C:\Windows\System32\svchost.exe", id))
            .ToList();
        var platform = new WindowsSpatialAnalyzerProcessPlatform(source);

        var observations = platform.ObserveEligibleProcesses(ExecutablePath);

        Assert.Equal(
            [new SpatialAnalyzerProcessIdentity(41, 4100), new SpatialAnalyzerProcessIdentity(43, 4300)],
            observations.Select(observation => observation.Identity));
        Assert.Equal(1, target.MainModuleReads);
        Assert.Equal(1, otherInstallation.MainModuleReads);
        Assert.Equal(1, recased.MainModuleReads);
        Assert.All(nearMisses.Concat(unrelated), process => Assert.Equal(0, process.DetailReads));
        Assert.Equal(3, source.Processes.Sum(process => process.MainModuleReads));
        Assert.Equal(2, source.Processes.Sum(process => process.StartTimeReads));
        Assert.All(source.Processes, process => Assert.True(process.Disposed));
    }

    [Fact]
    public void ScanSkipsACandidateWhoseExecutableCannotBeRead()
    {
        var source = new FakeProcessSource();
        source.Add(41, "Spatial Analyzer64", ExecutablePath, startTicks: 4100).MainModuleFailure =
            new Win32Exception(299);
        source.Add(42, "Spatial Analyzer64", ExecutablePath, startTicks: 4200).MainModuleFailure =
            new InvalidOperationException("exited");
        source.Add(43, "Spatial Analyzer64", ExecutablePath, startTicks: 4300);
        var platform = new WindowsSpatialAnalyzerProcessPlatform(source);

        var observations = platform.ObserveEligibleProcesses(ExecutablePath);

        Assert.Equal(new SpatialAnalyzerProcessIdentity(43, 4300), Assert.Single(observations).Identity);
        Assert.All(source.Processes, process => Assert.True(process.Disposed));
    }

    [Fact]
    public void DirectCheckConfirmsTheSameProcessWithoutReadingModules()
    {
        var source = new FakeProcessSource();
        var selected = source.Add(41, "Spatial Analyzer64", ExecutablePath, startTicks: 4100);
        selected.MainModuleFailure = new Win32Exception(299);
        var other = source.Add(42, "Spatial Analyzer64", ExecutablePath, startTicks: 4200);
        var unrelated = Enumerable.Range(1_000, 2_000)
            .Select(id => source.Add(id, "unrelated", @"C:\Windows\System32\svchost.exe", id))
            .ToList();
        var platform = new WindowsSpatialAnalyzerProcessPlatform(source);

        var presence = platform.ObserveProcess(
            ExecutablePath,
            new SpatialAnalyzerProcessIdentity(41, 4100));

        Assert.Equal(SpatialAnalyzerProcessPresence.Running, presence);
        Assert.Equal(0, selected.MainModuleReads);
        Assert.Equal(1, selected.StartTimeReads);
        Assert.Equal(0, other.DetailReads);
        Assert.All(unrelated, process => Assert.Equal(0, process.DetailReads));
        Assert.All(source.Processes, process => Assert.True(process.Disposed));
    }

    [Theory]
    [InlineData("pid-unused")]
    [InlineData("pid-used-by-another-program")]
    [InlineData("pid-used-by-a-later-process")]
    [InlineData("process-exited")]
    public void DirectCheckReportsAnExitedProcess(string situation)
    {
        var source = new FakeProcessSource();
        source.Add(42, "Spatial Analyzer64", ExecutablePath, startTicks: 4200);
        switch (situation)
        {
            case "pid-used-by-another-program":
                // The other program cannot be opened; its image name alone shows the
                // observed process is gone.
                source.Add(41, "unrelated", @"C:\Windows\System32\svchost.exe", startTicks: 9900)
                    .StartTimeFailure = new Win32Exception(5);
                break;
            case "pid-used-by-a-later-process":
                source.Add(41, "Spatial Analyzer64", ExecutablePath, startTicks: 9900);
                break;
            case "process-exited":
                source.Add(41, "Spatial Analyzer64", ExecutablePath, startTicks: 4100).HasExited = true;
                break;
        }

        var platform = new WindowsSpatialAnalyzerProcessPlatform(source);

        var presence = platform.ObserveProcess(
            ExecutablePath,
            new SpatialAnalyzerProcessIdentity(41, 4100));

        Assert.Equal(SpatialAnalyzerProcessPresence.Exited, presence);
        Assert.All(source.Processes, process => Assert.Equal(0, process.MainModuleReads));
        Assert.All(source.Processes, process => Assert.True(process.Disposed));
    }

    [Theory]
    [InlineData("access-denied")]
    [InlineData("exiting")]
    [InlineData("exit-state")]
    public void DirectCheckThatCannotReadTheProcessIsUnknown(string failure)
    {
        var source = new FakeProcessSource();
        var selected = source.Add(41, "Spatial Analyzer64", ExecutablePath, startTicks: 4100);
        switch (failure)
        {
            case "access-denied":
                selected.StartTimeFailure = new Win32Exception(5);
                break;
            case "exiting":
                selected.StartTimeFailure = new InvalidOperationException("exited");
                break;
            default:
                selected.HasExitedFailure = new Win32Exception(5);
                break;
        }

        var platform = new WindowsSpatialAnalyzerProcessPlatform(source);

        var presence = platform.ObserveProcess(
            ExecutablePath,
            new SpatialAnalyzerProcessIdentity(41, 4100));

        Assert.Equal(SpatialAnalyzerProcessPresence.Unknown, presence);
        Assert.Equal(0, selected.MainModuleReads);
        Assert.True(selected.Disposed);
    }

    // The production source on the real process table: the test host observes itself
    // through the same image-name and exact-path rules, and the direct check tells its
    // own identity from a different start time. No SpatialAnalyzer is involved.
    [Fact]
    public void SystemSourceObservesTheCurrentProcessByImageNameAndPath()
    {
        using var current = Process.GetCurrentProcess();
        var identity = new SpatialAnalyzerProcessIdentity(
            current.Id,
            current.StartTime.ToUniversalTime().Ticks);
        var executablePath = current.MainModule!.FileName;
        var platform = new WindowsSpatialAnalyzerProcessPlatform();

        var observations = platform.ObserveEligibleProcesses(executablePath);

        Assert.Contains(identity, observations.Select(observation => observation.Identity));
        Assert.Equal(
            SpatialAnalyzerProcessPresence.Running,
            platform.ObserveProcess(executablePath, identity));
        Assert.Equal(
            SpatialAnalyzerProcessPresence.Exited,
            platform.ObserveProcess(executablePath, identity with
            {
                StartTimeUtcTicks = identity.StartTimeUtcTicks - TimeSpan.TicksPerSecond
            }));
    }

    private sealed class FakeProcessSource : ISpatialAnalyzerProcessSource
    {
        public List<FakeProcess> Processes { get; } = [];

        public FakeProcess Add(int processId, string processName, string mainModulePath, long startTicks)
        {
            var process = new FakeProcess(processId, processName, mainModulePath, startTicks);
            Processes.Add(process);
            return process;
        }

        public IReadOnlyList<ISpatialAnalyzerProcessEntry> GetProcesses() => Processes;
    }

    private sealed class FakeProcess(
        int processId,
        string processName,
        string mainModulePath,
        long startTicks) : ISpatialAnalyzerProcessEntry
    {
        public int ProcessId { get; } = processId;

        public string ProcessName { get; } = processName;

        public Exception? MainModuleFailure { get; set; }

        public Exception? StartTimeFailure { get; set; }

        public Exception? HasExitedFailure { get; set; }

        public bool HasExited { get; set; }

        public int MainModuleReads { get; private set; }

        public int StartTimeReads { get; private set; }

        public int HasExitedReads { get; private set; }

        public int DetailReads => MainModuleReads + StartTimeReads + HasExitedReads;

        public bool Disposed { get; private set; }

        public string? ReadMainModulePath()
        {
            MainModuleReads++;
            return MainModuleFailure is null ? mainModulePath : throw MainModuleFailure;
        }

        public long ReadStartTimeUtcTicks()
        {
            StartTimeReads++;
            return StartTimeFailure is null ? startTicks : throw StartTimeFailure;
        }

        public bool ReadHasExited()
        {
            HasExitedReads++;
            return HasExitedFailure is null ? HasExited : throw HasExitedFailure;
        }

        public void Dispose() => Disposed = true;
    }
}
