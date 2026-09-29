using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class SetAlignmentProjectorOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.set_alignment_projector", "Set Alignment Projector",
        "briosa.InstrumentOperations", "SetAlignmentProjector", "/briosa.InstrumentOperations/SetAlignmentProjector",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetAlignmentProjectorRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Projector Profile", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.HasProjectorProfile ? request.ProjectorProfile : string.Empty), "SetStringArg"),
                new("User Prompt", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.HasUserPrompt ? request.UserPrompt : string.Empty), "SetStringArg")
            ], []);
    }

    public static Api.SetAlignmentProjectorResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
