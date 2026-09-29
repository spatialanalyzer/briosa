using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class AddChartsToReportBarOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.add_charts_to_report_bar", "Add Charts to Report Bar", "briosa.ReportingOperations",
        "AddChartsToReportBar", "/briosa.ReportingOperations/AddChartsToReportBar", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.AddChartsToReportBarRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Chart(s)", WorkerMpValueKind.CollectionItemNameList,
                CollectionItemNameMapper.RequiredList(request.Charts, "charts"), "SetCollectionObjectNameRefListArg"),
            new("Clear Existing?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.ClearExisting), "SetBoolArg")
        ], []);
    }

    public static Api.AddChartsToReportBarResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
