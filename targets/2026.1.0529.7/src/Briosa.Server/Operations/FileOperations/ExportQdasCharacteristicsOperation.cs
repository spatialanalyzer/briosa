using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class ExportQdasCharacteristicsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.export_qdas_characteristics", "Export QDAS Characteristics", "briosa.FileOperations",
        "ExportQdasCharacteristics", "/briosa.FileOperations/ExportQdasCharacteristics", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["fixture_validation_pending"]);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ExportQdasCharacteristicsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var dateTimeStamp = RequiredDateTimeStamp(
            request.HasK0004DateTimeStamp, request.K0004DateTimeStamp);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("QDAS Export File Path", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.QdasExportFilePath, "qdas_export_file_path"), "SetFilePathArg"),
            new("K1001: Part Number", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasK1001PartNumber ? request.K1001PartNumber : string.Empty), "SetStringArg"),
            new("K1002: Part Description", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasK1002PartDescription ? request.K1002PartDescription : string.Empty), "SetStringArg"),
            new("K1071: Supplier Number", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasK1071SupplierNumber ? request.K1071SupplierNumber : string.Empty), "SetStringArg"),
            new("K1072: Supplier Description", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasK1072SupplierDescription ? request.K1072SupplierDescription : string.Empty), "SetStringArg"),
            new("K1203: Reason for Test", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasK1203ReasonForTest ? request.K1203ReasonForTest : string.Empty), "SetStringArg"),
            new("K1303: Plant", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasK1303Plant ? request.K1303Plant : string.Empty), "SetStringArg"),
            new("K1900: Part Remark", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasK1900PartRemark ? request.K1900PartRemark : string.Empty), "SetStringArg"),
            new("K0006: Batch Number", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasK0006BatchNumber ? request.K0006BatchNumber : string.Empty), "SetStringArg"),
            new("K0014: Part ID", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasK0014PartId ? request.K0014PartId : string.Empty), "SetStringArg"),
            new("K0053: Order Number", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasK0053OrderNumber ? request.K0053OrderNumber : string.Empty), "SetStringArg"),
            new("K0004: Date Time Stamp", WorkerMpValueKind.Text,
                new WorkerTextValue(dateTimeStamp), "SetStringArg"),
            new("K0008: Operator Identifier", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.HasK0008OperatorIdentifier ? request.K0008OperatorIdentifier : -1), "SetIntegerArg"),
            new("K0010: Machine Identifier", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.HasK0010MachineIdentifier ? request.K0010MachineIdentifier : -1), "SetIntegerArg"),
            new("K0012: Gage Identifier", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.HasK0012GageIdentifier ? request.K0012GageIdentifier : -1), "SetIntegerArg"),
            new("Relationship List", WorkerMpValueKind.CollectionItemNameList,
                CollectionItemNameMapper.RequiredList(request.RelationshipList, "relationship_list"), "SetCollectionObjectNameRefListArg"),
            new("Feature Check List", WorkerMpValueKind.CollectionItemNameList,
                CollectionItemNameMapper.RequiredList(request.FeatureCheckList, "feature_check_list"), "SetCollectionObjectNameRefListArg"),
            new("Vector Group List", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.VectorGroupList, "vector_group_list"), "SetCollectionObjectNameRefListArg")
        ], []);
    }

    private static string RequiredDateTimeStamp(bool hasValue, string value)
    {
        // Captured catalog timestamps are observations, not reusable defaults.
        if (!hasValue || string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Request field 'k0004_date_time_stamp' is required.", nameof(value));
        }

        return value;
    }

    public static Api.ExportQdasCharacteristicsResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
