using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class CombineSaReportsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.combine_sa_reports", "Combine SA Reports", "briosa.ReportingOperations",
        "CombineSaReports", "/briosa.ReportingOperations/CombineSaReports", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.CombineSaReportsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("SA Reports to Combine", WorkerMpValueKind.CollectionItemNameList,
                CollectionItemNameMapper.RequiredList(request.SaReportsToCombine, "sa_reports_to_combine"),
                "SetCollectionObjectNameRefListArg"),
            new("Output SA Report Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.OutputSaReportName, "output_sa_report_name"),
                "SetCollectionObjectNameArg2"),
            new("Show Report?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.ShowReport), "SetBoolArg")
        ], []);
    }

    public static Api.CombineSaReportsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
