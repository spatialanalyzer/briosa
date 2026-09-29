using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructFrameOnInstrumentBaseOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_frame_on_instrument_base", "Construct Frame on Instrument Base",
        "briosa.ConstructionOperations", "ConstructFrameOnInstrumentBase",
        "/briosa.ConstructionOperations/ConstructFrameOnInstrumentBase", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructFrameOnInstrumentBaseRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var inputs = new List<WorkerMpInputArgument>
        {
            new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                CollectionInstrumentIdMapper.Required(request.InstrumentId, "instrument_id"), "SetColInstIdArg")
        };
        if (request.HasFrameName)
            inputs.Add(new("Frame Name (Optional)", WorkerMpValueKind.FrameName,
                new WorkerTextValue(request.FrameName), "SetFrameNameArg"));
        return new(Descriptor.OperationId, Descriptor.MpStep, inputs, []);
    }

    public static Api.ConstructFrameOnInstrumentBaseResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
