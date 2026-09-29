using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class GetInstrumentsWithObservationsOnTargetOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.get_instruments_with_observations_on_target", "Get Instruments with Observations on Target",
        "briosa.InstrumentOperations", "GetInstrumentsWithObservationsOnTarget",
        "/briosa.InstrumentOperations/GetInstrumentsWithObservationsOnTarget",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("instruments", "Resultant Collection Instrument Reference List", WorkerMpValueKind.CollectionInstrumentIdList)];

    public static WorkerMpCommand CreateCommand(Api.GetInstrumentsWithObservationsOnTargetRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.Point, "point"), "SetPointNameArg")],
            [new("Resultant Collection Instrument Reference List", WorkerMpValueKind.CollectionInstrumentIdList,
                "GetColInstIdRefListArg")]);
    }

    public static Api.GetInstrumentsWithObservationsOnTargetResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.GetInstrumentsWithObservationsOnTargetResult { Execution = completed.Details };
        result.Instruments.AddRange(InstrumentIdMapper.ToProtocolList(
            completed.Execution.OutputValues[0].RequireValue<WorkerCollectionInstrumentIdListValue>()));
        return result;
    }
}