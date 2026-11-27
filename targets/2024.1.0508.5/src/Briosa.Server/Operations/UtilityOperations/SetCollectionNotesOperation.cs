using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class SetCollectionNotesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.set_collection_notes", "Set Collection Notes", "briosa.UtilityOperations",
        "SetCollectionNotes", "/briosa.UtilityOperations/SetCollectionNotes", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetCollectionNotesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Collection is null)
            throw RequestFieldViolation.Required("collection");
        if (string.IsNullOrWhiteSpace(request.Collection.Name))
            throw RequestFieldViolation.NotBlank("collection.name");
        if (request.Notes.Count == 0)
            throw RequestFieldViolation.NotEmpty("notes");
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Collection", WorkerMpValueKind.CollectionName,
                new WorkerTextValue(request.Collection.Name), "SetCollectionNameArg"),
            new("Notes", WorkerMpValueKind.EditText, new WorkerStringListValue(request.Notes), "SetEditTextArg"),
            new("Append? (FALSE = Overwrite)", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasAppend || request.Append), "SetBoolArg")
        ], []);
    }

    public static Api.SetCollectionNotesResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
