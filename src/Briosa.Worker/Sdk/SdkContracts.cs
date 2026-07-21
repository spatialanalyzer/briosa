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

internal sealed class SdkCommand
{
    public SdkCommand(string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        OperationId = operationId;
    }

    public string OperationId { get; }
}

internal sealed record SdkMpResult(
    bool Succeeded,
    int ResultCode,
    string? DiagnosticCode);

internal sealed record SdkExecutionResult(
    bool ExecuteStepReturned,
    SdkMpResult MpResult);
