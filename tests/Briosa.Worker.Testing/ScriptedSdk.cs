using System.Collections.Concurrent;
using Briosa.Worker.Sdk;

namespace Briosa.Worker.Testing;

internal enum ScriptedExecutionKind
{
    Success,
    MpFailure,
    Delay,
    Hang,
    Crash
}

internal enum ScriptedCallPhase
{
    Started,
    Completed,
    Crashed
}

internal sealed record ScriptedCallEvent(
    long Sequence,
    string OperationId,
    ScriptedCallPhase Phase,
    ScriptedExecutionKind Behavior);

internal sealed record ScriptedExecution(
    ScriptedExecutionKind Kind,
    ManualResetEventSlim? Gate = null)
{
    public static ScriptedExecution Success() => new(ScriptedExecutionKind.Success);

    public static ScriptedExecution MpFailure() => new(ScriptedExecutionKind.MpFailure);

    public static ScriptedExecution Delay(ManualResetEventSlim gate) =>
        new(ScriptedExecutionKind.Delay, gate ?? throw new ArgumentNullException(nameof(gate)));

    public static ScriptedExecution Hang(ManualResetEventSlim gate) =>
        new(ScriptedExecutionKind.Hang, gate ?? throw new ArgumentNullException(nameof(gate)));

    public static ScriptedExecution Crash() => new(ScriptedExecutionKind.Crash);
}

public sealed class SimulatedWorkerCrashException : Exception
{
    public SimulatedWorkerCrashException()
        : base("The scripted fake simulated abrupt worker-process loss.")
    {
    }

    public SimulatedWorkerCrashException(string message)
        : base(message)
    {
    }

    public SimulatedWorkerCrashException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

internal sealed class ScriptedSdkPlan
{
    private readonly ConcurrentQueue<SdkConnectionResult> _connections = new();
    private readonly ConcurrentQueue<ScriptedExecution> _executions = new();
    private readonly ConcurrentQueue<ScriptedCallEvent> _events = new();
    private readonly ConcurrentBag<ManualResetEventSlim> _blockingGates = [];
    private long _eventSequence;

    public IReadOnlyList<ScriptedCallEvent> Events => [.. _events];

    public ApartmentState? AdapterApartmentState { get; private set; }

    public ScriptedSdkPlan ConnectsSuccessfully(int statusCode = 0)
    {
        _connections.Enqueue(new SdkConnectionResult(SdkConnectionStatus.Connected, statusCode, null));
        return this;
    }

    public ScriptedSdkPlan FailsConnection(int statusCode = -1)
    {
        _connections.Enqueue(
            new SdkConnectionResult(
                SdkConnectionStatus.Unavailable,
                statusCode,
                "scripted-connection-failure"));
        return this;
    }

    public ScriptedSdkPlan Then(ScriptedExecution execution)
    {
        ArgumentNullException.ThrowIfNull(execution);
        _executions.Enqueue(execution);
        if (execution.Gate is not null)
        {
            _blockingGates.Add(execution.Gate);
        }

        return this;
    }

    public ISpatialAnalyzerSdk CreateSdk() => new Adapter(this);

    public void ReleaseBlockedCalls()
    {
        foreach (var gate in _blockingGates)
        {
            gate.Set();
        }
    }

    private sealed class Adapter(ScriptedSdkPlan plan) : ISpatialAnalyzerSdk
    {
        public SdkConnectionResult Connect(string host)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(host);
            plan.AdapterApartmentState = Thread.CurrentThread.GetApartmentState();
            return plan._connections.TryDequeue(out var result)
                ? result
                : new SdkConnectionResult(SdkConnectionStatus.Connected, 0, null);
        }

        public SdkExecutionResult Execute(SdkCommand command)
        {
            ArgumentNullException.ThrowIfNull(command);
            plan.AdapterApartmentState = Thread.CurrentThread.GetApartmentState();
            var execution = plan._executions.TryDequeue(out var scripted)
                ? scripted
                : ScriptedExecution.Success();

            plan.Record(command, ScriptedCallPhase.Started, execution.Kind);
            if (execution.Kind is ScriptedExecutionKind.Delay or ScriptedExecutionKind.Hang)
            {
                execution.Gate!.Wait();
            }

            if (execution.Kind == ScriptedExecutionKind.Crash)
            {
                plan.Record(command, ScriptedCallPhase.Crashed, execution.Kind);
                throw new SimulatedWorkerCrashException();
            }

            plan.Record(command, ScriptedCallPhase.Completed, execution.Kind);
            return execution.Kind == ScriptedExecutionKind.MpFailure
                ? new SdkExecutionResult(
                    ExecuteStepReturned: true,
                    new SdkMpResult(false, 42, "scripted-mp-failure"))
                : new SdkExecutionResult(
                    ExecuteStepReturned: true,
                    new SdkMpResult(true, 0, null));
        }

        public void Dispose()
        {
        }
    }

    private void Record(SdkCommand command, ScriptedCallPhase phase, ScriptedExecutionKind behavior)
    {
        var sequence = Interlocked.Increment(ref _eventSequence);
        _events.Enqueue(new ScriptedCallEvent(sequence, command.OperationId, phase, behavior));
    }
}
