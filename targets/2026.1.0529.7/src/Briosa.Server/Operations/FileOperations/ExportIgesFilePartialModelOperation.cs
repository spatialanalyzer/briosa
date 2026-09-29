using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class ExportIgesFilePartialModelOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.export_iges_file_partial_model", "Export IGES File - Partial Model", "briosa.FileOperations", "ExportIgesFilePartialModel",
        "/briosa.FileOperations/ExportIgesFilePartialModel", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.ExportIgesFilePartialModelRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("IGES File Path", WorkerMpValueKind.FileReference, FileReferenceMapper.Required(request.IgesFilePath, "iges_file_path"), "SetFilePathArg"),
            new("Object Name List", WorkerMpValueKind.CollectionObjectNameList, CollectionObjectNameMapper.RequiredList(request.ObjectNameList, "object_name_list"), "SetCollectionObjectNameRefListArg")
        ], []);
    }
    public static Api.ExportIgesFilePartialModelResult CreateResult(SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}
