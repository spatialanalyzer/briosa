using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class GeomRelationshipIgnoreInputPointsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.geom_relationship_ignore_input_points", "Geom Relationship Ignore Input Points",
        "briosa.RelationshipOperations", "GeomRelationshipIgnoreInputPoints",
        "/briosa.RelationshipOperations/GeomRelationshipIgnoreInputPoints",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe,
        ["fixture_validation_pending"]);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.GeomRelationshipIgnoreInputPointsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2")], []);
    }

    public static Api.GeomRelationshipIgnoreInputPointsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
