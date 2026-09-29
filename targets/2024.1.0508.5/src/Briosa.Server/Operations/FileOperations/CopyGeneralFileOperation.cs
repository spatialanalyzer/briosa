using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class CopyGeneralFileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.copy_general_file", "Copy General File", "briosa.FileOperations", "CopyGeneralFile",
        "/briosa.FileOperations/CopyGeneralFile", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.CopyGeneralFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Source File Name", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.SourceFileName, "source_file_name"), "SetFilePathArg"),
            new("Destination File Name", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.DestinationFileName, "destination_file_name"), "SetFilePathArg"),
            new("Overwrite?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasOverwrite && request.Overwrite), "SetBoolArg")
        ], []);
    }

    public static Api.CopyGeneralFileResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
