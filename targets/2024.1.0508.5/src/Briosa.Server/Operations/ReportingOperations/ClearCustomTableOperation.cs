using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class ClearCustomTableOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.clear_custom_table", "Clear Custom Table", "briosa.ReportingOperations",
        "ClearCustomTable", "/briosa.ReportingOperations/ClearCustomTable", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ClearCustomTableRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Table Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.TableName, "table_name"), "SetCollectionObjectNameArg2")], []);
    }

    public static Api.ClearCustomTableResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
