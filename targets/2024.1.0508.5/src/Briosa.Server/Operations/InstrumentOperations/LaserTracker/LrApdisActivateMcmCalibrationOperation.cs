using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class LrApdisActivateMcmCalibrationOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.lr_apdis_activate_mcm_calibration",
        "LR APDIS Activate MCM Calibration", "LrApdisActivateMcmCalibration");

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("active_mcm_name", "Active MCM Name", WorkerMpValueKind.Text)];

    public static WorkerMpCommand CreateCommand(Api.LrApdisActivateMcmCalibrationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var inputs = new List<WorkerMpInputArgument>
        {
            new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg")
        };
        if (request.HasCalibrationName)
        {
            inputs.Add(new("Calibration Name (Optional)", WorkerMpValueKind.Text,
                new WorkerTextValue(request.CalibrationName), "SetStringArg"));
        }
        if (request.HasCalibrationId)
        {
            inputs.Add(new("Calibration ID (Optional)", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.CalibrationId), "SetIntegerArg"));
        }

        return new(Descriptor.OperationId, Descriptor.MpStep, inputs,
            [new("Active MCM Name", WorkerMpValueKind.Text, "GetStringArg")]);
    }

    public static Api.LrApdisActivateMcmCalibrationResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        ActiveMcmName = completed.Execution.OutputValues[0].RequireValue<WorkerTextValue>().Value,
        Execution = completed.Details
    };
}
