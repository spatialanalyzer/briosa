using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class CreateMinMaxVectorGroupCalloutOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.create_min_max_vector_group_callout", "Create Min/Max Vector Group Callout",
        "briosa.ConstructionOperations", "CreateMinMaxVectorGroupCallout", "/briosa.ConstructionOperations/CreateMinMaxVectorGroupCallout",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.CreateMinMaxVectorGroupCalloutRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Destination Callout View", WorkerMpValueKind.CollectionItemName,
                CollectionItemNameMapper.Required(request.DestinationCalloutView, "destination_callout_view"), "SetCollectionObjectNameArg2"),
            new("Vector Group Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.VectorGroupName, "vector_group_name", WorkerObjectTypeValue.VectorGroup), "SetCollectionObjectNameArg2"),
            new("Number of vectors with Highest Mag?", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.HasNumberOfVectorsWithHighestMag ? request.NumberOfVectorsWithHighestMag : 1), "SetIntegerArg"),
            new("Number of vectors with Lowest Mag?", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.HasNumberOfVectorsWithLowestMag ? request.NumberOfVectorsWithLowestMag : 1), "SetIntegerArg"),
            new("Show Collection?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasShowCollection && request.ShowCollection), "SetBoolArg"),
            new("Show Vector Group?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasShowVectorGroup && request.ShowVectorGroup), "SetBoolArg"),
            new("Show Vector Name?", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasShowVectorName || request.ShowVectorName), "SetBoolArg"),
            new("Show dX?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasShowDx && request.ShowDx), "SetBoolArg"),
            new("Show dY?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasShowDy && request.ShowDy), "SetBoolArg"),
            new("Show dZ?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasShowDz && request.ShowDz), "SetBoolArg"),
            new("Show dMag?", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasShowDMag || request.ShowDMag), "SetBoolArg"),
            new("Show Tolerance Color?", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasShowToleranceColor || request.ShowToleranceColor), "SetBoolArg"),
            new("Tolerance Color Blue(+)/Green/Red(-)?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasToleranceColorBlueGreenRed && request.ToleranceColorBlueGreenRed), "SetBoolArg"),
            new("Show Out of Tolerance Value?", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasShowOutOfToleranceValue || request.ShowOutOfToleranceValue), "SetBoolArg"),
            new("Show Tolerance Range?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasShowToleranceRange && request.ShowToleranceRange), "SetBoolArg"),
            new("Show Vector Color?", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasShowVectorColor || request.ShowVectorColor), "SetBoolArg"),
            new("Show Start Point?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasShowStartPoint && request.ShowStartPoint), "SetBoolArg"),
            new("Show End Point?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasShowEndPoint && request.ShowEndPoint), "SetBoolArg"),
            new("Show Units?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasShowUnits && request.ShowUnits), "SetBoolArg"),
            new("Attach Callout to End Point?", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasAttachCalloutToEndPoint || request.AttachCalloutToEndPoint), "SetBoolArg"),
            new("Use default placement?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasUseDefaultPlacement && request.UseDefaultPlacement), "SetBoolArg")
        ], []);
    }

    public static Api.CreateMinMaxVectorGroupCalloutResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
