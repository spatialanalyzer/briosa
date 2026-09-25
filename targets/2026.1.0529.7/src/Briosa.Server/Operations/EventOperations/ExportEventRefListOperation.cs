using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.EventOperations;

internal static class ExportEventRefListOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "event_operations.export_event_ref_list", "Export Event Ref List",
        "briosa.EventOperations", "ExportEventRefList", "/briosa.EventOperations/ExportEventRefList",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ExportEventRefListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Event List", WorkerMpValueKind.CollectionItemNameList,
                    CollectionItemNameMapper.RequiredList(request.EventList, "event_list"), "SetCollectionObjectNameRefListArg"),
                new("File Path", WorkerMpValueKind.FileReference,
                    FileReferenceMapper.Required(request.FilePath, "file_path"), "SetFilePathArg"),
                new("Decimal Precision", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.HasDecimalPrecision ? request.DecimalPrecision : 6), "SetIntegerArg"),
                new("Overwrite existing file?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.OverwriteExistingFile), "SetBoolArg")
            ], []);
    }

    public static Api.ExportEventRefListResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
