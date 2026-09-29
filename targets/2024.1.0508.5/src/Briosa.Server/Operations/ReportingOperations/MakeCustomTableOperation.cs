using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class MakeCustomTableOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.make_custom_table", "Make Custom Table", "briosa.ReportingOperations",
        "MakeCustomTable", "/briosa.ReportingOperations/MakeCustomTable", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MakeCustomTableRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Table Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.TableName, "table_name"), "SetCollectionObjectNameArg2"),
            new("Decimal Precision", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.HasDecimalPrecision ? request.DecimalPrecision : 6), "SetIntegerArg")
        ], []);
    }

    public static Api.MakeCustomTableResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
