using System.Collections.Concurrent;
using Briosa.Server.Operations;
using Briosa.Server.Operations.FileOperations;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Briosa.Server.Tests;

/// <summary>
/// Abandonment of cancelled queued work (F9) and detachment of accepted lifecycle
/// exchanges from caller cancellation (F10), issue #305. In-process workers hold each
/// exchange until the test releases it, and every supervisor bound runs on the virtual
/// clock, so each race is ordered by handshakes rather than by real time.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Reliability",
    "CA2007:Consider calling ConfigureAwait on the awaited task",
    Justification = "xUnit tests intentionally retain their test synchronization behavior.")]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Reliability",
    "CA2000:Dispose objects before losing scope",
    Justification = "Scripted workers own no resources; the supervisor disposes each worker it starts.")]
public sealed class CancellationDetachmentTests
{
    private static readonly string[] FirstAndNext = ["first", "next"];
    private static readonly string[] RacingOnly = ["racing"];
    private static readonly string[] FirstOnly = ["first"];
    private static readonly string[] UnavailableBeforeDispatch =
        ["worker-not-ready", "worker-supervisor-stopping", "worker-execution-queue-closed"];

    // Distinct bounds, so a scheduled timer identifies the bound the supervisor armed.
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(17);
    private static readonly TimeSpan HeartbeatTimeout = TimeSpan.FromSeconds(19);
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(6);
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan ReadinessProbeTimeout = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan ExecutionBudget = TimeSpan.FromSeconds(3);

    // Only a hang guard: handshakes and virtual time order every step.
    private static readonly TimeSpan HangGuard = TimeSpan.FromSeconds(30);

    // F9: each queued item leaves Queued exactly once, by claim or by abandonment.
    [Fact]
    public void ClaimAndAbandonmentAreMutuallyExclusive()
    {
        var claimed = WorkItem();
        Assert.True(claimed.TryClaim());
        Assert.True(claimed.TryClaim());
        Assert.False(claimed.TryAbandon());
        Assert.False(claimed.IsAbandoned);

        var abandoned = WorkItem();
        Assert.True(abandoned.TryAbandon());
        Assert.False(abandoned.TryAbandon());
        Assert.False(abandoned.TryClaim());
        Assert.True(abandoned.IsAbandoned);
    }

    // F10: the caller's withdrawal and the supervisor's acceptance are decided once.
    [Fact]
    public async Task AcceptanceAndWithdrawalAreMutuallyExclusive()
    {
        using var cancelledFirst = new CancellationTokenSource();
        await cancelledFirst.CancelAsync();
        var withdrawn = new LifecycleAcceptance(cancelledFirst.Token);
        Assert.Throws<OperationCanceledException>(withdrawn.Accept);
        Assert.False(withdrawn.IsAccepted);
        Assert.True(withdrawn.TryWithdraw());

        using var cancelledLater = new CancellationTokenSource();
        var accepted = new LifecycleAcceptance(cancelledLater.Token);
        accepted.Accept();
        await cancelledLater.CancelAsync();
        accepted.Accept();
        Assert.True(accepted.IsAccepted);
        Assert.False(accepted.TryWithdraw());
    }

