namespace Briosa.Server.Workers;

/// <summary>
/// Decides one Start, Connect, Recover, or Stop request between its caller's
/// cancellation and the supervisor's acceptance (F10, #305). Exactly one wins. A
/// caller that withdraws first prevents the work and leaves state unchanged. Once
/// accepted, the exchange no longer observes the caller's token: it completes under
/// its own lifecycle bounds and records its terminal state.
/// </summary>
internal sealed class LifecycleAcceptance
{
    private const int Pending = 0;
    private const int Accepted = 1;
    private const int Withdrawn = 2;
    private int _state;

    public LifecycleAcceptance(CancellationToken callerToken) => CallerToken = callerToken;

    /// <summary>The caller's token. Only waits before acceptance may observe it.</summary>
    public CancellationToken CallerToken { get; }

    public bool IsAccepted => Volatile.Read(ref _state) == Accepted;

    /// <summary>
    /// Commits the request after its preconditions hold and before its first
    /// transition. Throws when the caller withdrew first; nothing has changed then.
    /// </summary>
    public void Accept()
    {
        if (CallerToken.IsCancellationRequested) TryWithdraw();
        if (Interlocked.CompareExchange(ref _state, Accepted, Pending) == Withdrawn)
            throw new OperationCanceledException(CallerToken);
    }

    /// <summary>Returns true when the withdrawal won, so the work never starts.</summary>
    public bool TryWithdraw() =>
        Interlocked.CompareExchange(ref _state, Withdrawn, Pending) != Accepted;

    /// <summary>
    /// Waits for <paramref name="exchange"/> as its caller. Cancellation before
    /// acceptance withdraws the request and rethrows the cancellation. Cancellation
    /// after acceptance returns <paramref name="stoppedWaiting"/> at once while the
    /// exchange continues; the caller then refreshes state for its terminal outcome.
    /// </summary>
    public async Task<T> WaitAsync<T>(Task<T> exchange, Func<T> stoppedWaiting)
    {
        ArgumentNullException.ThrowIfNull(exchange);
        ArgumentNullException.ThrowIfNull(stoppedWaiting);
        try
        {
            return await exchange.WaitAsync(CallerToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (CallerToken.IsCancellationRequested)
        {
            // An exchange that finished first keeps its own result or failure.
            if (exchange.IsCompleted) return await exchange.ConfigureAwait(false);

            // Nobody awaits the exchange any more; observe a late failure so it
            // cannot surface as an unobserved task exception.
            _ = exchange.ContinueWith(static task => _ = task.Exception, CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
            if (TryWithdraw()) throw;
            return stoppedWaiting();
        }
    }
}
