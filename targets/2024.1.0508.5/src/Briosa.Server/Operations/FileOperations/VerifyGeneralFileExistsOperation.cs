using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class VerifyGeneralFileExistsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.verify_general_file_exists", "Verify General File Exists", "briosa.FileOperations",
        "VerifyGeneralFileExists", "/briosa.FileOperations/VerifyGeneralFileExists", "read_only",
        Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.VerifyGeneralFileExistsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("File Name", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.FileName, "file_name"), "SetFilePathArg")], []);
    }

    public static Api.VerifyGeneralFileExistsResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
