using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class AddCustomTablesToReportBarOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.add_custom_tables_to_report_bar", "Add Custom Tables to Report Bar", "briosa.ReportingOperations",
        "AddCustomTablesToReportBar", "/briosa.ReportingOperations/AddCustomTablesToReportBar", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.AddCustomTablesToReportBarRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Custom Table(s) To Report", WorkerMpValueKind.CollectionItemNameList,
                CollectionItemNameMapper.RequiredList(request.CustomTablesToReport, "custom_tables_to_report"),
                "SetCollectionObjectNameRefListArg"),
            new("Clear Existing?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.ClearExisting), "SetBoolArg")
        ], []);
    }

    public static Api.AddCustomTablesToReportBarResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
