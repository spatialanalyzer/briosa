using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class LrSelfTestOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.lr_self_test", "LR Self Test", "LrSelfTest");

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("reference_arm_length", "Ref Arm Length (Inches)", WorkerMpValueKind.FloatingPoint),
        new("reference_arm_quality", "Ref Arm Quality", WorkerMpValueKind.FloatingPoint),
        new("mirror_measurement_count", "Mirror Measurement Count", WorkerMpValueKind.WholeNumber),
        new("mirror_measurement_range_mean", "Mirror Measurement Range - Mean (Inches)", WorkerMpValueKind.FloatingPoint),
        new("mirror_measurement_range_standard_deviation", "Mirror Measurement Range - StdDev (Inches)", WorkerMpValueKind.FloatingPoint),
        new("mirror_measurement_quality_mean", "Mirror Measurement Quality - Mean", WorkerMpValueKind.FloatingPoint),
        new("mirror_measurement_quality_standard_deviation", "Mirror Measurement Quality - StdDev", WorkerMpValueKind.FloatingPoint),
        new("passed_reference_arm_quality_threshold", "Passed Ref Arm Quality Threshold?", WorkerMpValueKind.Logical),
        new("passed_mirror_offset_delta_threshold", "Passed Mirror Offset Delta Threshold?", WorkerMpValueKind.Logical),
        new("passed_mirror_offset_standard_deviation_threshold", "Passed Mirror Offset StdDev Threshold?", WorkerMpValueKind.Logical),
        new("passed_mirror_mean_quality_threshold", "Passed Mirror Mean Quality Threshold?", WorkerMpValueKind.Logical),
        new("passed_overall", "Passed Overall?", WorkerMpValueKind.Logical)
    ];

    public static WorkerMpCommand CreateCommand(Api.LrSelfTestRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg")],
            [
                new("Ref Arm Length (Inches)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Ref Arm Quality", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Mirror Measurement Count", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
                new("Mirror Measurement Range - Mean (Inches)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Mirror Measurement Range - StdDev (Inches)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Mirror Measurement Quality - Mean", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Mirror Measurement Quality - StdDev", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Passed Ref Arm Quality Threshold?", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Passed Mirror Offset Delta Threshold?", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Passed Mirror Offset StdDev Threshold?", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Passed Mirror Mean Quality Threshold?", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Passed Overall?", WorkerMpValueKind.Logical, "GetBoolArg")
            ]);
    }

    public static Api.LrSelfTestResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            ReferenceArmLength = values[0].RequireValue<WorkerDoubleValue>().Value,
            ReferenceArmQuality = values[1].RequireValue<WorkerDoubleValue>().Value,
            MirrorMeasurementCount = values[2].RequireValue<WorkerIntegerValue>().Value,
            MirrorMeasurementRangeMean = values[3].RequireValue<WorkerDoubleValue>().Value,
            MirrorMeasurementRangeStandardDeviation = values[4].RequireValue<WorkerDoubleValue>().Value,
            MirrorMeasurementQualityMean = values[5].RequireValue<WorkerDoubleValue>().Value,
            MirrorMeasurementQualityStandardDeviation = values[6].RequireValue<WorkerDoubleValue>().Value,
            PassedReferenceArmQualityThreshold = values[7].RequireValue<WorkerBooleanValue>().Value,
            PassedMirrorOffsetDeltaThreshold = values[8].RequireValue<WorkerBooleanValue>().Value,
            PassedMirrorOffsetStandardDeviationThreshold = values[9].RequireValue<WorkerBooleanValue>().Value,
            PassedMirrorMeanQualityThreshold = values[10].RequireValue<WorkerBooleanValue>().Value,
            PassedOverall = values[11].RequireValue<WorkerBooleanValue>().Value,
            Execution = completed.Details
        };
    }
}
