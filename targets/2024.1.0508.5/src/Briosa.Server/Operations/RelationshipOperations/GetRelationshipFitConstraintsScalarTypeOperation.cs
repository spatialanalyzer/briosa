using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class GetRelationshipFitConstraintsScalarTypeOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.get_relationship_fit_constraints_scalar_type",
        "Get Relationship Fit Constraints (Scalar Type)",
        "briosa.RelationshipOperations", "GetRelationshipFitConstraintsScalarType",
        "/briosa.RelationshipOperations/GetRelationshipFitConstraintsScalarType",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("use_high_tolerance", "Use High Tolerance?", WorkerMpValueKind.Logical),
        new("high_tolerance", "High Tolerance", WorkerMpValueKind.FloatingPoint),
        new("use_low_tolerance", "Use Low Tolerance?", WorkerMpValueKind.Logical),
        new("low_tolerance", "Low Tolerance", WorkerMpValueKind.FloatingPoint),
        new("fit_constraint_options", "Fit Constraint Options", WorkerMpValueKind.FitConstraintScalarOptions)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetRelationshipFitConstraintsScalarTypeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var relationship = CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name");
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new WorkerMpInputArgument("Relationship Name", WorkerMpValueKind.CollectionObjectName, relationship, sdkBinding: "SetCollectionObjectNameArg2")],
            [
                new("Use High Tolerance?", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("High Tolerance", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Use Low Tolerance?", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Low Tolerance", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Fit Constraint Options", WorkerMpValueKind.FitConstraintScalarOptions, "GetFitConstraintScalarOptionsArg")
            ]);
    }

    public static Api.GetRelationshipFitConstraintsScalarTypeResult CreateResult(SuccessfulOperationExecution completed)
    {
        var outputs = completed.Execution.OutputValues;
        return new()
        {
            UseHighTolerance = outputs[0].RequireValue<WorkerBooleanValue>().Value,
            HighTolerance = outputs[1].RequireValue<WorkerDoubleValue>().Value,
            UseLowTolerance = outputs[2].RequireValue<WorkerBooleanValue>().Value,
            LowTolerance = outputs[3].RequireValue<WorkerDoubleValue>().Value,
            FitConstraintOptions = FitConstraintScalarOptionsMapper.ToProtocol(
                outputs[4].RequireValue<WorkerFitConstraintScalarOptionsValue>()),
            Execution = completed.Details
        };
    }
}
