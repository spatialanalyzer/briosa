using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.MpTaskOverview;

internal static class SetCurrentTaskOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "mp_task_overview.set_current_task", "Set Current Task",
        "briosa.MpTaskOverview", "SetCurrentTask", "/briosa.MpTaskOverview/SetCurrentTask",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.SetCurrentTaskRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Task Index", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.TaskIndex), "SetIntegerArg")], []);
    }
    public static Api.SetCurrentTaskResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
