using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class EnableDisablePointSetScanModeOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.enable_disable_point_set_scan_mode", "Enable/Disable Point Set Scan Mode", "EnableDisablePointSetScanMode");
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.EnableDisablePointSetScanModeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument's ID", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Enable Point Set Scan Mode", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasEnablePointSetScanMode || request.EnablePointSetScanMode), "SetBoolArg")
            ], []);
    }

    public static Api.EnableDisablePointSetScanModeResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
