using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class ExportStepFilePartialModelOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.export_step_file_partial_model", "Export STEP File - Partial Model", "briosa.FileOperations", "ExportStepFilePartialModel",
        "/briosa.FileOperations/ExportStepFilePartialModel", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.ExportStepFilePartialModelRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("STEP File Path", WorkerMpValueKind.FileReference, FileReferenceMapper.Required(request.StepFilePath, "step_file_path"), "SetFilePathArg"),
            new("Object Name List", WorkerMpValueKind.CollectionObjectNameList, CollectionObjectNameMapper.RequiredList(request.ObjectNameList, "object_name_list"), "SetCollectionObjectNameRefListArg")
        ], []);
    }
    public static Api.ExportStepFilePartialModelResult CreateResult(SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}
