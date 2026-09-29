using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class SetCustomTableHeaderCellOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.set_custom_table_header_cell", "Set Custom Table Header Cell", "briosa.ReportingOperations",
        "SetCustomTableHeaderCell", "/briosa.ReportingOperations/SetCustomTableHeaderCell", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetCustomTableHeaderCellRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Table Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.TableName, "table_name"), "SetCollectionObjectNameArg2"),
            new("Row", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.Row), "SetIntegerArg"),
            new("Column", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.Column), "SetIntegerArg"),
            new("Header Text", WorkerMpValueKind.Text, new WorkerTextValue(request.HeaderText), "SetStringArg"),
            new("Span", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.HasSpan ? request.Span : 1), "SetIntegerArg")
        ], []);
    }

    public static Api.SetCustomTableHeaderCellResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
