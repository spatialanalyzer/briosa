using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class GetInstrumentTargetingOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.get_instrument_targeting", "Get Instrument Targeting",
        "briosa.InstrumentOperations", "GetInstrumentTargeting",
        "/briosa.InstrumentOperations/GetInstrumentTargeting",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("targeting_name", "Targeting Name", WorkerMpValueKind.Text)];

    public static WorkerMpCommand CreateCommand(Api.GetInstrumentTargetingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg")],
            [new("Targeting Name", WorkerMpValueKind.Text, "GetStringArg")]);
    }

    public static Api.GetInstrumentTargetingResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        TargetingName = completed.Execution.OutputValues[0].RequireValue<WorkerTextValue>().Value,
        Execution = completed.Details
    };
}
