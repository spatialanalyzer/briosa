using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class StatusDialogOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.status_dialog", "Status Dialog", "briosa.UtilityOperations",
        "StatusDialog", "/briosa.UtilityOperations/StatusDialog", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.StatusDialogRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Dialog Title", WorkerMpValueKind.Text, new WorkerTextValue(request.DialogTitle), "SetStringArg"),
            new("Text Message", WorkerMpValueKind.Text, new WorkerTextValue(request.TextMessage), "SetStringArg"),
            new("Current Position", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.CurrentPosition), "SetIntegerArg"),
            new("Upper Limit", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.UpperLimit), "SetIntegerArg"),
            new("Suppress Time Remaining?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasSuppressTimeRemaining || request.SuppressTimeRemaining), "SetBoolArg"),
            new("Close Dialog?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.CloseDialog), "SetBoolArg")
        ], []);
    }

    public static Api.StatusDialogResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
