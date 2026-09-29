using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class GetRelationshipOutlierRejectionScalarTypeOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.get_relationship_outlier_rejection_scalar_type",
        "Get Relationship Outlier Rejection (Scalar Type)",
        "briosa.RelationshipOperations", "GetRelationshipOutlierRejectionScalarType",
        "/briosa.RelationshipOperations/GetRelationshipOutlierRejectionScalarType",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe,
        ["fixture_validation_pending"]);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("use_high_limit", "Use High Limit?", WorkerMpValueKind.Logical),
        new("high_limit", "High Limit", WorkerMpValueKind.FloatingPoint),
        new("use_low_limit", "Use Low Limit?", WorkerMpValueKind.Logical),
        new("low_limit", "Low Limit", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetRelationshipOutlierRejectionScalarTypeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2")],
            [
                new("Use High Limit?", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("High Limit", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Use Low Limit?", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Low Limit", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
            ]);
    }

    public static Api.GetRelationshipOutlierRejectionScalarTypeResult CreateResult(SuccessfulOperationExecution completed)
    {
        var outputs = completed.Execution.OutputValues;
        return new()
        {
            UseHighLimit = outputs[0].RequireValue<WorkerBooleanValue>().Value,
            HighLimit = outputs[1].RequireValue<WorkerDoubleValue>().Value,
            UseLowLimit = outputs[2].RequireValue<WorkerBooleanValue>().Value,
            LowLimit = outputs[3].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }
}
