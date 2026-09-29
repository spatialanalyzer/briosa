using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class GetRelationshipToleranceScalarTypeOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.get_relationship_tolerance_scalar_type", "Get Relationship Tolerance (Scalar Type)",
        "briosa.RelationshipOperations", "GetRelationshipToleranceScalarType",
        "/briosa.RelationshipOperations/GetRelationshipToleranceScalarType",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("use_high_tolerance", "Use High Tolerance?", WorkerMpValueKind.Logical),
        new("high_tolerance", "High Tolerance", WorkerMpValueKind.FloatingPoint),
        new("use_low_tolerance", "Use Low Tolerance?", WorkerMpValueKind.Logical),
        new("low_tolerance", "Low Tolerance", WorkerMpValueKind.FloatingPoint),
        new("tolerance_options", "Tolerance Options", WorkerMpValueKind.ToleranceScalarOptions)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetRelationshipToleranceScalarTypeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2")],
            [
                new("Use High Tolerance?", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("High Tolerance", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Use Low Tolerance?", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Low Tolerance", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Tolerance Options", WorkerMpValueKind.ToleranceScalarOptions, "GetToleranceScalarOptionsArg")
            ]);
    }

    public static Api.GetRelationshipToleranceScalarTypeResult CreateResult(SuccessfulOperationExecution completed)
    {
        var outputs = completed.Execution.OutputValues;
        var options = outputs[4].RequireValue<WorkerToleranceScalarOptionsValue>();
        return new()
        {
            UseHighTolerance = outputs[0].RequireValue<WorkerBooleanValue>().Value,
            HighTolerance = outputs[1].RequireValue<WorkerDoubleValue>().Value,
            UseLowTolerance = outputs[2].RequireValue<WorkerBooleanValue>().Value,
            LowTolerance = outputs[3].RequireValue<WorkerDoubleValue>().Value,
            ToleranceOptions = ToleranceScalarOptionsMapper.ToProtocol(options),
            Execution = completed.Details
        };
    }
}
