using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class CreatePointCalloutOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.create_point_callout", "Create Point Callout",
        "briosa.ConstructionOperations", "CreatePointCallout", "/briosa.ConstructionOperations/CreatePointCallout",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.CreatePointCalloutRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var coordinateSystem = request.HasDesiredCoordinateSystem
            ? request.DesiredCoordinateSystem : Api.CoordinateSystemType.Unspecified;
        if (coordinateSystem == Api.CoordinateSystemType.Unspecified || !Enum.IsDefined(coordinateSystem))
            throw new ArgumentException("Request field 'desired_coordinate_system' is required and must be supported.", nameof(request));

        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Destination Callout View", WorkerMpValueKind.CollectionItemName,
                CollectionItemNameMapper.Required(request.DestinationCalloutView, "destination_callout_view"), "SetCollectionObjectNameArg2"),
            new("Point", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.Point, "point"), "SetPointNameArg"),
            new("View X Position", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasViewXPosition ? request.ViewXPosition : 0), "SetDoubleArg"),
            new("View Y Position", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasViewYPosition ? request.ViewYPosition : 0), "SetDoubleArg"),
            new("Show Point Collection?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasShowPointCollection && request.ShowPointCollection), "SetBoolArg"),
            new("Show Point Group?", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasShowPointGroup || request.ShowPointGroup), "SetBoolArg"),
            new("Show Point Target?", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasShowPointTarget || request.ShowPointTarget), "SetBoolArg"),
            new("Show X (R)?", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasShowX || request.ShowX), "SetBoolArg"),
            new("Show Y (Theta)?", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasShowY || request.ShowY), "SetBoolArg"),
            new("Show Z (Phi)?", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasShowZ || request.ShowZ), "SetBoolArg"),
            new("Show Units?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasShowUnits && request.ShowUnits), "SetBoolArg"),
            new("Show Ux (Ur)?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasShowUx && request.ShowUx), "SetBoolArg"),
            new("Show Uy (Utheta)?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasShowUy && request.ShowUy), "SetBoolArg"),
            new("Show Uz (Uphi)?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasShowUz && request.ShowUz), "SetBoolArg"),
            new("Show Umag?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasShowUmag && request.ShowUmag), "SetBoolArg"),
            new("Desired Coordinate System", WorkerMpValueKind.CoordinateSystemType,
                new WorkerChoiceValue<WorkerCoordinateSystemTypeValue>((WorkerCoordinateSystemTypeValue)((int)coordinateSystem - 1)), "SetCoordinateSystemTypeArg"),
            new("Notes (blank for none)", WorkerMpValueKind.EditText, new WorkerStringListValue(request.Notes), "SetEditTextArg"),
            new("Use default placement?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasUseDefaultPlacement && request.UseDefaultPlacement), "SetBoolArg")
        ], []);
    }

    public static Api.CreatePointCalloutResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
