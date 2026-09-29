using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class GetWrtlChannelAndStatusOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.ReadOnly(
        "instrument_operations.get_wrtl_channel_and_status", "Get WRTL Channel and Status", "GetWrtlChannelAndStatus");

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("status", "Connection Status", WorkerMpValueKind.Logical),
        new("status", "Active Channel", WorkerMpValueKind.WholeNumber)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetWrtlChannelAndStatusRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Instrument", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg")],
            [
                new("Connection Status", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Active Channel", WorkerMpValueKind.WholeNumber, "GetIntegerArg")
            ]);
    }

    public static Api.GetWrtlChannelAndStatusResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Status = new Api.WrtlChannelStatus
        {
            ConnectionStatus = completed.Execution.OutputValues[0].RequireValue<WorkerBooleanValue>().Value,
            ActiveChannel = completed.Execution.OutputValues[1].RequireValue<WorkerIntegerValue>().Value
        },
        Execution = completed.Details
    };
}
