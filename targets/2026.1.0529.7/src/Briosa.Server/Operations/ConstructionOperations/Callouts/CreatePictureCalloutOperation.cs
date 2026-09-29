using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class CreatePictureCalloutOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.create_picture_callout", "Create Picture Callout",
        "briosa.ConstructionOperations", "CreatePictureCallout", "/briosa.ConstructionOperations/CreatePictureCallout",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.CreatePictureCalloutRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Destination Callout View", WorkerMpValueKind.CollectionItemName,
                CollectionItemNameMapper.Required(request.DestinationCalloutView, "destination_callout_view"), "SetCollectionObjectNameArg2"),
            new("Picture Name", WorkerMpValueKind.CollectionItemName,
                CollectionItemNameMapper.Required(request.PictureName, "picture_name"), "SetCollectionObjectNameArg2"),
            new("View X Position", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasViewXPosition ? request.ViewXPosition : 0.4), "SetDoubleArg"),
            new("View Y Position", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasViewYPosition ? request.ViewYPosition : 0.6), "SetDoubleArg"),
            .. (request.HasScaleImagePercent
                ? new[] { new WorkerMpInputArgument("Scale Image Percent (10-200)", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.ScaleImagePercent), "SetIntegerArg") }
                : Array.Empty<WorkerMpInputArgument>()),
            .. (request.ObjectForCalloutAnchorPoint is not null
                ? new[] { new WorkerMpInputArgument("Object for Callout Anchor Point", WorkerMpValueKind.CollectionItemName,
                    CollectionItemNameMapper.Required(request.ObjectForCalloutAnchorPoint, "object_for_callout_anchor_point"), "SetCollectionObjectNameArg2") }
                : Array.Empty<WorkerMpInputArgument>())
        ], []);
    }

    public static Api.CreatePictureCalloutResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
