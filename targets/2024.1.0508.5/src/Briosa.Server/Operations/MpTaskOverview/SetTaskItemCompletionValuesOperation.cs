using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.MpTaskOverview;

internal static class SetTaskItemCompletionValuesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "mp_task_overview.set_task_item_completion_values", "Set Task Item Completion Values",
        "briosa.MpTaskOverview", "SetTaskItemCompletionValues", "/briosa.MpTaskOverview/SetTaskItemCompletionValues",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.SetTaskItemCompletionValuesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Task Index", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.TaskIndex), "SetIntegerArg"),
            new("Increments Completed", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.IncrementsCompleted), "SetIntegerArg"),
            new("Total Increments", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.TotalIncrements), "SetIntegerArg")
        ], []);
    }
    public static Api.SetTaskItemCompletionValuesResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
