using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class MakePointsToObjectsRelationshipOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.make_points_to_objects_relationship", "Make Points to Objects Relationship",
        "briosa.RelationshipOperations", "MakePointsToObjectsRelationship",
        "/briosa.RelationshipOperations/MakePointsToObjectsRelationship",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MakePointsToObjectsRelationshipRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionItemName,
                    CollectionItemNameMapper.Required(request.RelationshipName, "relationship_name", WorkerItemTypeValue.Relationship), "SetCollectionObjectNameArg2"),
                new("Points in Relationship", WorkerMpValueKind.PointNameList,
                    PointNameMapper.RequiredList(request.PointsInRelationship, "points_in_relationship"), "SetPointNameRefListArg"),
                new("Objects in Relationship", WorkerMpValueKind.CollectionObjectNameList,
                    CollectionObjectNameMapper.RequiredList(request.ObjectsInRelationship, "objects_in_relationship"), "SetCollectionObjectNameRefListArg"),
                new("Projection Options", WorkerMpValueKind.ProjectionOptions,
                    ProjectionOptionsMapper.FromRequest(request.ProjectionOptions ??
                        throw new ArgumentException("Request field 'projection_options' is required.", nameof(request))), "SetProjectionOptionsArg"),
                new("Auto Update a Vector Group?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasAutoUpdateAVectorGroup && request.AutoUpdateAVectorGroup), "SetBoolArg")
            ], []);
    }

    public static Api.MakePointsToObjectsRelationshipResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
