namespace Briosa.Worker.Control;

public enum WorkerExecutionReadinessState
{
    Unverified,
    Verifying,
    ExecutionReady,
    CompetingClientSuspected,
    OperatorRecoveryRequired
}
