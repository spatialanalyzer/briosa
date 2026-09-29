using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class TransformInstrumentFrameToFrameOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.transform_instrument_frame_to_frame", "Transform Instrument - Frame To Frame",
        "briosa.InstrumentOperations", "TransformInstrumentFrameToFrame", "/briosa.InstrumentOperations/TransformInstrumentFrameToFrame",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.TransformInstrumentFrameToFrameRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument to move", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Initial Frame Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.InitialFrame, "initial_frame"), "SetCollectionObjectNameArg2"),
                new("Destination Frame Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.DestinationFrame, "destination_frame"), "SetCollectionObjectNameArg2"),
                new("Number of Steps", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.NumberOfSteps), "SetIntegerArg")
            ], []);
    }

    public static Api.TransformInstrumentFrameToFrameResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
