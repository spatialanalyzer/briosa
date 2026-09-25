using Briosa.Worker.Control;
namespace Briosa.Worker.Sdk;

internal sealed record SdkConnectionResult(
    SdkConnectionStatus Status,
    int? StatusCode,
    string? DiagnosticCode,
    WorkerConnectionFailure Failure = WorkerConnectionFailure.None);
