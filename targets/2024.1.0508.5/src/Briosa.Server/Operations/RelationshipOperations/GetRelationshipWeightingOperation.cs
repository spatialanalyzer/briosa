using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class GetRelationshipWeightingOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.get_relationship_weighting", "Get Relationship Weighting",
        "briosa.RelationshipOperations", "GetRelationshipWeighting",
        "/briosa.RelationshipOperations/GetRelationshipWeighting",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [new("weight", "Weight", WorkerMpValueKind.FloatingPoint)];

    public static WorkerMpCommand CreateCommand(Api.GetRelationshipWeightingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2")],
            [new("Weight", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")]);
    }

    public static Api.GetRelationshipWeightingResult CreateResult(SuccessfulOperationExecution completed) =>
        new()
        {
            Weight = completed.Execution.OutputValues[0].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
}
