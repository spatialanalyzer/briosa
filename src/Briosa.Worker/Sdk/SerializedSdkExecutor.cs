using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Briosa.Worker.Sdk;

/// <summary>
/// Owns one SDK adapter on one STA and serializes all access to it.
/// </summary>
internal sealed class SerializedSdkExecutor : IAsyncDisposable
{
    private readonly BlockingCollection<IWorkItem> _queue = [];
    private readonly Func<ISpatialAnalyzerSdk> _sdkFactory;
    private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Thread _thread;
    private int _disposeState;

    public SerializedSdkExecutor(Func<ISpatialAnalyzerSdk> sdkFactory)
    {
        ArgumentNullException.ThrowIfNull(sdkFactory);
        _sdkFactory = sdkFactory;
        _thread = new Thread(Run)
        {
            IsBackground = true,
            Name = "Briosa SDK STA"
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        try
        {
            _started.Task.GetAwaiter().GetResult();
        }
        catch
        {
            _queue.CompleteAdding();
            _thread.Join();
            _queue.Dispose();
            throw;
        }
    }

    public Task<SdkConnectionResult> ConnectAsync(string host, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        return Enqueue(sdk => sdk.Connect(host), cancellationToken);
    }

    public Task<SdkExecutionResult> ExecuteAsync(
        SdkCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Enqueue(sdk => sdk.Execute(command), cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0)
        {
            return ValueTask.CompletedTask;
        }

        _queue.CompleteAdding();
        _thread.Join();
        _queue.Dispose();
        return ValueTask.CompletedTask;
    }

    private Task<T> Enqueue<T>(Func<ISpatialAnalyzerSdk, T> operation, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposeState) != 0, this);

        var item = new WorkItem<T>(operation);
        try
        {
            _queue.Add(item, cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            throw new ObjectDisposedException(GetType().FullName, exception);
        }

        return item.Task.WaitAsync(cancellationToken);
    }

    [SuppressMessage(
        "Design", "CA1031:Do not catch general exception types",
        Justification = "Every adapter initialization failure must fault queued work before the STA exits.")]
    private void Run()
    {
        ISpatialAnalyzerSdk? sdk = null;
        try
        {
            sdk = _sdkFactory();
            _started.SetResult();

            foreach (var item in _queue.GetConsumingEnumerable())
            {
                item.Execute(sdk);
            }
        }
        catch (Exception exception)
        {
            _started.TrySetException(exception);
            while (_queue.TryTake(out var item))
            {
                item.Fail(exception);
            }
        }
        finally
        {
            sdk?.Dispose();
        }
    }

    private interface IWorkItem
    {
        void Execute(ISpatialAnalyzerSdk sdk);

        void Fail(Exception exception);
    }

    private sealed class WorkItem<T>(Func<ISpatialAnalyzerSdk, T> operation) : IWorkItem
    {
        private readonly TaskCompletionSource<T> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<T> Task => _completion.Task;

        [SuppressMessage(
            "Design", "CA1031:Do not catch general exception types",
            Justification = "Every synchronous SDK failure must cross the thread boundary through its task.")]
        public void Execute(ISpatialAnalyzerSdk sdk)
        {
            try
            {
                _completion.SetResult(operation(sdk));
            }
            catch (Exception exception)
            {
                _completion.SetException(exception);
            }
        }

        public void Fail(Exception exception) => _completion.TrySetException(exception);
    }
}
