using Api = global::Briosa;

namespace Briosa.LicensedProbes;

/// <summary>
/// Runs one SDK-ownership probe (#252, #402) as a fixed script. The run binds, once, the
/// endpoint's server process, the worker that server starts for it, and the SDK generation it
/// starts; it acts only on those. A process it may terminate is held through a handle opened
/// when it was bound, so neither a check nor a termination can reach a replacement. When
/// ownership cannot be proven, the bound process has gone, or the generation has changed, the
/// run records that and stops without acting. Every call is bounded, every MP step is sent at
/// most once and never replayed, and the process list is only read. A failed or unexpected step
/// ends the script's own steps, but cleanup of the bound generation still runs, except after a
/// license prompt, when nothing more is sent. Cancellation stops dispatching, records whether
/// the interrupted step had been sent, and cleans up on its own bounded token.
/// </summary>
internal static class OwnershipRunner
{
    public const string PreconditionStop = "ownership-precondition-not-met";
    public const string LifecycleStop = "lifecycle-call-failed";
    public const string TerminationStop = "termination-failed";
    public const string IdentificationStop = "process-not-bound";
    public const string BoundProcessGoneStop = "bound-process-not-running";
    public const string GenerationStop = "generation-changed-not-stopped";
    public const string SecondWorkerStop = "second-worker-not-admitted";
    public const string CancelledStop = "cancelled-by-operator";

    public static TimeSpan PollInterval { get; } = TimeSpan.FromMilliseconds(500);

    // How long worker-crash waits for the server to report the lost worker.
    public static TimeSpan FaultedWait { get; } = TimeSpan.FromSeconds(30);

