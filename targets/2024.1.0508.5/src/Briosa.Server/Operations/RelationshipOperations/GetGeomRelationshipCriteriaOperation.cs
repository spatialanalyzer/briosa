using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class GetGeomRelationshipCriteriaOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.get_geom_relationship_criteria", "Get Geom Relationship Criteria",
        "briosa.RelationshipOperations", "GetGeomRelationshipCriteria",
        "/briosa.RelationshipOperations/GetGeomRelationshipCriteria",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("nominal", "Nominal", WorkerMpValueKind.FloatingPoint),
        new("measured", "Measured", WorkerMpValueKind.FloatingPoint),
        new("delta", "Delta", WorkerMpValueKind.FloatingPoint),
        new("low_tolerance", "Low Tolerance", WorkerMpValueKind.FloatingPoint),
        new("high_tolerance", "High Tolerance", WorkerMpValueKind.FloatingPoint),
        new("optimization_delta_weight", "Optimization: Delta Weight", WorkerMpValueKind.FloatingPoint),
        new("optimization_out_of_tolerance_weight", "Optimization: Out of Tolerance Weight", WorkerMpValueKind.FloatingPoint),
        new("is_within_tolerance", "Is within Tolerance?", WorkerMpValueKind.Text),
        new("has_uncertainty", "Has Uncertainty?", WorkerMpValueKind.Logical),
        new("uncertainty", "Uncertainty", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetGeomRelationshipCriteriaRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2"),
                new("Criteria", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.HasCriteria ? request.Criteria : "Empty"), "SetStringArg")
            ],
            [
                new("Nominal", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Measured", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Delta", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Low Tolerance", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("High Tolerance", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Optimization: Delta Weight", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Optimization: Out of Tolerance Weight", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Is within Tolerance?", WorkerMpValueKind.Text, "GetStringArg"),
                new("Has Uncertainty?", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Uncertainty", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
            ]);
    }

    public static Api.GetGeomRelationshipCriteriaResult CreateResult(SuccessfulOperationExecution completed)
    {
        var outputs = completed.Execution.OutputValues;
        return new()
        {
            Nominal = outputs[0].RequireValue<WorkerDoubleValue>().Value,
            Measured = outputs[1].RequireValue<WorkerDoubleValue>().Value,
            Delta = outputs[2].RequireValue<WorkerDoubleValue>().Value,
            LowTolerance = outputs[3].RequireValue<WorkerDoubleValue>().Value,
            HighTolerance = outputs[4].RequireValue<WorkerDoubleValue>().Value,
            OptimizationDeltaWeight = outputs[5].RequireValue<WorkerDoubleValue>().Value,
            OptimizationOutOfToleranceWeight = outputs[6].RequireValue<WorkerDoubleValue>().Value,
            IsWithinTolerance = outputs[7].RequireValue<WorkerTextValue>().Value,
            HasUncertainty = outputs[8].RequireValue<WorkerBooleanValue>().Value,
            Uncertainty = outputs[9].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }
}
