using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.MpTaskOverview;

internal static class SetOverviewTitleOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "mp_task_overview.set_overview_title", "Set Overview Title",
        "briosa.MpTaskOverview", "SetOverviewTitle", "/briosa.MpTaskOverview/SetOverviewTitle",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.SetOverviewTitleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Overview Title", WorkerMpValueKind.Text, new WorkerTextValue(request.OverviewTitle), "SetStringArg")], []);
    }
    public static Api.SetOverviewTitleResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
