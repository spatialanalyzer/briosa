using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class GetRelationshipTypeOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.get_relationship_type", "Get Relationship Type",
        "briosa.RelationshipOperations", "GetRelationshipType",
        "/briosa.RelationshipOperations/GetRelationshipType",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [new("relationship_type", "Relationship Type", WorkerMpValueKind.Text)];

    public static WorkerMpCommand CreateCommand(Api.GetRelationshipTypeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2")],
            [new("Relationship Type", WorkerMpValueKind.Text, "GetStringArg")]);
    }

    public static Api.GetRelationshipTypeResult CreateResult(SuccessfulOperationExecution completed) =>
        new()
        {
            RelationshipType = completed.Execution.OutputValues[0].RequireValue<WorkerTextValue>().Value,
            Execution = completed.Details
        };
}
