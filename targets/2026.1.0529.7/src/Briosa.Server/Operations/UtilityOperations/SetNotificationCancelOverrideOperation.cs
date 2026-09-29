using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class SetNotificationCancelOverrideOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.set_notification_cancel_override", "Set Notification Cancel Override", "briosa.UtilityOperations",
        "SetNotificationCancelOverride", "/briosa.UtilityOperations/SetNotificationCancelOverride", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetNotificationCancelOverrideRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Prohibit Cancel?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasProhibitCancel || request.ProhibitCancel), "SetBoolArg")], []);
    }

    public static Api.SetNotificationCancelOverrideResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
