using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class ExportStepFileEntireModelOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.export_step_file_entire_model", "Export STEP File - Entire Model", "briosa.FileOperations", "ExportStepFileEntireModel",
        "/briosa.FileOperations/ExportStepFileEntireModel", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.ExportStepFileEntireModelRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("STEP File Path", WorkerMpValueKind.FileReference, FileReferenceMapper.Required(request.StepFilePath, "step_file_path"), "SetFilePathArg")], []);
    }
    public static Api.ExportStepFileEntireModelResult CreateResult(SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}
