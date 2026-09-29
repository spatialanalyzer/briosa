using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class AddItemToSaReportAtLocationOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.add_item_to_sa_report_at_location", "Add Item to SA Report at Location", "briosa.ReportingOperations",
        "AddItemToSaReportAtLocation", "/briosa.ReportingOperations/AddItemToSaReportAtLocation", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.AddItemToSaReportAtLocationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Report Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ReportName, "report_name"), "SetCollectionObjectNameArg2"),
            new("Item Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ItemName, "item_name"), "SetCollectionObjectNameArg2"),
            new("Page Number", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.PageNumber), "SetIntegerArg"),
            new("Horizontal Location", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasHorizontalLocation ? request.HorizontalLocation : 1d), "SetDoubleArg"),
            new("Vertical Location", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasVerticalLocation ? request.VerticalLocation : 1d), "SetDoubleArg"),
            new("Show Report?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.ShowReport), "SetBoolArg")
        ], []);
    }

    public static Api.AddItemToSaReportAtLocationResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
