using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.MpTaskOverview;

internal static class ShowProgressForTaskItemOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "mp_task_overview.show_progress_for_task_item", "Show Progress for Task Item",
        "briosa.MpTaskOverview", "ShowProgressForTaskItem", "/briosa.MpTaskOverview/ShowProgressForTaskItem",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.ShowProgressForTaskItemRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Task Index", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.TaskIndex), "SetIntegerArg"),
            new("Show Progress?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.ShowProgress), "SetBoolArg")
        ], []);
    }
    public static Api.ShowProgressForTaskItemResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
