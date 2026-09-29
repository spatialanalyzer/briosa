using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class GetPointToleranceOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.get_point_tolerance", "Get Point Tolerance",
        "briosa.AnalysisOperations", "GetPointTolerance", "/briosa.AnalysisOperations/GetPointTolerance",
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

    public static WorkerMpCommand CreateCommand(Api.GetPointToleranceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.PointName, "point_name"), "SetPointNameArg")],
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

    public static Api.GetPointToleranceResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            UseHighXTolerance = values[0].RequireValue<WorkerBooleanValue>().Value,
            HighXTolerance = values[1].RequireValue<WorkerDoubleValue>().Value,
            UseHighYTolerance = values[2].RequireValue<WorkerBooleanValue>().Value,
            HighYTolerance = values[3].RequireValue<WorkerDoubleValue>().Value,
            UseHighZTolerance = values[4].RequireValue<WorkerBooleanValue>().Value,
            HighZTolerance = values[5].RequireValue<WorkerDoubleValue>().Value,
            UseHighMagTolerance = values[6].RequireValue<WorkerBooleanValue>().Value,
            HighMagTolerance = values[7].RequireValue<WorkerDoubleValue>().Value,
            UseLowXTolerance = values[8].RequireValue<WorkerBooleanValue>().Value,
            LowXTolerance = values[9].RequireValue<WorkerDoubleValue>().Value,
            UseLowYTolerance = values[10].RequireValue<WorkerBooleanValue>().Value,
            LowYTolerance = values[11].RequireValue<WorkerDoubleValue>().Value,
            UseLowZTolerance = values[12].RequireValue<WorkerBooleanValue>().Value,
            LowZTolerance = values[13].RequireValue<WorkerDoubleValue>().Value,
            UseLowMagTolerance = values[14].RequireValue<WorkerBooleanValue>().Value,
            LowMagTolerance = values[15].RequireValue<WorkerDoubleValue>().Value,
            VectorTolerance = ToleranceVectorOptionsMapper.ToProtocol(
                values[16].RequireValue<WorkerToleranceVectorOptionsValue>()),
            Execution = completed.Details
        };
    }
}
