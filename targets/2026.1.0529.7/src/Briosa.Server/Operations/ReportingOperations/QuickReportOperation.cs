using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class QuickReportOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.quick_report", "Quick Report", "briosa.ReportingOperations",
        "QuickReport", "/briosa.ReportingOperations/QuickReport", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.QuickReportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Item Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ItemName, "item_name"), "SetCollectionObjectNameArg2"),
            new("Report Name (optional)", WorkerMpValueKind.Text, new WorkerTextValue(request.ReportName), "SetStringArg"),
            new("Open Report?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.OpenReport), "SetBoolArg")
        ], []);
    }

    public static Api.QuickReportResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
