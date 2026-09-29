using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class GetInstrumentInterfaceResponseTimeoutOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.ReadOnly(
        "instrument_operations.get_instrument_interface_response_timeout", "Get Instrument Interface Response Timeout", "GetInstrumentInterfaceResponseTimeout");
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("timeout", "Resulting Timeout Value (secs)", WorkerMpValueKind.FloatingPoint)];

    public static WorkerMpCommand CreateCommand(Api.GetInstrumentInterfaceResponseTimeoutRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg")],
            [new("Resulting Timeout Value (secs)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")]);
    }

    public static Api.GetInstrumentInterfaceResponseTimeoutResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Timeout = completed.Execution.OutputValues[0].RequireValue<WorkerDoubleValue>().Value,
        Execution = completed.Details
    };
}