    [Theory]
    [InlineData("withdrawn")]
    [InlineData("accepted")]
    [InlineData("completed")]
    public async Task CallerWaitEndsByWithdrawalDetachmentOrTheExchangeResult(string race)
    {
        using var cancellation = new CancellationTokenSource();
        var acceptance = new LifecycleAcceptance(cancellation.Token);
        var exchange = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (race == "accepted") acceptance.Accept();
        if (race == "completed") exchange.SetResult("result");

        var waiting = acceptance.WaitAsync(exchange.Task, () => "stopped-waiting");
        await cancellation.CancelAsync();

        try
        {
            if (race == "withdrawn")
            {
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting.WaitAsync(HangGuard));
                // The exchange can no longer commit once the caller has withdrawn.
                Assert.Throws<OperationCanceledException>(acceptance.Accept);
            }
            else
            {
                Assert.Equal(race == "accepted" ? "stopped-waiting" : "result", await waiting.WaitAsync(HangGuard));
            }
        }
        finally
        {
            exchange.TrySetResult("late");
        }
    }

    // F9: cancel while queued. The caller wins abandonment, receives typed NotStarted
    // evidence, and the item never reaches the worker.
    [Fact]
    public async Task CancellationWhileQueuedAbandonsTheItemWithoutSendingIt()
    {
        var active = new Hold();
        var worker = new ScriptedWorker { ExecuteHold = id => id == "first" ? active : null };
        var (supervisor, _, _) = Create(worker);
        await using var scope = supervisor.ConfigureAwait(false);
        Assert.True((await supervisor.StartAsync()).Succeeded);

        var first = supervisor.ExecuteAsync(Plain("first"));
        await active.Entered.WaitAsync(HangGuard);
        using var cancellation = new CancellationTokenSource();
        var queued = supervisor.ExecuteAsync(Plain("queued"), cancellation.Token);
        await cancellation.CancelAsync();
        var abandoned = await queued.WaitAsync(HangGuard);

        Assert.Equal(WorkerExecutionStatus.ClientCancelled, abandoned.Status);
        Assert.Equal(WorkerExecutionDisposition.NotStarted, abandoned.ExecutionDisposition);
        Assert.Equal("queued-request-abandoned", abandoned.DiagnosticCode);
        Assert.Null(abandoned.Execution);
        Assert.Equal(1, abandoned.Generation);
        Assert.Equal(1, supervisor.ExecutionSnapshot.AbandonedRequests);

        active.Release();
        Assert.Equal(WorkerExecutionStatus.Completed, (await first.WaitAsync(HangGuard)).Status);
        // The worker pipe stays synchronized and only the active command reached it.
        Assert.Equal(WorkerExecutionStatus.Completed, (await supervisor.ExecuteAsync(Plain("next"))).Status);
        Assert.Equal(FirstAndNext, worker.ExecutedOperations);
        // The consumer releases a request's retained bytes just after resolving it.
        await WaitUntil(() => supervisor.ExecutionSnapshot.ReservedWorkBytes == 0);
        var drained = supervisor.ExecutionSnapshot;
        Assert.Equal(3, drained.AdmittedRequests);
        Assert.Equal(3, drained.TerminalRequests);
        Assert.Equal(0, drained.QueuedRequests);
        Assert.Equal(0, drained.ReservedWorkBytes);
        Assert.Equal(1, drained.ClientCancellationsAfterAdmission);
        Assert.True(supervisor.Current.ReadyForExecution);
    }

    // F9 through the public seam: the abandoned call fails with a typed Cancelled
    // trailer that proves NotStarted, and both the RPC and the queue audit it once.
    [Fact]
    public async Task AbandonedPublicCallIsTypedNotStartedAndAuditedOnce()
    {
        var active = new Hold();
        var worker = new ScriptedWorker { ExecuteHold = id => id == "first" ? active : null };
        var supervisorLog = new CapturingLogger<WorkerProcessSupervisor>();
        var (supervisor, _, _) = Create(worker, supervisorLogger: supervisorLog);
        await using var scope = supervisor.ConfigureAwait(false);
        Assert.True((await supervisor.StartAsync()).Succeeded);
        var auditLog = new CapturingLogger<OperationAuditLogger>();
        var executor = new OperationExecutor(supervisor, new OperationAuditLogger(auditLog), TimeProvider.System);

        var first = supervisor.ExecuteAsync(Plain("first"));
        await active.Entered.WaitAsync(HangGuard);
        using var cancellation = new CancellationTokenSource();
        var correlation = Guid.NewGuid();
        var call = executor.ExecuteAsync<object, object>(new object(), GetWorkingDirectoryOperation.Descriptor,
            _ => new WorkerMpCommand(GetWorkingDirectoryOperation.OperationId, "Scripted Step", [], []),
            [], _ => new object(), cancellation.Token, correlationId: correlation);
        await WaitUntil(() => supervisor.ExecutionSnapshot.QueuedRequests == 1);
        await cancellation.CancelAsync();
        var error = await Assert.ThrowsAsync<RpcException>(() => call.WaitAsync(HangGuard));
        active.Release();
        await first.WaitAsync(HangGuard);

        Assert.Equal(StatusCode.Cancelled, error.StatusCode);
        var detail = global::Briosa.OperationError.Parser.ParseFrom(Assert.Single(error.Trailers).ValueBytes);
        Assert.Equal(global::Briosa.OperationFailureKind.CallerCancelled, detail.Kind);
        Assert.Equal(global::Briosa.ExecutionDisposition.NotStarted, detail.ExecutionDisposition);
        Assert.Equal(global::Briosa.ReplayGuidance.MayReplay, detail.ReplayGuidance);
        Assert.Equal("queued-request-abandoned", detail.DiagnosticCode);
        Assert.DoesNotContain(GetWorkingDirectoryOperation.OperationId, worker.ExecutedOperations);
        var resolved = Assert.Single(supervisorLog.Entries, entry =>
            entry.EventId.Id == 1301 && Equals(entry.Value("CorrelationId"), correlation));
        Assert.Equal("not_started", resolved.Value("ExecutionDisposition"));
        Assert.Equal("queued-request-abandoned", resolved.Value("DiagnosticCode"));
        Assert.DoesNotContain(supervisorLog.Entries, entry =>
            entry.EventId.Id == 1300 && Equals(entry.Value("CorrelationId"), correlation));
        var failed = Assert.Single(auditLog.Entries, entry => entry.EventId.Id == 2005);
        Assert.Equal(correlation, failed.Value("CorrelationId"));
        Assert.Equal("not_started", failed.Value("ExecutionDisposition"));
        Assert.Equal(StatusCode.Cancelled, failed.Value("GrpcStatus"));
        Assert.Equal("queued-request-abandoned", failed.Value("DiagnosticCode"));
    }

    // F9: once dispatched, the exchange drains under its execution budget with its own
    // typed outcome, while the caller that stopped waiting receives StartedOutcomeUnknown.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellationAfterDispatchDrainsUnderTheExecutionBudget(bool exceedBudget)
    {
        var active = new Hold();
        var worker = new ScriptedWorker { ExecuteHold = id => id == "dispatched" ? active : null };
        var (supervisor, clock, _) = Create(worker);
        await using var scope = supervisor.ConfigureAwait(false);
        Assert.True((await supervisor.StartAsync()).Succeeded);

        using var cancellation = new CancellationTokenSource();
        var correlation = Guid.NewGuid();
        var dispatched = supervisor.ExecuteAsync(new WorkerCommandSubmission("dispatched", () => Plain("dispatched")),
            correlation, cancellation.Token);
        await active.Entered.WaitAsync(HangGuard);
        await cancellation.CancelAsync();
        var cancelled = await dispatched.WaitAsync(HangGuard);

        Assert.Equal(WorkerExecutionStatus.ClientCancelled, cancelled.Status);
        Assert.Equal(WorkerExecutionDisposition.StartedOutcomeUnknown, cancelled.ExecutionDisposition);
        Assert.Equal("client-wait-cancelled", cancelled.DiagnosticCode);
        Assert.Equal(0, supervisor.ExecutionSnapshot.AbandonedRequests);
        Assert.Equal(0, supervisor.ExecutionSnapshot.TerminalRequests);
        Assert.Equal(1, supervisor.ExecutionSnapshot.ActiveExecutions);

        if (exceedBudget)
        {
            await clock.FireNextAsync(ExecutionBudget);
            await WaitUntil(() => supervisor.ExecutionSnapshot.TerminalRequests == 1);
            Assert.Equal(1, supervisor.ExecutionSnapshot.WatchdogTimeouts);
            Assert.Equal(WorkerLifecycleState.Degraded, supervisor.Current.State);
            Assert.Equal(WorkerIncidentKind.WatchdogTerminated, supervisor.Current.LastIncident!.Kind);
            Assert.Equal(WorkerExecutionDisposition.StartedOutcomeUnknown,
                supervisor.Current.LastIncident.ExecutionDisposition);
            Assert.True(worker.HasExited);
        }
        else
        {
            active.Release();
            await WaitUntil(() => supervisor.ExecutionSnapshot.TerminalRequests == 1);
            Assert.True(supervisor.Current.ReadyForExecution);
            Assert.Equal(WorkerExecutionStatus.Completed, (await supervisor.ExecuteAsync(Plain("next"))).Status);
        }

        string[] executed = exceedBudget ? ["dispatched"] : ["dispatched", "next"];
        Assert.Equal(executed, worker.ExecutedOperations);
    }

    // F9: abandonment and the consumer's claim race for one queued item. Each order
    // has exactly one outcome: abandoned and never sent, or claimed and drained.
    [Theory]
    [InlineData("abandon-first")]
    [InlineData("claim-first")]
    [InlineData("concurrent")]
    public async Task CancellationRacingTheClaimHasExactlyOneOutcome(string order)
    {
        var iterations = order == "concurrent" ? 40 : 1;
        var abandonedCount = 0;
        for (var iteration = 0; iteration < iterations; iteration++)
        {
            // A held heartbeat owns the gate, so the dequeued item waits before its claim.
            // A held execution keeps a dispatched item from completing before the cancel.
            var ping = new Hold();
            var execution = new Hold();
            var worker = new ScriptedWorker
            {
                PingHold = count => count == 1 ? ping : null,
                ExecuteHold = id => id == "racing" ? execution : null
            };
            var (supervisor, clock, _) = Create(worker);
            await using var scope = supervisor.ConfigureAwait(false);
            Assert.True((await supervisor.StartAsync()).Succeeded);
            await clock.FireNextAsync(HeartbeatInterval);
            await ping.Entered.WaitAsync(HangGuard);

            using var cancellation = new CancellationTokenSource();
            var racing = supervisor.ExecuteAsync(Plain("racing"), cancellation.Token);
            await WaitUntil(() => supervisor.ExecutionSnapshot.ActiveExecutions == 1);
            switch (order)
            {
                case "abandon-first":
                    await cancellation.CancelAsync();
                    _ = await racing.WaitAsync(HangGuard);
                    ping.Release();
                    break;
                case "claim-first":
                    ping.Release();
                    await execution.Entered.WaitAsync(HangGuard);
                    await cancellation.CancelAsync();
                    break;
                default:
                    await Task.WhenAll(Task.Run(cancellation.CancelAsync), Task.Run(ping.Release));
                    break;
            }

            var outcome = await racing.WaitAsync(HangGuard);
            execution.Release();
            await WaitUntil(() => supervisor.ExecutionSnapshot.TerminalRequests == 1 &&
                supervisor.ExecutionSnapshot.ActiveExecutions == 0);
            var snapshot = supervisor.ExecutionSnapshot;
            Assert.Equal(WorkerExecutionStatus.ClientCancelled, outcome.Status);
            if (outcome.ExecutionDisposition == WorkerExecutionDisposition.NotStarted)
            {
                abandonedCount++;
                Assert.Equal("queued-request-abandoned", outcome.DiagnosticCode);
                Assert.Equal(1, snapshot.AbandonedRequests);
                Assert.Empty(worker.ExecutedOperations);
            }
            else
            {
                Assert.Equal(WorkerExecutionDisposition.StartedOutcomeUnknown, outcome.ExecutionDisposition);
                Assert.Equal(0, snapshot.AbandonedRequests);
                Assert.Equal(RacingOnly, worker.ExecutedOperations);
            }

            Assert.Equal(1, snapshot.AdmittedRequests);
            Assert.Equal(1, snapshot.TerminalRequests);
            Assert.Equal(WorkerExecutionStatus.Completed, (await supervisor.ExecuteAsync(Plain("next"))).Status);
        }

        if (order == "abandon-first") Assert.Equal(1, abandonedCount);
        if (order == "claim-first") Assert.Equal(0, abandonedCount);
    }

    // F9: Stop races the abandonment of two admitted items: one already waiting for the
    // gate, one still in the channel. Exactly one party resolves each, NotStarted, and
    // neither reaches the worker. A held heartbeat owns the gate first, and Stop queues
    // for it before the items are admitted, so Stop closes admission ahead of them.
    [Theory]
    [InlineData("abandon-first")]
    [InlineData("close-first")]
    [InlineData("concurrent")]
    public async Task ShutdownRacingAbandonmentResolvesEachQueuedItemOnce(string order)
    {
        var iterations = order == "concurrent" ? 40 : 1;
        for (var iteration = 0; iteration < iterations; iteration++)
        {
            var ping = new Hold();
            var worker = new ScriptedWorker { PingHold = count => count == 1 ? ping : null };
            var (supervisor, clock, _) = Create(worker);
            await using var scope = supervisor.ConfigureAwait(false);
            Assert.True((await supervisor.StartAsync()).Succeeded);
            await clock.FireNextAsync(HeartbeatInterval);
            await ping.Entered.WaitAsync(HangGuard);
            // Stop runs synchronously up to its wait for the gate, so it waits first.
            var stopping = supervisor.StopAsync();
            Assert.False(stopping.IsCompleted);
            using var cancellation = new CancellationTokenSource();
            var waiting = supervisor.ExecuteAsync(Plain("waiting"), cancellation.Token);
            await WaitUntil(() => supervisor.ExecutionSnapshot.ActiveExecutions == 1);
            var channeled = supervisor.ExecuteAsync(Plain("channeled"), cancellation.Token);
            await WaitUntil(() => supervisor.ExecutionSnapshot.QueuedRequests == 1);

            switch (order)
            {
                case "abandon-first":
                    await cancellation.CancelAsync();
                    _ = await Task.WhenAll(waiting, channeled).WaitAsync(HangGuard);
                    ping.Release();
                    break;
                case "close-first":
                    ping.Release();
                    _ = await Task.WhenAll(waiting, channeled).WaitAsync(HangGuard);
                    await cancellation.CancelAsync();
                    break;
                default:
                    await Task.WhenAll(Task.Run(cancellation.CancelAsync), Task.Run(ping.Release));
                    break;
            }

            var stopped = await stopping.WaitAsync(HangGuard);
            var outcomes = await Task.WhenAll(waiting, channeled).WaitAsync(HangGuard);
            var snapshot = supervisor.ExecutionSnapshot;
            var abandoned = 0;
            foreach (var outcome in outcomes)
            {
                if (outcome.ExecutionDisposition == WorkerExecutionDisposition.NotStarted &&
                    outcome.Status == WorkerExecutionStatus.ClientCancelled)
                {
                    abandoned++;
                    Assert.Equal("queued-request-abandoned", outcome.DiagnosticCode);
                }
                else if (outcome.Status == WorkerExecutionStatus.ClientCancelled)
                {
                    // The caller lost the claim and cannot prove NotStarted, although the
                    // consumer resolved the item before dispatch (asserted below).
                    Assert.Equal("concurrent", order);
                    Assert.Equal(WorkerExecutionDisposition.StartedOutcomeUnknown, outcome.ExecutionDisposition);
                    Assert.Equal("client-wait-cancelled", outcome.DiagnosticCode);
                }
                else
                {
                    Assert.Equal(WorkerExecutionStatus.Unavailable, outcome.Status);
                    Assert.Equal(WorkerExecutionDisposition.NotStarted, outcome.ExecutionDisposition);
                    Assert.Contains(outcome.DiagnosticCode, UnavailableBeforeDispatch);
                }
            }

            if (order == "abandon-first") Assert.Equal(2, abandoned);
            if (order == "close-first") Assert.Equal(0, abandoned);
            Assert.Equal(abandoned, snapshot.AbandonedRequests);
            // Whoever resolved each item, neither reached the worker.
            Assert.Empty(worker.ExecutedOperations);
            Assert.Equal(2, snapshot.AdmittedRequests);
            Assert.Equal(2, snapshot.TerminalRequests);
            Assert.Equal(0, snapshot.QueuedRequests);
            Assert.Equal(0, snapshot.ActiveExecutions);
            Assert.Equal(0, snapshot.ReservedWorkBytes);
            Assert.True(stopped.Succeeded);
            Assert.Equal(WorkerTerminationKind.Graceful, stopped.Snapshot.LastTermination);
            Assert.Equal(1, worker.StopCount);
        }
    }

    // F9: when the queue closes while a request is still in its channel, the final
    // drain resolves the request once, unless its caller already abandoned it. A
    // consumer failure closes the queue while the consumer is still busy, so the
    // drain alone can resolve the queued request.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QueueDrainResolvesEachQueuedRequestOnce(bool abandonFirst)
    {
        var active = new Hold();
        var worker = new ScriptedWorker
        {
            ExecuteHold = id => id == "first" ? active : null,
            ExecuteFaults = id => id == "first"
        };
        var (supervisor, _, _) = Create(worker);
        await using var scope = supervisor.ConfigureAwait(false);
        Assert.True((await supervisor.StartAsync()).Succeeded);
        var first = supervisor.ExecuteAsync(Plain("first"));
        await active.Entered.WaitAsync(HangGuard);
        using var cancellation = new CancellationTokenSource();
        var queued = supervisor.ExecuteAsync(Plain("queued"), cancellation.Token);
        await WaitUntil(() => supervisor.ExecutionSnapshot.QueuedRequests == 1);
        if (abandonFirst)
        {
            await cancellation.CancelAsync();
            _ = await queued.WaitAsync(HangGuard);
        }

        active.Release();
        var failed = await first.WaitAsync(HangGuard);
        var outcome = await queued.WaitAsync(HangGuard);
        await cancellation.CancelAsync();
        // An abandoned request leaves the channel only when the drain dequeues it.
        await WaitUntil(() => supervisor.ExecutionSnapshot is { QueuedRequests: 0, ReservedWorkBytes: 0 });
        var snapshot = supervisor.ExecutionSnapshot;

        Assert.Equal(WorkerExecutionStatus.WorkerFailure, failed.Status);
        Assert.Equal(WorkerExecutionDisposition.NotStarted, outcome.ExecutionDisposition);
        Assert.Equal(abandonFirst ? WorkerExecutionStatus.ClientCancelled : WorkerExecutionStatus.Unavailable,
            outcome.Status);
        Assert.Equal(abandonFirst ? "queued-request-abandoned" : "worker-execution-queue-closed",
            outcome.DiagnosticCode);
        Assert.Equal(abandonFirst ? 1 : 0, snapshot.AbandonedRequests);
        Assert.Equal(2, snapshot.AdmittedRequests);
        Assert.Equal(2, snapshot.TerminalRequests);
        Assert.Equal(0, snapshot.QueuedRequests);
        Assert.Equal(0, snapshot.ReservedWorkBytes);
        Assert.Equal(FirstOnly, worker.ExecutedOperations);
        Assert.Equal(WorkerLifecycleState.Degraded, supervisor.Current.State);
    }

    // F10: cancellation before acceptance still prevents the work and changes nothing.
    [Theory]
    [InlineData("start")]
    [InlineData("connect")]
    [InlineData("recover")]
    public async Task CancellationBeforeAcceptancePreventsTheWork(string exchange)
    {
        var first = new ScriptedWorker { ReadyConnected = exchange != "connect", ReadyFaulted = exchange == "recover" };
        var replacement = new ScriptedWorker();
        var (supervisor, _, factory) = Create(first, replacement);
        await using var scope = supervisor.ConfigureAwait(false);
        if (exchange != "start") _ = await supervisor.StartAsync();
        var before = supervisor.Current;
        var terminations = first.TerminationCount;
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => exchange switch
        {
            "start" => supervisor.StartAsync(cancellation.Token),
            "connect" => supervisor.ConnectAsync(before.Generation, cancellation.Token),
            _ => supervisor.RecoverSdkAsync(before.Generation, cancellation.Token)
        });

        Assert.Equal(before, supervisor.Current);
        Assert.Equal(exchange == "start" ? 0 : 1, factory.Starts);
        Assert.Equal(0, first.ConnectCount);
        Assert.Equal(terminations, first.TerminationCount);
    }

    // F10: a caller that withdraws while the request still waits for the supervisor's
    // gate prevents the work; nothing reaches the worker.
    [Fact]
    public async Task CancellationWhileWaitingToBeginPreventsTheConnect()
    {
        var ping = new Hold();
        var worker = new ScriptedWorker { ReadyConnected = false, PingHold = count => count == 1 ? ping : null };
        var (supervisor, clock, _) = Create(worker);
        await using var scope = supervisor.ConfigureAwait(false);
        Assert.True((await supervisor.StartAsync()).Succeeded);
        await clock.FireNextAsync(HeartbeatInterval);
        await ping.Entered.WaitAsync(HangGuard);
        var before = supervisor.Current;
        using var cancellation = new CancellationTokenSource();

        var connecting = supervisor.ConnectAsync(before.Generation, cancellation.Token);
        Assert.False(connecting.IsCompleted);
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => connecting.WaitAsync(HangGuard));
        ping.Release();
        await WaitUntil(() => worker.PingCount == 1);

        Assert.Equal(0, worker.ConnectCount);
        Assert.Equal(before.Generation, supervisor.Current.Generation);
        Assert.Equal(WorkerConnectionState.Disconnected, supervisor.Current.Connection!.State);
        Assert.True((await supervisor.ConnectAsync(before.Generation)).Succeeded);
    }

    // F10: Start. After acceptance the caller stops waiting at once, and worker launch,
    // COM activation, and Ready finish under the startup bound.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellationAfterStartAcceptanceLeavesLaunchToTheStartupBound(bool exceedBound)
    {
        var ready = new Hold();
        var worker = new ScriptedWorker { ReadyHold = ready };
        var (supervisor, clock, _) = Create(worker);
        await using var scope = supervisor.ConfigureAwait(false);
        using var cancellation = new CancellationTokenSource();

        var starting = supervisor.StartAsync(cancellation.Token);
        await ready.Entered.WaitAsync(HangGuard);
        await cancellation.CancelAsync();
        var detached = Assert.IsType<WorkerLifecycleDetached>(await starting.WaitAsync(HangGuard));

        Assert.False(detached.Succeeded);
        Assert.Equal(WorkerLifecycleState.Starting, detached.Snapshot.State);
        Assert.Equal(1, detached.Snapshot.Generation);
        Assert.False(worker.HasExited);
        Assert.Equal(0, worker.TerminationCount);
        if (exceedBound)
        {
            await clock.FireNextAsync(StartupTimeout);
            var timedOut = await WaitFor(supervisor, state => state.State == WorkerLifecycleState.Degraded);
            Assert.Equal("worker-startup-timeout", timedOut.DiagnosticCode);
            Assert.Equal(WorkerLifecycleFailure.StartupTimeout, timedOut.LifecycleFailure);
            Assert.Equal(1, worker.TerminationCount);
        }
        else
        {
            ready.Release();
            var verified = await WaitFor(supervisor, state => state.ReadyForExecution);
            Assert.Equal(1, verified.Generation);
            Assert.Equal(1, worker.VerifyCount);
            Assert.Equal(0, worker.TerminationCount);
        }

        Assert.DoesNotContain(supervisor.History, state => state.DiagnosticCode.Contains("cancelled", StringComparison.Ordinal));
        // The lifecycle gate was held by the detached exchange, never by the caller.
        Assert.True((await supervisor.StopAsync().WaitAsync(HangGuard)).Succeeded);
    }

    // F10: Verify. Caller cancellation during the readiness probe no longer quarantines
    // the target; only the probe's own bound or its result decides readiness.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellationDuringTheReadinessProbeLeavesItToTheProbeBound(bool exceedBound)
    {
        var verify = new Hold();
        var worker = new ScriptedWorker { VerifyHold = verify };
        var (supervisor, clock, _) = Create(worker);
        await using var scope = supervisor.ConfigureAwait(false);
        using var cancellation = new CancellationTokenSource();

        var starting = supervisor.StartAsync(cancellation.Token);
        await verify.Entered.WaitAsync(HangGuard);
        await cancellation.CancelAsync();
        var detached = Assert.IsType<WorkerLifecycleDetached>(await starting.WaitAsync(HangGuard));

        Assert.Equal(WorkerExecutionReadinessState.Verifying, detached.Snapshot.Connection!.ExecutionReadinessState);
        Assert.Equal(WorkerExecutionReadinessState.Verifying, supervisor.Current.Connection!.ExecutionReadinessState);
        if (exceedBound)
        {
            await clock.FireNextAsync(ReadinessProbeTimeout);
            var quarantined = await WaitFor(supervisor, state => state.State == WorkerLifecycleState.Degraded &&
                state.ProcessId is null);
            Assert.Equal("execution-readiness-probe-timeout", quarantined.DiagnosticCode);
            Assert.Equal(WorkerLifecycleFailure.ReadinessTimeout, quarantined.LifecycleFailure);
            Assert.Equal(WorkerExecutionReadinessState.OperatorRecoveryRequired,
                quarantined.Connection!.ExecutionReadinessState);
        }
        else
        {
            verify.Release();
            var ready = await WaitFor(supervisor, state => state.ReadyForExecution);
            Assert.Equal(WorkerExecutionReadinessState.ExecutionReady, ready.Connection!.ExecutionReadinessState);
            Assert.DoesNotContain(supervisor.History, state =>
                state.Connection?.ExecutionReadinessState is WorkerExecutionReadinessState.CompetingClientSuspected or
                    WorkerExecutionReadinessState.OperatorRecoveryRequired);
            Assert.Equal(WorkerExecutionStatus.Completed, (await supervisor.ExecuteAsync(Plain("after-probe"))).Status);
        }

        Assert.DoesNotContain(supervisor.History, state => state.DiagnosticCode.Contains("cancelled", StringComparison.Ordinal));
    }

    // F10: Connect. The accepted ConnectEx exchange runs under the startup bound, and a
    // caller that stopped waiting cannot retire the generation.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellationAfterConnectAcceptanceLeavesConnectExToTheStartupBound(bool exceedBound)
    {
        var connect = new Hold();
        var worker = new ScriptedWorker { ReadyConnected = false, ConnectHold = connect };
        var (supervisor, clock, _) = Create(worker);
        await using var scope = supervisor.ConfigureAwait(false);
        Assert.True((await supervisor.StartAsync()).Succeeded);
        using var cancellation = new CancellationTokenSource();

        var connecting = supervisor.ConnectAsync(1, cancellation.Token);
        await connect.Entered.WaitAsync(HangGuard);
        await cancellation.CancelAsync();
        var detached = Assert.IsType<WorkerLifecycleDetached>(await connecting.WaitAsync(HangGuard));

        Assert.Equal(WorkerConnectionState.Connecting, detached.Snapshot.Connection!.State);
        Assert.False(worker.HasExited);
        if (exceedBound)
        {
            await clock.FireNextAsync(StartupTimeout);
            var retired = await WaitFor(supervisor, state => state.State == WorkerLifecycleState.Degraded);
            Assert.Equal("connect-ex-timeout", retired.DiagnosticCode);
            Assert.Equal(WorkerLifecycleFailure.ConnectionTimeout, retired.LifecycleFailure);
        }
        else
        {
            connect.Release();
            var ready = await WaitFor(supervisor, state => state.ReadyForExecution);
            Assert.Equal(WorkerConnectionState.Connected, ready.Connection!.State);
            Assert.Equal(1, worker.VerifyCount);
            Assert.False(worker.HasExited);
        }

        Assert.Equal(1, worker.ConnectCount);
        Assert.DoesNotContain(supervisor.History, state => state.DiagnosticCode.Contains("cancelled", StringComparison.Ordinal));
    }

    // F10: Recover. Once accepted, cleanup of the faulted generation and launch of its
    // replacement finish under their bounds even though the caller left.
    [Fact]
    public async Task CancellationAfterRecoverAcceptanceLetsTheReplacementFinish()
    {
        var ready = new Hold();
        var faulted = new ScriptedWorker { ReadyFaulted = true };
        var replacement = new ScriptedWorker { ReadyHold = ready };
        var (supervisor, _, factory) = Create(faulted, replacement);
        await using var scope = supervisor.ConfigureAwait(false);
        Assert.False((await supervisor.StartAsync()).Succeeded);
        Assert.Equal(WorkerLifecycleState.Degraded, supervisor.Current.State);
        using var cancellation = new CancellationTokenSource();

        var recovering = supervisor.RecoverSdkAsync(1, cancellation.Token);
        await ready.Entered.WaitAsync(HangGuard);
        await cancellation.CancelAsync();
        var detached = Assert.IsType<WorkerLifecycleDetached>(await recovering.WaitAsync(HangGuard));
        ready.Release();
        var recovered = await WaitFor(supervisor, state => state.ReadyForExecution);

        Assert.Equal(2, detached.Snapshot.Generation);
        Assert.Equal(WorkerLifecycleState.Starting, detached.Snapshot.State);
        Assert.Equal(2, recovered.Generation);
        Assert.Equal(1, recovered.RecoveryCount);
        Assert.Equal(2, factory.Starts);
        Assert.False(replacement.HasExited);
    }

    // F10 and shutdown: Stop waits for a detached exchange, which finishes under its own
    // bound; teardown then reuses the pipe without desynchronizing it.
    [Fact]
    public async Task StopWaitsForADetachedStartThenTearsDownCleanly()
    {
        var ready = new Hold();
        var worker = new ScriptedWorker { ReadyHold = ready };
        var (supervisor, _, _) = Create(worker);
        await using var scope = supervisor.ConfigureAwait(false);
        using var cancellation = new CancellationTokenSource();
        var starting = supervisor.StartAsync(cancellation.Token);
        await ready.Entered.WaitAsync(HangGuard);
        await cancellation.CancelAsync();
        Assert.IsType<WorkerLifecycleDetached>(await starting.WaitAsync(HangGuard));

        var stopping = supervisor.StopAsync();
        Assert.False(stopping.IsCompleted);
        Assert.Equal(0, worker.StopCount);
        ready.Release();
        var stopped = await stopping.WaitAsync(HangGuard);

        Assert.True(stopped.Succeeded);
        Assert.Equal(WorkerTerminationKind.Graceful, stopped.Snapshot.LastTermination);
        Assert.Equal(1, worker.VerifyCount);
        Assert.Equal(1, worker.StopCount);
        Assert.Contains(supervisor.History, state => state.ReadyForExecution);
    }

    // F10 at the coordinator: the caller that stops waiting receives a typed Cancelled
    // detail; the transition keeps the gate, finishes, and records its terminal state.
    [Theory]
    [InlineData("start")]
    [InlineData("connect")]
    [InlineData("recover")]
    public async Task CoordinatorReportsTypedCancellationAndCompletesTheTransition(string exchange)
    {
        var hold = new Hold();
        var first = exchange switch
        {
            "start" => new ScriptedWorker { ReadyHold = hold },
            "connect" => new ScriptedWorker { ReadyConnected = false, ConnectHold = hold },
            _ => new ScriptedWorker { ReadyFaulted = true }
        };
        var replacement = new ScriptedWorker { ReadyConnected = false, ReadyHold = hold };
        var (supervisor, _, _) = Create(first, replacement);
        await using var supervisorScope = supervisor.ConfigureAwait(false);
        var coordinator = new SpatialAnalyzerSdkLifecycleCoordinator(supervisor,
            new SpatialAnalyzerSdkLifecycleStateProjection(supervisor), new RunningApplication(5));
        await using var coordinatorScope = coordinator.ConfigureAwait(false);
        if (exchange == "connect") _ = await coordinator.StartAsync(CancellationToken.None);
        if (exchange == "recover")
            _ = await Assert.ThrowsAsync<SdkLifecycleException>(() => coordinator.StartAsync(CancellationToken.None));
        using var cancellation = new CancellationTokenSource();

        var transition = exchange switch
        {
            "start" => coordinator.StartAsync(cancellation.Token),
            "connect" => coordinator.ConnectAsync(1, reconnect: false, cancellation.Token),
            _ => coordinator.RecoverAsync(1, global::Briosa.SpatialAnalyzerSdkRecoveryMode.ReplaceWithoutReplay,
                cancellation.Token)
        };
        await hold.Entered.WaitAsync(HangGuard);
        await cancellation.CancelAsync();
        var stoppedWaiting = await Assert.ThrowsAsync<SdkLifecycleException>(() => transition.WaitAsync(HangGuard));

        Assert.Equal(StatusCode.Cancelled, stoppedWaiting.StatusCode);
        Assert.Equal(global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.StateConflict, stoppedWaiting.Detail.Kind);
        Assert.Equal("sdk-lifecycle-caller-stopped-waiting", stoppedWaiting.Detail.DiagnosticCode);
        Assert.Equal(global::Briosa.LifecycleRecoveryGuidance.RefreshState, stoppedWaiting.Detail.RecoveryGuidance);
        Assert.Equal(exchange == "connect"
            ? global::Briosa.SpatialAnalyzerSdkState.Connecting
            : global::Briosa.SpatialAnalyzerSdkState.Starting, stoppedWaiting.Detail.State.SdkState);
        // The detached transition still owns the coordinator until it finishes.
        var inProgress = await Assert.ThrowsAsync<SdkLifecycleException>(() =>
            coordinator.StopAsync(stoppedWaiting.Detail.State.SdkGeneration, CancellationToken.None));
        Assert.Equal(StatusCode.Aborted, inProgress.StatusCode);
        Assert.Equal("sdk-lifecycle-transition-in-progress", inProgress.Detail.DiagnosticCode);

        hold.Release();
        var finished = await WaitForCoordinator(coordinator, state =>
            state.SdkState is global::Briosa.SpatialAnalyzerSdkState.Running or global::Briosa.SpatialAnalyzerSdkState.Ready &&
            (exchange != "connect" || state.HasApplicationGeneration));
        if (exchange == "connect")
        {
            // The association belongs to the accepted transition, not to the caller.
            Assert.True(finished.ReadyForMp);
            Assert.Equal(5, finished.ApplicationGeneration);
        }

        var stopped = await StopWhenIdleAsync(coordinator, finished.SdkGeneration);
        Assert.Equal(global::Briosa.SpatialAnalyzerSdkState.Stopped, stopped.SdkState);
    }

    // F10 at the coordinator: a caller that withdraws while its request still waits for
    // the supervisor's gate gets a typed withdrawal, and ConnectEx never starts.
    [Fact]
    public async Task CoordinatorWithdrawalWhileWaitingForTheSupervisorIsTyped()
    {
        var ping = new Hold();
        var worker = new ScriptedWorker { ReadyConnected = false, PingHold = count => count == 1 ? ping : null };
        var (supervisor, clock, _) = Create(worker);
        await using var supervisorScope = supervisor.ConfigureAwait(false);
        var coordinator = new SpatialAnalyzerSdkLifecycleCoordinator(supervisor,
            new SpatialAnalyzerSdkLifecycleStateProjection(supervisor), new RunningApplication(5));
        await using var coordinatorScope = coordinator.ConfigureAwait(false);
        _ = await coordinator.StartAsync(CancellationToken.None);
        await clock.FireNextAsync(HeartbeatInterval);
        await ping.Entered.WaitAsync(HangGuard);
        using var cancellation = new CancellationTokenSource();

        var connecting = coordinator.ConnectAsync(1, reconnect: false, cancellation.Token);
        Assert.False(connecting.IsCompleted);
        await cancellation.CancelAsync();
        var withdrawn = await Assert.ThrowsAsync<SdkLifecycleException>(() => connecting.WaitAsync(HangGuard));
        ping.Release();

        Assert.Equal(StatusCode.Cancelled, withdrawn.StatusCode);
        Assert.Equal("sdk-lifecycle-request-withdrawn", withdrawn.Detail.DiagnosticCode);
        Assert.Equal(global::Briosa.SpatialAnalyzerSdkState.Running, withdrawn.Detail.State.SdkState);
        await WaitUntil(() => worker.PingCount == 1);
        Assert.Equal(0, worker.ConnectCount);
        var connected = await ConnectWhenIdleAsync(coordinator, 1);
        Assert.True(connected.ReadyForMp);
    }

    // F10 at the coordinator: a caller cancelled before acceptance gets a typed
    // withdrawal and the supervisor is never asked to act.
    [Theory]
    [InlineData("start")]
    [InlineData("connect")]
    [InlineData("stop")]
    [InlineData("recover")]
    public async Task CoordinatorReportsTypedWithdrawalBeforeAcceptance(string exchange)
    {
        var worker = new ScriptedWorker { ReadyConnected = false, ReadyFaulted = exchange == "recover" };
        var (supervisor, _, factory) = Create(worker);
        await using var supervisorScope = supervisor.ConfigureAwait(false);
        var coordinator = new SpatialAnalyzerSdkLifecycleCoordinator(supervisor,
            new SpatialAnalyzerSdkLifecycleStateProjection(supervisor), new RunningApplication(5));
        await using var coordinatorScope = coordinator.ConfigureAwait(false);
        if (exchange != "start") _ = await supervisor.StartAsync();
        var before = supervisor.Current;
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var withdrawn = await Assert.ThrowsAsync<SdkLifecycleException>(() => exchange switch
        {
            "start" => coordinator.StartAsync(cancellation.Token),
            "connect" => coordinator.ConnectAsync(1, reconnect: false, cancellation.Token),
            "stop" => coordinator.StopAsync(1, cancellation.Token),
            _ => coordinator.RecoverAsync(1, global::Briosa.SpatialAnalyzerSdkRecoveryMode.ReplaceWithoutReplay,
                cancellation.Token)
        });

        Assert.Equal(StatusCode.Cancelled, withdrawn.StatusCode);
        Assert.Equal("sdk-lifecycle-request-withdrawn", withdrawn.Detail.DiagnosticCode);
        Assert.Equal(global::Briosa.LifecycleRecoveryGuidance.None, withdrawn.Detail.RecoveryGuidance);
        Assert.Equal(before, supervisor.Current);
        Assert.Equal(exchange == "start" ? 0 : 1, factory.Starts);
        Assert.Equal(0, worker.ConnectCount);
        Assert.Equal(0, worker.StopCount);
    }

    // F10 through the gRPC service: the cancelled caller's typed detail is audited even
    // though gRPC does not deliver a response to a caller that stopped waiting.
    [Fact]
    public async Task LifecycleServiceAuditsACallerThatStoppedWaiting()
    {
        var ready = new Hold();
        var worker = new ScriptedWorker { ReadyHold = ready };
        var (supervisor, _, _) = Create(worker);
        await using var supervisorScope = supervisor.ConfigureAwait(false);
        var coordinator = new SpatialAnalyzerSdkLifecycleCoordinator(supervisor,
            new SpatialAnalyzerSdkLifecycleStateProjection(supervisor), new RunningApplication(5));
        await using var coordinatorScope = coordinator.ConfigureAwait(false);
        var audit = new CapturingLogger<LifecycleAuditLogger>();
        var service = new SpatialAnalyzerSdkLifecycleService(coordinator, new LifecycleAuditLogger(audit));
        using var cancellation = new CancellationTokenSource();

        var call = service.StartSpatialAnalyzerSdk(new global::Briosa.StartSpatialAnalyzerSdkRequest(),
            new InMemoryServerCallContext("/briosa.SpatialAnalyzerSdkLifecycle/StartSpatialAnalyzerSdk", null,
                cancellation.Token));
        await ready.Entered.WaitAsync(HangGuard);
        await cancellation.CancelAsync();
        var error = await Assert.ThrowsAsync<RpcException>(() => call.WaitAsync(HangGuard));
        ready.Release();
        _ = await WaitFor(supervisor, state => state.ReadyForExecution);

        Assert.Equal(StatusCode.Cancelled, error.StatusCode);
        var detail = global::Briosa.SpatialAnalyzerSdkLifecycleError.Parser.ParseFrom(
            Assert.Single(error.Trailers).ValueBytes);
        Assert.Equal("StartSpatialAnalyzerSdk", detail.Rpc);
        Assert.Equal("sdk-lifecycle-caller-stopped-waiting", detail.DiagnosticCode);
        var rejected = Assert.Single(audit.Entries, entry => entry.EventId.Id == 1302);
        Assert.Equal("StartSpatialAnalyzerSdk", rejected.Value("Rpc"));
        Assert.Equal(StatusCode.Cancelled, rejected.Value("GrpcStatus"));
        Assert.Equal("sdk-lifecycle-caller-stopped-waiting", rejected.Value("DiagnosticCode"));
    }

    private static WorkerMpCommand Plain(string operationId) => new(operationId, "Cancellation", [], []);

    private static ExecutionWorkItem WorkItem() => new(Plain("unit"), Guid.NewGuid(), 1, default, 1,
        OperationDurationClass.Quick, ExecutionBudget);

    private static (WorkerProcessSupervisor Supervisor, HeartbeatTestClock Clock, SequenceFactory Factory) Create(
        ScriptedWorker first,
        ScriptedWorker? second = null,
        ILogger<WorkerProcessSupervisor>? supervisorLogger = null)
    {
        var clock = new HeartbeatTestClock();
        var factory = new SequenceFactory(second is null ? [first] : [first, second]);
        var supervisor = new WorkerProcessSupervisor(factory,
            new WorkerLifecyclePolicy(HeartbeatInterval, HeartbeatTimeout, StartupTimeout, ShutdownTimeout,
                readinessProbeTimeout: ReadinessProbeTimeout),
            TestDurationClasses.SyntheticQuickPolicy(ExecutionBudget, queueCapacity: 4),
            clock,
            supervisorLogger);
        return (supervisor, clock, factory);
    }

    private static async Task WaitUntil(Func<bool> predicate)
    {
        using var timeout = new CancellationTokenSource(HangGuard);
        while (!predicate()) await Task.Delay(TimeSpan.FromMilliseconds(5), timeout.Token);
    }

    private static async Task<WorkerLifecycleSnapshot> WaitFor(
        WorkerProcessSupervisor supervisor,
        Func<WorkerLifecycleSnapshot, bool> predicate)
    {
        await WaitUntil(() => predicate(supervisor.Current));
        return supervisor.Current;
    }

    // A caller that stopped waiting refreshes state and retries only once the detached
    // transition has released the coordinator, exactly as a client would.
    private static Task<global::Briosa.SpatialAnalyzerSdkLifecycleState> StopWhenIdleAsync(
        SpatialAnalyzerSdkLifecycleCoordinator coordinator, int generation) =>
        WhenIdleAsync(() => coordinator.StopAsync(generation, CancellationToken.None));

    private static Task<global::Briosa.SpatialAnalyzerSdkLifecycleState> ConnectWhenIdleAsync(
        SpatialAnalyzerSdkLifecycleCoordinator coordinator, int generation) =>
        WhenIdleAsync(() => coordinator.ConnectAsync(generation, reconnect: false, CancellationToken.None));

    private static async Task<global::Briosa.SpatialAnalyzerSdkLifecycleState> WhenIdleAsync(
        Func<Task<global::Briosa.SpatialAnalyzerSdkLifecycleState>> transition)
    {
        using var timeout = new CancellationTokenSource(HangGuard);
        while (true)
        {
            try
            {
                return await transition();
            }
            catch (SdkLifecycleException exception) when (
                exception.Detail.DiagnosticCode == "sdk-lifecycle-transition-in-progress")
            {
                await Task.Delay(TimeSpan.FromMilliseconds(5), timeout.Token);
            }
        }
    }

    private static async Task<global::Briosa.SpatialAnalyzerSdkLifecycleState> WaitForCoordinator(
        SpatialAnalyzerSdkLifecycleCoordinator coordinator,
        Func<global::Briosa.SpatialAnalyzerSdkLifecycleState, bool> predicate)
    {
        await WaitUntil(() => predicate(coordinator.Current));
        return coordinator.Current;
    }

    // One exchange held until the test releases it; the exchange's own bound can
    // still cancel the wait, exactly as for a real worker that never answers.
    private sealed class Hold
    {
        private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Entered => _entered.Task;

        public void Release() => _released.TrySetResult();

        public async Task WaitAsync(CancellationToken cancellationToken)
        {
            _entered.TrySetResult();
            try
            {
                await _released.Task.WaitAsync(HangGuard, cancellationToken).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                // A failed test never releases its holds; let disposal finish anyway.
            }
        }
    }

    private sealed class SequenceFactory(IReadOnlyList<ScriptedWorker> workers) : IWorkerProcessFactory
    {
        private int _starts;

        public int Starts => Volatile.Read(ref _starts);

        public ValueTask<IWorkerProcess> StartAsync(int generation, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IWorkerProcess>(workers[Interlocked.Increment(ref _starts) - 1]);
    }

    private sealed class RunningApplication(int generation) : ISpatialAnalyzerLifecycleStateProvider
    {
        public Task<global::Briosa.SpatialAnalyzerLifecycleState> GetCurrentAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new global::Briosa.SpatialAnalyzerLifecycleState
            {
                StateRevision = 1,
                ApplicationState = global::Briosa.SpatialAnalyzerApplicationState.Running,
                Ownership = global::Briosa.SpatialAnalyzerOwnership.External,
                ApplicationGeneration = generation
            });
        }
    }

    // An in-process worker that answers with real control-message encoding. Each
    // exchange kind may be held; the supervisor's bound still cancels a held wait.
    private sealed class ScriptedWorker : IWorkerProcess
    {
        private readonly ConcurrentQueue<string> _executed = new();
        private WorkerControlMessage? _request;
        private bool _readySent;
        private int _connectCount;
        private int _verifyCount;
        private int _pingCount;
        private int _stopCount;
        private int _terminationCount;
        private int _exited;

        public bool ReadyConnected { get; init; } = true;
        public bool ReadyFaulted { get; init; }
        public Hold? ReadyHold { get; init; }
        public Hold? ConnectHold { get; init; }
        public Hold? VerifyHold { get; init; }
        public Func<int, Hold?> PingHold { get; init; } = _ => null;
        public Func<string, Hold?> ExecuteHold { get; init; } = _ => null;

        // An unexpected failure escapes the exchange path and fails the consumer.
        public Func<string, bool> ExecuteFaults { get; init; } = _ => false;

        public int ConnectCount => Volatile.Read(ref _connectCount);
        public int VerifyCount => Volatile.Read(ref _verifyCount);
        public int PingCount => Volatile.Read(ref _pingCount);
        public int StopCount => Volatile.Read(ref _stopCount);
        public int TerminationCount => Volatile.Read(ref _terminationCount);
        public IReadOnlyList<string> ExecutedOperations => [.. _executed];
        public bool HasExited => Volatile.Read(ref _exited) != 0;
        public int? ExitCode => HasExited ? 0 : null;

        public async ValueTask SendAsync(WorkerControlMessage message, CancellationToken cancellationToken = default)
        {
            using var stream = new MemoryStream();
            using var channel = new WorkerControlChannel(stream);
            await channel.SendAsync(message, cancellationToken).ConfigureAwait(false);
            _request = message;
        }

        public async ValueTask<WorkerControlMessage> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            if (!_readySent)
            {
                if (ReadyHold is not null) await ReadyHold.WaitAsync(cancellationToken).ConfigureAwait(false);
                _readySent = true;
                return WorkerControlMessage.Ready(123, ReadyFaulted
                    ? Connection(WorkerConnectionState.Faulted, WorkerExecutionReadinessState.Unverified,
                        WorkerConnectionFailure.ActivationFailed)
                    : Connection(ReadyConnected ? WorkerConnectionState.Connected : WorkerConnectionState.Disconnected,
                        WorkerExecutionReadinessState.Unverified));
            }

            var request = _request ?? throw new InvalidOperationException("No scripted request is pending.");
            switch (request.Kind)
            {
                case WorkerControlMessageKind.Connect:
                    Interlocked.Increment(ref _connectCount);
                    if (ConnectHold is not null) await ConnectHold.WaitAsync(cancellationToken).ConfigureAwait(false);
                    return WorkerControlMessage.ConnectionResult(request.CorrelationId,
                        Connection(WorkerConnectionState.Connected, WorkerExecutionReadinessState.Unverified));
                case WorkerControlMessageKind.VerifyExecution:
                    Interlocked.Increment(ref _verifyCount);
                    if (VerifyHold is not null) await VerifyHold.WaitAsync(cancellationToken).ConfigureAwait(false);
                    return WorkerControlMessage.ExecutionVerificationResult(request.CorrelationId,
                        Connection(WorkerConnectionState.Connected, WorkerExecutionReadinessState.ExecutionReady));
                case WorkerControlMessageKind.Ping:
                    var ping = PingHold(Volatile.Read(ref _pingCount) + 1);
                    if (ping is not null) await ping.WaitAsync(cancellationToken).ConfigureAwait(false);
                    Interlocked.Increment(ref _pingCount);
                    return WorkerControlMessage.Pong(request.CorrelationId,
                        Connection(WorkerConnectionState.Connected, WorkerExecutionReadinessState.ExecutionReady));
                case WorkerControlMessageKind.Execute:
                    var operationId = request.Command!.OperationId;
                    _executed.Enqueue(operationId);
                    var execute = ExecuteHold(operationId);
                    if (execute is not null) await execute.WaitAsync(cancellationToken).ConfigureAwait(false);
                    if (ExecuteFaults(operationId)) throw new ArgumentException("Scripted consumer fault.");
                    return WorkerControlMessage.ExecutionResult(request.CorrelationId, new(
                        WorkerExecutionResponseStatus.Completed,
                        WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, [], null),
                        Connection(WorkerConnectionState.Connected, WorkerExecutionReadinessState.ExecutionReady),
                        null));
                case WorkerControlMessageKind.Stop:
                    Interlocked.Increment(ref _stopCount);
                    MarkExited();
                    return WorkerControlMessage.Stopped(request.CorrelationId);
                default:
                    throw new InvalidOperationException("Unexpected scripted request.");
            }
        }

        public Task WaitForExitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public ValueTask TerminateAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _terminationCount);
            MarkExited();
            return ValueTask.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            MarkExited();
            return ValueTask.CompletedTask;
        }

        private void MarkExited() => Interlocked.Exchange(ref _exited, 1);

        private static WorkerConnectionSnapshot Connection(
            WorkerConnectionState state,
            WorkerExecutionReadinessState readiness,
            WorkerConnectionFailure failure = WorkerConnectionFailure.None) => new(
            state, readiness, 0, 1, 1, "scripted", DateTimeOffset.UnixEpoch,
            new(new(SpatialAnalyzerApi.TargetVersion, WorkerRuntimeIdentityEvidenceSource.RuntimeVerified),
                new(SpatialAnalyzerApi.TargetVersion, WorkerRuntimeIdentityEvidenceSource.RuntimeVerified)))
        {
            Failure = failure
        };
    }

    // Captures structured log state so tests can assert value-free audit evidence.
    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public ConcurrentQueue<LogEntry> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Entries.Enqueue(new LogEntry(eventId,
                state as IReadOnlyList<KeyValuePair<string, object?>> ?? []));
    }

    private sealed record LogEntry(EventId EventId, IReadOnlyList<KeyValuePair<string, object?>> State)
    {
        public object? Value(string name) => State.FirstOrDefault(pair => pair.Key == name).Value;
    }
}
