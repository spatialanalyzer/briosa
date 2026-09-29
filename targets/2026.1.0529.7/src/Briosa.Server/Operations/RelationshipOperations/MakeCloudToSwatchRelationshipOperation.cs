using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class MakeCloudToSwatchRelationshipOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.make_cloud_to_swatch_relationship", "Make Cloud to Swatch Relationship",
        "briosa.RelationshipOperations", "MakeCloudToSwatchRelationship",
        "/briosa.RelationshipOperations/MakeCloudToSwatchRelationship",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MakeCloudToSwatchRelationshipRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionItemName,
                    CollectionItemNameMapper.Required(request.RelationshipName, "relationship_name", WorkerItemTypeValue.Relationship), "SetCollectionObjectNameArg2"),
                new("Input Cloud Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.InputCloudName, "input_cloud_name", WorkerObjectTypeValue.Cloud), "SetCollectionObjectNameArg2"),
                new("Surface Face List", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.HasSurfaceFaceList ? request.SurfaceFaceList : "Empty"), "SetStringArg"),
                new("Reference Point", WorkerMpValueKind.PointName,
                    PointNameMapper.Required(request.ReferencePoint, "reference_point"), "SetPointNameArg"),
                new("Maximum Radial Offset", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasMaximumRadialOffset ? request.MaximumRadialOffset : 0.125), "SetDoubleArg"),
                new("Minimum Axial Offset", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasMinimumAxialOffset ? request.MinimumAxialOffset : -0.125), "SetDoubleArg"),
                new("Maximum Axial Offset", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasMaximumAxialOffset ? request.MaximumAxialOffset : 0.125), "SetDoubleArg"),
                new("Cardinal Pt Group Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.CardinalPointGroupName, "cardinal_point_group_name", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2")
            ], []);
    }

    public static Api.MakeCloudToSwatchRelationshipResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
