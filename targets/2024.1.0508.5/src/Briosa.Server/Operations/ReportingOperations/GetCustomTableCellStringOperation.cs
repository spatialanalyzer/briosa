using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class GetCustomTableCellStringOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.get_custom_table_cell_string", "Get Custom Table Cell String", "briosa.ReportingOperations",
        "GetCustomTableCellString", "/briosa.ReportingOperations/GetCustomTableCellString", "read_only",
        Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("value", "Value", WorkerMpValueKind.Text)];

    public static WorkerMpCommand CreateCommand(Api.GetCustomTableCellStringRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Table Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.TableName, "table_name"), "SetCollectionObjectNameArg2"),
            new("Row", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.Row), "SetIntegerArg"),
            new("Column", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.Column), "SetIntegerArg")
        ], [new("Value", WorkerMpValueKind.Text, "GetStringArg")]);
    }

    public static Api.GetCustomTableCellStringResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details,
        Value = completed.Execution.OutputValues[0].RequireValue<WorkerTextValue>().Value
    };
}
