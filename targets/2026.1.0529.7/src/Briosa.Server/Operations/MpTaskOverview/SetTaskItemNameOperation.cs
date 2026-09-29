using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.MpTaskOverview;

internal static class SetTaskItemNameOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "mp_task_overview.set_task_item_name", "Set Task Item Name",
        "briosa.MpTaskOverview", "SetTaskItemName", "/briosa.MpTaskOverview/SetTaskItemName",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.SetTaskItemNameRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Task Item Index", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.TaskItemIndex), "SetIntegerArg"),
            new("Task Name", WorkerMpValueKind.Text, new WorkerTextValue(request.TaskName), "SetStringArg")
        ], []);
    }
    public static Api.SetTaskItemNameResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
