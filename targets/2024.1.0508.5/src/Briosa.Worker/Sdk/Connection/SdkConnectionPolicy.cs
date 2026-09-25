using Briosa.Worker.Control;
namespace Briosa.Worker.Sdk;

internal sealed class SdkConnectionPolicy
{
    private readonly HashSet<int> _transientStatusCodes;

    public SdkConnectionPolicy(
        int maximumAttempts,
        TimeSpan retryDelay,
        IEnumerable<int>? transientStatusCodes = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumAttempts, 1);
        if (retryDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(retryDelay),
                retryDelay,
                "The connection retry delay cannot be negative.");
        }

        MaximumAttempts = maximumAttempts;
        RetryDelay = retryDelay;
        _transientStatusCodes = transientStatusCodes?.ToHashSet() ?? [];
    }

    public int MaximumAttempts { get; }

    public TimeSpan RetryDelay { get; }

    public bool ShouldRetry(SdkConnectionResult result) =>
        result.Status == SdkConnectionStatus.Unavailable &&
        result.StatusCode is { } statusCode &&
        _transientStatusCodes.Contains(statusCode);
}
