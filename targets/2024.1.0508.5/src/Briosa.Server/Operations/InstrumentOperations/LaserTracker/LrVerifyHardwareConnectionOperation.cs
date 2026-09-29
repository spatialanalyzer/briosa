using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class LrVerifyHardwareConnectionOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.lr_verify_hardware_connection", "LR Verify Hardware Connection", "LrVerifyHardwareConnection");
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("connected_to_hardware", "Connected to Hardware?", WorkerMpValueKind.Logical)];

    public static WorkerMpCommand CreateCommand(Api.LrVerifyHardwareConnectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg")],
            [new("Connected to Hardware?", WorkerMpValueKind.Logical, "GetBoolArg")]);
    }

    public static Api.LrVerifyHardwareConnectionResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        ConnectedToHardware = completed.Execution.OutputValues[0].RequireValue<WorkerBooleanValue>().Value,
        Execution = completed.Details
    };
}
