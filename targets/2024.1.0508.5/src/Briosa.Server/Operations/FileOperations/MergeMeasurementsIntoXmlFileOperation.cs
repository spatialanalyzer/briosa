using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class MergeMeasurementsIntoXmlFileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.merge_measurements_into_xml_file", "Merge Measurements into XML File",
        "briosa.FileOperations", "MergeMeasurementsIntoXmlFile", "/briosa.FileOperations/MergeMeasurementsIntoXmlFile",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MergeMeasurementsIntoXmlFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("File Path", WorkerMpValueKind.FileReference,
                    FileReferenceMapper.Required(request.FilePath, "file_path"), "SetFilePathArg"),
                new("Group Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.GroupName, "group_name"), "SetCollectionObjectNameArg2")], []);
    }

    public static Api.MergeMeasurementsIntoXmlFileResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
