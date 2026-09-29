using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class VerifyInstrumentConnectionOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.verify_instrument_connection", "Verify Instrument Connection",
        "briosa.InstrumentOperations", "VerifyInstrumentConnection", "/briosa.InstrumentOperations/VerifyInstrumentConnection",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("connected", "Connected?", WorkerMpValueKind.Logical)];

    public static WorkerMpCommand CreateCommand(Api.VerifyInstrumentConnectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Instrument's ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg")],
            [new("Connected?", WorkerMpValueKind.Logical, "GetBoolArg")]);
    }

    public static Api.VerifyInstrumentConnectionResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Connected = completed.Execution.OutputValues[0].RequireValue<WorkerBooleanValue>().Value,
        Execution = completed.Details
    };
}