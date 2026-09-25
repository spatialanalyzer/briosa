using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.MpTaskOverview;

internal static class ShowTaskOverviewListOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "mp_task_overview.show_task_overview_list", "Show Task Overview List",
        "briosa.MpTaskOverview", "ShowTaskOverviewList", "/briosa.MpTaskOverview/ShowTaskOverviewList",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.ShowTaskOverviewListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Show?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.Show), "SetBoolArg")], []);
    }
    public static Api.ShowTaskOverviewListResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
