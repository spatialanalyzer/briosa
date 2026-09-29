using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class GetInstrumentTransformOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.get_instrument_transform", "Get Instrument Transform",
        "briosa.InstrumentOperations", "GetInstrumentTransform", "/briosa.InstrumentOperations/GetInstrumentTransform",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("transform", "Transform", WorkerMpValueKind.Transform)];

    public static WorkerMpCommand CreateCommand(Api.GetInstrumentTransformRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Reference Frame", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.ReferenceFrame, "reference_frame", WorkerObjectTypeValue.Frame),
                    "SetCollectionObjectNameArg2")
            ],
            [new("Transform", WorkerMpValueKind.Transform, "GetTransformArg")]);
    }

    public static Api.GetInstrumentTransformResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Transform = TransformMapper.ToProtocol(completed.Execution.OutputValues[0].RequireValue<WorkerTransformValue>()),
        Execution = completed.Details
    };
}
