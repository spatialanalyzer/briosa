using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class ImportVstarsCamerasOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.import_vstars_cameras", "Import VSTARS Cameras", "briosa.FileOperations",
        "ImportVstarsCameras", "/briosa.FileOperations/ImportVstarsCameras", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["fixture_validation_pending"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ImportVstarsCamerasRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("File Path", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.FilePath, "file_path"), "SetFilePathArg")], []);
    }

    public static Api.ImportVstarsCamerasResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
