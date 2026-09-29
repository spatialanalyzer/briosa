using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class LrHardwareConnectOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.lr_hardware_connect", "LR Hardware Connect", "LrHardwareConnect");
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.LrHardwareConnectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Host", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.HasHost ? request.Host : string.Empty), "SetStringArg"),
                new("Port", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.HasPort ? request.Port : 0), "SetIntegerArg")
            ], []);
    }

    public static Api.LrHardwareConnectResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
