using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class GetFolderCollectionsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.get_folder_collections", "Get Folder Collections", "briosa.UtilityOperations",
        "GetFolderCollections", "/briosa.UtilityOperations/GetFolderCollections", "read_only",
        Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("collection_list", "Collection List", WorkerMpValueKind.StringList)];

    public static WorkerMpCommand CreateCommand(Api.GetFolderCollectionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Folder Path", WorkerMpValueKind.Text,
                new WorkerTextValue(request.FolderPath), "SetStringArg")],
            [new("Collection List", WorkerMpValueKind.StringList, "GetStringRefListArg")]);
    }

    public static Api.GetFolderCollectionsResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.GetFolderCollectionsResult { Execution = completed.Details };
        result.CollectionList.AddRange(completed.Execution.OutputValues[0]
            .RequireValue<WorkerStringListValue>().Values);
        return result;
    }
}
