using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.VectorOperations;

internal static class GetVectorGroupPropertiesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "vector_operations.get_vector_group_properties", "Get Vector Group Properties",
        "briosa.VectorOperations", "GetVectorGroupProperties",
        "/briosa.VectorOperations/GetVectorGroupProperties",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("total_vectors", "Total Vectors", WorkerMpValueKind.WholeNumber),
        new("vectors_in_tolerance", "Vectors In Tolerance", WorkerMpValueKind.WholeNumber),
        new("vectors_out_of_tolerance", "Vectors Out Of Tolerance", WorkerMpValueKind.WholeNumber),
        new("invalid_vectors", "Invalid Vectors", WorkerMpValueKind.WholeNumber),
        new("vectors_in_tolerance_2", "% Vectors In Tolerance", WorkerMpValueKind.WholeNumber),
        new("vectors_out_of_tolerance_2", "% Vectors Out Of Tolerance", WorkerMpValueKind.WholeNumber),
        new("absolute_max_magnitude", "Absolute Max Magnitude", WorkerMpValueKind.FloatingPoint),
        new("absolute_min_magnitude", "Absolute Min Magnitude", WorkerMpValueKind.FloatingPoint),
        new("max_magnitude", "Max Magnitude", WorkerMpValueKind.FloatingPoint),
        new("min_magnitude", "Min Magnitude", WorkerMpValueKind.FloatingPoint),
        new("standard_deviation_from_zero", "Standard Deviation From Zero", WorkerMpValueKind.FloatingPoint),
        new("standard_deviation_from_mean", "Standard Deviation From Mean", WorkerMpValueKind.FloatingPoint),
        new("avg_magnitude", "Avg Magnitude", WorkerMpValueKind.FloatingPoint),
        new("avg_of_abs_magnitude", "Avg of Abs Magnitude", WorkerMpValueKind.FloatingPoint),
        new("high_tolerance_value", "High Tolerance Value", WorkerMpValueKind.FloatingPoint),
        new("low_tolerance_value", "Low Tolerance Value", WorkerMpValueKind.FloatingPoint),
        new("rms_value", "RMS Value", WorkerMpValueKind.FloatingPoint)
    ];
    public static WorkerMpCommand CreateCommand(Api.GetVectorGroupPropertiesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Vector Group Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.VectorGroupName, "vector_group_name"), "SetCollectionObjectNameArg2")],
            [
                new("Total Vectors", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
                new("Vectors In Tolerance", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
                new("Vectors Out Of Tolerance", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
                new("Invalid Vectors", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
                new("% Vectors In Tolerance", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
                new("% Vectors Out Of Tolerance", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
                new("Absolute Max Magnitude", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Absolute Min Magnitude", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Max Magnitude", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Min Magnitude", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Standard Deviation From Zero", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Standard Deviation From Mean", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Avg Magnitude", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Avg of Abs Magnitude", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("High Tolerance Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Low Tolerance Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("RMS Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
            ]);
    }
    public static Api.GetVectorGroupPropertiesResult CreateResult(SuccessfulOperationExecution completed)
    {
        var v = completed.Execution.OutputValues;
        return new()
        {
            TotalVectors = v[0].RequireValue<WorkerIntegerValue>().Value,
            VectorsInTolerance = v[1].RequireValue<WorkerIntegerValue>().Value,
            VectorsOutOfTolerance = v[2].RequireValue<WorkerIntegerValue>().Value,
            InvalidVectors = v[3].RequireValue<WorkerIntegerValue>().Value,
            VectorsInTolerance2 = v[4].RequireValue<WorkerIntegerValue>().Value,
            VectorsOutOfTolerance2 = v[5].RequireValue<WorkerIntegerValue>().Value,
            AbsoluteMaxMagnitude = v[6].RequireValue<WorkerDoubleValue>().Value,
            AbsoluteMinMagnitude = v[7].RequireValue<WorkerDoubleValue>().Value,
            MaxMagnitude = v[8].RequireValue<WorkerDoubleValue>().Value,
            MinMagnitude = v[9].RequireValue<WorkerDoubleValue>().Value,
            StandardDeviationFromZero = v[10].RequireValue<WorkerDoubleValue>().Value,
            StandardDeviationFromMean = v[11].RequireValue<WorkerDoubleValue>().Value,
            AvgMagnitude = v[12].RequireValue<WorkerDoubleValue>().Value,
            AvgOfAbsMagnitude = v[13].RequireValue<WorkerDoubleValue>().Value,
            HighToleranceValue = v[14].RequireValue<WorkerDoubleValue>().Value,
            LowToleranceValue = v[15].RequireValue<WorkerDoubleValue>().Value,
            RmsValue = v[16].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }
}
