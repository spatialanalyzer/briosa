using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class SetGroupToNominalGroupViewZoomingOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.set_group_to_nominal_group_view_zooming",
        "Set Group To Nominal Group View Zooming",
        "briosa.RelationshipOperations", "SetGroupToNominalGroupViewZooming",
        "/briosa.RelationshipOperations/SetGroupToNominalGroupViewZooming",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetGroupToNominalGroupViewZoomingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var relationship = CollectionItemNameMapper.Required(
            request.RelationshipName, "relationship_name", WorkerItemTypeValue.Relationship);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionItemName,
                    relationship, "SetCollectionObjectNameArg2"),
                new("Use Closest Point", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasUseClosestPoint || request.UseClosestPoint), "SetBoolArg"),
                new("Show Closest Point Watch Window", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasShowClosestPointWatchWindow && request.ShowClosestPointWatchWindow), "SetBoolArg"),
                new("Use View Zooming", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasUseViewZooming || request.UseViewZooming), "SetBoolArg"),
                new("Ignore Points Beyond Threshold", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasIgnorePointsBeyondThreshold || request.IgnorePointsBeyondThreshold), "SetBoolArg"),
                new("Proximity Threshold", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasProximityThreshold ? request.ProximityThreshold : 0.01), "SetDoubleArg")
            ], []);
    }

    public static Api.SetGroupToNominalGroupViewZoomingResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
