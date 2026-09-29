using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class EnableDisableFrameSetScanModeByInstrumentOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.enable_disable_frame_set_scan_mode_by_instrument", "Enable/Disable Frame Set Scan Mode (By Instrument)", "EnableDisableFrameSetScanModeByInstrument");
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.EnableDisableFrameSetScanModeByInstrumentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Enable Frame Set Scan Mode", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasEnableFrameSetScanMode || request.EnableFrameSetScanMode), "SetBoolArg")
            ], []);
    }

    public static Api.EnableDisableFrameSetScanModeByInstrumentResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
