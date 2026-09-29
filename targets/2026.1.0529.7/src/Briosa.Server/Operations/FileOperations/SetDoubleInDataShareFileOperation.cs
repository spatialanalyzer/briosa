using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class SetDoubleInDataShareFileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.set_double_in_data_share_file", "Set Double In DataShare File",
        "briosa.FileOperations", "SetDoubleInDataShareFile", "/briosa.FileOperations/SetDoubleInDataShareFile",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetDoubleInDataShareFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("DataShare File Path", WorkerMpValueKind.FileReference,
                    FileReferenceMapper.Required(request.DataShareFilePath, "data_share_file_path"), "SetFilePathArg"),
                new("Double Name", WorkerMpValueKind.Text, new WorkerTextValue(request.DoubleName), "SetStringArg"),
                new("Double Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.DoubleValue), "SetDoubleArg")], []);
    }

    public static Api.SetDoubleInDataShareFileResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
