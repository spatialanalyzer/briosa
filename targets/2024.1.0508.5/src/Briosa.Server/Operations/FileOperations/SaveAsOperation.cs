using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class SaveAsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.save_as", "Save As...", "briosa.FileOperations", "SaveAs",
        "/briosa.FileOperations/SaveAs", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SaveAsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("File Name", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.FileName, "file_name"), "SetFilePathArg"),
            new("Add Serial Number?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasAddSerialNumber && request.AddSerialNumber), "SetBoolArg"),
            new("Optional Number", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.HasOptionalNumber ? request.OptionalNumber : 0), "SetIntegerArg")
        ], []);
    }

    public static Api.SaveAsResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
