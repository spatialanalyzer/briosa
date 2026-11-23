using System.Text.Json;
using Google.Protobuf;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.LicensedProbes.Tests;

// The SDK-ownership probes (#252) for licensed session 4 (#402), against a scripted process
// table, server, terminator and second worker. Nothing here starts, attaches to or ends a
// real process.
public sealed class OwnershipProbeTests
{
    private const int HarnessId = 7001;
    private const int SaId = 424201;
    private const int ServerId = 424202;
    private const int WorkerId = 424203;
    private const int SdkId = 424204;
    private const int SecondWorkerId = 424205;
    private const int SecondSdkId = 424206;
    private const int DcomHostId = 424207;

    private static readonly DateTimeOffset Epoch = new(2026, 11, 23, 9, 0, 0, TimeSpan.Zero);

    private static OwnershipOptions Options(OwnershipScenario scenario, int observeSeconds = 10) => new()
    {
        Scenario = scenario,
        OutputDirectory = "unused",
        ObserveWindow = TimeSpan.FromSeconds(observeSeconds),
        InteractionMode = new InteractionModeSettings(Api.SaInteractionMode.Manual, Supplied: true)
    };

    private static async Task<(OwnershipRecord Record, World World)> RunAsync(
        OwnershipScenario scenario, Action<World>? arrange = null, int observeSeconds = 10)
    {
        var world = new World();
        arrange?.Invoke(world);
        var record = await OwnershipRunner.RunAsync(Options(scenario, observeSeconds), world.Environment(), CancellationToken.None);
        return (record, world);
    }

    // Release: identity and a clean stop

    [Fact]
    public async Task ReleaseLabelsTheActivatedSdkAndSeesTheCleanStopReleaseIt()
    {
        var (record, world) = await RunAsync(OwnershipScenario.Release);

        Assert.True(record.Completed);
        Assert.Empty(record.Residual);
        Assert.Equal(["census-start", "describe", "sdk-state-before", "start-sdk", "census-started", "connect", "census-connected", "stop-sdk", "window-after-stop"],
            record.Steps.Select(static step => step.Id));
        Assert.Equal(["start", "connect:1", "stop:1"], world.Server.Calls);
        var sdk = Assert.Single(record.Processes, static process => process.Role == ProcessRoles.Sdk);
        Assert.Equal("sdk-1", sdk.Label);
        Assert.Equal(ProcessRoles.ServiceHost, sdk.ParentRole);
        Assert.False(sdk.PresentAtStart);
        Assert.NotNull(sdk.ExitedMilliseconds);
        var worker = Assert.Single(record.Processes, static process => process.Role == ProcessRoles.Worker);
        Assert.Equal(ProcessRoles.Server, worker.ParentRole);
        // Labels in first-seen order, then by name; the scripted clock has not moved yet.
        Assert.Equal(["sdk-1", "server-1", "worker-1"], record.Steps.Single(static step => step.Id == "census-started").Alive);
        Assert.Equal("settled", record.Steps[^1].Result);
        Assert.Equal(12000, record.Server!.LifecycleBoundsMilliseconds["start_sdk_milliseconds"]);
    }

