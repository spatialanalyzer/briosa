using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class SetWrtlChannelOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.set_wrtl_channel", "Set WRTL Channel", "SetWrtlChannel");

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetWrtlChannelRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Channel", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.Channel), "SetIntegerArg")
            ], []);
    }

    public static Api.SetWrtlChannelResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
