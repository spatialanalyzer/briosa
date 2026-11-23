using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Google.Protobuf;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.LicensedProbes.Tests;

// The SDK-ownership probes (#252) for licensed session 4 (#402), against a scripted process
// table, server, process control and second worker. Nothing here starts, attaches to or ends
// a real process.
public sealed class OwnershipProbeTests
{
    private const int HarnessId = 7001;
    private const int Session = 1;
    private const int SaId = 424201;
    private const int ServerId = 424202;
    private const int WorkerId = 424203;
    private const int SdkId = 424204;
    private const int SecondWorkerId = 424205;
    private const int SecondSdkId = 424206;
    private const int DcomHostId = 424207;
    private const int ReplacementWorkerId = 424208;
    private const int WorkerChildId = 424209;

    private static readonly DateTimeOffset Epoch = new(2026, 11, 23, 9, 0, 0, TimeSpan.Zero);

    private static OwnershipOptions Options(OwnershipScenario scenario, int observeSeconds = 10) => new()
    {
        Scenario = scenario,
        OutputDirectory = "unused",
        WorkerPath = scenario == OwnershipScenario.CompetingClient ? "unused-worker-path" : null,
        ObserveWindow = TimeSpan.FromSeconds(observeSeconds),
        InteractionMode = new InteractionModeSettings(Api.SaInteractionMode.Manual, Supplied: true)
    };

    private static async Task<(OwnershipRecord Record, World World)> RunAsync(
        OwnershipScenario scenario, Action<World>? arrange = null, int observeSeconds = 10)
    {
        var world = new World();
        arrange?.Invoke(world);
        var record = await OwnershipRunner.RunAsync(Options(scenario, observeSeconds), world.Environment(), world.Cancellation.Token);
        return (record, world);
    }

    private static OwnershipStepRecord StepOf(OwnershipRecord record, string id) => record.Steps.Single(step => step.Id == id);

    // Release: binding, identity and a clean stop

    [Fact]
    public async Task ReleaseBindsTheServerTheWorkerAndTheGenerationAndSeesTheCleanStop()
    {
        var (record, world) = await RunAsync(OwnershipScenario.Release);

        Assert.True(record.Completed);
        Assert.True(record.ResidualKnown);
        Assert.Empty(record.Residual);
        Assert.Equal(["census-start", "bind-server", "describe", "sdk-state-before", "start-sdk", "census-started", "bind-generation", "bind-worker",
            "connect", "census-connected", "census-worker-descendants", "stop-sdk", "window-after-stop", "census-end"], record.Steps.Select(static step => step.Id));
        Assert.Equal("bound", StepOf(record, "bind-generation").Result);
        Assert.Empty(world.Control.TerminableIds);
        Assert.Equal((ServerId, 1, 0), (world.Server.Peer, record.VerifiedConnections, record.RefusedConnections));
        Assert.Equal(["start", "connect:1", "stop:1"], world.Server.Calls);
        Assert.Equal(("server-1", "worker-1", 1), (record.BoundServer, record.BoundWorker, record.BoundGeneration));
        var sdk = Assert.Single(record.Processes, static process => process.Role == ProcessRoles.Sdk);
        Assert.Equal(("sdk-1", ProcessRoles.ServiceHost, "appeared-during-start"), (sdk.Label, sdk.ParentRole, sdk.Attribution));
        Assert.False(sdk.PresentAtStart);
        Assert.NotNull(sdk.FirstAbsentMilliseconds);
        Assert.True(sdk.LastSeenMilliseconds <= sdk.FirstAbsentMilliseconds);
        var worker = Assert.Single(record.Processes, static process => process.Role == ProcessRoles.Worker);
        Assert.Equal((ProcessRoles.Server, "child-of-bound-server"), (worker.ParentRole, worker.Attribution));
        Assert.Equal("bound-server", record.Processes.Single(static process => process.Role == ProcessRoles.Server).Attribution);
        Assert.Equal(0, StepOf(record, "census-worker-descendants").DescendantCount);
        Assert.Equal("settled", StepOf(record, "window-after-stop").Result);
        Assert.Equal(12000, record.Server!.LifecycleBoundsMilliseconds["start_sdk_milliseconds"]);
        Assert.Equal([ServerId, WorkerId], world.Control.BoundIds);
        Assert.Empty(world.Control.Terminated);
    }

