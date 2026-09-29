using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class InstrumentOperationalCheckOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.instrument_operational_check", "Instrument Operational Check",
        "briosa.InstrumentOperations", "InstrumentOperationalCheck", "/briosa.InstrumentOperations/InstrumentOperationalCheck",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.InstrumentOperationalCheckRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument to Check", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Check Type", WorkerMpValueKind.Text, new WorkerTextValue(request.CheckType), "SetStringArg")
            ], []);
    }

    public static Api.InstrumentOperationalCheckResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}