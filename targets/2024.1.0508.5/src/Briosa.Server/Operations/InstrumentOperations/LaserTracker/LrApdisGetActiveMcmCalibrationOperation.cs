using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class LrApdisGetActiveMcmCalibrationOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.lr_apdis_get_active_mcm_calibration",
        "LR APDIS Get Active MCM Calibration", "LrApdisGetActiveMcmCalibration");

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("active_mcm_name", "Active MCM Name", WorkerMpValueKind.Text)];

    public static WorkerMpCommand CreateCommand(Api.LrApdisGetActiveMcmCalibrationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg")],
            [new("Active MCM Name", WorkerMpValueKind.Text, "GetStringArg")]);
    }

    public static Api.LrApdisGetActiveMcmCalibrationResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        ActiveMcmName = completed.Execution.OutputValues[0].RequireValue<WorkerTextValue>().Value,
        Execution = completed.Details
    };
}
