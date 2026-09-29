using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class CreateVectorCalloutOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.create_vector_callout", "Create Vector Callout",
        "briosa.ConstructionOperations", "CreateVectorCallout", "/briosa.ConstructionOperations/CreateVectorCallout",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.CreateVectorCalloutRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Destination Callout View", WorkerMpValueKind.CollectionItemName,
                CollectionItemNameMapper.Required(request.DestinationCalloutView, "destination_callout_view"), "SetCollectionObjectNameArg2"),
            new("Vector Group Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.VectorGroupName, "vector_group_name", WorkerObjectTypeValue.VectorGroup), "SetCollectionObjectNameArg2"),
            new("Vector Name", WorkerMpValueKind.Text, new WorkerTextValue(request.VectorName), "SetStringArg"),
            new("View X Position", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.HasViewXPosition ? request.ViewXPosition : 0), "SetDoubleArg"),
            new("View Y Position", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.HasViewYPosition ? request.ViewYPosition : 0), "SetDoubleArg"),
            new("Show Collection?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasShowCollection && request.ShowCollection), "SetBoolArg"),
            new("Show Vector Group?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasShowVectorGroup && request.ShowVectorGroup), "SetBoolArg"),
            new("Show Vector Name?", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasShowVectorName || request.ShowVectorName), "SetBoolArg"),
            new("Show dX?", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasShowDx || request.ShowDx), "SetBoolArg"),
            new("Show dY?", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasShowDy || request.ShowDy), "SetBoolArg"),
            new("Show dZ?", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasShowDz || request.ShowDz), "SetBoolArg"),
            new("Show dMag?", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasShowDMag || request.ShowDMag), "SetBoolArg"),
            new("Show Tolerance Color?", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasShowToleranceColor || request.ShowToleranceColor), "SetBoolArg"),
            new("Show Out of Tolerance Value?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasShowOutOfToleranceValue && request.ShowOutOfToleranceValue), "SetBoolArg"),
            new("Show Tolerance Range?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasShowToleranceRange && request.ShowToleranceRange), "SetBoolArg"),
            new("Show Vector Color?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasShowVectorColor && request.ShowVectorColor), "SetBoolArg"),
            new("Show Start Point?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasShowStartPoint && request.ShowStartPoint), "SetBoolArg"),
            new("Show End Point?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasShowEndPoint && request.ShowEndPoint), "SetBoolArg"),
            new("Show Units?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasShowUnits && request.ShowUnits), "SetBoolArg"),
            new("Additional Notes (blank for none)", WorkerMpValueKind.EditText, new WorkerStringListValue(request.AdditionalNotes), "SetEditTextArg"),
            new("Attach Callout to End Point?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasAttachCalloutToEndPoint && request.AttachCalloutToEndPoint), "SetBoolArg"),
            new("Use default placement?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasUseDefaultPlacement && request.UseDefaultPlacement), "SetBoolArg")
        ], []);
    }

    public static Api.CreateVectorCalloutResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
