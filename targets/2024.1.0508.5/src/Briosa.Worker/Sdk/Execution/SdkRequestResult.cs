using Briosa.Worker.Control;
namespace Briosa.Worker.Sdk;

internal sealed record SdkRequestResult(
    SdkRequestStatus Status,
    WorkerMpExecutionResult? Execution,
    SdkConnectionSnapshot Connection,
    string? DiagnosticCode);
