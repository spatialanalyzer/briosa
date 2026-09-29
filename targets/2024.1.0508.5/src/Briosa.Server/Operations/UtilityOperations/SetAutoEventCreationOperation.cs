using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class SetAutoEventCreationOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.set_auto_event_creation", "Set Auto Event Creation", "briosa.UtilityOperations",
        "SetAutoEventCreation", "/briosa.UtilityOperations/SetAutoEventCreation", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetAutoEventCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Active?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.Active), "SetBoolArg")], []);
    }

    public static Api.SetAutoEventCreationResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
