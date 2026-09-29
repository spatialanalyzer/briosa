using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class GeomRelationshipReuseIgnoredInputPointsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.geom_relationship_reuse_ignored_input_points", "Geom Relationship Reuse Ignored Input Points",
        "briosa.RelationshipOperations", "GeomRelationshipReuseIgnoredInputPoints",
        "/briosa.RelationshipOperations/GeomRelationshipReuseIgnoredInputPoints",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.GeomRelationshipReuseIgnoredInputPointsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2")], []);
    }

    public static Api.GeomRelationshipReuseIgnoredInputPointsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
