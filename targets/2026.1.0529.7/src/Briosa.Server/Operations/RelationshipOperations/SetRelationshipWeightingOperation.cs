using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class SetRelationshipWeightingOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.set_relationship_weighting", "Set Relationship Weighting",
        "briosa.RelationshipOperations", "SetRelationshipWeighting",
        "/briosa.RelationshipOperations/SetRelationshipWeighting",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetRelationshipWeightingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2"),
                new("Weight", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasWeight ? request.Weight : 0), "SetDoubleArg")
            ], []);
    }

    public static Api.SetRelationshipWeightingResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
