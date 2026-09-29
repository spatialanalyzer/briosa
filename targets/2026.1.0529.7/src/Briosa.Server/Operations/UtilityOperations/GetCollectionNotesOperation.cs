using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class GetCollectionNotesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.get_collection_notes", "Get Collection Notes", "briosa.UtilityOperations",
        "GetCollectionNotes", "/briosa.UtilityOperations/GetCollectionNotes", "read_only",
        Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("notes", "Notes", WorkerMpValueKind.EditText)];

    public static WorkerMpCommand CreateCommand(Api.GetCollectionNotesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Collection is null || string.IsNullOrWhiteSpace(request.Collection.Name))
            throw new ArgumentException("Collection is required.", nameof(request));
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Collection", WorkerMpValueKind.CollectionName,
                new WorkerTextValue(request.Collection.Name), "SetCollectionNameArg")],
            [new("Notes", WorkerMpValueKind.EditText, "GetEditTextArg")]);
    }

    public static Api.GetCollectionNotesResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.GetCollectionNotesResult { Execution = completed.Details };
        result.Notes.AddRange(completed.Execution.OutputValues[0].RequireValue<WorkerStringListValue>().Values);
        return result;
    }
}