    public static async Task<OwnershipRecord> RunAsync(OwnershipOptions options, OwnershipEnvironment environment, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(environment);
        var run = new Run(options, environment);
        try
        {
            await run.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            return run.ToRecord();
        }
        finally
        {
            run.ReleaseBindings();
        }
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

    /// <summary>The independent budget for cleanup after a cancellation.</summary>
    internal static TimeSpan CleanupBudget(OwnershipOptions options) => options.StepTimeout + options.StepTimeout + TimeSpan.FromSeconds(10);

    private sealed class Run(OwnershipOptions options, OwnershipEnvironment environment)
    {
        private readonly ProcessTracker _tracker = new(environment.HarnessProcessId);
        private readonly List<OwnershipStepRecord> _steps = [];
        private readonly DateTimeOffset _started = environment.Clock.GetUtcNow();
        private string? _stopReason;
        private ServerDescription? _server;
        private bool _mpAllowed = true;
        private bool _licenseAbort;

        // What this run is bound to.
        private IBoundProcess? _serverBinding;
        private string? _serverLabel;
        private int? _serverId;
        private IBoundProcess? _workerBinding;
        private string? _workerLabel;
        private int? _workerId;
        private int? _generation;

        // The step in progress, and whether it has sent anything.
        private string? _current;
        private string _currentAction = "none";
        private bool _currentSent;

        public OwnershipRecord ToRecord() => new(options.Scenario, _started, environment.Clock.GetUtcNow(), _stopReason, _server,
            [.. _steps], _tracker.Processes, options.ObserveWindow, options.HarnessRevision)
        {
            BoundServer = _serverLabel,
            BoundWorker = _workerLabel,
            BoundGeneration = _generation,
            CompleteSnapshots = _tracker.CompleteSnapshots,
            IncompleteSnapshots = _tracker.IncompleteSnapshots,
            FailedSnapshots = _tracker.FailedSnapshots,
            FinalSnapshot = _tracker.LastStatus
        };

        public void ReleaseBindings()
        {
            _workerBinding?.Dispose();
            _serverBinding?.Dispose();
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                await ScriptAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Nothing more is dispatched on the cancelled token. The record says what was
                // interrupted and whether it had been sent; cleanup runs on its own budget.
                _steps.Add(new OwnershipStepRecord("cancelled", _currentAction, Offset(), 0,
                    _current is null ? "between-steps" : _currentSent ? "sent-outcome-unknown" : "interrupted-before-send")
                {
                    Target = _current
                });
                _current = null;

                // The cancellation is what ended the run, whatever had failed before it.
                _stopReason = CancelledStop;
                if (environment.Server is { } server && !_licenseAbort)
                {
                    using var budget = new CancellationTokenSource(CleanupBudget(options));
                    try
                    {
                        await CleanupAsync(server, budget.Token, windows: false).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (budget.IsCancellationRequested)
                    {
                        _steps.Add(new OwnershipStepRecord("cleanup-budget-elapsed", "cleanup", Offset(), 0, "interrupted") { Target = _current });
                    }
                }
            }

            // Only snapshots are taken here, so the end state is always recorded.
            Census("census-end");
        }

        private async Task ScriptAsync(CancellationToken cancellationToken)
        {
            Census("census-start");
            if (options.Scenario == OwnershipScenario.Observe)
            {
                await WindowAsync("window-observe", () => _tracker.NoneConfirmed(ProcessRoles.Sdk, ProcessRoles.Worker, ProcessRoles.Server),
                    cancellationToken).ConfigureAwait(false);
                return;
            }

            var server = environment.Server ?? throw new InvalidOperationException("An ownership probe needs the server client.");
            if (!await PreconditionsAsync(server, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            var started = await LifecycleAsync("start-sdk", "start-sdk", server.StartSdkAsync, cancellationToken).ConfigureAwait(false);
            Census("census-started");

            // The generation this run's start created, even when the start then failed.
            _generation = started.Generation;
            if (!started.Ok || started.Generation is null)
            {
                Stop(LifecycleStop);
                await CleanupAsync(server, cancellationToken).ConfigureAwait(false);
                return;
            }

            AttributeSdk();
            if (!BindWorker())
            {
                Stop(IdentificationStop);
                await CleanupAsync(server, cancellationToken).ConfigureAwait(false);
                return;
            }

            var generation = started.Generation.Value;
            var connected = await LifecycleAsync("connect", "connect", token => server.ConnectAsync(generation, token), cancellationToken)
                .ConfigureAwait(false);
            Census("census-connected");
            if (!connected.Ok || !connected.ReadyForMp)
            {
                Stop(LifecycleStop);
                await CleanupAsync(server, cancellationToken).ConfigureAwait(false);
                return;
            }

            DescendantCensus("census-worker-descendants");
            switch (options.Scenario)
            {
                case OwnershipScenario.Release:
                    await StopBoundGenerationAsync(server, "stop-sdk", cancellationToken).ConfigureAwait(false);
                    await WindowAsync("window-after-stop", () => _tracker.NoneConfirmed(ProcessRoles.Sdk, ProcessRoles.Worker), cancellationToken)
                        .ConfigureAwait(false);
                    break;
                case OwnershipScenario.WorkerCrash:
                    await WorkerCrashAsync(server, cancellationToken).ConfigureAwait(false);
                    break;
                case OwnershipScenario.ParentDeath:
                    await ParentDeathAsync(server, cancellationToken).ConfigureAwait(false);
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
            // A complete census with one SA, one server and no worker or SDK process: everything
            // that appears later is this run's.
            var servers = _tracker.Alive(ProcessRoles.Server);
            var refusal =
                _tracker.LastStatus != SnapshotStatus.Complete ? "process-census-incomplete" :
                _tracker.Alive(ProcessRoles.SpatialAnalyzer).Count != 1 ? "exactly-one-spatial-analyzer-required" :
                servers.Count != 1 ? "exactly-one-briosa-server-required" :
                _tracker.Alive(ProcessRoles.Worker).Count != 0 || _tracker.Alive(ProcessRoles.Sdk).Count != 0 ? "residual-worker-or-sdk-process" :
                null;
            refusal ??= BindServer(servers[0]);
            if (refusal is null)
            {
                var offset = Begin("describe", "describe");
                _server = await server.DescribeAsync(cancellationToken).ConfigureAwait(false);
                refusal = !_server.Reachable ? "server-discovery-unavailable" : !_server.TargetMatches ? "server-target-identity-mismatch" :
                    // The client deadline must outlast the server's own bound, or a slow start or
                    // connect would end on the client side with its outcome unknown.
                    _server.LifecycleBoundsMilliseconds
                        .Where(static bound => bound.Key is "start_sdk_milliseconds" or "connect_spatial_analyzer_milliseconds" or "stop_sdk_milliseconds")
                        .Any(bound => bound.Value >= (long)options.StepTimeout.TotalMilliseconds) ? "step-timeout-below-published-bound" : null;
                Add(new OwnershipStepRecord("describe", "describe", offset, Offset() - offset, refusal ?? "observed"));
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

            Add(new OwnershipStepRecord("preconditions", "refuse", Offset(), 0, refusal));
            Stop(PreconditionStop);
            return false;
        }

        // The server must be in the operator's session, own the endpoint's listener, and open
        // with the name and start time the census saw. Returns a refusal, or null once bound.
        private string? BindServer(TrackedProcess server)
        {
            var offset = Offset();
            string? refusal;
            if (_tracker.Identity(server.Label) is not { } identity)
            {
                refusal = "server-identity-unproven";
            }
            else if (identity.SessionId != environment.Control.HarnessSessionId)
            {
                refusal = "server-not-in-operator-session";
            }
            else
            {
                var listeners = environment.Control.EndpointListeners(options.Address);
                refusal = listeners is null ? "endpoint-owner-unreadable" :
                    listeners.Count == 0 || listeners.Distinct().Count() != 1 || listeners[0] != identity.Id ? "endpoint-owner-not-the-server" :
                    null;
                if (refusal is null)
                {
                    _serverBinding = environment.Control.Bind(identity.Id, identity.StartedAt, ProcessCensus.BriosaServer);
                    refusal = _serverBinding is null ? "server-identity-unproven" : null;
                }

                if (refusal is null)
                {
                    (_serverLabel, _serverId) = (server.Label, identity.Id);
                    _tracker.Attribute(server.Label, "bound-server");
                }
            }

            Add(new OwnershipStepRecord("bind-server", "bind", offset, Offset() - offset, refusal ?? "bound") { Target = server.Label });
            return refusal;
        }

        // The worker must be new in this run, a child of the bound server, in the operator's
        // session, and open with the name and start time the census saw.
        private bool BindWorker()
        {
            var offset = Offset();
            var candidates = _tracker.Processes
                .Where(process => process is { Alive: true, PresentAtStart: false, Role: ProcessRoles.Worker } &&
                    _tracker.Identity(process.Label) is { } identity && identity.ParentId == _serverId &&
                    identity.SessionId == environment.Control.HarnessSessionId)
                .ToList();
            string result;
            if (candidates.Count != 1)
            {
                result = candidates.Count == 0 ? "no-worker-of-the-bound-server" : "several-workers-of-the-bound-server";
            }
            else
            {
                var identity = _tracker.Identity(candidates[0].Label)!.Value;
                _workerBinding = environment.Control.Bind(identity.Id, identity.StartedAt, ProcessCensus.BriosaWorker);
                result = _workerBinding is null ? "worker-identity-unproven" : "bound";
                if (_workerBinding is not null)
                {
                    (_workerLabel, _workerId) = (candidates[0].Label, identity.Id);
                    _tracker.Attribute(candidates[0].Label, "child-of-bound-server");
                }
            }

            Add(new OwnershipStepRecord("bind-worker", "bind", offset, Offset() - offset, result)
            {
                Target = candidates.Count == 1 ? candidates[0].Label : null
            });
            return _workerBinding is not null;
        }

        // An SDK process first seen while this run's own start was in flight. Its parent is DCOM's
        // service host, so this is attribution by timing, not proof of ownership.
        private void AttributeSdk()
        {
            foreach (var sdk in _tracker.Processes.Where(static process => process is { Role: ProcessRoles.Sdk, PresentAtStart: false, Attribution: null }))
            {
                _tracker.Attribute(sdk.Label, "appeared-during-start");
            }
        }

        private void DescendantCensus(string id)
        {
            var offset = Offset();
            _tracker.Observe(environment.Processes.Snapshot(), offset);
            var (labels, count) = _workerId is { } worker ? _tracker.Descendants(worker) : ([], 0);
            Add(new OwnershipStepRecord(id, "census", offset, 0, _tracker.LastStatus == SnapshotStatus.Complete ? "observed" : "census-incomplete")
            {
                Target = _workerLabel,
                Alive = labels,
                DescendantCount = count,
                Snapshot = _tracker.LastStatus
            });
        }

        private async Task WorkerCrashAsync(IOwnershipServer server, CancellationToken cancellationToken)
        {
            if (!Terminate("terminate-worker", _workerBinding!, _workerLabel!, cancellationToken))
            {
                await CleanupAsync(server, cancellationToken).ConfigureAwait(false);
                return;
            }

            // The server notices the lost worker on its own; the probe only reads its state.
            var offset = Begin("await-faulted", "get-sdk-state");
            LifecycleResult state;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _currentSent = true;
                state = await server.GetSdkStateAsync(cancellationToken).ConfigureAwait(false);
                if ((state.Ok && string.Equals(state.SdkState, Api.SpatialAnalyzerSdkState.Faulted.ToString(), StringComparison.Ordinal)) ||
                    Offset() - offset >= (long)FaultedWait.TotalMilliseconds)
                {
                    break;
                }

                await environment.Pause.DelayAsync(PollInterval, cancellationToken).ConfigureAwait(false);
                _tracker.Observe(environment.Processes.Snapshot(), Offset());
            }

            Add(new OwnershipStepRecord("await-faulted", "get-sdk-state", offset, Offset() - offset,
                string.Equals(state.SdkState, Api.SpatialAnalyzerSdkState.Faulted.ToString(), StringComparison.Ordinal) ? "faulted" : "not-faulted-in-time")
            {
                Lifecycle = state,
                Alive = AliveLabels()
            });
            await WindowAsync("window-after-crash", () => _tracker.NoneConfirmed(ProcessRoles.Sdk, ProcessRoles.Worker), cancellationToken).ConfigureAwait(false);
            await CleanupAsync(server, cancellationToken).ConfigureAwait(false);
        }

        private async Task ParentDeathAsync(IOwnershipServer server, CancellationToken cancellationToken)
        {
            // The server is gone afterwards, so there is no lifecycle cleanup: only the window.
            if (!Terminate("terminate-server", _serverBinding!, _serverLabel!, cancellationToken))
            {
                await CleanupAsync(server, cancellationToken).ConfigureAwait(false);
                return;
            }

            await WindowAsync("window-after-parent-death", () => _tracker.NoneConfirmed(ProcessRoles.Sdk, ProcessRoles.Worker, ProcessRoles.Server),
                cancellationToken).ConfigureAwait(false);
        }

        // Observes the product's own watchdog, which terminates the worker's whole process tree;
        // the descendant census before the stall records what that tree held.
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

            DescendantCensus("census-descendants-before-stall");
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
            await WindowAsync("window-dialog-open", () => _tracker.NoneConfirmed(ProcessRoles.Sdk), cancellationToken).ConfigureAwait(false);
            await CheckpointAsync("checkpoint-dismiss-dialog", stall, OperatorCheckpoint.After, cancellationToken).ConfigureAwait(false);
            if (_licenseAbort)
            {
                return;
            }

            await WindowAsync("window-dialog-dismissed", () => _tracker.NoneConfirmed(ProcessRoles.Sdk), cancellationToken).ConfigureAwait(false);
            await CleanupAsync(server, cancellationToken).ConfigureAwait(false);
        }

        // Each step needs the one before it: a successful baseline, a worker from the bound server's
        // own package, a ready worker with an exact activated-SDK match, and a successful attach.
        // Otherwise only bounded cleanup runs, and the record says which gate failed.
        private async Task CompetingClientAsync(IOwnershipServer server, CancellationToken cancellationToken)
        {
            var baseline = ReadOnlyStep("mp-before");
            var before = await MpAsync("mp-before", server, baseline, cancellationToken).ConfigureAwait(false);
            if (before is null || !baseline.Acceptable.Accepts(before))
            {
                await CleanupAsync(server, cancellationToken).ConfigureAwait(false);
                return;
            }

            var offset = Offset();
            var packaged = environment.Control.WorkerFromServerPackage(_serverBinding!, options.WorkerPath ?? string.Empty);
            Add(new OwnershipStepRecord("second-worker-package", "check", offset, Offset() - offset,
                packaged switch { true => "server-package", false => "not-server-package", _ => "unverifiable" }));
            var factory = environment.SecondWorker;
            if (packaged != true || factory is null)
            {
                Stop(SecondWorkerStop);
                await CleanupAsync(server, cancellationToken).ConfigureAwait(false);
                return;
            }

            var admitted = false;
            var second = factory();
            await using (second.ConfigureAwait(false))
            {
                var start = await SecondAsync("second-start", second.StartAsync, cancellationToken).ConfigureAwait(false);
                Census("census-second-started");
                if (start is { Result: SecondWorkerResult.Ready, ActivatedSdkMatchesTarget: true })
                {
                    var attach = await SecondAsync("second-connect", second.ConnectAsync, cancellationToken).ConfigureAwait(false);
                    Census("census-second-connected");
                    admitted = string.Equals(attach.Result, SecondWorkerResult.Connected, StringComparison.Ordinal);
                    if (admitted)
                    {
                        await MpAsync("mp-during", server, ReadOnlyStep("mp-during"), cancellationToken).ConfigureAwait(false);
                    }
                }

                if (!admitted)
                {
                    Stop(SecondWorkerStop);
                }

                await SecondAsync("second-stop", second.StopAsync, cancellationToken).ConfigureAwait(false);
            }

            await WindowAsync("window-second-stopped", () => _tracker.LastStatus == SnapshotStatus.Complete &&
                _tracker.Processes.All(static process => !process.Alive || process.Role != ProcessRoles.Worker || process.ParentRole != ProcessRoles.Harness),
                cancellationToken).ConfigureAwait(false);
            if (admitted)
            {
                await MpAsync("mp-after", server, ReadOnlyStep("mp-after"), cancellationToken).ConfigureAwait(false);
            }

            await CleanupAsync(server, cancellationToken).ConfigureAwait(false);
        }

        // Stops the bound generation, and only while the bound server still runs and the server
        // still reports that generation. Nothing is sent after a license prompt.
        private async Task CleanupAsync(IOwnershipServer server, CancellationToken cancellationToken, bool windows = true)
        {
            if (_licenseAbort)
            {
                return;
            }

            if (_generation is null || _serverBinding is null)
            {
                Add(new OwnershipStepRecord("cleanup", "cleanup", Offset(), 0, "no-bound-generation"));
            }
            else if (_serverBinding.Check() != BindingCheck.Running)
            {
                Add(new OwnershipStepRecord("cleanup", "cleanup", Offset(), 0, "bound-server-not-running") { Target = _serverLabel });
            }
            else
            {
                var state = await LifecycleAsync("sdk-state-cleanup", "get-sdk-state", server.GetSdkStateAsync, cancellationToken).ConfigureAwait(false);
                if (state.Ok && state.Generation != _generation)
                {
                    Add(new OwnershipStepRecord("generation-check", "cleanup", Offset(), 0, "generation-changed"));
                    Stop(GenerationStop);
                }
                else if (state.Ok && !string.Equals(state.SdkState, Api.SpatialAnalyzerSdkState.Stopped.ToString(), StringComparison.Ordinal))
                {
                    await StopBoundGenerationAsync(server, "stop-sdk-cleanup", cancellationToken).ConfigureAwait(false);
                }
            }

            if (windows)
            {
                await WindowAsync("window-after-cleanup", () => _tracker.NoneConfirmed(ProcessRoles.Sdk, ProcessRoles.Worker), cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async Task StopBoundGenerationAsync(IOwnershipServer server, string id, CancellationToken cancellationToken)
        {
            var generation = _generation!.Value;
            var stopped = await LifecycleAsync(id, "stop-sdk", token => server.StopSdkAsync(generation, token), cancellationToken).ConfigureAwait(false);
            if (!stopped.Ok)
            {
                Stop(LifecycleStop);
            }
        }

        // Only through the handle held since binding: a bound process that has gone is never
        // replaced by another, and the run stops without acting.
        private bool Terminate(string id, IBoundProcess binding, string label, CancellationToken cancellationToken)
        {
            var offset = Begin(id, "terminate");
            cancellationToken.ThrowIfCancellationRequested();
            _tracker.Observe(environment.Processes.Snapshot(), Offset());
            var check = binding.Check();
            if (check != BindingCheck.Running)
            {
                Add(new OwnershipStepRecord(id, "terminate", offset, Offset() - offset,
                    check == BindingCheck.Exited ? "bound-process-exited" : "bound-process-unverifiable")
                {
                    Target = label,
                    Alive = AliveLabels()
                });
                Stop(BoundProcessGoneStop);
                return false;
            }

            _currentSent = true;
            var result = binding.Terminate();
            _tracker.Observe(environment.Processes.Snapshot(), Offset());
            Add(new OwnershipStepRecord(id, "terminate", offset, Offset() - offset, result) { Target = label, Alive = AliveLabels() });
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
                Add(new OwnershipStepRecord(id, "mp", Offset(), 0, "not-run"));
                return null;
            }

            var offset = Begin(id, "mp");
            cancellationToken.ThrowIfCancellationRequested();
            _currentSent = true;
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
            Add(new OwnershipStepRecord(id, "mp", offset, Offset() - offset, result) { Outcome = outcome, Alive = AliveLabels() });
            return outcome;
        }

        private async Task<SecondWorkerResult> SecondAsync(string id, Func<CancellationToken, Task<SecondWorkerResult>> call, CancellationToken cancellationToken)
        {
            var offset = Begin(id, "second-worker");
            cancellationToken.ThrowIfCancellationRequested();
            _currentSent = true;
            var result = await call(cancellationToken).ConfigureAwait(false);
            _tracker.Observe(environment.Processes.Snapshot(), Offset());
            Add(new OwnershipStepRecord(id, "second-worker", offset, Offset() - offset, result.Result)
            {
                SecondWorker = result,
                Alive = AliveLabels()
            });
            return result;
        }

        private async Task<bool> CheckpointAsync(string id, ProbeStep step, OperatorCheckpoint timing, CancellationToken cancellationToken)
        {
            var offset = Begin(id, "checkpoint");
            var answer = await environment.Console.CheckpointAsync(step, timing, cancellationToken).ConfigureAwait(false);
            if (answer is null)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            var license = OperatorObservationText.IsLicensePromptAbort(answer);
            Add(new OwnershipStepRecord(id, "checkpoint", offset, Offset() - offset, answer is null ? "not-answered" : license ? "license-prompt" : "answered")
            {
                OperatorAnswer = answer
            });
            if (license)
            {
                // The prompt is never copied or dismissed by the run, and nothing more is sent.
                _licenseAbort = true;
                Stop(ProbeSession.LicensePromptStop);
                return false;
            }

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
            var offset = Begin(id, action);
            cancellationToken.ThrowIfCancellationRequested();
            _currentSent = true;
            var result = await call(cancellationToken).ConfigureAwait(false);
            _tracker.Observe(environment.Processes.Snapshot(), Offset());
            Add(new OwnershipStepRecord(id, action, offset, Offset() - offset, result.Ok ? "ok" : result.Cancelled ? "sent-outcome-unknown" : "failed")
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
            Add(new OwnershipStepRecord(id, "census", offset, 0, _tracker.LastStatus == SnapshotStatus.Complete ? "observed" : "census-incomplete")
            {
                Alive = AliveLabels(),
                Snapshot = _tracker.LastStatus
            });
        }

        // Snapshots every PollInterval until the condition holds on a complete snapshot, or the
        // observation window ends.
        private async Task WindowAsync(string id, Func<bool> settled, CancellationToken cancellationToken)
        {
            var offset = Begin(id, "window");
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

            Add(new OwnershipStepRecord(id, "window", offset, Offset() - offset, result) { Alive = AliveLabels(), Snapshot = _tracker.LastStatus });
        }

        private long Begin(string id, string action)
        {
            (_current, _currentAction, _currentSent) = (id, action, false);
            return Offset();
        }

        private void Add(OwnershipStepRecord step)
        {
            _steps.Add(step);
            if (string.Equals(step.Id, _current, StringComparison.Ordinal))
            {
                _current = null;
            }
        }

        private IReadOnlyList<string> AliveLabels() =>
            [.. _tracker.Processes.Where(static process => process.Alive && process.Role != ProcessRoles.SpatialAnalyzer).Select(static process => process.Label)];

        private void Stop(string reason) => _stopReason ??= reason;

        private long Offset() => (long)(environment.Clock.GetUtcNow() - _started).TotalMilliseconds;
    }
}
