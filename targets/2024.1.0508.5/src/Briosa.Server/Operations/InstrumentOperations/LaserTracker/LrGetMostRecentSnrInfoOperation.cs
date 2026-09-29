using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class LrGetMostRecentSnrInfoOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.lr_get_most_recent_snr_info", "LR Get Most Recent SNR Info", "LrGetMostRecentSnrInfo");
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("info", "SNR", WorkerMpValueKind.FloatingPoint),
        new("info", "Size of Data Array", WorkerMpValueKind.WholeNumber),
        new("info", "Peak Value Index", WorkerMpValueKind.WholeNumber),
        new("info", "Peak Value (dB)", WorkerMpValueKind.FloatingPoint),
        new("info", "Measured Range (m)", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.LrGetMostRecentSnrInfoRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg")],
            [
                new("SNR", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Size of Data Array", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
                new("Peak Value Index", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
                new("Peak Value (dB)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Measured Range (m)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
            ]);
    }

    public static Api.LrGetMostRecentSnrInfoResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            Info = new Api.LrSnrInfo
            {
                Snr = values[0].RequireValue<WorkerDoubleValue>().Value,
                SizeOfDataArray = values[1].RequireValue<WorkerIntegerValue>().Value,
                PeakValueIndex = values[2].RequireValue<WorkerIntegerValue>().Value,
                PeakValue = values[3].RequireValue<WorkerDoubleValue>().Value,
                MeasuredRange = values[4].RequireValue<WorkerDoubleValue>().Value
            },
            Execution = completed.Details
        };
    }
}
