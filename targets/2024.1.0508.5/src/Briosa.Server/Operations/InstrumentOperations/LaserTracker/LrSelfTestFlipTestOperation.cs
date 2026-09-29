using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class LrSelfTestFlipTestOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.lr_self_test_flip_test", "LR Self Test - Flip Test", "LrSelfTestFlipTest");

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("result", "Front Measurement - Range (Inches)", WorkerMpValueKind.FloatingPoint),
        new("result", "Front Measurement - Azimuth (Degs)", WorkerMpValueKind.FloatingPoint),
        new("result", "Front Measurement - Elevation (Degs)", WorkerMpValueKind.FloatingPoint),
        new("result", "Front Measurement - Quality", WorkerMpValueKind.FloatingPoint),
        new("result", "Back Measurement - Range (Inches)", WorkerMpValueKind.FloatingPoint),
        new("result", "Back Measurement - Azimuth (Degs)", WorkerMpValueKind.FloatingPoint),
        new("result", "Back Measurement - Elevation (Degs)", WorkerMpValueKind.FloatingPoint),
        new("result", "Back Measurement - Quality", WorkerMpValueKind.FloatingPoint),
        new("result", "Front/Back Difference - Range (Inches)", WorkerMpValueKind.FloatingPoint),
        new("result", "Front/Back Difference - Azimuth (Degs)", WorkerMpValueKind.FloatingPoint),
        new("result", "Front/Back Difference - Elevation (Degs)", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.LrSelfTestFlipTestRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg")],
            [
                new("Front Measurement - Range (Inches)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Front Measurement - Azimuth (Degs)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Front Measurement - Elevation (Degs)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Front Measurement - Quality", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Back Measurement - Range (Inches)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Back Measurement - Azimuth (Degs)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Back Measurement - Elevation (Degs)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Back Measurement - Quality", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Front/Back Difference - Range (Inches)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Front/Back Difference - Azimuth (Degs)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Front/Back Difference - Elevation (Degs)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
            ]);
    }

    public static Api.LrSelfTestFlipTestResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            Result = new Api.LrFlipTestResult
            {
                FrontRange = values[0].RequireValue<WorkerDoubleValue>().Value,
                FrontAzimuth = values[1].RequireValue<WorkerDoubleValue>().Value,
                FrontElevation = values[2].RequireValue<WorkerDoubleValue>().Value,
                FrontQuality = values[3].RequireValue<WorkerDoubleValue>().Value,
                BackRange = values[4].RequireValue<WorkerDoubleValue>().Value,
                BackAzimuth = values[5].RequireValue<WorkerDoubleValue>().Value,
                BackElevation = values[6].RequireValue<WorkerDoubleValue>().Value,
                BackQuality = values[7].RequireValue<WorkerDoubleValue>().Value,
                FrontBackDifferenceRange = values[8].RequireValue<WorkerDoubleValue>().Value,
                FrontBackDifferenceAzimuth = values[9].RequireValue<WorkerDoubleValue>().Value,
                FrontBackDifferenceElevation = values[10].RequireValue<WorkerDoubleValue>().Value
            },
            Execution = completed.Details
        };
    }
}
