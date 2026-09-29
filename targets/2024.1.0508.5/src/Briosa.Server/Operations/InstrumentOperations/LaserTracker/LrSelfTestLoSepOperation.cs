using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class LrSelfTestLoSepOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.lr_self_test_lo_sep", "LR Self Test - LO Sep", "LrSelfTestLoSep");

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("result", "Primary LO (indexed from 1)", WorkerMpValueKind.WholeNumber),
        new("result", "Secondary LO (indexed from 1)", WorkerMpValueKind.WholeNumber),
        new("result", "Primary LO Measurement Count", WorkerMpValueKind.WholeNumber),
        new("result", "Primary LO Measurement Range - Mean (Inches)", WorkerMpValueKind.FloatingPoint),
        new("result", "Primary LO Measurement Range - StdDev (Inches)", WorkerMpValueKind.FloatingPoint),
        new("result", "Primary LO Measurement Quality - Mean", WorkerMpValueKind.FloatingPoint),
        new("result", "Primary LO Measurement Quality - StdDev", WorkerMpValueKind.FloatingPoint),
        new("result", "Secondary LO Measurement Count", WorkerMpValueKind.WholeNumber),
        new("result", "Secondary LO Measurement Range - Mean (Inches)", WorkerMpValueKind.FloatingPoint),
        new("result", "Secondary LO Measurement Range - StdDev (Inches)", WorkerMpValueKind.FloatingPoint),
        new("result", "Secondary LO Measurement Quality - Mean", WorkerMpValueKind.FloatingPoint),
        new("result", "Secondary LO Measurement Quality - StdDev", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.LrSelfTestLoSepRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Region (1=Region12,2=Region23,3=Region34)", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.Region), "SetIntegerArg"),
                new("Num Range Measurements", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.NumRangeMeasurements), "SetIntegerArg")
            ],
            [
                new("Primary LO (indexed from 1)", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
                new("Secondary LO (indexed from 1)", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
                new("Primary LO Measurement Count", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
                new("Primary LO Measurement Range - Mean (Inches)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Primary LO Measurement Range - StdDev (Inches)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Primary LO Measurement Quality - Mean", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Primary LO Measurement Quality - StdDev", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Secondary LO Measurement Count", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
                new("Secondary LO Measurement Range - Mean (Inches)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Secondary LO Measurement Range - StdDev (Inches)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Secondary LO Measurement Quality - Mean", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Secondary LO Measurement Quality - StdDev", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
            ]);
    }

    public static Api.LrSelfTestLoSepResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            Result = new Api.LrLoSeparationTestResult
            {
                PrimaryLo = values[0].RequireValue<WorkerIntegerValue>().Value,
                SecondaryLo = values[1].RequireValue<WorkerIntegerValue>().Value,
                PrimaryLoMeasurementCount = values[2].RequireValue<WorkerIntegerValue>().Value,
                PrimaryLoRangeMean = values[3].RequireValue<WorkerDoubleValue>().Value,
                PrimaryLoRangeStandardDeviation = values[4].RequireValue<WorkerDoubleValue>().Value,
                PrimaryLoQualityMean = values[5].RequireValue<WorkerDoubleValue>().Value,
                PrimaryLoQualityStandardDeviation = values[6].RequireValue<WorkerDoubleValue>().Value,
                SecondaryLoMeasurementCount = values[7].RequireValue<WorkerIntegerValue>().Value,
                SecondaryLoRangeMean = values[8].RequireValue<WorkerDoubleValue>().Value,
                SecondaryLoRangeStandardDeviation = values[9].RequireValue<WorkerDoubleValue>().Value,
                SecondaryLoQualityMean = values[10].RequireValue<WorkerDoubleValue>().Value,
                SecondaryLoQualityStandardDeviation = values[11].RequireValue<WorkerDoubleValue>().Value
            },
            Execution = completed.Details
        };
    }
}
