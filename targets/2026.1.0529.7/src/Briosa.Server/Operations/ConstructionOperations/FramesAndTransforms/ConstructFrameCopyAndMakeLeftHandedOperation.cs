using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructFrameCopyAndMakeLeftHandedOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_frame_copy_and_make_left_handed",
        "Construct Frame - Copy And Make Left Handed", "briosa.ConstructionOperations",
        "ConstructFrameCopyAndMakeLeftHanded", "/briosa.ConstructionOperations/ConstructFrameCopyAndMakeLeftHanded",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructFrameCopyAndMakeLeftHandedRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var inputs = new List<WorkerMpInputArgument>
        {
            new("Reference Frame", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ReferenceFrame, "reference_frame", WorkerObjectTypeValue.Frame),
                "SetCollectionObjectNameArg2")
        };
        if (request.FrameName is not null)
            inputs.Add(new("Frame Name (Optional)", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.FrameName, "frame_name"), "SetCollectionObjectNameArg2"));
        inputs.Add(new("Axis to reverse", WorkerMpValueKind.AxisIdentifier,
            FrameConstructionChoiceMapper.FrameAxis(request.AxisToReverse), "SetAxisNameArg"));
        return new(Descriptor.OperationId, Descriptor.MpStep, inputs, []);
    }

    public static Api.ConstructFrameCopyAndMakeLeftHandedResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
