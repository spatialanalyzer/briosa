using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class GetRelationshipToleranceVectorTypeOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.get_relationship_tolerance_vector_type", "Get Relationship Tolerance (Vector Type)",
        "briosa.RelationshipOperations", "GetRelationshipToleranceVectorType",
        "/briosa.RelationshipOperations/GetRelationshipToleranceVectorType",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("use_high_x_tolerance", "Use High X Tolerance?", WorkerMpValueKind.Logical),
        new("high_x_tolerance", "High X Tolerance", WorkerMpValueKind.FloatingPoint),
        new("use_high_y_tolerance", "Use High Y Tolerance?", WorkerMpValueKind.Logical),
        new("high_y_tolerance", "High Y Tolerance", WorkerMpValueKind.FloatingPoint),
        new("use_high_z_tolerance", "Use High Z Tolerance?", WorkerMpValueKind.Logical),
        new("high_z_tolerance", "High Z Tolerance", WorkerMpValueKind.FloatingPoint),
        new("use_high_mag_tolerance", "Use High Mag Tolerance?", WorkerMpValueKind.Logical),
        new("high_mag_tolerance", "High Mag Tolerance", WorkerMpValueKind.FloatingPoint),
        new("use_low_x_tolerance", "Use Low X Tolerance?", WorkerMpValueKind.Logical),
        new("low_x_tolerance", "Low X Tolerance", WorkerMpValueKind.FloatingPoint),
        new("use_low_y_tolerance", "Use Low Y Tolerance?", WorkerMpValueKind.Logical),
        new("low_y_tolerance", "Low Y Tolerance", WorkerMpValueKind.FloatingPoint),
        new("use_low_z_tolerance", "Use Low Z Tolerance?", WorkerMpValueKind.Logical),
        new("low_z_tolerance", "Low Z Tolerance", WorkerMpValueKind.FloatingPoint),
        new("use_low_mag_tolerance", "Use Low Mag Tolerance?", WorkerMpValueKind.Logical),
        new("low_mag_tolerance", "Low Mag Tolerance", WorkerMpValueKind.FloatingPoint),
        new("vector_tolerance", "Vector Tolerance", WorkerMpValueKind.ToleranceVectorOptions)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetRelationshipToleranceVectorTypeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2")],
            [
                new("Use High X Tolerance?", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("High X Tolerance", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Use High Y Tolerance?", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("High Y Tolerance", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Use High Z Tolerance?", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("High Z Tolerance", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Use High Mag Tolerance?", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("High Mag Tolerance", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Use Low X Tolerance?", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Low X Tolerance", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Use Low Y Tolerance?", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Low Y Tolerance", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Use Low Z Tolerance?", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Low Z Tolerance", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Use Low Mag Tolerance?", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Low Mag Tolerance", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Vector Tolerance", WorkerMpValueKind.ToleranceVectorOptions, "GetToleranceVectorOptionsArg")
            ]);
    }

    public static Api.GetRelationshipToleranceVectorTypeResult CreateResult(SuccessfulOperationExecution completed)
    {
        var outputs = completed.Execution.OutputValues;
        var tolerance = outputs[16].RequireValue<WorkerToleranceVectorOptionsValue>();
        return new()
        {
            UseHighXTolerance = outputs[0].RequireValue<WorkerBooleanValue>().Value,
            HighXTolerance = outputs[1].RequireValue<WorkerDoubleValue>().Value,
            UseHighYTolerance = outputs[2].RequireValue<WorkerBooleanValue>().Value,
            HighYTolerance = outputs[3].RequireValue<WorkerDoubleValue>().Value,
            UseHighZTolerance = outputs[4].RequireValue<WorkerBooleanValue>().Value,
            HighZTolerance = outputs[5].RequireValue<WorkerDoubleValue>().Value,
            UseHighMagTolerance = outputs[6].RequireValue<WorkerBooleanValue>().Value,
            HighMagTolerance = outputs[7].RequireValue<WorkerDoubleValue>().Value,
            UseLowXTolerance = outputs[8].RequireValue<WorkerBooleanValue>().Value,
            LowXTolerance = outputs[9].RequireValue<WorkerDoubleValue>().Value,
            UseLowYTolerance = outputs[10].RequireValue<WorkerBooleanValue>().Value,
            LowYTolerance = outputs[11].RequireValue<WorkerDoubleValue>().Value,
            UseLowZTolerance = outputs[12].RequireValue<WorkerBooleanValue>().Value,
            LowZTolerance = outputs[13].RequireValue<WorkerDoubleValue>().Value,
            UseLowMagTolerance = outputs[14].RequireValue<WorkerBooleanValue>().Value,
            LowMagTolerance = outputs[15].RequireValue<WorkerDoubleValue>().Value,
            VectorTolerance = new()
            {
                HighX = ToProtocol(tolerance.HighX),
                HighY = ToProtocol(tolerance.HighY),
                HighZ = ToProtocol(tolerance.HighZ),
                HighMagnitude = ToProtocol(tolerance.HighMagnitude),
                LowX = ToProtocol(tolerance.LowX),
                LowY = ToProtocol(tolerance.LowY),
                LowZ = ToProtocol(tolerance.LowZ),
                LowMagnitude = ToProtocol(tolerance.LowMagnitude)
            },
            Execution = completed.Details
        };
    }

    private static Api.ToleranceLimit ToProtocol(WorkerToleranceLimit value) =>
        new() { Enabled = value.Enabled, Value = value.Value };
}
