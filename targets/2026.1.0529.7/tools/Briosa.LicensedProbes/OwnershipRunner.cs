using Api = global::Briosa;

namespace Briosa.LicensedProbes;

/// <summary>
/// Runs one SDK-ownership probe (#252, #402) as a fixed script. Every call is bounded, every
/// MP step is sent at most once and never replayed, and the process list is only read. The
/// harness terminates nothing but the one Briosa worker or server process a script names,
/// after checking it is still the process the tracker labelled; it never touches
/// SpatialAnalyzer or an SDK process. A failed or unexpected step ends the script's own
/// steps, but the cleanup and the final observation window still run, so the record always
/// says what was left behind.
/// </summary>
internal static class OwnershipRunner
{
    public const string PreconditionStop = "ownership-precondition-not-met";
    public const string LifecycleStop = "lifecycle-call-failed";
    public const string TerminationStop = "termination-failed";
    public const string IdentificationStop = "process-not-identified";
    public const string SecondWorkerStop = "second-worker-not-ready";

    public static TimeSpan PollInterval { get; } = TimeSpan.FromMilliseconds(500);

    // How long worker-crash waits for the server to report the lost worker.
    public static TimeSpan FaultedWait { get; } = TimeSpan.FromSeconds(30);

    public static async Task<OwnershipRecord> RunAsync(OwnershipOptions options, OwnershipEnvironment environment, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(environment);
        var run = new Run(options, environment);
        await run.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        return run.ToRecord();
    }

    /// <summary>The read-only MP step that competing-client sends through the first client.</summary>
    internal static ProbeStep ReadOnlyStep(string id) =>
        new(id, "ownership", ProbeStepKind.Guard, ProbePhase.PublicApi, "Count collections through the first client (read-only)",
            ProbeOperations.GetNumberOfCollections, new Api.GetNumberOfCollectionsRequest(), CommandVariant.Shipped, ProbeOutcomes.Succeeded);

    /// <summary>The steps a scenario sends through the server, for the dry run and admission arguments.</summary>
    internal static IReadOnlyList<ProbeStep> MpSteps(OwnershipScenario scenario, InteractionModeSettings mode) => scenario switch
    {
        OwnershipScenario.WatchdogKill => ProbeCatalog.CreateOwnershipWatchdogSteps(mode),
        OwnershipScenario.CompetingClient => [ReadOnlyStep("mp-before"), ReadOnlyStep("mp-during"), ReadOnlyStep("mp-after")],
        _ => []
    };

    private sealed class Run(OwnershipOptions options, OwnershipEnvironment environment)
    {
        private readonly ProcessTracker _tracker = new(environment.HarnessProcessId);
        private readonly List<OwnershipStepRecord> _steps = [];
        private readonly DateTimeOffset _started = environment.Clock.GetUtcNow();
        private string? _stopReason;
        private ServerDescription? _server;
        private bool _mpAllowed = true;

