using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructFrameKnownOriginObjectDirectionObjectDirectionOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_frame_known_origin_object_direction_object_direction",
        "Construct Frame, Known Origin, Object Direction, Object Direction", "briosa.ConstructionOperations",
        "ConstructFrameKnownOriginObjectDirectionObjectDirection",
        "/briosa.ConstructionOperations/ConstructFrameKnownOriginObjectDirectionObjectDirection",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructFrameKnownOriginObjectDirectionObjectDirectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var inputs = new List<WorkerMpInputArgument>
        {
            new("Known Point", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.KnownPoint, "known_point"), "SetPointNameArg"),
            new("Known Point Value in New Frame", WorkerMpValueKind.Vector,
                VectorMapper.Required(request.KnownPointValueInNewFrame, "known_point_value_in_new_frame"), "SetVectorArg"),
            new("Primary Axis Object", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.PrimaryAxisObject, "primary_axis_object"),
                "SetCollectionObjectNameArg2"),
            new("Primary Axis Defines Which Axis", WorkerMpValueKind.AxisIdentifier,
                FrameConstructionChoiceMapper.Axis(request.PrimaryAxisDefinesWhichAxis), "SetAxisNameArg"),
            new("Secondary Axis Object", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.SecondaryAxisObject, "secondary_axis_object"),
                "SetCollectionObjectNameArg2"),
            new("Secondary Axis Defines Which Axis", WorkerMpValueKind.AxisIdentifier,
                FrameConstructionChoiceMapper.Axis(request.SecondaryAxisDefinesWhichAxis), "SetAxisNameArg")
        };
        if (request.FrameName is not null)
            inputs.Add(new("Frame Name (Optional)", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.FrameName, "frame_name", WorkerObjectTypeValue.Frame),
                "SetCollectionObjectNameArg2"));
        return new(Descriptor.OperationId, Descriptor.MpStep, inputs, []);
    }

    public static Api.ConstructFrameKnownOriginObjectDirectionObjectDirectionResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
