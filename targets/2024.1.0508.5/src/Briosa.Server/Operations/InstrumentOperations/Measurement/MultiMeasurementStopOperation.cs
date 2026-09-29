using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class MultiMeasurementStopOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.multi_measurement_stop", "Multi Measurement Stop",
        "briosa.InstrumentOperations", "MultiMeasurementStop",
        "/briosa.InstrumentOperations/MultiMeasurementStop",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MultiMeasurementStopRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Instruments", WorkerMpValueKind.CollectionInstrumentIdList,
                InstrumentIdMapper.RequiredList(request.Instruments, "instruments"), "SetColInstIdRefListArg")
        ], []);
    }

    public static Api.MultiMeasurementStopResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
