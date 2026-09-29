using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class VerifyMpFileExistsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.verify_mp_file_exists", "Verify MP File Exists", "briosa.FileOperations",
        "VerifyMpFileExists", "/briosa.FileOperations/VerifyMpFileExists", "read_only",
        Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.VerifyMpFileExistsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("MP File Name", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.MpFileName, "mp_file_name"), "SetFilePathArg")], []);
    }

    public static Api.VerifyMpFileExistsResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
