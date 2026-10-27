using System.Collections.Concurrent;
using System.Threading.Channels;
using Briosa.Server.Workers;
using Briosa.Worker.Control;

namespace Briosa.Server.Tests;

/// <summary>
/// An in-memory worker generation that reports a connected SDK at Ready, answers the
/// readiness probe, and executes each command through a test-supplied script. A
/// script that returns <see langword="null"/> holds the exchange until the
/// supervisor terminates the process, as a blocked STA does.
/// </summary>
internal sealed class ScriptedWorkerProcess : IWorkerProcess
{
    private static readonly WorkerRuntimeIdentitySnapshot Identity = new(
        new WorkerRuntimeIdentityEvidence(Version: null, WorkerRuntimeIdentityEvidenceSource.Unavailable),
        new WorkerRuntimeIdentityEvidence(Version: null, WorkerRuntimeIdentityEvidenceSource.Unavailable));

    private readonly Func<WorkerMpCommand, WorkerMpExecutionResult?> _execute;
    private readonly bool _holdVerification;
    private readonly Channel<WorkerControlMessage> _responses =
        Channel.CreateUnbounded<WorkerControlMessage>(new UnboundedChannelOptions { SingleReader = true });
    private readonly TaskCompletionSource _exit = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _held = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _exited;

    public ScriptedWorkerProcess(
        Func<WorkerMpCommand, WorkerMpExecutionResult?>? execute = null,
        bool holdVerification = false,
        bool connectedAtReady = true)
    {
        _execute = execute ?? (_ => Completed(2));
        _holdVerification = holdVerification;
        Assert.True(_responses.Writer.TryWrite(WorkerControlMessage.Ready(
            processId: 4321, connectedAtReady
                ? Connection(WorkerExecutionReadinessState.Unverified, "connect-ex-connected")
                : Connection(WorkerExecutionReadinessState.Unverified, "sdk-started") with
                {
                    State = WorkerConnectionState.Disconnected,
                    StatusCode = null
                })));
    }

    public ConcurrentQueue<WorkerControlMessageKind> Received { get; } = new();

    public ConcurrentQueue<WorkerMpCommand> Commands { get; } = new();

    /// <summary>Runs as each Execute arrives, before its response is written.</summary>
    public Action<WorkerMpCommand>? OnExecute { get; set; }

    /// <summary>Runs as each answered readiness probe arrives, before its response is written.</summary>
    public Action? OnVerify { get; set; }

    /// <summary>Completes when a probe or command is being held.</summary>
    public Task Held => _held.Task;

    public bool HasExited => Volatile.Read(ref _exited) != 0;

    public int? ExitCode => HasExited ? 0 : null;

    public static WorkerMpExecutionResult Completed(int mpResultCode) =>
        new WorkerMpResultAvailable(mpResultCode, durationMilliseconds: 1, outputs: [], diagnosticCode: null);

    public ValueTask SendAsync(WorkerControlMessage message, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(HasExited, this);
        Received.Enqueue(message.Kind);
        switch (message.Kind)
        {
            case WorkerControlMessageKind.VerifyExecution when _holdVerification:
                _held.TrySetResult();
                break;
            case WorkerControlMessageKind.VerifyExecution:
                OnVerify?.Invoke();
                Write(WorkerControlMessage.ExecutionVerificationResult(message.CorrelationId,
                    Connection(WorkerExecutionReadinessState.ExecutionReady, "execution-readiness-verified")));
                break;
            case WorkerControlMessageKind.Execute:
                Commands.Enqueue(message.Command!);
                OnExecute?.Invoke(message.Command!);
                if (_execute(message.Command!) is { } execution)
                {
                    Write(WorkerControlMessage.ExecutionResult(message.CorrelationId, new WorkerExecutionResponse(
                        WorkerExecutionResponseStatus.Completed, execution,
                        Connection(WorkerExecutionReadinessState.ExecutionReady, "execution-readiness-verified"),
                        DiagnosticCode: null)));
                }
                else
                {
                    _held.TrySetResult();
                }

                break;
            case WorkerControlMessageKind.Connect:
                Write(WorkerControlMessage.ConnectionResult(message.CorrelationId,
                    Connection(WorkerExecutionReadinessState.Unverified, "connect-ex-connected")));
                break;
            case WorkerControlMessageKind.Ping:
                Write(WorkerControlMessage.Pong(message.CorrelationId,
                    Connection(WorkerExecutionReadinessState.ExecutionReady, "execution-readiness-verified")));
                break;
            case WorkerControlMessageKind.Stop:
                Write(WorkerControlMessage.Stopped(message.CorrelationId));
                MarkExited();
                break;
            default:
                throw new InvalidDataException($"Unexpected message kind {message.Kind}.");
        }

        return ValueTask.CompletedTask;
    }

    public async ValueTask<WorkerControlMessage> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _responses.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ChannelClosedException)
        {
            throw new IOException("The scripted worker exited.");
        }
    }

    public Task WaitForExitAsync(CancellationToken cancellationToken = default) =>
        _exit.Task.WaitAsync(cancellationToken);

    public ValueTask TerminateAsync(CancellationToken cancellationToken = default)
    {
        MarkExited();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        MarkExited();
        return ValueTask.CompletedTask;
    }

    private static WorkerConnectionSnapshot Connection(
        WorkerExecutionReadinessState readiness, string diagnosticCode) =>
        new(WorkerConnectionState.Connected, readiness, StatusCode: 0, Attempt: 1, MaximumAttempts: 1,
            diagnosticCode, DateTimeOffset.UnixEpoch, Identity);

    private void Write(WorkerControlMessage message) => Assert.True(_responses.Writer.TryWrite(message));

    private void MarkExited()
    {
        if (Interlocked.Exchange(ref _exited, 1) != 0) return;
        _responses.Writer.TryComplete();
        _exit.TrySetResult();
    }
}

/// <summary>Starts one scripted generation per call and keeps every generation.</summary>
internal sealed class ScriptedWorkerProcessFactory(Func<int, ScriptedWorkerProcess> create) : IWorkerProcessFactory
{
    public ConcurrentDictionary<int, ScriptedWorkerProcess> Generations { get; } = new();

    public ValueTask<IWorkerProcess> StartAsync(int generation, CancellationToken cancellationToken = default)
    {
        var process = create(generation);
        Assert.True(Generations.TryAdd(generation, process));
        return ValueTask.FromResult<IWorkerProcess>(process);
    }
}
