namespace Briosa.Worker.Control;

public sealed record WorkerConnectionSnapshot(
    WorkerConnectionState State,
    WorkerExecutionReadinessState ExecutionReadinessState,
    int? StatusCode,
    int Attempt,
    int MaximumAttempts,
    string DiagnosticCode,
    DateTimeOffset TransitionedAt,
    WorkerRuntimeIdentitySnapshot? RuntimeIdentity = null,
    WorkerConnectionFailure Failure = WorkerConnectionFailure.None);
