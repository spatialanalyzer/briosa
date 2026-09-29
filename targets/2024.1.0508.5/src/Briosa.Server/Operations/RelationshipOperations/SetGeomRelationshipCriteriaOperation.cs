using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class SetGeomRelationshipCriteriaOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.set_geom_relationship_criteria", "Set Geom Relationship Criteria",
        "briosa.RelationshipOperations", "SetGeomRelationshipCriteria",
        "/briosa.RelationshipOperations/SetGeomRelationshipCriteria",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetGeomRelationshipCriteriaRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2"),
                new("Criteria", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.HasCriteria ? request.Criteria : "Empty"), "SetStringArg"),
                new("Show in Report", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasShowInReport || request.ShowInReport), "SetBoolArg"),
                new("Tolerance Options", WorkerMpValueKind.ToleranceScalarOptions,
                    ToleranceScalarOptionsMapper.ToWorker(request.ToleranceOptions), "SetToleranceScalarOptionsArg"),
                new("Optimization: Delta Weight", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasOptimizationDeltaWeight ? request.OptimizationDeltaWeight : 0), "SetDoubleArg"),
                new("Optimization: Out of Tolerance Weight", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasOptimizationOutOfToleranceWeight ? request.OptimizationOutOfToleranceWeight : 0), "SetDoubleArg")
            ], []);
    }

    public static Api.SetGeomRelationshipCriteriaResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
