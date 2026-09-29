using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class SetObjectNotesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.set_object_notes", "Set Object Notes", "briosa.UtilityOperations",
        "SetObjectNotes", "/briosa.UtilityOperations/SetObjectNotes", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetObjectNotesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Notes.Count == 0)
            throw new ArgumentException("Notes are required.", nameof(request));
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Object", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.Object, "object"), "SetCollectionObjectNameArg2"),
            new("Notes", WorkerMpValueKind.EditText, new WorkerStringListValue(request.Notes), "SetEditTextArg"),
            new("Append? (FALSE = Overwrite)", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasAppend || request.Append), "SetBoolArg")
        ], []);
    }

    public static Api.SetObjectNotesResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
