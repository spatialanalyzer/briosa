using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class MakeGroupToNominalGroupRelationshipOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.make_group_to_nominal_group_relationship", "Make Group to Nominal Group Relationship",
        "briosa.RelationshipOperations", "MakeGroupToNominalGroupRelationship",
        "/briosa.RelationshipOperations/MakeGroupToNominalGroupRelationship",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MakeGroupToNominalGroupRelationshipRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionItemName,
                    CollectionItemNameMapper.Required(request.RelationshipName, "relationship_name", WorkerItemTypeValue.Relationship), "SetCollectionObjectNameArg2"),
                new("Nominal Group Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.NominalGroupName, "nominal_group_name", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
                new("Measured Group Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.MeasuredGroupName, "measured_group_name", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
                new("Auto Update a Vector Group?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasAutoUpdateAVectorGroup && request.AutoUpdateAVectorGroup), "SetBoolArg"),
                new("Use Closest Point?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasUseClosestPoint ? request.UseClosestPoint : true), "SetBoolArg"),
                new("Display Closest Point Watch Window?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasDisplayClosestPointWatchWindow && request.DisplayClosestPointWatchWindow), "SetBoolArg"),
                new("Use View Zooming With Proximity?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasUseViewZoomingWithProximity && request.UseViewZoomingWithProximity), "SetBoolArg"),
                new("Ignore Points Beyond Threshold?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasIgnorePointsBeyondThreshold && request.IgnorePointsBeyondThreshold), "SetBoolArg"),
                new("Proximity Threshold?", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasProximityThreshold ? request.ProximityThreshold : 0.01), "SetDoubleArg"),
                new("Tolerance", WorkerMpValueKind.ToleranceVectorOptions,
                    ToleranceVectorOptionsMapper.Required(request.Tolerance, "tolerance"), "SetToleranceVectorOptionsArg"),
                new("Constraint", WorkerMpValueKind.ToleranceVectorOptions,
                    ToleranceVectorOptionsMapper.Required(request.Constraint, "constraint"), "SetToleranceVectorOptionsArg"),
                new("Fit Weight", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasFitWeight ? request.FitWeight : 1d), "SetDoubleArg")
            ], []);
    }

    public static Api.MakeGroupToNominalGroupRelationshipResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
