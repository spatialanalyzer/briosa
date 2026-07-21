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

internal sealed record ScriptedConnection(
    SdkConnectionResult Result,
    ManualResetEventSlim? Gate = null);

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
    private readonly ConcurrentQueue<ScriptedConnection> _connections = new();
    private readonly ConcurrentQueue<string> _connectionHosts = new();
    private readonly ConcurrentQueue<ScriptedExecution> _executions = new();
    private readonly ConcurrentQueue<ScriptedCallEvent> _events = new();
    private readonly ConcurrentBag<ManualResetEventSlim> _blockingGates = [];
    private int _activeAdapterCount;
    private int _adapterCreationCount;
    private int _adapterDisposalCount;
    private int _connectionCallCount;
    private int _maximumActiveAdapterCount;
    private long _eventSequence;

    public IReadOnlyList<ScriptedCallEvent> Events => [.. _events];

    public IReadOnlyList<string> ConnectionHosts => [.. _connectionHosts];

    public ApartmentState? AdapterApartmentState { get; private set; }

    public ApartmentState? AdapterDisposalApartmentState { get; private set; }

    public int AdapterCreationCount => Volatile.Read(ref _adapterCreationCount);

    public int AdapterDisposalCount => Volatile.Read(ref _adapterDisposalCount);

    public int ConnectionCallCount => Volatile.Read(ref _connectionCallCount);

    public int MaximumActiveAdapterCount => Volatile.Read(ref _maximumActiveAdapterCount);

    public ScriptedSdkPlan ConnectsSuccessfully(int statusCode = 0)
    {
        _connections.Enqueue(
            new ScriptedConnection(
                new SdkConnectionResult(SdkConnectionStatus.Connected, statusCode, null)));
        return this;
    }

    public ScriptedSdkPlan FailsConnection(int statusCode = -1)
    {
        _connections.Enqueue(
            new ScriptedConnection(
                new SdkConnectionResult(
                    SdkConnectionStatus.Unavailable,
                    statusCode,
                    "scripted-connection-failure")));
        return this;
    }

    public ScriptedSdkPlan DelaysConnection(
        ManualResetEventSlim gate,
        SdkConnectionResult result)
    {
        ArgumentNullException.ThrowIfNull(gate);
        ArgumentNullException.ThrowIfNull(result);
        _connections.Enqueue(new ScriptedConnection(result, gate));
        _blockingGates.Add(gate);
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

    public ISpatialAnalyzerSdk CreateSdk()
    {
        Interlocked.Increment(ref _adapterCreationCount);
        var active = Interlocked.Increment(ref _activeAdapterCount);
        UpdateMaximumActiveAdapters(active);
        return new Adapter(this);
    }

    public void ReleaseBlockedCalls()
    {
        foreach (var gate in _blockingGates)
        {
            gate.Set();
        }
    }

    private void UpdateMaximumActiveAdapters(int active)
    {
        while (true)
        {
            var maximum = Volatile.Read(ref _maximumActiveAdapterCount);
            if (active <= maximum ||
                Interlocked.CompareExchange(
                    ref _maximumActiveAdapterCount,
                    active,
                    maximum) == maximum)
            {
                return;
            }
        }
    }

    private sealed class Adapter(ScriptedSdkPlan plan) : ISpatialAnalyzerSdk
    {
        private int _disposeState;

        public SdkConnectionResult Connect(string host)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(host);
            plan.AdapterApartmentState = Thread.CurrentThread.GetApartmentState();
            plan._connectionHosts.Enqueue(host);
            Interlocked.Increment(ref plan._connectionCallCount);
            var connection = plan._connections.TryDequeue(out var scripted)
                ? scripted
                : new ScriptedConnection(
                    new SdkConnectionResult(SdkConnectionStatus.Connected, 0, null));
            connection.Gate?.Wait();
            return connection.Result;
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
            if (Interlocked.Exchange(ref _disposeState, 1) != 0)
            {
                return;
            }

            plan.AdapterDisposalApartmentState = Thread.CurrentThread.GetApartmentState();
            Interlocked.Increment(ref plan._adapterDisposalCount);
            Interlocked.Decrement(ref plan._activeAdapterCount);
        }
    }

    private void Record(SdkCommand command, ScriptedCallPhase phase, ScriptedExecutionKind behavior)
    {
        var sequence = Interlocked.Increment(ref _eventSequence);
        _events.Enqueue(new ScriptedCallEvent(sequence, command.OperationId, phase, behavior));
    }
}
