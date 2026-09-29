using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class ImportStepFileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.import_step_file", "Import STEP File", "briosa.FileOperations", "ImportStepFile",
        "/briosa.FileOperations/ImportStepFile", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.ImportStepFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("STEP File Path", WorkerMpValueKind.FileReference, FileReferenceMapper.Required(request.StepFilePath, "step_file_path"), "SetFilePathArg"),
            new("Display Entity Filters", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasDisplayEntityFilters && request.DisplayEntityFilters), "SetBoolArg"),
            new("Display Residuals", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasDisplayResiduals && request.DisplayResiduals), "SetBoolArg")
        ], []);
    }
    public static Api.ImportStepFileResult CreateResult(SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}