    [Fact]
    public async Task AnSdkProcessThatOutlivesTheStopIsAResidualAfterTheWindow()
    {
        var (record, _) = await RunAsync(OwnershipScenario.Release, static world => world.SdkSurvivesStop = true, observeSeconds: 3);

        Assert.True(record.Completed);
        Assert.Equal(["sdk-1"], record.Residual.Select(static process => process.Label));
        var window = record.Steps[^1];
        Assert.Equal("window-elapsed", window.Result);
        Assert.True(window.ElapsedMilliseconds >= 3000);
        Assert.Contains("Residual: `sdk-1` alive when the run ended.", OwnershipRecorder.ToMarkdown(record), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("sdk", "residual-worker-or-sdk-process")]
    [InlineData("no-sa", "exactly-one-spatial-analyzer-required")]
    [InlineData("no-server", "exactly-one-briosa-server-required")]
    [InlineData("started", "server-sdk-not-stopped")]
    [InlineData("other-target", "server-target-identity-mismatch")]
    [InlineData("long-bound", "step-timeout-below-published-bound")]
    public async Task UnmetPreconditionsRefuseBeforeAnyLifecycleCall(string condition, string refusal)
    {
        var (record, world) = await RunAsync(OwnershipScenario.WorkerCrash, world =>
        {
            switch (condition)
            {
                case "sdk": world.Add(SdkId, DcomHostId, ProcessCensus.SpatialAnalyzerSdk); break;
                case "no-sa": world.Remove(SaId); break;
                case "no-server": world.Remove(ServerId); break;
                case "started": world.Server.InitialState = Api.SpatialAnalyzerSdkState.Ready; break;
                case "other-target": world.Server.TargetMatches = false; break;
                case "long-bound": world.Server.StartBound = 130_000; break;
            }
        });

        Assert.Equal(OwnershipRunner.PreconditionStop, record.StopReason);
        Assert.Equal(refusal, record.Steps.Single(static step => step.Id == "preconditions").Result);
        Assert.Empty(world.Server.Calls);
        Assert.Empty(world.Terminator.Calls);
    }

    [Fact]
    public async Task AFailedStartEndsTheScriptAndStillCleansUp()
    {
        var (record, world) = await RunAsync(OwnershipScenario.ParentDeath, static world => world.Server.StartFails = true);

        Assert.Equal(OwnershipRunner.LifecycleStop, record.StopReason);
        Assert.Equal("failed", record.Steps.Single(static step => step.Id == "start-sdk").Result);
        Assert.Equal("grpc:FailedPrecondition", record.Steps.Single(static step => step.Id == "start-sdk").Lifecycle!.Transport);
        Assert.Equal("worker-startup-timeout", record.Steps.Single(static step => step.Id == "start-sdk").Lifecycle!.DiagnosticCode);
        Assert.Equal(["start"], world.Server.Calls);
        Assert.Empty(world.Terminator.Calls);
        Assert.Equal("window-after-cleanup", record.Steps[^1].Id);
    }

    [Fact]
    public async Task AConnectThatTimesOutStopsTheScriptAndStopsTheStartedGeneration()
    {
        var (record, world) = await RunAsync(OwnershipScenario.CompetingClient, static world => world.Server.ConnectAnswer =
            LifecycleResult.Failed("grpc:DeadlineExceeded", "client-cancelled"));

        Assert.Equal(OwnershipRunner.LifecycleStop, record.StopReason);
        Assert.Equal(["start", "connect:1", "stop:1"], world.Server.Calls);
        Assert.Empty(world.Second.Calls);
        Assert.Empty(world.Server.Executed);
        Assert.Empty(record.Residual);
    }

    // Worker crash and parent death

    [Fact]
    public async Task WorkerCrashTerminatesOnlyTheWorkerWaitsForFaultedAndStopsTheGeneration()
    {
        var (record, world) = await RunAsync(OwnershipScenario.WorkerCrash, static world => world.SdkSurvivesWorker = true);

        Assert.Equal([$"{WorkerId}:{ProcessCensus.BriosaWorker}"], world.Terminator.Calls);
        var terminate = record.Steps.Single(static step => step.Id == "terminate-worker");
        Assert.Equal("terminated", terminate.Result);
        Assert.Equal("worker-1", terminate.Target);
        var faulted = record.Steps.Single(static step => step.Id == "await-faulted");
        Assert.Equal("faulted", faulted.Result);
        Assert.Equal("WorkerProcessExited", faulted.Lifecycle!.IncidentTermination);
        Assert.Equal(["window-after-crash", "sdk-state-cleanup", "stop-sdk-cleanup", "window-after-cleanup"],
            record.Steps.SkipWhile(static step => step.Id != "window-after-crash").Select(static step => step.Id));
        Assert.Contains("stop:1", world.Server.Calls);
        Assert.Equal(["sdk-1"], record.Residual.Select(static process => process.Label));
        Assert.True(record.Completed);
    }

    [Fact]
    public async Task ATerminationThatHitsADifferentProcessIsRefusedAndRecorded()
    {
        var (record, world) = await RunAsync(OwnershipScenario.WorkerCrash, static world => world.Terminator.Answer = "identity-changed");

        Assert.Equal(OwnershipRunner.TerminationStop, record.StopReason);
        Assert.Equal("identity-changed", record.Steps.Single(static step => step.Id == "terminate-worker").Result);
        Assert.DoesNotContain(record.Steps, static step => step.Id == "await-faulted");
        Assert.Contains("stop:1", world.Server.Calls);
    }

    [Fact]
    public async Task ParentDeathTerminatesOnlyTheServerAndSendsNothingAfterIt()
    {
        var (record, world) = await RunAsync(OwnershipScenario.ParentDeath);

        Assert.Equal([$"{ServerId}:{ProcessCensus.BriosaServer}"], world.Terminator.Calls);
        Assert.Equal(["start", "connect:1"], world.Server.Calls);
        Assert.Equal("window-after-parent-death", record.Steps[^1].Id);
        Assert.Equal("settled", record.Steps[^1].Result);
        Assert.Empty(record.Residual);
        Assert.True(record.Completed);
    }

    [Fact]
    public async Task AParentDeathThatLeavesTheWorkerRunningIsAResidual()
    {
        var (record, _) = await RunAsync(OwnershipScenario.ParentDeath, static world => world.WorkerSurvivesServer = true, observeSeconds: 2);

        Assert.Equal(["sdk-1", "worker-1"], record.Residual.Select(static process => process.Label));
        Assert.Equal("window-elapsed", record.Steps[^1].Result);
    }

    // Watchdog kill

    [Fact]
    public async Task WatchdogKillSendsTheBaselineAndTheStallOnceAndWatchesWithTheDialogOpen()
    {
        var console = new FakeOperator();
        var (record, world) = await RunAsync(OwnershipScenario.WatchdogKill, world =>
        {
            world.Console = console;
            world.SdkSurvivesWorker = true;
        });

        Assert.Equal(["p26-baseline", "o-watchdog-stall"], world.Server.Executed);
        Assert.Equal(["o-watchdog-stall:before", "o-watchdog-stall:after"], console.Checkpoints);
        Assert.Equal("planned-stall", record.Steps.Single(static step => step.Id == "stall").Result);
        Assert.Equal(["window-dialog-open", "checkpoint-dismiss-dialog", "window-dialog-dismissed", "sdk-state-cleanup", "stop-sdk-cleanup", "window-after-cleanup"],
            record.Steps.SkipWhile(static step => step.Id != "window-dialog-open").Select(static step => step.Id));
        Assert.True(record.Completed);
        Assert.Equal(["sdk-1"], record.Residual.Select(static process => process.Label));
        var stall = ProbeCatalog.CreateOwnershipWatchdogSteps(new InteractionModeSettings(Api.SaInteractionMode.Manual, true))[^1];
        Assert.Equal("*", ((Api.MakeCollectionObjectNameRefListWildcardSelectionRequest)stall.Request).CollectionWildcardCriteria);
    }

    [Fact]
    public async Task AStallThatEndsAnotherWayStopsTheScriptButStillWatchesAndCleansUp()
    {
        var (record, world) = await RunAsync(OwnershipScenario.WatchdogKill, static world => world.Server.StallAnswer =
            ProbeOutcome.Unknown("grpc:Unavailable", "worker-execution-watchdog-timeout"));

        Assert.Equal(ProbeSession.UnknownOutcomeStop, record.StopReason);
        Assert.Equal("completion-unknown", record.Steps.Single(static step => step.Id == "stall").Result);
        Assert.Equal(2, world.Server.Executed.Count);
        Assert.Contains(record.Steps, static step => step.Id == "window-after-cleanup");
    }

    // Competing client

    [Fact]
    public async Task TheCompetingClientActivatesConnectsAndStopsWithoutAnMp()
    {
        var (record, world) = await RunAsync(OwnershipScenario.CompetingClient, static world => world.SecondActivatesNewSdk = true);

        Assert.Equal(["mp-before", "mp-during", "mp-after"], world.Server.Executed);
        Assert.Equal(["start", "connect", "stop"], world.Second.Calls);
        var second = Assert.Single(record.Processes, static process => process.ParentRole == ProcessRoles.Harness);
        Assert.Equal("worker-2", second.Label);
        Assert.NotNull(second.ExitedMilliseconds);
        Assert.Contains(record.Processes, static process => process.Label == "sdk-2");
        Assert.Equal("connected", record.Steps.Single(static step => step.Id == "second-connect").Result);
        Assert.Equal("settled", record.Steps.Single(static step => step.Id == "window-second-stopped").Result);
        Assert.True(record.Completed);
    }

    [Fact]
    public async Task AnUnknownOutcomeThroughTheFirstClientEndsEveryLaterMp()
    {
        var (record, world) = await RunAsync(OwnershipScenario.CompetingClient, static world => world.Server.ReadOnlyAnswer =
            ProbeOutcome.Unknown("grpc:DeadlineExceeded", "untyped-rpc-failure"));

        Assert.Equal(["mp-before"], world.Server.Executed);
        Assert.Equal("not-run", record.Steps.Single(static step => step.Id == "mp-during").Result);
        Assert.Equal("not-run", record.Steps.Single(static step => step.Id == "mp-after").Result);
        Assert.Equal(ProbeSession.UnknownOutcomeStop, record.StopReason);
        Assert.Equal(["start", "connect", "stop"], world.Second.Calls);
    }

    [Fact]
    public async Task ASecondWorkerThatNeverReadiesIsStoppedAndRecorded()
    {
        var (record, world) = await RunAsync(OwnershipScenario.CompetingClient, static world => world.Second.StartResult = "startup-timeout");

        Assert.Equal(OwnershipRunner.SecondWorkerStop, record.StopReason);
        Assert.Equal(["start", "stop"], world.Second.Calls);
        Assert.Equal(["mp-before", "mp-after"], world.Server.Executed);
    }

    // Observe

    [Fact]
    public async Task ObserveOnlyWatchesAndNamesWhatRemains()
    {
        var (record, world) = await RunAsync(OwnershipScenario.Observe, static world =>
        {
            world.Remove(SaId);
            world.Remove(ServerId);
            world.Add(SdkId, DcomHostId, ProcessCensus.SpatialAnalyzerSdk);
            world.SdkExitsAfterTicks = 4;
        });

        Assert.Empty(world.Server.Calls);
        Assert.Equal(["census-start", "window-observe"], record.Steps.Select(static step => step.Id));
        Assert.Equal("settled", record.Steps[^1].Result);
        var sdk = Assert.Single(record.Processes);
        Assert.True(sdk.PresentAtStart);
        Assert.Empty(record.Residual);
    }

    // Tracker, records and options

    [Fact]
    public void TheTrackerKnowsAProcessByIdAndStartTimeAndNamesOnlyKnownParents()
    {
        var tracker = new ProcessTracker(HarnessId);
        tracker.Observe(
        [
            new ProcessEntry(DcomHostId, 4, "svchost", null),
            new ProcessEntry(SdkId, DcomHostId, ProcessCensus.SpatialAnalyzerSdk, Epoch),
            new ProcessEntry(WorkerId, 999, ProcessCensus.BriosaWorker, Epoch),
            new ProcessEntry(SecondWorkerId, HarnessId, ProcessCensus.BriosaWorker, Epoch),
            new ProcessEntry(31337, 4, "SENTINEL-UNRELATED", Epoch)
        ], 0);
        tracker.Observe([new ProcessEntry(SdkId, 12, ProcessCensus.SpatialAnalyzerSdk, Epoch.AddMinutes(5))], 500);

        Assert.Equal(["sdk-1", "worker-1", "worker-2", "sdk-2"], tracker.Processes.Select(static process => process.Label));
        Assert.Equal(ProcessRoles.ServiceHost, tracker.Processes[0].ParentRole);
        Assert.Equal(ProcessRoles.Exited, tracker.Processes[1].ParentRole);
        Assert.Equal(ProcessRoles.Harness, tracker.Processes[2].ParentRole);
        Assert.Equal(500, tracker.Processes[0].ExitedMilliseconds);
        Assert.Equal(["sdk-2"], tracker.Alive(ProcessRoles.Sdk).Select(static process => process.Label));
        Assert.Equal(ProcessRoles.Other, ProcessRoles.ParentRoleOf(1, new ProcessEntry(1, 0, "SENTINEL-UNRELATED", null), HarnessId));
        Assert.Equal(ProcessRoles.Harness, ProcessRoles.ParentRoleOf(HarnessId, null, HarnessId));
    }

    [Fact]
    public async Task TheRecordsHoldLabelsAndNeverAProcessIdOrAnUnrelatedName()
    {
        var (record, _) = await RunAsync(OwnershipScenario.CompetingClient, static world =>
        {
            world.SecondActivatesNewSdk = true;
            world.Add(31337, DcomHostId, "SENTINEL-UNRELATED");
        });
        var json = OwnershipRecorder.ToJson(record);
        var markdown = OwnershipRecorder.ToMarkdown(record);
        using var document = JsonDocument.Parse(json);

        foreach (var id in new[] { SaId, ServerId, WorkerId, SdkId, SecondWorkerId, SecondSdkId, DcomHostId, 31337 })
        {
            Assert.DoesNotContain(id.ToString(System.Globalization.CultureInfo.InvariantCulture), json, StringComparison.Ordinal);
            Assert.DoesNotContain(id.ToString(System.Globalization.CultureInfo.InvariantCulture), markdown, StringComparison.Ordinal);
        }

        Assert.DoesNotContain(TestSupport.Sentinel, json, StringComparison.Ordinal);
        Assert.Equal(OwnershipRecorder.SchemaVersion, document.RootElement.GetProperty("schema_version").GetInt32());
        Assert.Equal("competing-client", document.RootElement.GetProperty("scenario").GetString());
        Assert.Equal(ProbeTarget.Session4Issue, document.RootElement.GetProperty("issue").GetString());
        Assert.Equal(JsonValueKind.Array, document.RootElement.GetProperty("residual").ValueKind);
        Assert.Contains(document.RootElement.GetProperty("processes").EnumerateArray(),
            static process => process.GetProperty("parent_role").GetString() == ProcessRoles.Harness);
    }

    [Theory]
    [InlineData("An unknown option", "--ownership", "release", "--dry-run", "--phase", "worker")]
    [InlineData("must be release", "--ownership", "everything", "--dry-run")]
    [InlineData("requires --confirm-licensed-sa", "--ownership", "release", "--output-directory", "x")]
    [InlineData("requires --output-directory", "--ownership", "release", ProbeTarget.ConfirmFlag)]
    [InlineData("another target's confirmation", "--ownership", "release", "--confirm-licensed-sa1999")]
    [InlineData("never connects", "--ownership", "release", "--dry-run", ProbeTarget.ConfirmFlag)]
    [InlineData("loopback HTTP", "--ownership", "release", ProbeTarget.ConfirmFlag, "--output-directory", "x", "--address", "http://192.0.2.1:50051")]
    [InlineData("applies only to competing-client", "--ownership", "release", ProbeTarget.ConfirmFlag, "--output-directory", "x", "--worker-path", "w")]
    [InlineData("requires --worker-path", "--ownership", "competing-client", ProbeTarget.ConfirmFlag, "--output-directory", "x")]
    [InlineData("requires --restore-sa-interaction-mode manual", "--ownership", "watchdog-kill", ProbeTarget.ConfirmFlag, "--output-directory", "x")]
    [InlineData("must be manual", "--ownership", "watchdog-kill", "--dry-run", "--restore-sa-interaction-mode", "automatic")]
    [InlineData("applies only to watchdog-kill", "--ownership", "release", "--dry-run", "--restore-sa-interaction-mode", "manual")]
    [InlineData("--address does not apply", "--ownership", "observe", ProbeTarget.ConfirmFlag, "--output-directory", "x", "--address", "http://127.0.0.1:1")]
    [InlineData("between 10 and 1800", "--ownership", "observe", "--dry-run", "--observe-seconds", "5")]
    public void OwnershipOptionsAreStrict(string message, params string[] arguments)
    {
        var exception = Assert.Throws<ProbeUsageException>(() => OwnershipOptions.Parse(arguments));
        Assert.Contains(message, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TheDryRunPrintsTheScriptAndTheAdmissionArgumentsAndConnectsToNothing()
    {
        var watchdog = OwnershipProgram.RenderDryRun(OwnershipOptions.Parse(
            ["--ownership", "watchdog-kill", "--dry-run", "--restore-sa-interaction-mode", "manual"]));
        var release = OwnershipProgram.RenderDryRun(OwnershipOptions.Parse(["--ownership", "release", "--dry-run"]));

        Assert.Contains("DRY RUN: nothing was started, connected, or terminated.", watchdog, StringComparison.Ordinal);
        Assert.Contains("--Briosa:Security:Operations:Profile=device", watchdog, StringComparison.Ordinal);
        Assert.Contains("--Briosa:SpatialAnalyzer:InteractionMode=Unchanged", watchdog, StringComparison.Ordinal);
        Assert.Contains("MP step o-watchdog-stall", watchdog, StringComparison.Ordinal);
        Assert.Contains("stop SDK; watch until the worker and the SDK process are gone.", release, StringComparison.Ordinal);
        Assert.DoesNotContain("Server admission", release, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheCommandRefusesAUsedOutputFolderAndWritesBothRecords()
    {
        var root = Directory.CreateTempSubdirectory("briosa-ownership-");
        try
        {
            var used = Directory.CreateDirectory(Path.Combine(root.FullName, "used"));
            await File.WriteAllTextAsync(Path.Combine(used.FullName, "x.txt"), "x");
            var output = new StringWriter();
            var error = new StringWriter();
            var refused = await OwnershipProgram.RunAsync(
                ["--ownership", "release", ProbeTarget.ConfirmFlag, "--output-directory", used.FullName], output, error,
                static (_, _) => throw new InvalidOperationException("no environment for a refused run"), CancellationToken.None);
            var fresh = Path.Combine(root.FullName, "fresh");
            var world = new World();
            var completed = await OwnershipProgram.RunAsync(
                ["--ownership", "release", ProbeTarget.ConfirmFlag, "--output-directory", fresh, "--observe-seconds", "10"], output, error,
                (_, _) => world.Environment(), CancellationToken.None);

            Assert.Equal(2, refused);
            Assert.Equal(0, completed);
            Assert.True(world.Server.Disposed);
            Assert.True(File.Exists(Path.Combine(fresh, $"ownership-release-{ProbeTarget.SpatialAnalyzerTarget}.json")));
            Assert.True(File.Exists(Path.Combine(fresh, $"ownership-release-{ProbeTarget.SpatialAnalyzerTarget}.md")));
            Assert.Contains("residual=none", output.ToString(), StringComparison.Ordinal);
            Assert.DoesNotContain(root.FullName, output.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Fact]
    public void TheTerminatorRefusesAnythingButABriosaWorkerOrServer()
    {
        Assert.Throws<ArgumentException>(() => ProcessTerminator.Instance.Terminate(SdkId, null, ProcessCensus.SpatialAnalyzerSdk));
        Assert.Throws<ArgumentException>(() => ProcessTerminator.Instance.Terminate(SaId, null, ProcessCensus.SpatialAnalyzer));
    }

    [Fact]
    public void TheWindowsProcessTableListsTheCurrentProcessReadOnly()
    {
        var snapshot = WindowsProcessTable.Instance.Snapshot();

        Assert.Contains(snapshot, static entry => entry.Id == Environment.ProcessId);
        Assert.All(snapshot, static entry => Assert.DoesNotContain('\\', entry.Name));
    }

    [Fact]
    public void LifecycleFailuresKeepOnlyTheTypedStateAndCode()
    {
        var error = new Api.SpatialAnalyzerSdkLifecycleError
        {
            DiagnosticCode = "worker-startup-timeout",
            State = new Api.SpatialAnalyzerSdkLifecycleState
            {
                SdkState = Api.SpatialAnalyzerSdkState.Faulted,
                SdkGeneration = 2,
                LastIncident = new Api.SpatialAnalyzerSdkIncident { TerminationKind = Api.SpatialAnalyzerSdkTerminationKind.StartFailed }
            }
        };
        var typed = OwnershipServer.FromRpcException(new RpcException(new Status(StatusCode.FailedPrecondition, "C:\\SENTINEL"),
            new Metadata { { OwnershipServer.SdkLifecycleErrorTrailer, error.ToByteArray() } }));
        var untyped = OwnershipServer.FromRpcException(new RpcException(new Status(StatusCode.Unavailable, "C:\\SENTINEL")));
        var unsafeCode = error.Clone();
        unsafeCode.DiagnosticCode = "C:\\SENTINEL";
        var withheld = OwnershipServer.FromRpcException(new RpcException(new Status(StatusCode.Internal, "x"),
            new Metadata { { OwnershipServer.SdkLifecycleErrorTrailer, unsafeCode.ToByteArray() } }));

        Assert.Equal("grpc:FailedPrecondition", typed.Transport);
        Assert.Equal("worker-startup-timeout", typed.DiagnosticCode);
        Assert.Equal("Faulted", typed.SdkState);
        Assert.Equal(2, typed.Generation);
        Assert.Equal("StartFailed", typed.IncidentTermination);
        Assert.Equal("grpc:Unavailable", untyped.Transport);
        Assert.Null(untyped.DiagnosticCode);
        Assert.Null(withheld.DiagnosticCode);
        Assert.Equal(["start_sdk_milliseconds"], OwnershipServer.Bounds(new Api.LifecycleBounds { StartSdkMilliseconds = 13000 }).Keys);
        Assert.Empty(OwnershipServer.Bounds(null));
    }

    // A scripted machine: a process table that the server, terminator and second worker change.
    private sealed class World
    {
        private readonly List<ProcessEntry> _processes = [];
        private int _ticks;

        public World()
        {
            Server = new ScriptedServer(this);
            Terminator = new ScriptedTerminator(this);
            Second = new ScriptedSecondWorker(this);
            Add(DcomHostId, 4, "svchost");
            Add(SaId, 1, ProcessCensus.SpatialAnalyzer);
            Add(ServerId, 1, ProcessCensus.BriosaServer);
        }

        public ManualClock Clock { get; } = new(Epoch);

        public ScriptedServer Server { get; }

        public ScriptedTerminator Terminator { get; }

        public ScriptedSecondWorker Second { get; }

        public IOperatorConsole Console { get; set; } = new FakeOperator();

        public bool SdkSurvivesStop { get; set; }

        public bool SdkSurvivesWorker { get; set; }

        public bool WorkerSurvivesServer { get; set; }

        public bool SecondActivatesNewSdk { get; set; }

        public int? SdkExitsAfterTicks { get; set; }

        public OwnershipEnvironment Environment() =>
            new(new Table(this), Server, Terminator, () => Second, Console, new Pause(this), Clock, HarnessId);

        public void Add(int id, int parent, string name) =>
            _processes.Add(new ProcessEntry(id, parent, name, ProcessRoles.IsTracked(name) ? Epoch.AddSeconds(id % 100) : null));

        public void Remove(int id) => _processes.RemoveAll(entry => entry.Id == id);

        public bool Has(int id) => _processes.Exists(entry => entry.Id == id);

        public IReadOnlyList<ProcessEntry> Snapshot() => [.. _processes];

        public void Tick()
        {
            if (SdkExitsAfterTicks is { } ticks && ++_ticks >= ticks)
            {
                Remove(SdkId);
            }
        }

        private sealed class Table(World world) : IProcessTable
        {
            public IReadOnlyList<ProcessEntry> Snapshot() => world.Snapshot();
        }

        private sealed class Pause(World world) : IPause
        {
            public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
            {
                world.Clock.Advance(delay);
                world.Tick();
                return Task.CompletedTask;
            }
        }
    }

    private sealed class ManualClock(DateTimeOffset start) : TimeProvider
    {
        private DateTimeOffset _now = start;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }

    private sealed class ScriptedServer(World world) : IOwnershipServer
    {
        private Api.SpatialAnalyzerSdkState _state = Api.SpatialAnalyzerSdkState.Stopped;
        private int? _generation;
        private bool _initialized;

        public List<string> Calls { get; } = [];

        public List<string> Executed { get; } = [];

        public Api.SpatialAnalyzerSdkState InitialState { get; set; } = Api.SpatialAnalyzerSdkState.Stopped;

        public bool TargetMatches { get; set; } = true;

        public long StartBound { get; set; } = 12000;

        public bool StartFails { get; set; }

        public LifecycleResult? ConnectAnswer { get; set; }

        public ProbeOutcome? StallAnswer { get; set; }

        public ProbeOutcome? ReadOnlyAnswer { get; set; }

        public bool Disposed { get; private set; }

        public Task<ServerDescription> DescribeAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new ServerDescription(true, TargetMatches, "0.10.1-probe-s4", "abc",
                new SortedDictionary<string, long>(StringComparer.Ordinal) { ["start_sdk_milliseconds"] = StartBound }));

        public Task<LifecycleResult> GetSdkStateAsync(CancellationToken cancellationToken)
        {
            if (!_initialized)
            {
                _initialized = true;
                _state = InitialState;
            }

            // The server notices a lost worker on its own.
            if (_state == Api.SpatialAnalyzerSdkState.Ready && !world.Has(WorkerId))
            {
                _state = Api.SpatialAnalyzerSdkState.Faulted;
            }

            return Task.FromResult(State("grpc:OK"));
        }

        public Task<LifecycleResult> StartSdkAsync(CancellationToken cancellationToken)
        {
            Calls.Add("start");
            if (StartFails)
            {
                return Task.FromResult(LifecycleResult.Failed("grpc:FailedPrecondition", "worker-startup-timeout"));
            }

            _generation = 1;
            _state = Api.SpatialAnalyzerSdkState.Running;
            world.Add(WorkerId, ServerId, ProcessCensus.BriosaWorker);
            world.Add(SdkId, DcomHostId, ProcessCensus.SpatialAnalyzerSdk);
            return Task.FromResult(State("grpc:OK"));
        }

        public Task<LifecycleResult> ConnectAsync(int generation, CancellationToken cancellationToken)
        {
            Calls.Add($"connect:{generation}");
            if (ConnectAnswer is { } answer)
            {
                return Task.FromResult(answer);
            }

            _state = Api.SpatialAnalyzerSdkState.Ready;
            return Task.FromResult(State("grpc:OK"));
        }

        public Task<LifecycleResult> StopSdkAsync(int generation, CancellationToken cancellationToken)
        {
            Calls.Add($"stop:{generation}");
            _state = Api.SpatialAnalyzerSdkState.Stopped;
            world.Remove(WorkerId);
            if (!world.SdkSurvivesStop && !world.SdkSurvivesWorker)
            {
                world.Remove(SdkId);
            }

            return Task.FromResult(State("grpc:OK"));
        }

        public Task<ProbeOutcome> ExecuteAsync(ProbeStep step, CancellationToken cancellationToken)
        {
            Executed.Add(step.Id);
            if (step.ExpectedStall is not null)
            {
                // The server's watchdog ends the stalled worker.
                world.Remove(WorkerId);
                if (!world.SdkSurvivesWorker)
                {
                    world.Remove(SdkId);
                }

                _state = Api.SpatialAnalyzerSdkState.Faulted;
                return Task.FromResult(StallAnswer ?? ServerReportsModalDialog());
            }

            return Task.FromResult(step.Id.StartsWith("mp-", StringComparison.Ordinal) && ReadOnlyAnswer is { } answer
                ? answer
                : TestSupport.Outcome(ProbeOutcomeKind.Succeeded, ("collection_count", "3")));
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }

        private LifecycleResult State(string transport) => new(transport, _state.ToString(), _generation,
            _state == Api.SpatialAnalyzerSdkState.Ready, _state == Api.SpatialAnalyzerSdkState.Ready ? "Connected" : "Disconnected", null,
            _state == Api.SpatialAnalyzerSdkState.Faulted ? Api.SpatialAnalyzerSdkTerminationKind.WorkerProcessExited.ToString() : null);

        private static ProbeOutcome ServerReportsModalDialog()
        {
            var error = new Api.OperationError
            {
                Kind = Api.OperationFailureKind.WorkerWatchdogTimeout,
                DiagnosticCode = StallExpectation.ModalDialogDiagnosticCode,
                ExecutionDisposition = Api.ExecutionDisposition.StartedOutcomeUnknown,
                RecoveryGuidance = Api.RecoveryGuidance.OperatorInterventionRequired
            };
            return PublicOutcomes.FromRpcException(new RpcException(new Status(StatusCode.Unavailable, "x"),
                new Metadata { { PublicOutcomes.ErrorTrailerName, error.ToByteArray() } }));
        }
    }

    private sealed class ScriptedTerminator(World world) : IProcessTerminator
    {
        public List<string> Calls { get; } = [];

        public string Answer { get; set; } = "terminated";

        public string Terminate(int processId, DateTimeOffset? startedAt, string expectedName)
        {
            Calls.Add($"{processId}:{expectedName}");
            if (Answer != "terminated")
            {
                return Answer;
            }

            world.Remove(processId);
            if (processId == WorkerId && !world.SdkSurvivesWorker)
            {
                world.Remove(SdkId);
            }

            // A worker notices its parent's death and releases the SDK.
            if (processId == ServerId && !world.WorkerSurvivesServer)
            {
                world.Remove(WorkerId);
                world.Remove(SdkId);
            }

            return "terminated";
        }
    }

    private sealed class ScriptedSecondWorker(World world) : ISecondWorker
    {
        public List<string> Calls { get; } = [];

        public string StartResult { get; set; } = "ready";

        public Task<SecondWorkerResult> StartAsync(CancellationToken cancellationToken)
        {
            Calls.Add("start");
            if (StartResult != "ready")
            {
                return Task.FromResult(new SecondWorkerResult(StartResult, null, null, null));
            }

            world.Add(SecondWorkerId, HarnessId, ProcessCensus.BriosaWorker);
            if (world.SecondActivatesNewSdk)
            {
                world.Add(SecondSdkId, DcomHostId, ProcessCensus.SpatialAnalyzerSdk);
            }

            return Task.FromResult(new SecondWorkerResult("ready", "Disconnected", "worker-ready", true));
        }

        public Task<SecondWorkerResult> ConnectAsync(CancellationToken cancellationToken)
        {
            Calls.Add("connect");
            return Task.FromResult(new SecondWorkerResult("connected", "Connected", "connect-ex-connected", null));
        }

        public Task<SecondWorkerResult> StopAsync(CancellationToken cancellationToken)
        {
            Calls.Add("stop");
            world.Remove(SecondWorkerId);
            world.Remove(SecondSdkId);
            return Task.FromResult(new SecondWorkerResult("stopped", null, null, null));
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
