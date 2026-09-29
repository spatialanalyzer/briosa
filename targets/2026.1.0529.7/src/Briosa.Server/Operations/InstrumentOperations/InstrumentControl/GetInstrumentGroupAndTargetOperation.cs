using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class GetInstrumentGroupAndTargetOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.get_instrument_group_and_target", "Get Instrument Group and Target",
        "briosa.InstrumentOperations", "GetInstrumentGroupAndTarget",
        "/briosa.InstrumentOperations/GetInstrumentGroupAndTarget",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("point", "Point Name", WorkerMpValueKind.PointName)];

    public static WorkerMpCommand CreateCommand(Api.GetInstrumentGroupAndTargetRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg")],
            [new("Point Name", WorkerMpValueKind.PointName, "GetPointNameArg")]);
    }

    public static Api.GetInstrumentGroupAndTargetResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Point = PointNameMapper.ToProtocol(
            completed.Execution.OutputValues[0].RequireValue<WorkerPointNameValue>()),
        Execution = completed.Details
    };
}
