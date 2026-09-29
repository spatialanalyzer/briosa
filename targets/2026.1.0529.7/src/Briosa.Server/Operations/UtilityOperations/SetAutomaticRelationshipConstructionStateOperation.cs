using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class SetAutomaticRelationshipConstructionStateOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.set_automatic_relationship_construction_state", "Set Automatic Relationship Construction State", "briosa.UtilityOperations",
        "SetAutomaticRelationshipConstructionState", "/briosa.UtilityOperations/SetAutomaticRelationshipConstructionState", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetAutomaticRelationshipConstructionStateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Active?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.Active), "SetBoolArg")], []);
    }

    public static Api.SetAutomaticRelationshipConstructionStateResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
