using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class GetRelationshipSigmoidalGapFitConstraintsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.get_relationship_sigmoidal_gap_fit_constraints",
        "Get Relationship Sigmoidal Gap Fit Constraints",
        "briosa.RelationshipOperations", "GetRelationshipSigmoidalGapFitConstraints",
        "/briosa.RelationshipOperations/GetRelationshipSigmoidalGapFitConstraints",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("use_sigmoidal_gap_constraints", "Use Sigmoidal Gap Constraints", WorkerMpValueKind.Logical),
        new("minimum_gap_boundary", "Minimum Gap Boundary", WorkerMpValueKind.FloatingPoint),
        new("minimum_gap_weight", "Minimum Gap Weight", WorkerMpValueKind.FloatingPoint),
        new("maximum_gap_boundary", "Maximum Gap Boundary", WorkerMpValueKind.FloatingPoint),
        new("maximum_gap_weight", "Maximum Gap Weight", WorkerMpValueKind.FloatingPoint),
        new("nominal_gap", "Nominal Gap", WorkerMpValueKind.FloatingPoint),
        new("nominal_gap_weight", "Nominal Gap Weight", WorkerMpValueKind.FloatingPoint),
        new("gradient_steepness_factor", "Gradient Steepness Factor", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetRelationshipSigmoidalGapFitConstraintsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var relationship = CollectionItemNameMapper.Required(
            request.RelationshipName, "relationship_name", WorkerItemTypeValue.Relationship);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Relationship Name", WorkerMpValueKind.CollectionItemName,
                relationship, "SetCollectionObjectNameArg2")],
            [
                new("Use Sigmoidal Gap Constraints", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Minimum Gap Boundary", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Minimum Gap Weight", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Maximum Gap Boundary", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Maximum Gap Weight", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Nominal Gap", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Nominal Gap Weight", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Gradient Steepness Factor", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
            ]);
    }

    public static Api.GetRelationshipSigmoidalGapFitConstraintsResult CreateResult(SuccessfulOperationExecution completed)
    {
        var outputs = completed.Execution.OutputValues;
        return new()
        {
            Constraints = new()
            {
                UseSigmoidalGapConstraints = outputs[0].RequireValue<WorkerBooleanValue>().Value,
                MinimumGapBoundary = outputs[1].RequireValue<WorkerDoubleValue>().Value,
                MinimumGapWeight = outputs[2].RequireValue<WorkerDoubleValue>().Value,
                MaximumGapBoundary = outputs[3].RequireValue<WorkerDoubleValue>().Value,
                MaximumGapWeight = outputs[4].RequireValue<WorkerDoubleValue>().Value,
                NominalGap = outputs[5].RequireValue<WorkerDoubleValue>().Value,
                NominalGapWeight = outputs[6].RequireValue<WorkerDoubleValue>().Value,
                GradientSteepnessFactor = outputs[7].RequireValue<WorkerDoubleValue>().Value
            },
            Execution = completed.Details
        };
    }
}
