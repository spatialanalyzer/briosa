using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Briosa.Worker.Sdk;

/// <summary>
/// Owns at most one active SDK adapter and exposes its deterministic connection lifecycle.
/// </summary>
internal sealed class SdkConnectionManager : IAsyncDisposable
{
    internal const string NotReadyDiagnosticCode = "sdk-connection-not-ready";

    [SuppressMessage(
        "Usage",
        "CA2213:Disposable fields should be disposed",
        Justification = "The gate remains usable so racing callers can observe the stable Stopping outcome.")]
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly List<SdkConnectionSnapshot> _history = [];
    private readonly object _historyLock = new();
    private readonly SdkConnectionPolicy _policy;
    private readonly Func<ISpatialAnalyzerSdk> _sdkFactory;
    private readonly string _targetHost;
    private readonly TimeProvider _timeProvider;
    private SerializedSdkExecutor? _executor;
    private SdkConnectionSnapshot _current;
    private int _disposeState;

    public SdkConnectionManager(
        string targetHost,
        SdkConnectionPolicy policy,
        Func<ISpatialAnalyzerSdk> sdkFactory,
        TimeProvider? timeProvider = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetHost);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(sdkFactory);

        _targetHost = targetHost;
        _policy = policy;
        _sdkFactory = sdkFactory;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _current = new SdkConnectionSnapshot(
            SdkConnectionState.Disconnected,
            _targetHost,
            StatusCode: null,
            Attempt: 0,
            _policy.MaximumAttempts,
            "connection-not-started",
            _timeProvider.GetUtcNow());
        _history.Add(_current);
    }

    public SdkConnectionSnapshot Current
    {
        get
        {
            lock (_historyLock)
            {
                return _current;
            }
        }
    }

    public IReadOnlyList<SdkConnectionSnapshot> History
    {
        get
        {
            lock (_historyLock)
            {
                return _history.ToArray();
            }
        }
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Vendor activation and ConnectEx failures are converted into safe connection outcomes.")]
    public async Task<SdkConnectionSnapshot> ConnectAsync(
        CancellationToken cancellationToken = default)
    {
        if (Current.State == SdkConnectionState.Stopping)
        {
            return Current;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (Current.State is SdkConnectionState.Connected or SdkConnectionState.Stopping)
            {
                return Current;
            }

            for (var attempt = 1; attempt <= _policy.MaximumAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Transition(
                    SdkConnectionState.Connecting,
                    statusCode: null,
                    attempt,
                    "connect-ex-started");

                SdkConnectionResult result;
                try
                {
                    var executor = _executor;
                    if (executor is null)
                    {
                        executor = new SerializedSdkExecutor(_sdkFactory);
                        _executor = executor;
                    }

                    result = await executor.ConnectAsync(_targetHost, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    var diagnosticCode = _executor is null
                        ? "sdk-client-activation-failed"
                        : "connect-ex-failed";
                    await ReleaseExecutor().ConfigureAwait(false);
                    result = new SdkConnectionResult(
                        SdkConnectionStatus.Unavailable,
                        StatusCode: null,
                        diagnosticCode);
                }

                if (result.Status == SdkConnectionStatus.Connected)
                {
                    Transition(
                        SdkConnectionState.Connected,
                        result.StatusCode,
                        attempt,
                        result.DiagnosticCode ?? "connect-ex-connected");
                    return Current;
                }

                var failureCode = result.DiagnosticCode ?? "connect-ex-unavailable";
                if (attempt == _policy.MaximumAttempts)
                {
                    Transition(
                        SdkConnectionState.Faulted,
                        result.StatusCode,
                        attempt,
                        failureCode);
                    return Current;
                }

                Transition(
                    SdkConnectionState.Connecting,
                    result.StatusCode,
                    attempt,
                    failureCode);
                try
                {
                    await Task.Delay(
                        _policy.RetryDelay,
                        _timeProvider,
                        cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    Transition(
                        SdkConnectionState.Faulted,
                        result.StatusCode,
                        attempt,
                        "connection-retry-cancelled");
                    throw;
                }
            }

            throw new UnreachableException();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<SdkRequestResult> ExecuteAsync(
        SdkCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var observed = Current;
        if (observed.State != SdkConnectionState.Connected)
        {
            return Unavailable(observed);
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            observed = Current;
            var executor = _executor;
            if (executor is null)
            {
                return Unavailable(observed);
            }

            var execution = await executor.ExecuteAsync(command, CancellationToken.None)
                .ConfigureAwait(false);
            return new SdkRequestResult(
                SdkRequestStatus.Completed,
                execution,
                observed,
                DiagnosticCode: null);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0)
        {
            return;
        }

        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            Transition(
                SdkConnectionState.Stopping,
                Current.StatusCode,
                Current.Attempt,
                "connection-stopping");
            await ReleaseExecutor().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static SdkRequestResult Unavailable(SdkConnectionSnapshot connection) =>
        new(
            SdkRequestStatus.Unavailable,
            Execution: null,
            connection,
            NotReadyDiagnosticCode);

    private async ValueTask ReleaseExecutor()
    {
        var executor = _executor;
        _executor = null;
        if (executor is not null)
        {
            await executor.DisposeAsync().ConfigureAwait(false);
        }
    }

    private void Transition(
        SdkConnectionState state,
        int? statusCode,
        int attempt,
        string diagnosticCode)
    {
        var snapshot = new SdkConnectionSnapshot(
            state,
            _targetHost,
            statusCode,
            attempt,
            _policy.MaximumAttempts,
            diagnosticCode,
            _timeProvider.GetUtcNow());
        lock (_historyLock)
        {
            _current = snapshot;
            _history.Add(snapshot);
        }
    }
}
