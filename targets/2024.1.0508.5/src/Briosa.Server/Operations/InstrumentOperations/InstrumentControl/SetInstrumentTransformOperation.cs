using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class SetInstrumentTransformOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.set_instrument_transform", "Set Instrument Transform",
        "briosa.InstrumentOperations", "SetInstrumentTransform", "/briosa.InstrumentOperations/SetInstrumentTransform",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetInstrumentTransformRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument to Move", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Destination Transform", WorkerMpValueKind.Transform,
                    TransformMapper.Required(request.DestinationTransform, "destination_transform"), "SetTransformArg"),
                new("Reference Frame", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.ReferenceFrame, "reference_frame", WorkerObjectTypeValue.Frame),
                    "SetCollectionObjectNameArg2"),
                new("Number of Steps", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.NumberOfSteps), "SetIntegerArg")
            ], []);
    }

    public static Api.SetInstrumentTransformResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
