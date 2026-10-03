using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class ImportMpFileAsEmbeddedMpOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.import_mp_file_as_embedded_mp", "Import MP File as Embedded MP", "briosa.FileOperations",
        "ImportMpFileAsEmbeddedMp", "/briosa.FileOperations/ImportMpFileAsEmbeddedMp", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ImportMpFileAsEmbeddedMpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("External MP File Name", WorkerMpValueKind.FileReference,
                    FileReferenceMapper.Required(request.ExternalMpFileName, "external_mp_file_name"), "SetFilePathArg"),
                new("Replace Existing?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.ReplaceExisting), "SetBoolArg")], []);
    }

    public static Api.ImportMpFileAsEmbeddedMpResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