    [Fact]
    public async Task AnSdkProcessThatOutlivesTheStopIsAResidualAfterTheWindow()
    {
        var (record, _) = await RunAsync(OwnershipScenario.Release, static world => world.SdkSurvivesStop = true, observeSeconds: 3);

        Assert.True(record.Completed);
        Assert.Equal(["sdk-1"], record.Residual.Select(static process => process.Label));
        var window = StepOf(record, "window-after-stop");
        Assert.Equal("window-elapsed", window.Result);
        Assert.True(window.ElapsedMilliseconds >= 3000);
        Assert.Contains("unresolved until it exits on its own", OwnershipRecorder.ToMarkdown(record), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("sdk", "residual-worker-or-sdk-process")]
    [InlineData("no-sa", "exactly-one-spatial-analyzer-required")]
    [InlineData("no-server", "exactly-one-briosa-server-required")]
    [InlineData("started", "server-sdk-not-stopped")]
    [InlineData("other-target", "server-target-identity-mismatch")]
    [InlineData("long-bound", "step-timeout-below-published-bound")]
    [InlineData("endpoint-elsewhere", "endpoint-owner-not-the-server")]
    [InlineData("endpoint-shared", "endpoint-owner-not-the-server")]
    [InlineData("endpoint-unreadable", "endpoint-owner-unreadable")]
    [InlineData("other-session", "server-not-in-operator-session")]
    [InlineData("unbindable", "server-identity-unproven")]
    [InlineData("incomplete", "process-census-incomplete")]
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
                case "endpoint-elsewhere": world.Control.EndpointOwners = [SaId]; break;
                case "endpoint-shared": world.Control.EndpointOwners = [ServerId, SaId]; break;
                case "endpoint-unreadable": world.Control.EndpointOwners = null; break;
                case "other-session": world.Remove(ServerId); world.Add(ServerId, 1, ProcessCensus.BriosaServer, session: 7); break;
                case "unbindable": world.Control.Unbindable.Add(ServerId); break;
                case "incomplete": world.Statuses.Enqueue(SnapshotStatus.Incomplete); break;
            }
        });

        Assert.Equal(OwnershipRunner.PreconditionStop, record.StopReason);
        Assert.Equal(refusal, StepOf(record, "preconditions").Result);
        Assert.Empty(world.Server.Calls);
        Assert.Empty(world.Control.Terminated);
    }

    [Fact]
    public async Task AFailedStartEndsTheScriptAndActsOnNothingItDidNotBind()
    {
        var (record, world) = await RunAsync(OwnershipScenario.ParentDeath, static world => world.Server.StartFails = true);

        Assert.Equal(OwnershipRunner.LifecycleStop, record.StopReason);
        var start = StepOf(record, "start-sdk");
        Assert.Equal(("failed", "grpc:FailedPrecondition", "worker-startup-timeout"), (start.Result, start.Lifecycle!.Transport, start.Lifecycle.DiagnosticCode));
        Assert.Equal(["start"], world.Server.Calls);
        Assert.Equal("no-bound-generation", StepOf(record, "cleanup").Result);
        Assert.Empty(world.Control.Terminated);
    }

    [Fact]
    public async Task AGenerationInAFailedStartsErrorIsObservedAndNeverStopped()
    {
        // sdk-already-active carries the server's current generation, which this run did not create.
        var (record, world) = await RunAsync(OwnershipScenario.Release, static world => world.Server.AlreadyActiveGeneration = 7);

        Assert.Equal(OwnershipRunner.LifecycleStop, record.StopReason);
        Assert.Null(record.BoundGeneration);
        Assert.Equal(7, StepOf(record, "start-sdk").Lifecycle!.Generation);
        Assert.Equal("observed-in-error-not-bound", StepOf(record, "bind-generation").Result);
        Assert.Equal("no-bound-generation", StepOf(record, "cleanup").Result);
        Assert.Equal(["start"], world.Server.Calls);
        Assert.DoesNotContain(record.Steps, static step => step.Id == "bind-worker");
    }

    [Fact]
    public async Task AWorkerIsBoundOnlyFromACompletePostStartCensus()
    {
        // The post-start census is incomplete and hides a second worker of the same server: the
        // visible one is not provably the only one, so nothing is bound or terminated.
        var (record, world) = await RunAsync(OwnershipScenario.WorkerCrash, static world =>
        {
            world.Server.AfterStart = () => world.Add(ReplacementWorkerId, ServerId, ProcessCensus.BriosaWorker);
            world.Hidden.Add(ReplacementWorkerId);
            world.DegradeAfterSnapshots = 3;
        });

        Assert.Equal(OwnershipRunner.IdentificationStop, record.StopReason);
        Assert.Equal("census-incomplete", StepOf(record, "bind-worker").Result);
        Assert.Null(record.BoundWorker);
        Assert.Equal([ServerId], world.Control.BoundIds);
        Assert.Empty(world.Control.Terminated);
        Assert.Equal(["start", "stop:1"], world.Server.Calls);
    }

    [Fact]
    public async Task AConnectThatTimesOutStopsTheScriptAndStopsTheBoundGeneration()
    {
        var (record, world) = await RunAsync(OwnershipScenario.CompetingClient, static world => world.Server.ConnectAnswer =
            LifecycleResult.Failed("grpc:DeadlineExceeded", null));

        Assert.Equal(OwnershipRunner.LifecycleStop, record.StopReason);
        Assert.Equal(["start", "connect:1", "stop:1"], world.Server.Calls);
        Assert.Empty(world.Second.Calls);
        Assert.Empty(world.Server.Executed);
        Assert.Empty(record.Residual);
    }

    // Worker crash and parent death: only through the bindings

    [Fact]
    public async Task WorkerCrashTerminatesOnlyTheBoundWorkerThroughItsHandle()
    {
        var (record, world) = await RunAsync(OwnershipScenario.WorkerCrash, static world => world.SdkSurvivesWorker = true);

        Assert.Equal([WorkerId], world.Control.Terminated);
        var terminate = StepOf(record, "terminate-worker");
        Assert.Equal(("terminated", "worker-1"), (terminate.Result, terminate.Target));
        var faulted = StepOf(record, "await-faulted");
        Assert.Equal(("faulted", "WorkerProcessExited"), (faulted.Result, faulted.Lifecycle!.IncidentTermination));
        Assert.Contains("stop:1", world.Server.Calls);
        Assert.Equal(["sdk-1"], record.Residual.Select(static process => process.Label));
        Assert.True(record.Completed);
    }

    [Fact]
    public async Task AReplacementWorkerIsNeverSelectedWhenTheBoundOneHasGone()
    {
        // After connect the bound worker exits and the same server starts another one. The run
        // refuses to act instead of terminating the replacement.
        var (record, world) = await RunAsync(OwnershipScenario.WorkerCrash, static world => world.Server.AfterConnect = () =>
        {
            world.Remove(WorkerId);
            world.Add(ReplacementWorkerId, ServerId, ProcessCensus.BriosaWorker);
        });

        Assert.Equal(OwnershipRunner.BoundProcessGoneStop, record.StopReason);
        Assert.Equal(("bound-process-exited", "worker-1"), (StepOf(record, "terminate-worker").Result, StepOf(record, "terminate-worker").Target));
        Assert.Empty(world.Control.Terminated);
        Assert.Contains(record.Processes, static process => process.Label == "worker-2" && process.Attribution is null);
        Assert.DoesNotContain(record.Steps, static step => step.Id == "await-faulted");
    }

    [Theory]
    [InlineData("other-parent", "no-worker-of-the-bound-server")]
    [InlineData("other-session", "no-worker-of-the-bound-server")]
    [InlineData("two-workers", "several-workers-of-the-bound-server")]
    [InlineData("unbindable", "worker-identity-unproven")]
    public async Task AWorkerTheRunCannotProveIsItsOwnIsNeverBoundOrTerminated(string condition, string result)
    {
        var (record, world) = await RunAsync(OwnershipScenario.WorkerCrash, world =>
        {
            switch (condition)
            {
                case "other-parent": world.WorkerParent = 999; break;
                case "other-session": world.WorkerSession = 7; break;
                case "two-workers": world.Server.AfterStart = () => world.Add(ReplacementWorkerId, ServerId, ProcessCensus.BriosaWorker); break;
                case "unbindable": world.Control.Unbindable.Add(WorkerId); break;
            }
        });

        Assert.Equal(OwnershipRunner.IdentificationStop, record.StopReason);
        Assert.Equal(result, StepOf(record, "bind-worker").Result);
        Assert.Equal(["start", "stop:1"], world.Server.Calls);
        Assert.Empty(world.Control.Terminated);
    }

    [Fact]
    public async Task CleanupNeverStopsAGenerationThisRunDidNotStart()
    {
        var (record, world) = await RunAsync(OwnershipScenario.WorkerCrash, static world => world.Server.GenerationAfterTerminate = 2);

        Assert.Equal(OwnershipRunner.GenerationStop, record.StopReason);
        Assert.Equal("generation-changed", StepOf(record, "generation-check").Result);
        Assert.DoesNotContain(world.Server.Calls, static call => call.StartsWith("stop:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ParentDeathTerminatesOnlyTheBoundServerAndSendsNothingAfterIt()
    {
        var (record, world) = await RunAsync(OwnershipScenario.ParentDeath);

        Assert.Equal([ServerId], world.Control.Terminated);
        Assert.Equal(["start", "connect:1"], world.Server.Calls);
        Assert.Equal("settled", StepOf(record, "window-after-parent-death").Result);
        Assert.Empty(record.Residual);
        Assert.True(record.Completed);
    }

    [Theory]
    [InlineData("worker-replaced", "bound-worker-exited", OwnershipRunner.BoundProcessGoneStop)]
    [InlineData("generation-changed", "generation-changed", OwnershipRunner.GenerationStop)]
    [InlineData("not-ready", "sdk-not-ready", OwnershipRunner.LifecycleStop)]
    public async Task ParentDeathReChecksTheBoundWorkerAndGenerationBeforeTerminatingTheServer(string condition, string result, string stop)
    {
        // The second state read is the re-check just before the server would be terminated.
        var (record, world) = await RunAsync(OwnershipScenario.ParentDeath, world => world.Server.OnStateRead = read =>
        {
            if (read != 2)
            {
                return;
            }

            switch (condition)
            {
                case "worker-replaced":
                    world.Remove(WorkerId);
                    world.Add(ReplacementWorkerId, ServerId, ProcessCensus.BriosaWorker);
                    break;
                case "generation-changed": world.Server.ChangeGeneration(2); break;
                case "not-ready": world.Server.ChangeState(Api.SpatialAnalyzerSdkState.Running); break;
            }
        });

        Assert.Equal(stop, record.StopReason);
        Assert.Equal(result, StepOf(record, "recheck-before-termination").Result);
        Assert.Empty(world.Control.Terminated);
        Assert.DoesNotContain(record.Steps, static step => step.Id == "terminate-server");
        Assert.Equal(condition != "generation-changed", world.Server.Calls.Contains("stop:1"));
    }

    [Fact]
    public async Task OnlyTheScenarioThatTerminatesAProcessHoldsTheRightTo()
    {
        var (_, parent) = await RunAsync(OwnershipScenario.ParentDeath);
        var (_, crash) = await RunAsync(OwnershipScenario.WorkerCrash);
        var (_, release) = await RunAsync(OwnershipScenario.Release);

        Assert.Equal([ServerId], parent.Control.TerminableIds);
        Assert.Equal([WorkerId], crash.Control.TerminableIds);
        Assert.Empty(release.Control.TerminableIds);
    }

    [Fact]
    public async Task AParentDeathThatLeavesTheWorkerRunningIsAResidual()
    {
        var (record, _) = await RunAsync(OwnershipScenario.ParentDeath, static world => world.WorkerSurvivesServer = true, observeSeconds: 2);

        Assert.Equal(["sdk-1", "worker-1"], record.Residual.Select(static process => process.Label));
        Assert.Equal("window-elapsed", StepOf(record, "window-after-parent-death").Result);
    }

    // Watchdog kill: the product's watchdog ends the worker's tree; the tree is recorded first

    [Fact]
    public async Task WatchdogKillRecordsTheWorkersDescendantsBeforeTheStall()
    {
        var console = new FakeOperator(static (_, _) => OperatorObservationText.DialogDismissed);
        var (record, world) = await RunAsync(OwnershipScenario.WatchdogKill, world =>
        {
            world.Console = console;
            world.SdkSurvivesWorker = true;
            world.Server.AfterConnect = () => world.Add(WorkerChildId, WorkerId, "conhost");
        });

        Assert.Equal(["p26-baseline", "o-watchdog-stall"], world.Server.Executed);
        Assert.Equal(["o-watchdog-stall:before", "o-watchdog-stall:after"], console.Checkpoints);
        var descendants = StepOf(record, "census-descendants-before-stall");
        Assert.Equal((1, 0, "worker-1", "observed"), (descendants.DescendantCount, descendants.UnapprovedDescendantCount, descendants.Target, descendants.Result));

        // The census is the last thing before dispatch: after the operator's checkpoint.
        var ids = record.Steps.Select(static step => step.Id).ToList();
        Assert.True(ids.IndexOf("checkpoint-before-stall") < ids.IndexOf("census-descendants-before-stall"));
        Assert.Equal(ids.IndexOf("census-descendants-before-stall") + 1, ids.IndexOf("stall"));
        Assert.Empty(descendants.Alive);
        Assert.Equal("planned-stall", StepOf(record, "stall").Result);
        Assert.Equal(OperatorObservationText.DialogDismissed, StepOf(record, "checkpoint-dismiss-dialog").OperatorAnswer);
        Assert.Contains("stop-sdk-cleanup", record.Steps.Select(static step => step.Id));
        Assert.True(record.Completed);
        Assert.Equal(["sdk-1"], record.Residual.Select(static process => process.Label));
        var stall = ProbeCatalog.CreateOwnershipWatchdogSteps(new InteractionModeSettings(Api.SaInteractionMode.Manual, true))[^1];
        Assert.Equal("*", ((Api.MakeCollectionObjectNameRefListWildcardSelectionRequest)stall.Request).CollectionWildcardCriteria);
    }

    [Theory]
    [InlineData("unapproved", "unapproved-descendants", OwnershipRunner.DescendantStop)]
    [InlineData("sdk-descendant", "unapproved-descendants", OwnershipRunner.DescendantStop)]
    [InlineData("incomplete", "census-incomplete", OwnershipRunner.DescendantStop)]
    [InlineData("worker-gone", "observed", OwnershipRunner.BoundProcessGoneStop)]
    public async Task TheStallIsSentOnlyOnACompleteCensusOfApprovedDescendants(string condition, string census, string stop)
    {
        var world = new World();
        world.Console = new FakeOperator((_, timing) =>
        {
            if (timing == OperatorCheckpoint.Before)
            {
                switch (condition)
                {
                    case "unapproved": world.Add(WorkerChildId, WorkerId, "SENTINEL-UNRELATED"); break;
                    case "sdk-descendant": world.Add(SecondSdkId, WorkerId, ProcessCensus.SpatialAnalyzerSdk); break;
                    case "incomplete": world.Statuses.Enqueue(SnapshotStatus.Incomplete); break;
                    case "worker-gone": world.Remove(WorkerId); break;
                }
            }

            return OperatorObservationText.None;
        });
        var record = await OwnershipRunner.RunAsync(Options(OwnershipScenario.WatchdogKill), world.Environment(), world.Cancellation.Token);

        Assert.Equal(stop, record.StopReason);
        Assert.Equal(census, StepOf(record, "census-descendants-before-stall").Result);
        Assert.Equal("not-run", StepOf(record, "stall").Result);
        Assert.Equal(["p26-baseline"], world.Server.Executed);
        Assert.Contains("stop:1", world.Server.Calls);
        Assert.DoesNotContain(TestSupport.Sentinel, OwnershipRecorder.ToJson(record), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ALicensePromptStopsTheRunAndNothingMoreIsSent()
    {
        var (record, world) = await RunAsync(OwnershipScenario.WatchdogKill, static world => world.Console = new FakeOperator(static (_, timing) =>
            timing == OperatorCheckpoint.After ? OperatorObservationText.LicensePromptAbort : OperatorObservationText.None));

        Assert.Equal(ProbeSession.LicensePromptStop, record.StopReason);
        Assert.Equal("license-prompt", StepOf(record, "checkpoint-dismiss-dialog").Result);
        Assert.Equal(["start", "connect:1"], world.Server.Calls);
        Assert.DoesNotContain(record.Steps, static step => step.Id is "window-dialog-dismissed" or "sdk-state-cleanup");
        Assert.Equal("census-end", record.Steps[^1].Id);
    }

    [Fact]
    public async Task AStallThatEndsAnotherWayStopsTheScriptButStillCleansUp()
    {
        var (record, world) = await RunAsync(OwnershipScenario.WatchdogKill, static world => world.Server.StallAnswer =
            ProbeOutcome.Unknown("grpc:Unavailable", "worker-execution-watchdog-timeout"));

        Assert.Equal(ProbeSession.UnknownOutcomeStop, record.StopReason);
        Assert.Equal("completion-unknown", StepOf(record, "stall").Result);
        Assert.Equal(2, world.Server.Executed.Count);
        Assert.Contains(record.Steps, static step => step.Id == "window-after-cleanup");
    }

    // Competing client: every step needs the one before it

    [Fact]
    public async Task TheCompetingClientActivatesAttachesAndStopsWithoutAnMp()
    {
        var (record, world) = await RunAsync(OwnershipScenario.CompetingClient, static world => world.SecondActivatesNewSdk = true);

        Assert.Equal(["mp-before", "mp-during", "mp-after"], world.Server.Executed);
        Assert.Equal(["start", "connect", "stop"], world.Second.Calls);
        Assert.Equal("server-package", StepOf(record, "second-worker-package").Result);
        var second = Assert.Single(record.Processes, static process => process.ParentRole == ProcessRoles.Harness);
        Assert.Equal("worker-2", second.Label);
        Assert.NotNull(second.FirstAbsentMilliseconds);
        Assert.Contains(record.Processes, static process => process.Label == "sdk-2");
        Assert.Equal("settled", StepOf(record, "window-second-stopped").Result);
        Assert.True(record.Completed);
    }

    [Theory]
    [InlineData("baseline-failed")]
    [InlineData("baseline-unknown")]
    [InlineData("other-package")]
    [InlineData("package-unreadable")]
    public async Task NoSecondClientStartsWithoutABaselineAndTheServersOwnPackage(string condition)
    {
        ArgumentNullException.ThrowIfNull(condition);
        var (record, world) = await RunAsync(OwnershipScenario.CompetingClient, world =>
        {
            switch (condition)
            {
                case "baseline-failed": world.Server.ReadOnlyAnswer = TestSupport.Outcome(ProbeOutcomeKind.MpFailed); break;
                case "baseline-unknown": world.Server.ReadOnlyAnswer = ProbeOutcome.Unknown("grpc:DeadlineExceeded", "untyped-rpc-failure"); break;
                case "other-package": world.Control.PackageMatches = false; break;
                case "package-unreadable": world.Control.PackageMatches = null; break;
            }
        });

        Assert.Empty(world.Second.Calls);
        Assert.Equal(["mp-before"], world.Server.Executed);
        Assert.Contains("stop:1", world.Server.Calls);
        Assert.NotNull(record.StopReason);
        if (condition.StartsWith("baseline", StringComparison.Ordinal))
        {
            Assert.DoesNotContain(record.Steps, static step => step.Id == "second-worker-package");
        }
        else
        {
            Assert.Equal(OwnershipRunner.SecondWorkerStop, record.StopReason);
        }
    }

    [Theory]
    [InlineData("identity-mismatch", "start,stop")]
    [InlineData("not-ready", "start,stop")]
    [InlineData("not-connected", "start,connect,stop")]
    public async Task ASecondClientThatFailsAGateIsStoppedAndNothingAfterItRuns(string condition, string calls)
    {
        ArgumentNullException.ThrowIfNull(calls);
        var (record, world) = await RunAsync(OwnershipScenario.CompetingClient, world =>
        {
            switch (condition)
            {
                case "identity-mismatch": world.Second.StartAnswer = new SecondWorkerResult("ready", "Disconnected", null, false); break;
                case "not-ready": world.Second.StartAnswer = new SecondWorkerResult("startup-timeout", null, null, null); break;
                case "not-connected": world.Second.ConnectAnswer = new SecondWorkerResult("not-connected", "Faulted", "connect-ex-failed", null); break;
            }
        });

        Assert.Equal(OwnershipRunner.SecondWorkerStop, record.StopReason);
        Assert.Equal(calls.Split(','), world.Second.Calls);
        Assert.Equal(["mp-before"], world.Server.Executed);
        Assert.Contains("stop:1", world.Server.Calls);
    }

    [Theory]
    [InlineData("during-failed", ProbeSession.UnexpectedDispositionStop)]
    [InlineData("during-unknown", ProbeSession.UnknownOutcomeStop)]
    public async Task TheLastMpNeedsASuccessfulMpWhileTheSecondClientIsAttached(string condition, string stop)
    {
        var (record, world) = await RunAsync(OwnershipScenario.CompetingClient, world => world.Server.DuringAnswer = condition == "during-failed"
            ? TestSupport.Outcome(ProbeOutcomeKind.MpFailed)
            : ProbeOutcome.Unknown("grpc:DeadlineExceeded", "untyped-rpc-failure"));

        Assert.Equal(stop, record.StopReason);
        Assert.Equal(["mp-before", "mp-during"], world.Server.Executed);
        Assert.Equal(["start", "connect", "stop"], world.Second.Calls);
        Assert.Equal("not-run", StepOf(record, "mp-after").Result);
        Assert.DoesNotContain(record.Steps, static step => step.Id == "window-second-stopped");
        Assert.Contains("stop:1", world.Server.Calls);
    }

    [Theory]
    [InlineData("termination-unconfirmed")]
    [InlineData("stopped-but-listed")]
    public async Task TheLastMpNeedsTheSecondWorkersExitConfirmed(string condition)
    {
        var (record, world) = await RunAsync(OwnershipScenario.CompetingClient, world => world.Second.StopAnswer =
            new SecondWorkerResult(condition == "stopped-but-listed" ? "stopped" : condition, null, null, null), observeSeconds: 2);

        Assert.Equal(OwnershipRunner.SecondWorkerExitStop, record.StopReason);
        Assert.Equal(["mp-before", "mp-during"], world.Server.Executed);
        Assert.Equal("not-run", StepOf(record, "mp-after").Result);
        Assert.Equal(condition == "stopped-but-listed", record.Steps.Any(static step => step.Id == "window-second-stopped"));
        Assert.Contains("stop:1", world.Server.Calls);
    }

    // The endpoint: nothing is sent before binding, and only to the bound server

    [Fact]
    public async Task AnEndpointWhosePeerIsAnotherProcessIsRefusedBeforeAnythingIsSent()
    {
        var (record, world) = await RunAsync(OwnershipScenario.Release, static world => world.Server.PeerOwner = SaId);

        Assert.Equal(OwnershipRunner.PreconditionStop, record.StopReason);
        Assert.Equal("endpoint-peer-not-the-bound-server", StepOf(record, "preconditions").Result);
        Assert.Empty(world.Server.Calls);
        Assert.Equal((0, 1), (record.VerifiedConnections, record.RefusedConnections));
        Assert.False(record.Server!.PeerVerified);
    }

    [Fact]
    public async Task AReplacedConnectionToAnotherProcessIsNeverUsedForAStop()
    {
        // Calls: describe, state, start, connect, then the stop over a replaced connection.
        var (record, world) = await RunAsync(OwnershipScenario.Release, static world => world.Server.ReplaceConnectionAtCall = 5);

        Assert.Equal(OwnershipRunner.PeerStop, record.StopReason);
        Assert.Equal(("peer-unverified", LifecycleResult.PeerUnverifiedTransport), (StepOf(record, "stop-sdk").Result, StepOf(record, "stop-sdk").Lifecycle!.Transport));
        Assert.Equal(["start", "connect:1"], world.Server.Calls);
        Assert.Equal((1, 1), (record.VerifiedConnections, record.RefusedConnections));
        Assert.Contains("1 verified, 1 refused unused", OwnershipRecorder.ToMarkdown(record), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnMpOverAnUnprovenConnectionIsRecordedAsNeverSent()
    {
        var (record, world) = await RunAsync(OwnershipScenario.CompetingClient, static world => world.Server.ReplaceConnectionAtCall = 5);

        Assert.Equal(OwnershipRunner.PeerStop, record.StopReason);
        var before = StepOf(record, "mp-before");
        Assert.Equal(("peer-unverified", ProbeOutcomeKind.NotStarted), (before.Result, before.Outcome!.Kind));
        Assert.Empty(world.Server.Executed);
        Assert.Empty(world.Second.Calls);
    }

    // A license prompt outside a checkpoint

    [Fact]
    public async Task ALicensePromptSignalledBetweenStepsSendsNothingMoreNotEvenCleanup()
    {
        var console = new FakeOperator();
        var (record, world) = await RunAsync(OwnershipScenario.Release, world =>
        {
            world.Console = console;
            world.Server.AfterConnect = () => console.LicenseAbortRaised = true;
        });

        Assert.Equal(ProbeSession.LicensePromptStop, record.StopReason);
        var abort = StepOf(record, "license-prompt-abort");
        Assert.Equal(("interrupted-before-send", "stop-sdk"), (abort.Result, abort.Target));
        Assert.Equal(["start", "connect:1"], world.Server.Calls);
        Assert.DoesNotContain(record.Steps, static step => step.Id is "stop-sdk" or "window-after-stop" or "sdk-state-cleanup");
        Assert.Equal("census-end", record.Steps[^1].Id);
    }

    [Fact]
    public async Task ALicensePromptWhileTheSecondClientIsAttachedLeavesItAlone()
    {
        var console = new FakeOperator();
        var (record, world) = await RunAsync(OwnershipScenario.CompetingClient, world =>
        {
            world.Console = console;
            world.Second.AfterConnect = () => console.LicenseAbortRaised = true;
        });

        Assert.Equal(ProbeSession.LicensePromptStop, record.StopReason);
        Assert.True(world.Second.Abandoned);
        Assert.Equal(["start", "connect"], world.Second.Calls);
        Assert.Equal(["mp-before"], world.Server.Executed);
        Assert.DoesNotContain(world.Server.Calls, static call => call.StartsWith("stop:", StringComparison.Ordinal));
        Assert.Equal("mp-during", StepOf(record, "license-prompt-abort").Target);
    }

    // Observe and process census gaps

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
        Assert.Equal(["census-start", "window-observe", "census-end"], record.Steps.Select(static step => step.Id));
        Assert.Equal("settled", StepOf(record, "window-observe").Result);
        Assert.True(Assert.Single(record.Processes).PresentAtStart);
        Assert.Empty(record.Residual);
        Assert.True(record.ResidualKnown);
    }

    [Fact]
    public async Task AnIncompleteOrFailedCensusIsNeverAnAbsence()
    {
        var (record, world) = await RunAsync(OwnershipScenario.Observe, static world =>
        {
            world.Remove(SaId);
            world.Remove(ServerId);
            world.Add(SdkId, DcomHostId, ProcessCensus.SpatialAnalyzerSdk);
            world.Hidden.Add(SdkId);
            world.DegradeAfterSnapshots = 1;
        }, observeSeconds: 2);

        Assert.Equal("window-elapsed", StepOf(record, "window-observe").Result);
        Assert.True(Assert.Single(record.Processes).Alive);
        Assert.False(record.ResidualKnown);
        Assert.True(record.IncompleteSnapshots > 0);
        var markdown = OwnershipRecorder.ToMarkdown(record);
        Assert.Contains("Residual: unknown.", markdown, StringComparison.Ordinal);
        Assert.Contains("unknown: final census incomplete; see last seen", markdown, StringComparison.Ordinal);
        Assert.DoesNotContain("alive at end", markdown, StringComparison.Ordinal);
        Assert.Empty(world.Server.Calls);
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
        tracker.Observe(ProcessSnapshot.Of(new ProcessEntry(SdkId, DcomHostId, "spatialanalyzersdk", Epoch, Session)), 0);
        Assert.Equal(["sdk-1"], tracker.Alive(ProcessRoles.Sdk).Select(static process => process.Label));
        Assert.False(tracker.NoneConfirmed(ProcessRoles.Sdk));
    }

    [Fact]
    public void TheTrackerCountsGapsReappearancesAndDescendants()
    {
        var tracker = new ProcessTracker(HarnessId);
        ProcessEntry Sdk(DateTimeOffset at) => new(SdkId, DcomHostId, ProcessCensus.SpatialAnalyzerSdk, at, Session);
        tracker.Observe(ProcessSnapshot.Of(
            new ProcessEntry(DcomHostId, 4, "svchost", null),
            Sdk(Epoch),
            new ProcessEntry(WorkerId, 999, ProcessCensus.BriosaWorker, Epoch, Session),
            new ProcessEntry(WorkerChildId, WorkerId, "conhost", null),
            new ProcessEntry(SecondWorkerId, HarnessId, ProcessCensus.BriosaWorker, Epoch, Session),
            new ProcessEntry(31337, 4, "SENTINEL-UNRELATED", Epoch)), 0);
        Assert.Equal(1, tracker.Descendants(WorkerId).Count);

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
        Assert.Equal(["sdk-1", "worker-1", "worker-2", "sdk-2"], tracker.Processes.Select(static process => process.Label));
        Assert.Equal((1, 1, 1), (tracker.CompleteSnapshots - 3, tracker.IncompleteSnapshots, tracker.FailedSnapshots));
        Assert.Equal(ProcessRoles.Exited, tracker.Processes[1].ParentRole);
        Assert.Equal(ProcessRoles.Harness, tracker.Processes[2].ParentRole);
        Assert.Equal(ProcessRoles.Other, ProcessRoles.ParentRoleOf(1, new ProcessEntry(1, 0, "SENTINEL-UNRELATED", null), HarnessId));
    }

    // Cancellation: a record, the interrupted step, and cleanup on an independent token

    [Fact]
    public async Task CancellationDuringAnRpcRecordsTheSentStepAndStopsTheBoundGeneration()
    {
        var (record, world) = await RunAsync(OwnershipScenario.Release, static world => world.Server.CancelDuringConnect = true);

        Assert.Equal(OwnershipRunner.CancelledStop, record.StopReason);
        Assert.Equal("sent-outcome-unknown", StepOf(record, "connect").Result);
        Assert.Equal(("interrupted-before-send", "sdk-state-cleanup"), (StepOf(record, "cancelled").Result, StepOf(record, "cancelled").Target));
        Assert.Equal(["start", "connect:1", "stop:1"], world.Server.Calls);
        Assert.False(world.Server.StopTokenWasCancelled);
        Assert.Equal("census-end", record.Steps[^1].Id);
    }

    [Fact]
    public async Task CancellationDuringDiscoveryIsACancellationNeverAnUnreachableServer()
    {
        var (record, world) = await RunAsync(OwnershipScenario.Release, static world => world.Server.CancelDuringDescribe = true);

        Assert.Equal(OwnershipRunner.CancelledStop, record.StopReason);
        Assert.Equal(("sent-outcome-unknown", "describe"), (StepOf(record, "cancelled").Result, StepOf(record, "cancelled").Target));
        Assert.DoesNotContain(record.Steps, static step => step.Id == "preconditions");
        Assert.Equal("no-bound-generation", StepOf(record, "cleanup").Result);
        Assert.Empty(world.Server.Calls);
    }

    [Fact]
    public async Task CancellationDuringACheckpointSendsNothingMoreThroughTheStepButCleansUp()
    {
        var world = new World();
        world.Console = new FakeOperator((_, _) =>
        {
            world.Cancellation.Cancel();
            return null;
        });
        var record = await OwnershipRunner.RunAsync(Options(OwnershipScenario.WatchdogKill), world.Environment(), world.Cancellation.Token);

        Assert.Equal(OwnershipRunner.CancelledStop, record.StopReason);
        Assert.Equal(("interrupted-before-send", "checkpoint-before-stall"), (StepOf(record, "cancelled").Result, StepOf(record, "cancelled").Target));
        Assert.Equal(["p26-baseline"], world.Server.Executed);
        Assert.Contains("stop:1", world.Server.Calls);
        Assert.False(world.Server.StopTokenWasCancelled);
    }

    [Fact]
    public async Task CancellationDuringAWindowStillWritesBothRecords()
    {
        var root = Directory.CreateTempSubdirectory("briosa-ownership-");
        try
        {
            var world = new World { SdkSurvivesStop = true, CancelOnPause = 2 };
            var output = new StringWriter();
            var target = Path.Combine(root.FullName, "out");
            var code = await OwnershipProgram.RunAsync(
                ["--ownership", "release", ProbeTarget.ConfirmFlag, "--output-directory", target, "--observe-seconds", "30"], output, new StringWriter(),
                (_, _) => world.Environment(), world.Cancellation.Token);

            Assert.Equal(1, code);
            var json = await File.ReadAllTextAsync(Path.Combine(target, $"ownership-release-{ProbeTarget.SpatialAnalyzerTarget}.json"));
            using var document = JsonDocument.Parse(json);
            Assert.Equal(OwnershipRunner.CancelledStop, document.RootElement.GetProperty("stop_reason").GetString());
            var cancelled = document.RootElement.GetProperty("steps").EnumerateArray().Single(static step => step.GetProperty("id").GetString() == "cancelled");
            Assert.Equal(("window-after-stop", "interrupted-before-send"), (cancelled.GetProperty("target").GetString(), cancelled.GetProperty("result").GetString()));
            Assert.True(File.Exists(Path.Combine(target, $"ownership-release-{ProbeTarget.SpatialAnalyzerTarget}.md")));
            Assert.True(world.Server.Disposed);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    // Records, options and the real process helpers

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
        Assert.Equal("worker-1", document.RootElement.GetProperty("bound").GetProperty("worker").GetString());
        Assert.Equal("complete", document.RootElement.GetProperty("snapshots").GetProperty("final").GetString());
        Assert.True(document.RootElement.GetProperty("residual_known").GetBoolean());
        var process = document.RootElement.GetProperty("processes").EnumerateArray().First();
        Assert.True(process.TryGetProperty("last_seen_ms", out _));
        Assert.True(process.TryGetProperty("first_absent_ms", out _));
        Assert.False(process.TryGetProperty("exited_ms", out _));
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
    public void TheDryRunPrintsTheScriptTheBindingRuleAndTheTreeKill()
    {
        var watchdog = OwnershipProgram.RenderDryRun(OwnershipOptions.Parse(
            ["--ownership", "watchdog-kill", "--dry-run", "--restore-sa-interaction-mode", "manual"]));
        var release = OwnershipProgram.RenderDryRun(OwnershipOptions.Parse(["--ownership", "release", "--dry-run"]));

        Assert.Contains("DRY RUN: nothing was started, connected, or terminated.", watchdog, StringComparison.Ordinal);
        Assert.Contains("--Briosa:Security:Operations:Profile=device", watchdog, StringComparison.Ordinal);
        Assert.Contains("--Briosa:SpatialAnalyzer:InteractionMode=Unchanged", watchdog, StringComparison.Ordinal);
        Assert.Contains("AND ITS WHOLE PROCESS TREE", watchdog, StringComparison.Ordinal);
        Assert.Contains("it stays Manual", watchdog, StringComparison.Ordinal);
        Assert.Contains("every descendant is approved (conhost)", watchdog, StringComparison.Ordinal);
        Assert.Contains("own the endpoint's listener", release, StringComparison.Ordinal);
        Assert.Contains("every connection must reach it", release, StringComparison.Ordinal);
        Assert.Contains(FileOperatorConsole.LicenseAbortFileName, release, StringComparison.Ordinal);
        Assert.DoesNotContain("DCOM", watchdog + release, StringComparison.Ordinal);
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
            var refused = await OwnershipProgram.RunAsync(
                ["--ownership", "release", ProbeTarget.ConfirmFlag, "--output-directory", used.FullName], output, new StringWriter(),
                static (_, _) => throw new InvalidOperationException("no environment for a refused run"), CancellationToken.None);
            var fresh = Path.Combine(root.FullName, "fresh");
            var world = new World();
            var completed = await OwnershipProgram.RunAsync(
                ["--ownership", "release", ProbeTarget.ConfirmFlag, "--output-directory", fresh, "--observe-seconds", "10"], output, new StringWriter(),
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
    public void TheRealControlBindsOnlyBriosaProcessesAndRefusesAProcessItCannotProve()
    {
        using var self = System.Diagnostics.Process.GetCurrentProcess();
        var started = new DateTimeOffset(self.StartTime.ToUniversalTime(), TimeSpan.Zero);

        Assert.Throws<ArgumentException>(() => ProcessControl.Instance.Bind(SdkId, started, ProcessCensus.SpatialAnalyzerSdk, terminable: false));
        Assert.Throws<ArgumentException>(() => ProcessControl.Instance.Bind(SaId, started, ProcessCensus.SpatialAnalyzer, terminable: false));

        // The test process is not a Briosa worker, so its name never binds, whatever its start time.
        Assert.Null(ProcessControl.Instance.Bind(Environment.ProcessId, started, ProcessCensus.BriosaWorker, terminable: false));
        Assert.Equal(self.SessionId, ProcessControl.Instance.HarnessSessionId);
    }

    [Fact]
    public void AnObservationBindingCanQueryAndWaitButNeverTerminate()
    {
        using var self = System.Diagnostics.Process.GetCurrentProcess();
        var started = new DateTimeOffset(self.StartTime.ToUniversalTime(), TimeSpan.Zero);

        // The test process itself, opened without the right to terminate: the binding reads it
        // and refuses to end it. Nothing here can end the test process.
        using var bound = ProcessControl.Open(Environment.ProcessId, started, self.ProcessName, terminable: false);
        Assert.NotNull(bound);
        Assert.Equal(BindingCheck.Running, bound.Check());
        Assert.False(bound.Terminable);
        Assert.Equal("not-authorized", bound.Terminate());
        Assert.Equal(BindingCheck.Running, bound.Check());
        Assert.Null(ProcessControl.Open(Environment.ProcessId, started.AddSeconds(1), self.ProcessName, terminable: false));
    }

    [Fact]
    public void TheRealEndpointTableNamesTheListenersOwnerForItsOwnFamilyOnly()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var owners = ProcessControl.Instance.EndpointListeners(new Uri($"http://127.0.0.1:{port}"));
        var named = ProcessControl.Instance.EndpointListeners(new Uri($"http://localhost:{port}"));

        // An IPv4-only listener never answers for the IPv6 loopback address.
        var ipv6 = ProcessControl.Instance.EndpointListeners(new Uri($"http://[::1]:{port}"));

        Assert.NotNull(owners);
        Assert.Equal([Environment.ProcessId], owners.Distinct());
        Assert.Contains(Environment.ProcessId, named!);
        Assert.NotNull(ipv6);
        Assert.DoesNotContain(Environment.ProcessId, ipv6);
    }

    [Fact]
    public async Task TheRealConnectionTableNamesTheProcessAtTheOtherEnd()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        using var client = new TcpClient(AddressFamily.InterNetwork);
        await client.ConnectAsync(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port);
        using var accepted = await listener.AcceptTcpClientAsync();
        var local = (IPEndPoint)client.Client.LocalEndPoint!;
        var remote = (IPEndPoint)client.Client.RemoteEndPoint!;

        Assert.True(await PeerConnection.IsOwnedByAsync(local, remote, Environment.ProcessId, CancellationToken.None));
        Assert.False(await PeerConnection.IsOwnedByAsync(local, remote, 4, CancellationToken.None));
        Assert.False(await PeerConnection.IsOwnedByAsync(local, new IPEndPoint(IPAddress.Parse("192.0.2.1"), remote.Port), Environment.ProcessId,
            CancellationToken.None));
    }

    [Fact]
    public async Task TheRealServerClientSendsNothingBeforeBindingAndNothingToAnotherProcess()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        await using var server = new OwnershipServer(new Uri($"http://127.0.0.1:{port}"), TimeSpan.FromSeconds(5));
        using var bounded = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // Before binding: no connection is even opened.
        var unbound = await server.GetSdkStateAsync(CancellationToken.None);
        Assert.Equal(LifecycleResult.PeerUnverifiedTransport, unbound.Transport);
        Assert.False(listener.Pending());

        // Bound to another process: the connection opens, is proven not to reach it, and closes
        // before a byte is written.
        server.BindPeer(4);
        var other = await server.GetSdkStateAsync(CancellationToken.None);
        Assert.Equal(LifecycleResult.PeerUnverifiedTransport, other.Transport);
        using (var refused = await listener.AcceptTcpClientAsync(bounded.Token))
        {
            Assert.Equal(0, await refused.GetStream().ReadAsync(new byte[64], bounded.Token));
        }

        Assert.Equal((0, 2), (server.VerifiedConnections, server.RefusedConnections));

        // Bound to the process that owns the other end: the HTTP/2 preface is the first thing sent.
        server.BindPeer(Environment.ProcessId);
        var call = server.GetSdkStateAsync(CancellationToken.None);
        using (var verified = await listener.AcceptTcpClientAsync(bounded.Token))
        {
            var preface = new byte[3];
            await verified.GetStream().ReadExactlyAsync(preface, bounded.Token);
            Assert.Equal("PRI", Encoding.ASCII.GetString(preface));
        }

        Assert.False((await call).Ok);
        Assert.True(server.VerifiedConnections >= 1);
        Assert.Equal(2, server.RefusedConnections);
    }

    // Item 7: no harness-side termination ever names a whole process tree. Only the product's
    // own watchdog does, and the harness observes it.
    [Fact]
    public void NoHarnessSourceTerminatesAProcessTree()
    {
        var folder = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(SourcePath())!, "..", "..", "tools", "Briosa.LicensedProbes"));
        var sources = Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories)
            .Where(static file => !file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                !file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var forbidden = new Regex(@"entireProcessTree\s*:\s*true|\.Kill\(\s*true\s*\)|taskkill|TerminateJobObject|KillProcessTree",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        Assert.True(sources.Count > 10);
        Assert.Contains(sources, static file => File.ReadAllText(file).Contains("entireProcessTree: false", StringComparison.Ordinal));
        Assert.All(sources, file => Assert.False(forbidden.IsMatch(File.ReadAllText(file)), Path.GetFileName(file)));
    }

    private static string SourcePath([CallerFilePath] string path = "") => path;

    [Fact]
    public void TheWindowsProcessTableListsTheCurrentProcessReadOnly()
    {
        var snapshot = WindowsProcessTable.Instance.Snapshot();

        Assert.NotEqual(SnapshotStatus.Failed, snapshot.Status);
        Assert.Contains(snapshot.Entries, static entry => entry.Id == Environment.ProcessId);
        Assert.All(snapshot.Entries, static entry => Assert.DoesNotContain('\\', entry.Name));
    }

    [Fact]
    public void LifecycleFailuresKeepOnlyTheTypedStateAndAListedCode()
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
        var unlisted = error.Clone();
        unlisted.DiagnosticCode = "desktop-sentinel-01";
        unlisted.State.DiagnosticCode = "sentinel-account";
        var withheld = OwnershipServer.FromRpcException(new RpcException(new Status(StatusCode.Internal, "x"),
            new Metadata { { OwnershipServer.SdkLifecycleErrorTrailer, unlisted.ToByteArray() } }));

        Assert.Equal(("grpc:FailedPrecondition", "worker-startup-timeout", "Faulted", 2, "StartFailed"),
            (typed.Transport, typed.DiagnosticCode, typed.SdkState, typed.Generation, typed.IncidentTermination));
        Assert.Equal(("grpc:Unavailable", null), (untyped.Transport, untyped.DiagnosticCode));
        Assert.Equal(ExternalValues.Withheld, withheld.DiagnosticCode);
        Assert.Equal(["start_sdk_milliseconds"], OwnershipServer.Bounds(new Api.LifecycleBounds { StartSdkMilliseconds = 13000 }).Keys);
        Assert.Empty(OwnershipServer.Bounds(null));
    }

    [Fact]
    public void ServerVersionAndRevisionAreKeptOnlyInTheirShapes()
    {
        var clean = OwnershipServer.Describe(new Api.GetServerInfoResponse
        {
            Version = new Api.VersionCoordinates
            {
                SpatialAnalyzerTarget = ProbeTarget.SpatialAnalyzerTarget,
                BriosaVersion = "0.10.1-probe-s4",
                SourceRevision = new string('a', 40)
            }
        });
        var sensitive = OwnershipServer.Describe(new Api.GetServerInfoResponse
        {
            Version = new Api.VersionCoordinates
            {
                SpatialAnalyzerTarget = ProbeTarget.SpatialAnalyzerTarget,
                BriosaVersion = "0.10.1 built by SENTINEL-PC\\someone",
                SourceRevision = "license key SENTINEL-1234"
            }
        });

        Assert.Equal(("0.10.1-probe-s4", new string('a', 40)), (clean.ServerVersion, clean.SourceRevision));
        Assert.Equal((ExternalValues.Withheld, ExternalValues.Withheld), (sensitive.ServerVersion, sensitive.SourceRevision));
    }

    // A scripted machine: a process table that the server, process control and second worker change.
    private sealed class World
    {
        private readonly List<ProcessEntry> _processes = [];
        private int _ticks;
        private int _pauses;
        private int _snapshots;

        public World()
        {
            Server = new ScriptedServer(this);
            Control = new ScriptedControl(this);
            Second = new ScriptedSecondWorker(this);
            Add(DcomHostId, 4, "svchost");
            Add(SaId, 1, ProcessCensus.SpatialAnalyzer);
            Add(ServerId, 1, ProcessCensus.BriosaServer);
        }

        public ManualClock Clock { get; } = new(Epoch);

        public CancellationTokenSource Cancellation { get; } = new();

        public ScriptedServer Server { get; }

        public ScriptedControl Control { get; }

        public ScriptedSecondWorker Second { get; }

        public IOperatorConsole Console { get; set; } = new FakeOperator();

        public bool SdkSurvivesStop { get; set; }

        public bool SdkSurvivesWorker { get; set; }

        public bool WorkerSurvivesServer { get; set; }

        public bool SecondActivatesNewSdk { get; set; }

        public int? SdkExitsAfterTicks { get; set; }

        public int? CancelOnPause { get; set; }

        public int WorkerParent { get; set; } = ServerId;

        public int WorkerSession { get; set; } = Session;

        public Queue<SnapshotStatus> Statuses { get; } = new();

        // After this many snapshots, every snapshot is incomplete and leaves out the hidden processes.
        public int? DegradeAfterSnapshots { get; set; }

        public HashSet<int> Hidden { get; } = [];

        public OwnershipEnvironment Environment() =>
            new(new Table(this), Server, Control, () => Second, Console, new Pause(this), Clock, HarnessId);

        public void Add(int id, int parent, string name, int session = Session) =>
            _processes.Add(new ProcessEntry(id, parent, name, ProcessRoles.IsTracked(name) ? Epoch.AddSeconds(id % 100) : null,
                ProcessRoles.IsTracked(name) ? session : null));

        public void Remove(int id) => _processes.RemoveAll(entry => entry.Id == id);

        public ProcessEntry? Find(int id) => _processes.Find(entry => entry.Id == id);

        public bool Has(int id) => _processes.Exists(entry => entry.Id == id);

        public ProcessSnapshot Snapshot()
        {
            if (DegradeAfterSnapshots is { } after && ++_snapshots > after)
            {
                return new ProcessSnapshot(SnapshotStatus.Incomplete, [.. _processes.Where(entry => !Hidden.Contains(entry.Id))]);
            }

            return new ProcessSnapshot(Statuses.Count > 0 ? Statuses.Dequeue() : SnapshotStatus.Complete, [.. _processes]);
        }

        public void Tick()
        {
            if (SdkExitsAfterTicks is { } ticks && ++_ticks >= ticks)
            {
                Remove(SdkId);
            }
        }

        public void Paused()
        {
            if (CancelOnPause is { } pause && ++_pauses >= pause)
            {
                Cancellation.Cancel();
            }
        }

        private sealed class Table(World world) : IProcessTable
        {
            public ProcessSnapshot Snapshot() => world.Snapshot();
        }

        private sealed class Pause(World world) : IPause
        {
            public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
            {
                world.Paused();
                cancellationToken.ThrowIfCancellationRequested();
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

        public bool CancelDuringConnect { get; set; }

        public Action? AfterStart { get; set; }

        public Action? AfterConnect { get; set; }

        public int? GenerationAfterTerminate { get; set; }

        public ProbeOutcome? StallAnswer { get; set; }

        public ProbeOutcome? ReadOnlyAnswer { get; set; }

        public ProbeOutcome? DuringAnswer { get; set; }

        public bool StopTokenWasCancelled { get; private set; }

        public bool Disposed { get; private set; }

        // The process every connection must reach; null until the run binds the server.
        public int? Peer { get; private set; }

        // The process that actually owns the other end of the next connection.
        public int PeerOwner { get; set; } = ServerId;

        // The connection is replaced before this call (counted from 1), and the new one's other end
        // is owned by ReplacementOwner.
        public int? ReplaceConnectionAtCall { get; set; }

        public int ReplacementOwner { get; set; } = SaId;

        // The generation a refused start carries in its error, as sdk-already-active does.
        public int? AlreadyActiveGeneration { get; set; }

        public bool CancelDuringDescribe { get; set; }

        public int VerifiedConnections { get; private set; }

        public int RefusedConnections { get; private set; }

        // Called with the ordinal of each state read, before the state is answered.
        public Action<int>? OnStateRead { get; set; }

        private bool _connected;
        private int _calls;
        private int _stateReads;

        public void ChangeGeneration(int generation) => _generation = generation;

        public void ChangeState(Api.SpatialAnalyzerSdkState state) => _state = state;

        public void BindPeer(int serverProcessId) => Peer = serverProcessId;

        // Every call opens or reuses a connection; one whose other end is not the bound server is
        // refused and the call is never sent.
        private bool Connection()
        {
            _calls++;
            if (ReplaceConnectionAtCall == _calls)
            {
                (_connected, PeerOwner) = (false, ReplacementOwner);
            }

            if (_connected)
            {
                return true;
            }

            if (Peer is null || Peer != PeerOwner)
            {
                RefusedConnections++;
                return false;
            }

            (_connected, VerifiedConnections) = (true, VerifiedConnections + 1);
            return true;
        }

        public async Task<ServerDescription> DescribeAsync(CancellationToken cancellationToken)
        {
            if (!Connection())
            {
                return ServerDescription.PeerRefused;
            }

            if (CancelDuringDescribe)
            {
                await world.Cancellation.CancelAsync();
                cancellationToken.ThrowIfCancellationRequested();
            }

            return new ServerDescription(true, TargetMatches, "0.10.1-probe-s4", new string('a', 40),
                new SortedDictionary<string, long>(StringComparer.Ordinal) { ["start_sdk_milliseconds"] = StartBound });
        }

        public Task<LifecycleResult> GetSdkStateAsync(CancellationToken cancellationToken)
        {
            if (!Connection())
            {
                return Task.FromResult(LifecycleResult.Failed(LifecycleResult.PeerUnverifiedTransport, null));
            }

            if (!_initialized)
            {
                _initialized = true;
                _state = InitialState;
            }

            OnStateRead?.Invoke(++_stateReads);

            // The server notices a lost worker on its own.
            if (_state == Api.SpatialAnalyzerSdkState.Ready && !world.Has(WorkerId) && !world.Has(ReplacementWorkerId))
            {
                _state = Api.SpatialAnalyzerSdkState.Faulted;
                _generation = GenerationAfterTerminate ?? _generation;
            }

            return Task.FromResult(State("grpc:OK"));
        }

        public Task<LifecycleResult> StartSdkAsync(CancellationToken cancellationToken)
        {
            if (!Connection())
            {
                return Task.FromResult(LifecycleResult.Failed(LifecycleResult.PeerUnverifiedTransport, null));
            }

            Calls.Add("start");
            if (StartFails)
            {
                return Task.FromResult(LifecycleResult.Failed("grpc:FailedPrecondition", "worker-startup-timeout"));
            }

            // Refused because an SDK is already active: the error carries the current generation.
            if (AlreadyActiveGeneration is { } current)
            {
                return Task.FromResult(new LifecycleResult("grpc:FailedPrecondition", "Ready", current, true, "Connected", "sdk-already-active", null));
            }

            _generation = 1;
            _state = Api.SpatialAnalyzerSdkState.Running;
            world.Add(WorkerId, world.WorkerParent, ProcessCensus.BriosaWorker, world.WorkerSession);
            world.Add(SdkId, DcomHostId, ProcessCensus.SpatialAnalyzerSdk);
            AfterStart?.Invoke();
            return Task.FromResult(State("grpc:OK"));
        }

        public async Task<LifecycleResult> ConnectAsync(int generation, CancellationToken cancellationToken)
        {
            if (!Connection())
            {
                return LifecycleResult.Failed(LifecycleResult.PeerUnverifiedTransport, null);
            }

            Calls.Add($"connect:{generation}");
            if (CancelDuringConnect)
            {
                await world.Cancellation.CancelAsync();
                return LifecycleResult.Failed(LifecycleResult.ClientCancelledTransport, "client-cancelled");
            }

            if (ConnectAnswer is { } answer)
            {
                return answer;
            }

            _state = Api.SpatialAnalyzerSdkState.Ready;
            AfterConnect?.Invoke();
            return State("grpc:OK");
        }

        public Task<LifecycleResult> StopSdkAsync(int generation, CancellationToken cancellationToken)
        {
            if (!Connection())
            {
                return Task.FromResult(LifecycleResult.Failed(LifecycleResult.PeerUnverifiedTransport, null));
            }

            Calls.Add($"stop:{generation}");
            StopTokenWasCancelled = cancellationToken.IsCancellationRequested;
            _state = Api.SpatialAnalyzerSdkState.Stopped;
            world.Remove(WorkerId);
            world.Remove(ReplacementWorkerId);
            if (!world.SdkSurvivesStop && !world.SdkSurvivesWorker)
            {
                world.Remove(SdkId);
            }

            return Task.FromResult(State("grpc:OK"));
        }

        public Task<ProbeOutcome> ExecuteAsync(ProbeStep step, CancellationToken cancellationToken)
        {
            if (!Connection())
            {
                return Task.FromResult(OwnershipServer.PeerUnverifiedOutcome);
            }

            Executed.Add(step.Id);
            if (step.ExpectedStall is not null)
            {
                // The product's watchdog ends the stalled worker and its process tree.
                world.Remove(WorkerId);
                world.Remove(WorkerChildId);
                if (!world.SdkSurvivesWorker)
                {
                    world.Remove(SdkId);
                }

                _state = Api.SpatialAnalyzerSdkState.Faulted;
                return Task.FromResult(StallAnswer ?? ServerReportsModalDialog());
            }

            if (step.Id == "mp-during" && DuringAnswer is { } during)
            {
                return Task.FromResult(during);
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

    private sealed class ScriptedControl(World world) : IProcessControl
    {
        public List<int> BoundIds { get; } = [];

        public List<int> Terminated { get; } = [];

        public HashSet<int> Unbindable { get; } = [];

        public IReadOnlyList<int>? EndpointOwners { get; set; } = [ServerId];

        public bool? PackageMatches { get; set; } = true;

        public int HarnessSessionId => Session;

        // Which bindings were opened with the right to terminate.
        public List<int> TerminableIds { get; } = [];

        public IBoundProcess? Bind(int processId, DateTimeOffset startedAt, string expectedName, bool terminable)
        {
            if (Unbindable.Contains(processId) || world.Find(processId) is not { } entry ||
                entry.StartedAt != startedAt || !string.Equals(entry.Name, expectedName, StringComparison.Ordinal))
            {
                return null;
            }

            BoundIds.Add(processId);
            if (terminable)
            {
                TerminableIds.Add(processId);
            }

            return new Binding(world, this, processId, startedAt, terminable);
        }

        public IReadOnlyList<int>? EndpointListeners(Uri address) => EndpointOwners;

        public bool? WorkerFromServerPackage(IBoundProcess server, string workerPath) => PackageMatches;

        private sealed class Binding(World world, ScriptedControl control, int id, DateTimeOffset startedAt, bool terminable) : IBoundProcess
        {
            // The held handle names this process only: a replacement with another id or start time is not it.
            public BindingCheck Check() => world.Find(id) is { } entry && entry.StartedAt == startedAt ? BindingCheck.Running : BindingCheck.Exited;

            public string Terminate()
            {
                if (!terminable)
                {
                    return "not-authorized";
                }

                if (Check() != BindingCheck.Running)
                {
                    return "exited";
                }

                control.Terminated.Add(id);
                world.Remove(id);
                if (id == WorkerId && !world.SdkSurvivesWorker)
                {
                    world.Remove(SdkId);
                }

                // A worker notices its parent's death and releases the SDK.
                if (id == ServerId && !world.WorkerSurvivesServer)
                {
                    world.Remove(WorkerId);
                    world.Remove(SdkId);
                }

                return "terminated";
            }

            public void Dispose()
            {
            }
        }
    }

    private sealed class ScriptedSecondWorker(World world) : ISecondWorker
    {
        public List<string> Calls { get; } = [];

        public SecondWorkerResult? StartAnswer { get; set; }

        public SecondWorkerResult? ConnectAnswer { get; set; }

        public SecondWorkerResult? StopAnswer { get; set; }

        public bool Abandoned { get; private set; }

        public Action? AfterConnect { get; set; }

        public void Abandon() => Abandoned = true;

        public Task<SecondWorkerResult> StartAsync(CancellationToken cancellationToken)
        {
            Calls.Add("start");
            if (StartAnswer is { } answer)
            {
                return Task.FromResult(answer);
            }

            world.Add(SecondWorkerId, HarnessId, ProcessCensus.BriosaWorker);
            if (world.SecondActivatesNewSdk)
            {
                world.Add(SecondSdkId, DcomHostId, ProcessCensus.SpatialAnalyzerSdk);
            }

            return Task.FromResult(new SecondWorkerResult(SecondWorkerResult.Ready, "Disconnected", "worker-ready", true));
        }

        public Task<SecondWorkerResult> ConnectAsync(CancellationToken cancellationToken)
        {
            Calls.Add("connect");
            AfterConnect?.Invoke();
            return Task.FromResult(ConnectAnswer ?? new SecondWorkerResult(SecondWorkerResult.Connected, "Connected", "connect-ex-connected", null));
        }

        public Task<SecondWorkerResult> StopAsync(CancellationToken cancellationToken)
        {
            Calls.Add("stop");
            if (StopAnswer is { } answer)
            {
                return Task.FromResult(answer);
            }

            world.Remove(SecondWorkerId);
            world.Remove(SecondSdkId);
            return Task.FromResult(new SecondWorkerResult("stopped", null, null, null));
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
