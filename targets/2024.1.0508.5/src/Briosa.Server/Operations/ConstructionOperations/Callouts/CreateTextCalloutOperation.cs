using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class CreateTextCalloutOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.create_text_callout", "Create Text Callout",
        "briosa.ConstructionOperations", "CreateTextCallout", "/briosa.ConstructionOperations/CreateTextCallout",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.CreateTextCalloutRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Destination Callout View", WorkerMpValueKind.CollectionItemName,
                CollectionItemNameMapper.Required(request.DestinationCalloutView, "destination_callout_view"), "SetCollectionObjectNameArg2"),
            .. (request.Text.Count != 0
                ? new[] { new WorkerMpInputArgument("Text", WorkerMpValueKind.EditText,
                    new WorkerStringListValue(request.Text), "SetEditTextArg") }
                : Array.Empty<WorkerMpInputArgument>()),
            new("View X Position", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasViewXPosition ? request.ViewXPosition : 0.4), "SetDoubleArg"),
            new("View Y Position", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasViewYPosition ? request.ViewYPosition : 0.6), "SetDoubleArg"),
            .. (request.CalloutAnchorPoint is not null
                ? new[] { new WorkerMpInputArgument("Callout Anchor Point (Optional)", WorkerMpValueKind.PointName,
                    PointNameMapper.Required(request.CalloutAnchorPoint, "callout_anchor_point"), "SetPointNameArg") }
                : Array.Empty<WorkerMpInputArgument>())
        ], []);
    }

    public static Api.CreateTextCalloutResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
