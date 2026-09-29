using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class MakePointCloudsToObjectsRelationshipOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.make_point_clouds_to_objects_relationship", "Make Point Clouds to Objects Relationship",
        "briosa.RelationshipOperations", "MakePointCloudsToObjectsRelationship",
        "/briosa.RelationshipOperations/MakePointCloudsToObjectsRelationship",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MakePointCloudsToObjectsRelationshipRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionItemName,
                    CollectionItemNameMapper.Required(request.RelationshipName, "relationship_name", WorkerItemTypeValue.Relationship), "SetCollectionObjectNameArg2"),
                new("Point Clouds in Relationship", WorkerMpValueKind.CollectionObjectNameList,
                    CollectionObjectNameMapper.RequiredList(request.PointCloudsInRelationship, "point_clouds_in_relationship"), "SetCollectionObjectNameRefListArg"),
                new("Objects in Relationship", WorkerMpValueKind.CollectionObjectNameList,
                    CollectionObjectNameMapper.RequiredList(request.ObjectsInRelationship, "objects_in_relationship"), "SetCollectionObjectNameRefListArg"),
                new("Projection Options", WorkerMpValueKind.ProjectionOptions,
                    ProjectionOptionsMapper.FromRequest(request.ProjectionOptions ??
                        throw new ArgumentException("Request field 'projection_options' is required.", nameof(request))), "SetProjectionOptionsArg"),
                new("Auto Update a Vector Group?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasAutoUpdateAVectorGroup && request.AutoUpdateAVectorGroup), "SetBoolArg")
            ], []);
    }

    public static Api.MakePointCloudsToObjectsRelationshipResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
