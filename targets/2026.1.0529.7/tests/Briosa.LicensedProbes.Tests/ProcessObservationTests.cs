using System.Text.Json;
using System.Text.RegularExpressions;

namespace Briosa.LicensedProbes.Tests;

// The read-only process observation of session 4's preflight and postflight (#402), against a
// scripted process table. Nothing here starts, attaches to, stops or ends a process.
public sealed class ProcessObservationTests
{
    private const int HarnessId = 7001;
    private const int Session = 1;
    private const int SaId = 424201;
    private const int ServerId = 424202;
    private const int WorkerId = 424203;
    private const int SdkId = 424204;
    private const int ServiceHostId = 424207;

    private static readonly DateTimeOffset Epoch = new(2026, 11, 23, 9, 0, 0, TimeSpan.Zero);

    private static ProcessEntry Tracked(int id, int parent, string name) => new(id, parent, name, Epoch.AddSeconds(id % 100), Session);

    private static Task<ProcessObservation.Record> Observe(Table table, int seconds, CancellationToken cancellationToken = default)
    {
        var clock = new ManualClock(Epoch);
        return ProcessObservation.ObserveAsync(new ProcessObservation.Options("unused", TimeSpan.FromSeconds(seconds), null), table,
            new Pause(clock, table), clock, HarnessId, cancellationToken);
    }

    [Fact]
    public async Task TheObservationEndsOnceACompleteSnapshotShowsNoSdkWorkerOrServer()
    {
        var table = new Table(Tracked(SaId, 1, ProcessCensus.SpatialAnalyzer), Tracked(SdkId, ServiceHostId, ProcessCensus.SpatialAnalyzerSdk),
            new ProcessEntry(ServiceHostId, 4, "svchost", null)) { RemoveAfterPauses = (SdkId, 4) };

        var record = await Observe(table, 30);

        Assert.Equal("settled", record.Result);
        Assert.True(record.ResidualKnown);
        Assert.Empty(record.Residual);
        Assert.Equal(1, record.SpatialAnalyzerAlive);
        var sdk = Assert.Single(record.Processes, static process => process.Role == ProcessRoles.Sdk);
        Assert.Equal(("sdk-1", ProcessRoles.ServiceHost, true), (sdk.Label, sdk.ParentRole, sdk.PresentAtStart));
        Assert.Equal(2000, sdk.FirstAbsentMilliseconds);
        Assert.True(sdk.LastSeenMilliseconds < sdk.FirstAbsentMilliseconds);
    }

