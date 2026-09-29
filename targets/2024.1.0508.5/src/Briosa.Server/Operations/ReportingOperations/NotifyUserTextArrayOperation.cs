using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class NotifyUserTextArrayOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.notify_user_text_array", "Notify User Text Array", "briosa.ReportingOperations",
        "NotifyUserTextArray", "/briosa.ReportingOperations/NotifyUserTextArray", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["fixture_validation_pending"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.NotifyUserTextArrayRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Notification Text", WorkerMpValueKind.EditText,
                StringListMapper.RequiredList(request.NotificationText, "notification_text"), "SetEditTextArg"),
            new("Font", WorkerMpValueKind.Font, FontMapper.WithDefaults(request.Font), "SetFontTypeArg"),
            new("Auto expand to fit text?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.AutoExpandToFitText), "SetBoolArg"),
            new("Display Timeout", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.DisplayTimeout), "SetIntegerArg")
        ], []);
    }

    public static Api.NotifyUserTextArrayResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
