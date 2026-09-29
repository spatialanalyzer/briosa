using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class SetCustomTableHeaderRowOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.set_custom_table_header_row", "Set Custom Table Header Row", "briosa.ReportingOperations",
        "SetCustomTableHeaderRow", "/briosa.ReportingOperations/SetCustomTableHeaderRow", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetCustomTableHeaderRowRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Table Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.TableName, "table_name"), "SetCollectionObjectNameArg2"),
            new("Row", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.Row), "SetIntegerArg"),
            new("Value", WorkerMpValueKind.Text, new WorkerTextValue(request.Value), "SetStringArg")
        ], []);
    }

    public static Api.SetCustomTableHeaderRowResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
