using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class AppendItemsToSaReportOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.append_items_to_sa_report", "Append Items to SA Report", "briosa.ReportingOperations",
        "AppendItemsToSaReport", "/briosa.ReportingOperations/AppendItemsToSaReport", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.AppendItemsToSaReportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Report Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ReportName, "report_name"), "SetCollectionObjectNameArg2"),
            new("Items To Report", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.ItemsToReport, "items_to_report"), "SetCollectionObjectNameRefListArg"),
            new("Show Report?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.ShowReport), "SetBoolArg"),
            new("Begin On New Page?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.BeginOnNewPage), "SetBoolArg")
        ], []);
    }

    public static Api.AppendItemsToSaReportResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
