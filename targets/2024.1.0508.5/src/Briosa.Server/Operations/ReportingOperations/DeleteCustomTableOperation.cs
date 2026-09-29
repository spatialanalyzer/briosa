using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class DeleteCustomTableOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.delete_custom_table", "Delete Custom Table", "briosa.ReportingOperations",
        "DeleteCustomTable", "/briosa.ReportingOperations/DeleteCustomTable", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.DeleteCustomTableRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Table Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.TableName, "table_name"), "SetCollectionObjectNameArg2")], []);
    }

    public static Api.DeleteCustomTableResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
