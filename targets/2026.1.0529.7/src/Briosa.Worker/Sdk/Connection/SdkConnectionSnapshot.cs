using Briosa.Worker.Control;
namespace Briosa.Worker.Sdk;

internal sealed record SdkConnectionSnapshot(
    SdkConnectionState State,
    string TargetHost,
    int? StatusCode,
    int Attempt,
    int MaximumAttempts,
    string DiagnosticCode,
    DateTimeOffset TransitionedAt,
    SdkExecutionReadinessState ExecutionReadinessState =
        SdkExecutionReadinessState.Unverified,
    string? ActivatedSdkVersion = null,
    WorkerConnectionFailure Failure = WorkerConnectionFailure.None);
