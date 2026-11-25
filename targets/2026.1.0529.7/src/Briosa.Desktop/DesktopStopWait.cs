namespace Briosa.Desktop;

/// <summary>
/// How long Control Center observes its owned server's exit after the server accepts
/// StopServer (#400). The server publishes its host graceful shutdown bound in server
/// info; the wait is that bound plus <see cref="ExitMargin"/> for the process to exit
/// after its host stops. A missing, zero, unrecognized, or out-of-range bound falls
/// back to <see cref="Fallback"/>, the fixed wait of earlier releases. An explicitly
/// unbounded host is observed until the caller cancels. Neither expiry nor
/// cancellation kills, retries, or replays anything.
/// </summary>
public static class DesktopStopWait
{
    public static readonly TimeSpan Fallback = TimeSpan.FromSeconds(45);
    public static readonly TimeSpan ExitMargin = TimeSpan.FromSeconds(5);

    // The longest finite delay a cancellation timer accepts: 4,294,967,294 ms.
    private const ulong MaximumTimerMilliseconds = uint.MaxValue - 1UL;

    /// <summary>The wait to observe, or null to observe until the caller cancels.</summary>
    public static TimeSpan? For(GetServerInfoResponse? info)
    {
        // Check the case: an unset finite value reads as zero.
        var bound = info?.LifecycleBounds?.HostGracefulShutdown;
        switch (bound?.LimitCase)
        {
            case GracefulShutdownBound.LimitOneofCase.Unbounded:
                return null;
            case GracefulShutdownBound.LimitOneofCase.FiniteMilliseconds:
                var margin = (ulong)(ExitMargin.Ticks / TimeSpan.TicksPerMillisecond);
                var finite = bound.FiniteMilliseconds;
                return finite > 0 && finite <= MaximumTimerMilliseconds - margin
                    ? TimeSpan.FromTicks((long)(finite + margin) * TimeSpan.TicksPerMillisecond)
                    : Fallback;
            default:
                return Fallback;
        }
    }

    /// <summary>
    /// Waits for the exit under the caller's token, and under <paramref name="wait"/>
    /// when it is finite. Expiry and cancellation both end the wait with
    /// <see cref="OperationCanceledException"/>; the process is left as it is.
    /// </summary>
    public static async Task ObserveAsync(
        Func<CancellationToken, Task> waitForExit,
        TimeSpan? wait,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(waitForExit);
        if (wait is not { } finite)
        {
            await waitForExit(cancellationToken).ConfigureAwait(false);
            return;
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(finite);
        await waitForExit(timeout.Token).ConfigureAwait(false);
    }
}
