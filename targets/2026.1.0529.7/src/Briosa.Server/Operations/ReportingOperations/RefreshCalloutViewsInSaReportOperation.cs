using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class RefreshCalloutViewsInSaReportOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.refresh_callout_views_in_sa_report", "Refresh Callout Views in SA Report", "briosa.ReportingOperations",
        "RefreshCalloutViewsInSaReport", "/briosa.ReportingOperations/RefreshCalloutViewsInSaReport", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.RefreshCalloutViewsInSaReportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [new("Report Name", WorkerMpValueKind.CollectionItemName,
            CollectionItemNameMapper.Required(request.ReportName, "report_name"), "SetCollectionObjectNameArg2")], []);
    }

    public static Api.RefreshCalloutViewsInSaReportResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
