using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class MakeEmbeddedFileNameListOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.make_embedded_file_name_list", "Make Embedded File Name List", "briosa.FileOperations",
        "MakeEmbeddedFileNameList", "/briosa.FileOperations/MakeEmbeddedFileNameList", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("embedded_files", "Embedded Files", WorkerMpValueKind.StringList)];

    public static WorkerMpCommand CreateCommand(Api.MakeEmbeddedFileNameListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Collection Wildcard Criteria", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.HasCollectionWildcardCriteria ? request.CollectionWildcardCriteria : "*"), "SetStringArg"),
                new("File Name Pattern", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.HasFileNamePattern ? request.FileNamePattern : "*.*"), "SetStringArg")],
            [new("Embedded Files", WorkerMpValueKind.StringList, "GetStringRefListArg")]);
    }

    public static Api.MakeEmbeddedFileNameListResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.MakeEmbeddedFileNameListResult { Execution = completed.Details };
        result.EmbeddedFiles.AddRange(completed.Execution.OutputValues[0]
            .RequireValue<WorkerStringListValue>().Values);
        return result;
    }
}
