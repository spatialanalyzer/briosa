using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class CreatePointComparisonCalloutOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.create_point_comparison_callout", "Create Point Comparison Callout",
        "briosa.ConstructionOperations", "CreatePointComparisonCallout", "/briosa.ConstructionOperations/CreatePointComparisonCallout",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.CreatePointComparisonCalloutRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Destination Callout View", WorkerMpValueKind.CollectionItemName,
                CollectionItemNameMapper.Required(request.DestinationCalloutView, "destination_callout_view"), "SetCollectionObjectNameArg2"),
            new("First Point", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.FirstPoint, "first_point"), "SetPointNameArg"),
            new("Second Point", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.SecondPoint, "second_point"), "SetPointNameArg"),
            new("View X Position", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.HasViewXPosition ? request.ViewXPosition : 0), "SetDoubleArg"),
            new("View Y Position", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.HasViewYPosition ? request.ViewYPosition : 0), "SetDoubleArg"),
            new("Show First Point Collection?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasShowFirstPointCollection && request.ShowFirstPointCollection), "SetBoolArg"),
            new("Show First Point Group?", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasShowFirstPointGroup || request.ShowFirstPointGroup), "SetBoolArg"),
            new("Show First Point Target?", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasShowFirstPointTarget || request.ShowFirstPointTarget), "SetBoolArg"),
            new("Show First Point Coordinates?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasShowFirstPointCoordinates && request.ShowFirstPointCoordinates), "SetBoolArg"),
            new("Show Second Point Collection?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasShowSecondPointCollection && request.ShowSecondPointCollection), "SetBoolArg"),
            new("Show Second Point Group?", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasShowSecondPointGroup || request.ShowSecondPointGroup), "SetBoolArg"),
            new("Show Second Point Target?", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasShowSecondPointTarget || request.ShowSecondPointTarget), "SetBoolArg"),
            new("Show Second Point Coordinates?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasShowSecondPointCoordinates && request.ShowSecondPointCoordinates), "SetBoolArg"),
            new("Show dX?", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasShowDx || request.ShowDx), "SetBoolArg"),
            new("Show dY?", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasShowDy || request.ShowDy), "SetBoolArg"),
            new("Show dZ?", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasShowDz || request.ShowDz), "SetBoolArg"),
            new("Show dMag?", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasShowDMag || request.ShowDMag), "SetBoolArg"),
            new("Additional X Comments (blank for none)", WorkerMpValueKind.Text, new WorkerTextValue(request.HasAdditionalXComments ? request.AdditionalXComments : string.Empty), "SetStringArg"),
            new("Additional Y Comments (blank for none)", WorkerMpValueKind.Text, new WorkerTextValue(request.HasAdditionalYComments ? request.AdditionalYComments : string.Empty), "SetStringArg"),
            new("Additional Z Comments (blank for none)", WorkerMpValueKind.Text, new WorkerTextValue(request.HasAdditionalZComments ? request.AdditionalZComments : string.Empty), "SetStringArg"),
            new("Additional Notes (blank for none)", WorkerMpValueKind.EditText, new WorkerStringListValue(request.AdditionalNotes), "SetEditTextArg"),
            new("Use default placement?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasUseDefaultPlacement && request.UseDefaultPlacement), "SetBoolArg")
        ], []);
    }

    public static Api.CreatePointComparisonCalloutResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
