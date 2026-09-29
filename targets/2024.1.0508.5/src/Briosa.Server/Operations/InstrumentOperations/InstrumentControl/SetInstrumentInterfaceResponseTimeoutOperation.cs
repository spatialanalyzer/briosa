using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class SetInstrumentInterfaceResponseTimeoutOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.set_instrument_interface_response_timeout", "Set Instrument Interface Response Timeout", "SetInstrumentInterfaceResponseTimeout");
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetInstrumentInterfaceResponseTimeoutRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Timeout (secs)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasTimeout ? request.Timeout : 0), "SetDoubleArg")
            ], []);
    }

    public static Api.SetInstrumentInterfaceResponseTimeoutResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
