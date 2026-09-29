using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class SetGeomRelationshipCardinalPointsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.set_geom_relationship_cardinal_points", "Set Geom Relationship Cardinal Points",
        "briosa.RelationshipOperations", "SetGeomRelationshipCardinalPoints",
        "/briosa.RelationshipOperations/SetGeomRelationshipCardinalPoints",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetGeomRelationshipCardinalPointsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2"),
                new("Create Cardinal Pts when Fitting?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasCreateCardinalPtsWhenFitting ? request.CreateCardinalPtsWhenFitting : true), "SetBoolArg"),
                new("Prefix Cardinal Pts name with Rel name?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasPrefixCardinalPtsNameWithRelName ? request.PrefixCardinalPtsNameWithRelName : true), "SetBoolArg"),
                new("Cardinal Pts Group Name", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.HasCardinalPtsGroupName ? request.CardinalPtsGroupName : "GR-Cardinal Pts"), "SetStringArg")
            ], []);
    }

    public static Api.SetGeomRelationshipCardinalPointsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
