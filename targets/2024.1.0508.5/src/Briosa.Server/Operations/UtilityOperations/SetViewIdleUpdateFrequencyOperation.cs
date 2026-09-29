using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class SetViewIdleUpdateFrequencyOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.set_view_idle_update_frequency", "Set View Idle Update Frequency",
        "briosa.UtilityOperations", "SetViewIdleUpdateFrequency",
        "/briosa.UtilityOperations/SetViewIdleUpdateFrequency", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetViewIdleUpdateFrequencyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Idle Count", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.IdleCount), "SetIntegerArg")], []);
    }

    public static Api.SetViewIdleUpdateFrequencyResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
