using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.MpTaskOverview;

internal static class SetTaskItemCommentOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "mp_task_overview.set_task_item_comment", "Set Task Item Comment",
        "briosa.MpTaskOverview", "SetTaskItemComment", "/briosa.MpTaskOverview/SetTaskItemComment",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.SetTaskItemCommentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Task Index", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.TaskIndex), "SetIntegerArg"),
            new("Task Comment", WorkerMpValueKind.Text, new WorkerTextValue(request.TaskComment), "SetStringArg")
        ], []);
    }
    public static Api.SetTaskItemCommentResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
