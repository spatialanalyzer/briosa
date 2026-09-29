using Briosa.Server.Operations.Values;
using Briosa.Server.Operations.ViewControl;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class SetCustomTableCellColorOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.set_custom_table_cell_color", "Set Custom Table Cell Color", "briosa.ReportingOperations",
        "SetCustomTableCellColor", "/briosa.ReportingOperations/SetCustomTableCellColor", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetCustomTableCellColorRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Table Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.TableName, "table_name"), "SetCollectionObjectNameArg2"),
            new("Row", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.Row), "SetIntegerArg"),
            new("Column", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.Column), "SetIntegerArg"),
            new("Foreground Color Name", WorkerMpValueKind.RgbColor,
                ViewControlColorMapper.WithDefault(request.ForegroundColorName), "SetColorArg"),
            new("Background Color Name", WorkerMpValueKind.RgbColor,
                ViewControlColorMapper.WithDefault(request.BackgroundColorName), "SetColorArg")
        ], []);
    }

    public static Api.SetCustomTableCellColorResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