        public OwnershipRecord ToRecord() => new(options.Scenario, _started, environment.Clock.GetUtcNow(), _stopReason, _server,
            [.. _steps], _tracker.Processes, options.ObserveWindow, options.HarnessRevision);

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Census("census-start");
            if (options.Scenario == OwnershipScenario.Observe)
            {
                await WindowAsync("window-observe", () => None(ProcessRoles.Sdk, ProcessRoles.Worker, ProcessRoles.Server), cancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            var server = environment.Server ?? throw new InvalidOperationException("An ownership probe needs the server client.");
            if (!await PreconditionsAsync(server, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            var started = await LifecycleAsync("start-sdk", "start-sdk", server.StartSdkAsync, cancellationToken).ConfigureAwait(false);
            Census("census-started");
            if (!started.Ok || started.Generation is not { } generation)
            {
                Stop(LifecycleStop);
                await CleanupAsync(server, cancellationToken).ConfigureAwait(false);
                return;
            }

            var connected = await LifecycleAsync("connect", "connect", token => server.ConnectAsync(generation, token), cancellationToken)
                .ConfigureAwait(false);
            Census("census-connected");
            if (!connected.Ok || !connected.ReadyForMp)
            {
                Stop(LifecycleStop);
                await CleanupAsync(server, cancellationToken).ConfigureAwait(false);
                return;
            }

            switch (options.Scenario)
            {
                case OwnershipScenario.Release:
                    await StopSdkAsync(server, "stop-sdk", generation, cancellationToken).ConfigureAwait(false);
                    await WindowAsync("window-after-stop", () => None(ProcessRoles.Sdk, ProcessRoles.Worker), cancellationToken).ConfigureAwait(false);
                    break;
                case OwnershipScenario.WorkerCrash:
                    await WorkerCrashAsync(server, cancellationToken).ConfigureAwait(false);
                    break;
                case OwnershipScenario.ParentDeath:
                    await ParentDeathAsync(cancellationToken).ConfigureAwait(false);
                    break;
                case OwnershipScenario.WatchdogKill:
                    await WatchdogKillAsync(server, cancellationToken).ConfigureAwait(false);
                    break;
                case OwnershipScenario.CompetingClient:
                    await CompetingClientAsync(server, cancellationToken).ConfigureAwait(false);
                    break;
            }
        }

        private async Task<bool> PreconditionsAsync(IOwnershipServer server, CancellationToken cancellationToken)
        {
            // One SA, one inert server, and no worker or SDK process: everything that appears is this run's.
            var refusal =
                _tracker.Alive(ProcessRoles.SpatialAnalyzer).Count != 1 ? "exactly-one-spatial-analyzer-required" :
                _tracker.Alive(ProcessRoles.Server).Count != 1 ? "exactly-one-briosa-server-required" :
                _tracker.Alive(ProcessRoles.Worker).Count != 0 || _tracker.Alive(ProcessRoles.Sdk).Count != 0 ? "residual-worker-or-sdk-process" :
                null;
            if (refusal is null)
            {
                var offset = Offset();
                _server = await server.DescribeAsync(cancellationToken).ConfigureAwait(false);
                refusal = !_server.Reachable ? "server-discovery-unavailable" : !_server.TargetMatches ? "server-target-identity-mismatch" : null;
                _steps.Add(new OwnershipStepRecord("describe", "describe", offset, Offset() - offset, refusal ?? "observed"));
            }

            if (refusal is null)
            {
                var state = await LifecycleAsync("sdk-state-before", "get-sdk-state", server.GetSdkStateAsync, cancellationToken).ConfigureAwait(false);
                refusal = state.Ok && string.Equals(state.SdkState, Api.SpatialAnalyzerSdkState.Stopped.ToString(), StringComparison.Ordinal)
                    ? null
                    : "server-sdk-not-stopped";
            }

            if (refusal is null)
            {
                return true;
            }

            _steps.Add(new OwnershipStepRecord("preconditions", "refuse", Offset(), 0, refusal));
            Stop(PreconditionStop);
            return false;
        }

        private async Task WorkerCrashAsync(IOwnershipServer server, CancellationToken cancellationToken)
        {
            if (!await TerminateAsync("terminate-worker", ProcessRoles.Worker, ProcessCensus.BriosaWorker, cancellationToken).ConfigureAwait(false))
            {
                await CleanupAsync(server, cancellationToken).ConfigureAwait(false);
                return;
            }

            // The server notices the lost worker on its own; the probe only reads its state.
            var offset = Offset();
            LifecycleResult state;
            while (true)
            {
                state = await server.GetSdkStateAsync(cancellationToken).ConfigureAwait(false);
                if ((state.Ok && string.Equals(state.SdkState, Api.SpatialAnalyzerSdkState.Faulted.ToString(), StringComparison.Ordinal)) ||
                    Offset() - offset >= (long)FaultedWait.TotalMilliseconds)
                {
                    break;
                }

                await environment.Pause.DelayAsync(PollInterval, cancellationToken).ConfigureAwait(false);
                _tracker.Observe(environment.Processes.Snapshot(), Offset());
            }

            _steps.Add(new OwnershipStepRecord("await-faulted", "get-sdk-state", offset, Offset() - offset,
                string.Equals(state.SdkState, Api.SpatialAnalyzerSdkState.Faulted.ToString(), StringComparison.Ordinal) ? "faulted" : "not-faulted-in-time")
            {
                Lifecycle = state,
                Alive = AliveLabels()
            });
            await WindowAsync("window-after-crash", () => None(ProcessRoles.Sdk, ProcessRoles.Worker), cancellationToken).ConfigureAwait(false);
            await CleanupAsync(server, cancellationToken).ConfigureAwait(false);
        }

        private async Task ParentDeathAsync(CancellationToken cancellationToken)
        {
            // The server is gone afterwards, so there is no lifecycle cleanup: only the window.
            if (await TerminateAsync("terminate-server", ProcessRoles.Server, ProcessCensus.BriosaServer, cancellationToken).ConfigureAwait(false))
            {
                await WindowAsync("window-after-parent-death", () => None(ProcessRoles.Sdk, ProcessRoles.Worker, ProcessRoles.Server), cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async Task WatchdogKillAsync(IOwnershipServer server, CancellationToken cancellationToken)
        {
            var steps = ProbeCatalog.CreateOwnershipWatchdogSteps(options.InteractionMode);
            var baseline = steps[0];
            var stall = steps[^1];
            var mode = await MpAsync("set-interaction-mode", server, baseline, cancellationToken).ConfigureAwait(false);
            if (mode is null || !baseline.Acceptable.Accepts(mode))
            {
                Stop(ProbeSession.UnexpectedDispositionStop);
                await CleanupAsync(server, cancellationToken).ConfigureAwait(false);
                return;
            }

            if (!await CheckpointAsync("checkpoint-before-stall", stall, OperatorCheckpoint.Before, cancellationToken).ConfigureAwait(false))
            {
                await CleanupAsync(server, cancellationToken).ConfigureAwait(false);
                return;
            }

            var outcome = await MpAsync("stall", server, stall, cancellationToken).ConfigureAwait(false);
            if (outcome is null || stall.ExpectedStall?.Matches(outcome) != true)
            {
                Stop(outcome?.CompletionUnknown == true ? ProbeSession.UnknownOutcomeStop : ProbeSession.UnexpectedDispositionStop);
            }

            // The dialog is still open: first how the SDK process fares with it, then after it is dismissed.
            await WindowAsync("window-dialog-open", () => None(ProcessRoles.Sdk), cancellationToken).ConfigureAwait(false);
            await CheckpointAsync("checkpoint-dismiss-dialog", stall, OperatorCheckpoint.After, cancellationToken).ConfigureAwait(false);
            await WindowAsync("window-dialog-dismissed", () => None(ProcessRoles.Sdk), cancellationToken).ConfigureAwait(false);
            await CleanupAsync(server, cancellationToken).ConfigureAwait(false);
        }

        private async Task CompetingClientAsync(IOwnershipServer server, CancellationToken cancellationToken)
        {
            await MpAsync("mp-before", server, ReadOnlyStep("mp-before"), cancellationToken).ConfigureAwait(false);
            var factory = environment.SecondWorker ?? throw new InvalidOperationException("competing-client needs the second worker.");
            var second = factory();
            await using (second.ConfigureAwait(false))
            {
                var start = await SecondAsync("second-start", second.StartAsync, cancellationToken).ConfigureAwait(false);
                Census("census-second-started");
                if (string.Equals(start.Result, "ready", StringComparison.Ordinal))
                {
                    await SecondAsync("second-connect", second.ConnectAsync, cancellationToken).ConfigureAwait(false);
                    Census("census-second-connected");
                    await MpAsync("mp-during", server, ReadOnlyStep("mp-during"), cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    Stop(SecondWorkerStop);
                }

                await SecondAsync("second-stop", second.StopAsync, cancellationToken).ConfigureAwait(false);
            }

            await WindowAsync("window-second-stopped", () => _tracker.Processes.All(static process =>
                !process.Alive || process.Role != ProcessRoles.Worker || process.ParentRole != ProcessRoles.Harness), cancellationToken).ConfigureAwait(false);
            await MpAsync("mp-after", server, ReadOnlyStep("mp-after"), cancellationToken).ConfigureAwait(false);
            await CleanupAsync(server, cancellationToken).ConfigureAwait(false);
        }

        // Stops the server's current SDK generation, if it has one, and watches what remains.
        private async Task CleanupAsync(IOwnershipServer server, CancellationToken cancellationToken)
        {
            var state = await LifecycleAsync("sdk-state-cleanup", "get-sdk-state", server.GetSdkStateAsync, cancellationToken).ConfigureAwait(false);
            if (state.Ok && state.Generation is { } generation &&
                !string.Equals(state.SdkState, Api.SpatialAnalyzerSdkState.Stopped.ToString(), StringComparison.Ordinal))
            {
                await StopSdkAsync(server, "stop-sdk-cleanup", generation, cancellationToken).ConfigureAwait(false);
            }

            await WindowAsync("window-after-cleanup", () => None(ProcessRoles.Sdk, ProcessRoles.Worker), cancellationToken).ConfigureAwait(false);
        }

        private async Task StopSdkAsync(IOwnershipServer server, string id, int generation, CancellationToken cancellationToken)
        {
            var stopped = await LifecycleAsync(id, "stop-sdk", token => server.StopSdkAsync(generation, token), cancellationToken).ConfigureAwait(false);
            if (!stopped.Ok)
            {
                Stop(LifecycleStop);
            }
        }

        private Task<bool> TerminateAsync(string id, string role, string processName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Terminate(id, role, processName));
        }

        private bool Terminate(string id, string role, string processName)
        {
            _tracker.Observe(environment.Processes.Snapshot(), Offset());
            var candidates = _tracker.Alive(role);
            if (candidates.Count != 1 || _tracker.Identity(candidates[0].Label) is not { } identity)
            {
                _steps.Add(new OwnershipStepRecord(id, "terminate", Offset(), 0, "not-identified") { Alive = AliveLabels() });
                Stop(IdentificationStop);
                return false;
            }

            var offset = Offset();
            var result = environment.Terminator.Terminate(identity.Id, identity.StartedAt, processName);
            _tracker.Observe(environment.Processes.Snapshot(), Offset());
            _steps.Add(new OwnershipStepRecord(id, "terminate", offset, Offset() - offset, result)
            {
                Target = candidates[0].Label,
                Alive = AliveLabels()
            });
            if (!string.Equals(result, "terminated", StringComparison.Ordinal))
            {
                Stop(TerminationStop);
                return false;
            }

            return true;
        }

        private async Task<ProbeOutcome?> MpAsync(string id, IOwnershipServer server, ProbeStep step, CancellationToken cancellationToken)
        {
            if (!_mpAllowed)
            {
                _steps.Add(new OwnershipStepRecord(id, "mp", Offset(), 0, "not-run"));
                return null;
            }

            var offset = Offset();
            var outcome = await server.ExecuteAsync(step, cancellationToken).ConfigureAwait(false);
            var stalled = step.ExpectedStall?.Matches(outcome) == true;
            var result = stalled ? "planned-stall" : step.Acceptable.Accepts(outcome) ? "observed" : outcome.CompletionUnknown ? "completion-unknown" : "unexpected";

            // Nothing more is sent through the server after an outcome whose completion is unknown.
            if (outcome.CompletionUnknown)
            {
                _mpAllowed = false;
            }

            if (!stalled && !step.Acceptable.Accepts(outcome))
            {
                Stop(outcome.CompletionUnknown ? ProbeSession.UnknownOutcomeStop : ProbeSession.UnexpectedDispositionStop);
            }

            _tracker.Observe(environment.Processes.Snapshot(), Offset());
            _steps.Add(new OwnershipStepRecord(id, "mp", offset, Offset() - offset, result) { Outcome = outcome, Alive = AliveLabels() });
            return outcome;
        }

        private async Task<SecondWorkerResult> SecondAsync(string id, Func<CancellationToken, Task<SecondWorkerResult>> call, CancellationToken cancellationToken)
        {
            var offset = Offset();
            var result = await call(cancellationToken).ConfigureAwait(false);
            _tracker.Observe(environment.Processes.Snapshot(), Offset());
            _steps.Add(new OwnershipStepRecord(id, "second-worker", offset, Offset() - offset, result.Result)
            {
                SecondWorker = result,
                Alive = AliveLabels()
            });
            return result;
        }

        private async Task<bool> CheckpointAsync(string id, ProbeStep step, OperatorCheckpoint timing, CancellationToken cancellationToken)
        {
            var offset = Offset();
            var answer = await environment.Console.CheckpointAsync(step, timing, cancellationToken).ConfigureAwait(false);
            _steps.Add(new OwnershipStepRecord(id, "checkpoint", offset, Offset() - offset, answer is null ? "not-answered" : "answered")
            {
                OperatorAnswer = answer
            });
            if (answer is null)
            {
                Stop(ProbeSession.CheckpointStop);
                return false;
            }

            return true;
        }

        private async Task<LifecycleResult> LifecycleAsync(
            string id, string action, Func<CancellationToken, Task<LifecycleResult>> call, CancellationToken cancellationToken)
        {
            var offset = Offset();
            var result = await call(cancellationToken).ConfigureAwait(false);
            _tracker.Observe(environment.Processes.Snapshot(), Offset());
            _steps.Add(new OwnershipStepRecord(id, action, offset, Offset() - offset, result.Ok ? "ok" : "failed")
            {
                Lifecycle = result,
                Alive = AliveLabels()
            });
            return result;
        }

        private void Census(string id)
        {
            var offset = Offset();
            _tracker.Observe(environment.Processes.Snapshot(), offset);
            _steps.Add(new OwnershipStepRecord(id, "census", offset, 0, "observed") { Alive = AliveLabels() });
        }

        // Snapshots every PollInterval until the condition holds or the observation window ends.
        private async Task WindowAsync(string id, Func<bool> settled, CancellationToken cancellationToken)
        {
            var offset = Offset();
            var window = (long)options.ObserveWindow.TotalMilliseconds;
            string result;
            while (true)
            {
                _tracker.Observe(environment.Processes.Snapshot(), Offset());
                if (settled())
                {
                    result = "settled";
                    break;
                }

                if (Offset() - offset >= window)
                {
                    result = "window-elapsed";
                    break;
                }

                await environment.Pause.DelayAsync(PollInterval, cancellationToken).ConfigureAwait(false);
            }

            _steps.Add(new OwnershipStepRecord(id, "window", offset, Offset() - offset, result) { Alive = AliveLabels() });
        }

        private bool None(params string[] roles) => roles.All(role => _tracker.Alive(role).Count == 0);

        private IReadOnlyList<string> AliveLabels() =>
            [.. _tracker.Processes.Where(static process => process.Alive && process.Role != ProcessRoles.SpatialAnalyzer).Select(static process => process.Label)];

        private void Stop(string reason) => _stopReason ??= reason;

        private long Offset() => (long)(environment.Clock.GetUtcNow() - _started).TotalMilliseconds;
    }
}
