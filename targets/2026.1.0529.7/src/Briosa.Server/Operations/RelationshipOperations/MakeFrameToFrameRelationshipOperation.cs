using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class MakeFrameToFrameRelationshipOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.make_frame_to_frame_relationship", "Make Frame to Frame Relationship",
        "briosa.RelationshipOperations", "MakeFrameToFrameRelationship",
        "/briosa.RelationshipOperations/MakeFrameToFrameRelationship",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MakeFrameToFrameRelationshipRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionItemName,
                    CollectionItemNameMapper.Required(request.RelationshipName, "relationship_name", WorkerItemTypeValue.Relationship), "SetCollectionObjectNameArg2"),
                new("First Frame Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.FirstFrameName, "first_frame_name", WorkerObjectTypeValue.Frame), "SetCollectionObjectNameArg2"),
                new("Second Frame Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.SecondFrameName, "second_frame_name", WorkerObjectTypeValue.Frame), "SetCollectionObjectNameArg2"),
                new("Orientation Tolerance", WorkerMpValueKind.ToleranceScalarOptions,
                    ToleranceScalarOptionsMapper.ToWorker(request.OrientationTolerance ??
                        throw new ArgumentException("Request field 'orientation_tolerance' is required.", nameof(request))), "SetToleranceScalarOptionsArg"),
                new("Position Tolerance", WorkerMpValueKind.ToleranceVectorOptions,
                    ToleranceVectorOptionsMapper.Required(request.PositionTolerance, "position_tolerance"), "SetToleranceVectorOptionsArg")
            ], []);
    }

    public static Api.MakeFrameToFrameRelationshipResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
