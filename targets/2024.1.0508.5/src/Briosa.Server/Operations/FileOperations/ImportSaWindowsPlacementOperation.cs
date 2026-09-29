using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class ImportSaWindowsPlacementOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.import_sa_windows_placement", "Import SA Windows Placement", "briosa.FileOperations",
        "ImportSaWindowsPlacement", "/briosa.FileOperations/ImportSaWindowsPlacement", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ImportSaWindowsPlacementRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("File Path", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.FilePath, "file_path"), "SetFilePathArg")], []);
    }

    public static Api.ImportSaWindowsPlacementResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
