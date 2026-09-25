using Briosa.Worker.Control;
namespace Briosa.Worker.Sdk;

internal enum SdkExecutionReadinessState
{
    Unverified,
    Verifying,
    ExecutionReady,
    CompetingClientSuspected,
    OperatorRecoveryRequired
}
