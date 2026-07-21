namespace Briosa.Worker.Sdk;

internal enum SdkConnectionStatus
{
    Connected,
    Unavailable
}

internal sealed record SdkConnectionResult(
    SdkConnectionStatus Status,
    int? StatusCode,
    string? DiagnosticCode);

internal enum SdkConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Faulted,
    Stopping
}

internal sealed record SdkConnectionSnapshot(
    SdkConnectionState State,
    string TargetHost,
    int? StatusCode,
    int Attempt,
    int MaximumAttempts,
    string DiagnosticCode,
    DateTimeOffset TransitionedAt);

internal sealed class SdkConnectionPolicy
{
    public SdkConnectionPolicy(int maximumAttempts, TimeSpan retryDelay)
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
    }

    public int MaximumAttempts { get; }

    public TimeSpan RetryDelay { get; }
}

internal enum SdkRequestStatus
{
    Completed,
    Unavailable
}

internal sealed record SdkRequestResult(
    SdkRequestStatus Status,
    SdkExecutionResult? Execution,
    SdkConnectionSnapshot Connection,
    string? DiagnosticCode);

internal enum SdkArgumentKind
{
    Logical,
    WholeNumber,
    FloatingPoint,
    Text
}

internal sealed record SdkArgument(
    string Name,
    SdkArgumentKind Kind,
    bool? BooleanValue = null,
    int? IntegerValue = null,
    double? DoubleValue = null,
    string? StringValue = null);

internal sealed class SdkCommand
{
    public SdkCommand(string operationId)
        : this(operationId, operationId, [])
    {
    }

    public SdkCommand(
        string operationId,
        string stepName,
        IReadOnlyList<SdkArgument> arguments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(stepName);
        ArgumentNullException.ThrowIfNull(arguments);
        OperationId = operationId;
        StepName = stepName;
        Arguments = [.. arguments];
    }

    public string OperationId { get; }

    public string StepName { get; }

    public IReadOnlyList<SdkArgument> Arguments { get; }
}

internal sealed record SdkMpResult(
    bool Succeeded,
    int ResultCode,
    string? DiagnosticCode);

internal sealed record SdkExecutionResult(
    bool ExecuteStepReturned,
    SdkMpResult MpResult,
    TimeSpan Duration);
