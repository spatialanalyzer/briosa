using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class EnableDisableFrameSetScanModeAllInstrumentsOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.enable_disable_frame_set_scan_mode_all_instruments", "Enable/Disable Frame Set Scan Mode (All Instruments)", "EnableDisableFrameSetScanModeAllInstruments");
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.EnableDisableFrameSetScanModeAllInstrumentsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Enable Frame Set Scan Mode", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasEnableFrameSetScanMode || request.EnableFrameSetScanMode), "SetBoolArg")], []);
    }

    public static Api.EnableDisableFrameSetScanModeAllInstrumentsResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