    [Fact]
    public async Task AProcessStillAliveAtTheEndIsAResidualAndZeroSecondsTakesOneSnapshot()
    {
        var table = new Table(Tracked(ServerId, 1, ProcessCensus.BriosaServer), Tracked(WorkerId, ServerId, ProcessCensus.BriosaWorker));

        var window = await Observe(table, 3);
        var single = await Observe(table, 0);

        Assert.Equal("window-elapsed", window.Result);
        Assert.Equal(["server-1", "worker-1"], window.Residual.Select(static process => process.Label));
        Assert.Equal(1, single.CompleteSnapshots);
        Assert.Equal("window-elapsed", single.Result);
        Assert.Contains("Each is unresolved; the session never stops one.", ProcessObservation.ToMarkdown(window), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnIncompleteOrFailedSnapshotIsNeverAnAbsence()
    {
        var table = new Table(Tracked(SdkId, ServiceHostId, ProcessCensus.SpatialAnalyzerSdk)) { DegradeAfter = 1, Hidden = { SdkId } };

        var record = await Observe(table, 2);

        Assert.Equal("window-elapsed", record.Result);
        Assert.True(Assert.Single(record.Processes).Alive);
        Assert.False(record.ResidualKnown);
        Assert.True(record.IncompleteSnapshots > 0);
        var markdown = ProcessObservation.ToMarkdown(record);
        Assert.Contains("Residual: unknown.", markdown, StringComparison.Ordinal);
        Assert.Contains("unknown: final census incomplete; see last seen", markdown, StringComparison.Ordinal);
        Assert.DoesNotContain("alive at end", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CancellationEndsTheObservationAndKeepsWhatWasSeen()
    {
        using var cancellation = new CancellationTokenSource();
        var table = new Table(Tracked(SdkId, ServiceHostId, ProcessCensus.SpatialAnalyzerSdk)) { OnPause = cancellation.Cancel };

        var record = await Observe(table, 30, cancellation.Token);

        Assert.Equal("cancelled", record.Result);
        Assert.Equal(["sdk-1"], record.Residual.Select(static process => process.Label));
    }

    [Fact]
    public void TheTrackerCountsGapsAndReappearancesAndTellsReusedIdsApart()
    {
        var tracker = new ProcessTracker(HarnessId);
        ProcessEntry Sdk(DateTimeOffset at) => new(SdkId, ServiceHostId, ProcessCensus.SpatialAnalyzerSdk, at, Session);
        tracker.Observe(ProcessSnapshot.Of(
            new ProcessEntry(ServiceHostId, 4, "svchost", null),
            Sdk(Epoch),
            new ProcessEntry(WorkerId, 999, ProcessCensus.BriosaWorker, Epoch, Session),
            new ProcessEntry(31337, 4, "SENTINEL-UNRELATED", Epoch)), 0);

        // A failed or incomplete snapshot never marks the SDK absent.
        tracker.Observe(ProcessSnapshot.Failed, 500);
        tracker.Observe(new ProcessSnapshot(SnapshotStatus.Incomplete, []), 1000);
        Assert.True(tracker.Alive(ProcessRoles.Sdk).Count == 1 && !tracker.NoneConfirmed(ProcessRoles.Sdk));

        // A complete one does; a later listing of the same process is a reappearance, not a new one.
        tracker.Observe(ProcessSnapshot.Of(), 1500);
        Assert.Equal(1500, tracker.Processes[0].FirstAbsentMilliseconds);
        tracker.Observe(ProcessSnapshot.Of(Sdk(Epoch)), 2000);
        var sdk = tracker.Processes.Single(static process => process.Label == "sdk-1");
        Assert.Equal((true, 1, 2000L), (sdk.Alive, sdk.Reappearances, sdk.LastSeenMilliseconds));

        // A reused id with another start time is another process.
        tracker.Observe(ProcessSnapshot.Of(Sdk(Epoch.AddMinutes(5))), 2500);
        Assert.Equal(["sdk-1", "worker-1", "sdk-2"], tracker.Processes.Select(static process => process.Label));
        Assert.Equal((1, 1), (tracker.IncompleteSnapshots, tracker.FailedSnapshots));
        Assert.Equal(ProcessRoles.Exited, tracker.Processes[1].ParentRole);
        Assert.Equal(ProcessRoles.Other, ProcessRoles.ParentRoleOf(1, new ProcessEntry(1, 0, "SENTINEL-UNRELATED", null), HarnessId));
        Assert.Equal(ProcessRoles.Harness, ProcessRoles.ParentRoleOf(HarnessId, null, HarnessId));
    }

    [Fact]
    public void ProcessNamesAreMatchedAsWindowsMatchesThemWithoutCase()
    {
        Assert.Equal(ProcessRoles.Sdk, ProcessRoles.RoleOf("spatialanalyzersdk"));
        Assert.Equal(ProcessRoles.Worker, ProcessRoles.RoleOf("BRIOSA.WORKER"));
        Assert.Equal(ProcessRoles.SpatialAnalyzer, ProcessRoles.RoleOf("spatial analyzer64"));
        Assert.Null(ProcessRoles.RoleOf("SpatialAnalyzerSDKx"));

        // A lower-case SDK process is still tracked, so postflight cannot miss it.
        var tracker = new ProcessTracker(HarnessId);
        tracker.Observe(ProcessSnapshot.Of(new ProcessEntry(SdkId, ServiceHostId, "spatialanalyzersdk", Epoch, Session)), 0);
        Assert.Equal(["sdk-1"], tracker.Alive(ProcessRoles.Sdk).Select(static process => process.Label));
        Assert.False(tracker.NoneConfirmed(ProcessRoles.Sdk));
    }

    [Fact]
    public async Task TheRecordsHoldLabelsAndNeverAProcessIdOrAnUnrelatedName()
    {
        var table = new Table(Tracked(SaId, 1, ProcessCensus.SpatialAnalyzer), Tracked(SdkId, ServiceHostId, ProcessCensus.SpatialAnalyzerSdk),
            new ProcessEntry(ServiceHostId, 4, "svchost", null), new ProcessEntry(31337, ServiceHostId, "SENTINEL-UNRELATED", null));

        var record = await Observe(table, 1);
        var json = ProcessObservation.ToJson(record);
        var markdown = ProcessObservation.ToMarkdown(record);
        using var document = JsonDocument.Parse(json);

        foreach (var id in new[] { SaId, SdkId, ServiceHostId, 31337 })
        {
            Assert.DoesNotContain(id.ToString(System.Globalization.CultureInfo.InvariantCulture), json, StringComparison.Ordinal);
            Assert.DoesNotContain(id.ToString(System.Globalization.CultureInfo.InvariantCulture), markdown, StringComparison.Ordinal);
        }

        Assert.DoesNotContain(TestSupport.Sentinel, json + markdown, StringComparison.Ordinal);
        Assert.Equal(ProcessObservation.SchemaVersion, document.RootElement.GetProperty("schema_version").GetInt32());
        Assert.Equal("complete", document.RootElement.GetProperty("snapshots").GetProperty("final").GetString());
        Assert.Equal(["sdk-1"], document.RootElement.GetProperty("residual").EnumerateArray().Select(static label => label.GetString()));
        var process = document.RootElement.GetProperty("processes").EnumerateArray().First();
        Assert.True(process.TryGetProperty("last_seen_ms", out _));
        Assert.True(process.TryGetProperty("first_absent_ms", out _));
        Assert.False(process.TryGetProperty("exited_ms", out _));
    }

    [Theory]
    [InlineData("An unknown option", "--observe-processes", "--output-directory", "x", "--kill")]
    [InlineData("--output-directory is required", "--observe-processes")]
    [InlineData("between 0 and 1800", "--observe-processes", "--output-directory", "x", "--observe-seconds", "1801")]
    [InlineData("40-character", "--observe-processes", "--output-directory", "x", "--harness-revision", "abc")]
    public void ObservationOptionsAreStrict(string message, params string[] arguments)
    {
        var exception = Assert.Throws<ProbeUsageException>(() => ProcessObservation.Parse(arguments));
        Assert.Contains(message, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheCommandWritesBothRecordsAndRefusesAUsedFolderOrAFileWithoutAPath()
    {
        var root = Directory.CreateTempSubdirectory("briosa-observe-");
        try
        {
            var table = new Table(Tracked(SaId, 1, ProcessCensus.SpatialAnalyzer));
            var clock = new ManualClock(Epoch);
            var fresh = Path.Combine(root.FullName, "fresh");
            var output = new StringWriter();
            var error = new StringWriter();
            var code = await ProcessObservation.RunAsync(["--observe-processes", "--output-directory", fresh, "--observe-seconds", "5"], output, error,
                table, new Pause(clock, table), clock, CancellationToken.None);

            Assert.Equal(0, code);
            Assert.True(File.Exists(Path.Combine(fresh, $"process-observation-{ProbeTarget.SpatialAnalyzerTarget}.json")));
            Assert.True(File.Exists(Path.Combine(fresh, $"process-observation-{ProbeTarget.SpatialAnalyzerTarget}.md")));
            Assert.Contains("residual=none", output.ToString(), StringComparison.Ordinal);

            var file = Path.Combine(root.FullName, "a-file");
            await File.WriteAllTextAsync(file, "x");
            foreach (var used in new[] { fresh, file })
            {
                var refusal = new StringWriter();
                Assert.Equal(2, await ProcessObservation.RunAsync(["--observe-processes", "--output-directory", used], new StringWriter(), refusal,
                    table, new Pause(clock, table), clock, CancellationToken.None));
                Assert.DoesNotContain(root.FullName, refusal.ToString(), StringComparison.OrdinalIgnoreCase);
            }

            Assert.DoesNotContain(root.FullName, output.ToString() + error, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Fact]
    public void TheWindowsProcessTableListsTheCurrentProcessReadOnly()
    {
        var snapshot = WindowsProcessTable.Instance.Snapshot();

        Assert.NotEqual(SnapshotStatus.Failed, snapshot.Status);
        Assert.Contains(snapshot.Entries, static entry => entry.Id == Environment.ProcessId);
        Assert.All(snapshot.Entries, static entry => Assert.DoesNotContain('\\', entry.Name));
        using var self = System.Diagnostics.Process.GetCurrentProcess();
        using var handle = ProcessHandles.OpenForQuery(Environment.ProcessId);
        Assert.NotNull(handle);
        Assert.Equal(new DateTimeOffset(self.StartTime.ToUniversalTime(), TimeSpan.Zero), ProcessHandles.StartTime(handle));
        Assert.Equal(self.SessionId, ProcessHandles.SessionOf(Environment.ProcessId));
    }

    // No harness-side termination ever names a whole process tree, and no native termination or
    // signal exists in the harness at all. Only the product's own watchdog ends a tree.
    [Fact]
    public void NoHarnessSourceTerminatesAProcessTree()
    {
        var folder = HarnessSourceFolder();
        var sources = Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories)
            .Where(static file => !file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                !file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var forbidden = new Regex(
            @"entireProcessTree\s*:\s*true|\.Kill\(\s*true\s*\)|taskkill|TerminateJobObject|KillProcessTree|TerminateProcess|GenerateConsoleCtrlEvent|DebugBreakProcess",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        // The guard can never pass on an empty or wrong folder: it must see the harness itself.
        Assert.True(sources.Count > 10, $"Expected the harness sources under {folder}, but found {sources.Count} source files.");
        Assert.Contains(sources, static file => string.Equals(Path.GetFileName(file), "ProcessObservation.cs", StringComparison.Ordinal));
        Assert.Contains(sources, static file => File.ReadAllText(file).Contains("entireProcessTree: false", StringComparison.Ordinal));
        Assert.All(sources, file => Assert.False(forbidden.IsMatch(File.ReadAllText(file)), Path.GetFileName(file)));
    }

    // Found at run time from the test's own output folder, as the other tests here find
    // repository files: up to the target's Briosa.slnx, then its harness folder. A compile-time
    // source path cannot be used, because a CI build maps it to /_/.
    private static string HarnessSourceFolder()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Briosa.slnx")))
        {
            root = root.Parent;
        }

        if (root is null)
        {
            Assert.Fail($"No Briosa.slnx was found in {AppContext.BaseDirectory} or any folder above it, so the harness sources cannot be checked.");
        }

        var folder = Path.Combine(root.FullName, "tools", "Briosa.LicensedProbes");
        Assert.True(File.Exists(Path.Combine(folder, "Briosa.LicensedProbes.csproj")),
            $"The harness project was not found in {folder}, so the harness sources cannot be checked.");
        return folder;
    }

    private sealed class ManualClock(DateTimeOffset start) : TimeProvider
    {
        private DateTimeOffset _now = start;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }

    // A scripted process list: entries, an optional removal after some pauses, and a degraded mode.
    private sealed class Table(params ProcessEntry[] entries) : IProcessTable
    {
        private readonly List<ProcessEntry> _entries = [.. entries];
        private int _snapshots;
        private int _pauses;

        public (int Id, int Pauses)? RemoveAfterPauses { get; init; }

        // After this many snapshots, every snapshot is incomplete and leaves out the hidden processes.
        public int? DegradeAfter { get; init; }

        public HashSet<int> Hidden { get; } = [];

        public Action? OnPause { get; init; }

        public ProcessSnapshot Snapshot() => DegradeAfter is { } after && ++_snapshots > after
            ? new ProcessSnapshot(SnapshotStatus.Incomplete, [.. _entries.Where(entry => !Hidden.Contains(entry.Id))])
            : ProcessSnapshot.Of([.. _entries]);

        public void Paused()
        {
            OnPause?.Invoke();
            if (RemoveAfterPauses is { } removal && ++_pauses >= removal.Pauses)
            {
                _entries.RemoveAll(entry => entry.Id == removal.Id);
            }
        }
    }

    private sealed class Pause(ManualClock clock, Table table) : IPause
    {
        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            table.Paused();
            cancellationToken.ThrowIfCancellationRequested();
            clock.Advance(delay);
            return Task.CompletedTask;
        }
    }
}
