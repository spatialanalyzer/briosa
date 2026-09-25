using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.MpTaskOverview;

internal static class CreateClearTaskOverviewListOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "mp_task_overview.create_clear_task_overview_list", "Create/Clear Task Overview List",
        "briosa.MpTaskOverview", "CreateClearTaskOverviewList", "/briosa.MpTaskOverview/CreateClearTaskOverviewList",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.CreateClearTaskOverviewListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Task Name Font", WorkerMpValueKind.Font, FontMapper.WithDefaults(request.TaskNameFont), "SetFontTypeArg"),
            new("Task Comment Font", WorkerMpValueKind.Font, FontMapper.WithDefaults(request.TaskCommentFont), "SetFontTypeArg")
        ], []);
    }
    public static Api.CreateClearTaskOverviewListResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
