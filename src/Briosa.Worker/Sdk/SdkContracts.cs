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

internal enum SdkValueKind
{
    Logical,
    WholeNumber,
    FloatingPoint,
    Text,
    PointName,
    Vector,
    ToleranceVectorOptions
}

internal sealed record SdkPointNameValue(
    string CollectionName,
    string GroupName,
    string TargetName);

internal sealed record SdkVectorValue(double X, double Y, double Z);

internal sealed record SdkToleranceLimit(bool Enabled, double Value);

internal sealed record SdkToleranceVectorOptionsValue(
    SdkToleranceLimit HighX,
    SdkToleranceLimit HighY,
    SdkToleranceLimit HighZ,
    SdkToleranceLimit HighMagnitude,
    SdkToleranceLimit LowX,
    SdkToleranceLimit LowY,
    SdkToleranceLimit LowZ,
    SdkToleranceLimit LowMagnitude);

internal sealed record SdkInputArgument(
    string Name,
    SdkValueKind Kind,
    bool? BooleanValue = null,
    int? IntegerValue = null,
    double? DoubleValue = null,
    string? StringValue = null,
    SdkPointNameValue? PointNameValue = null,
    SdkVectorValue? VectorValue = null,
    SdkToleranceVectorOptionsValue? ToleranceVectorOptionsValue = null,
    string? SdkBinding = null);

internal sealed record SdkOutputArgument(
    string Name,
    SdkValueKind Kind,
    string? SdkBinding = null);

internal sealed record SdkOutputValue(
    string Name,
    SdkValueKind Kind,
    bool Retrieved,
    bool? BooleanValue = null,
    int? IntegerValue = null,
    double? DoubleValue = null,
    string? StringValue = null,
    SdkPointNameValue? PointNameValue = null,
    SdkVectorValue? VectorValue = null,
    SdkToleranceVectorOptionsValue? ToleranceVectorOptionsValue = null);

internal sealed class SdkCommand
{
    public SdkCommand(string operationId)
        : this(operationId, operationId, [], [])
    {
    }

    public SdkCommand(
        string operationId,
        string stepName,
        IReadOnlyList<SdkInputArgument> inputArguments,
        IReadOnlyList<SdkOutputArgument> outputArguments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(stepName);
        ArgumentNullException.ThrowIfNull(inputArguments);
        ArgumentNullException.ThrowIfNull(outputArguments);
        OperationId = operationId;
        StepName = stepName;
        InputArguments = [.. inputArguments];
        OutputArguments = [.. outputArguments];
    }

    public string OperationId { get; }

    public string StepName { get; }

    public IReadOnlyList<SdkInputArgument> InputArguments { get; }

    public IReadOnlyList<SdkOutputArgument> OutputArguments { get; }
}

internal sealed record SdkMpResult(
    bool Succeeded,
    int ResultCode,
    string? DiagnosticCode);

internal sealed record SdkExecutionResult(
    bool ExecuteStepReturned,
    SdkMpResult MpResult,
    TimeSpan Duration,
    IReadOnlyList<SdkOutputValue> OutputValues,
    string? DiagnosticCode);
