using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructFrameAverageOfOtherObjectFramesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_frame_average_of_other_object_frames",
        "Construct Frame - Average of Other Object Frames", "briosa.ConstructionOperations",
        "ConstructFrameAverageOfOtherObjectFrames",
        "/briosa.ConstructionOperations/ConstructFrameAverageOfOtherObjectFrames", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructFrameAverageOfOtherObjectFramesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var inputs = new List<WorkerMpInputArgument>
        {
            new("Objects", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.Objects, "objects"), "SetCollectionObjectNameRefListArg")
        };
        if (request.FrameName is not null)
            inputs.Add(new("Frame Name (Optional)", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.FrameName, "frame_name", WorkerObjectTypeValue.Frame),
                "SetCollectionObjectNameArg2"));
        return new(Descriptor.OperationId, Descriptor.MpStep, inputs, []);
    }

    public static Api.ConstructFrameAverageOfOtherObjectFramesResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
