using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class AddCustomTableToSaReportOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.add_custom_table_to_sa_report", "Add Custom Table to SA Report", "briosa.ReportingOperations",
        "AddCustomTableToSaReport", "/briosa.ReportingOperations/AddCustomTableToSaReport", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.AddCustomTableToSaReportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Table Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.TableName, "table_name"), "SetCollectionObjectNameArg2"),
            new("Report Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ReportName, "report_name"), "SetCollectionObjectNameArg2"),
            new("Show Report?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.ShowReport), "SetBoolArg")
        ], []);
    }

    public static Api.AddCustomTableToSaReportResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
