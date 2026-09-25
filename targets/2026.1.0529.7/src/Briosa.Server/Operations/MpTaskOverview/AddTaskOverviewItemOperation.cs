using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.MpTaskOverview;

internal static class AddTaskOverviewItemOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "mp_task_overview.add_task_overview_item", "Add Task Overview Item",
        "briosa.MpTaskOverview", "AddTaskOverviewItem", "/briosa.MpTaskOverview/AddTaskOverviewItem",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.AddTaskOverviewItemRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Task Name", WorkerMpValueKind.Text, new WorkerTextValue(request.TaskName), "SetStringArg"),
            new("Comment Text", WorkerMpValueKind.Text, new WorkerTextValue(request.CommentText), "SetStringArg"),
            new("Effort Index", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.EffortIndex), "SetDoubleArg")
        ], []);
    }
    public static Api.AddTaskOverviewItemResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
