using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class GetCustomTableCellDoubleOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.get_custom_table_cell_double", "Get Custom Table Cell Double", "briosa.ReportingOperations",
        "GetCustomTableCellDouble", "/briosa.ReportingOperations/GetCustomTableCellDouble", "read_only",
        Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("value", "Value", WorkerMpValueKind.FloatingPoint)];

    public static WorkerMpCommand CreateCommand(Api.GetCustomTableCellDoubleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Table Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.TableName, "table_name"), "SetCollectionObjectNameArg2"),
            new("Row", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.Row), "SetIntegerArg"),
            new("Column", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.Column), "SetIntegerArg")
        ], [new("Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")]);
    }

    public static Api.GetCustomTableCellDoubleResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details,
        Value = completed.Execution.OutputValues[0].RequireValue<WorkerDoubleValue>().Value
    };
}
