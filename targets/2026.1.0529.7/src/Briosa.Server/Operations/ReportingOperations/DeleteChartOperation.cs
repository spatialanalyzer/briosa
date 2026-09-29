using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class DeleteChartOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.delete_chart", "Delete Chart", "briosa.ReportingOperations",
        "DeleteChart", "/briosa.ReportingOperations/DeleteChart", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.DeleteChartRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Chart Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ChartName, "chart_name"), "SetCollectionObjectNameArg2")], []);
    }

    public static Api.DeleteChartResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
