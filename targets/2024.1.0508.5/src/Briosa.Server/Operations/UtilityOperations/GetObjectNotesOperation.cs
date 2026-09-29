using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class GetObjectNotesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.get_object_notes", "Get Object Notes", "briosa.UtilityOperations",
        "GetObjectNotes", "/briosa.UtilityOperations/GetObjectNotes", "read_only",
        Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("notes", "Notes", WorkerMpValueKind.EditText)];

    public static WorkerMpCommand CreateCommand(Api.GetObjectNotesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Object", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.Object, "object"), "SetCollectionObjectNameArg2")],
            [new("Notes", WorkerMpValueKind.EditText, "GetEditTextArg")]);
    }

    public static Api.GetObjectNotesResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.GetObjectNotesResult { Execution = completed.Details };
        result.Notes.AddRange(completed.Execution.OutputValues[0].RequireValue<WorkerStringListValue>().Values);
        return result;
    }
}
