using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.DimensionOperations;

internal static class GetDimensionValueOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "dimension_operations.get_dimension_value", "Get Dimension Value",
        "briosa.DimensionOperations", "GetDimensionValue", "/briosa.DimensionOperations/GetDimensionValue",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("dimensions_value", "Dimensions Value", WorkerMpValueKind.FloatingPoint),
        new("nominal_value_enabled", "Nominal Value Enabled?", WorkerMpValueKind.Logical),
        new("high_tolerance_enabled", "High Tolerance Enabled?", WorkerMpValueKind.Logical),
        new("low_tolerance_enabled", "Low Tolerance Enabled?", WorkerMpValueKind.Logical),
        new("nominal_value", "Nominal Value", WorkerMpValueKind.FloatingPoint),
        new("high_tolerance", "High Tolerance", WorkerMpValueKind.FloatingPoint),
        new("low_tolerance", "Low Tolerance", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetDimensionValueRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Dimension Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.DimensionName, "dimension_name"), "SetCollectionObjectNameArg2")],
            [
                new("Dimensions Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Nominal Value Enabled?", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("High Tolerance Enabled?", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Low Tolerance Enabled?", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Nominal Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("High Tolerance", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Low Tolerance", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
            ]);
    }

    public static Api.GetDimensionValueResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            DimensionsValue = values[0].RequireValue<WorkerDoubleValue>().Value,
            NominalValueEnabled = values[1].RequireValue<WorkerBooleanValue>().Value,
            HighToleranceEnabled = values[2].RequireValue<WorkerBooleanValue>().Value,
            LowToleranceEnabled = values[3].RequireValue<WorkerBooleanValue>().Value,
            NominalValue = values[4].RequireValue<WorkerDoubleValue>().Value,
            HighTolerance = values[5].RequireValue<WorkerDoubleValue>().Value,
            LowTolerance = values[6].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }
}
