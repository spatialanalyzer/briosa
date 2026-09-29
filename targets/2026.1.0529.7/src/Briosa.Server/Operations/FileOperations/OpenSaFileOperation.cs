using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class OpenSaFileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.open_sa_file", "Open SA File", "briosa.FileOperations", "OpenSaFile",
        "/briosa.FileOperations/OpenSaFile", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.OpenSaFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("SA File Name", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.SaFileName, "sa_file_name"), "SetFilePathArg")], []);
    }

    public static Api.OpenSaFileResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
