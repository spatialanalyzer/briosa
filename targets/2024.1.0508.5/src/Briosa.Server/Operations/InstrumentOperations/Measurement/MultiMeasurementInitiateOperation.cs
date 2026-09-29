using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class MultiMeasurementInitiateOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.multi_measurement_initiate", "Multi Measurement Initiate",
        "briosa.InstrumentOperations", "MultiMeasurementInitiate",
        "/briosa.InstrumentOperations/MultiMeasurementInitiate",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MultiMeasurementInitiateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Instruments", WorkerMpValueKind.CollectionInstrumentIdList,
                InstrumentIdMapper.RequiredList(request.Instruments, "instruments"), "SetColInstIdRefListArg"),
            new("Measurement Mode", WorkerMpValueKind.Text,
                new WorkerTextValue(request.MeasurementMode), "SetStringArg"),
            new("Wait for Completion", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.WaitForCompletion), "SetBoolArg")
        ], []);
    }

    public static Api.MultiMeasurementInitiateResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
