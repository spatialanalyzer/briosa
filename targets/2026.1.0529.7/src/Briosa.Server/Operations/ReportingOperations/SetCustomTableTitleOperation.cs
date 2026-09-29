using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class SetCustomTableTitleOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.set_custom_table_title", "Set Custom Table Title", "briosa.ReportingOperations",
        "SetCustomTableTitle", "/briosa.ReportingOperations/SetCustomTableTitle", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetCustomTableTitleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Table Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.TableName, "table_name"), "SetCollectionObjectNameArg2"),
            new("Title Line 1", WorkerMpValueKind.Text, new WorkerTextValue(request.TitleLine1), "SetStringArg"),
            new("Title Line 2", WorkerMpValueKind.Text, new WorkerTextValue(request.TitleLine2), "SetStringArg")
        ], []);
    }

    public static Api.SetCustomTableTitleResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
