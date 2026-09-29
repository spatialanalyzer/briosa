using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class GetTargetsMeasuredByInstrumentOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.get_targets_measured_by_instrument", "Get Targets Measured by Instrument",
        "briosa.InstrumentOperations", "GetTargetsMeasuredByInstrument",
        "/briosa.InstrumentOperations/GetTargetsMeasuredByInstrument",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("targets", "Points Measured by Instrument", WorkerMpValueKind.PointNameList)];

    public static WorkerMpCommand CreateCommand(Api.GetTargetsMeasuredByInstrumentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Measuring Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg")],
            [new("Points Measured by Instrument", WorkerMpValueKind.PointNameList, "GetPointNameRefListArg")]);
    }

    public static Api.GetTargetsMeasuredByInstrumentResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.GetTargetsMeasuredByInstrumentResult { Execution = completed.Details };
        result.Targets.AddRange(completed.Execution.OutputValues[0]
            .RequireValue<WorkerPointNameListValue>().Values.Select(PointNameMapper.ToProtocol));
        return result;
    }
}