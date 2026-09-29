using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class NotifyUserIntegerOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.notify_user_integer", "Notify User Integer", "briosa.ReportingOperations",
        "NotifyUserInteger", "/briosa.ReportingOperations/NotifyUserInteger", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["fixture_validation_pending"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.NotifyUserIntegerRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Leading Text", WorkerMpValueKind.Text, new WorkerTextValue(request.LeadingText), "SetStringArg"),
            new("Font", WorkerMpValueKind.Font, FontMapper.WithDefaults(request.Font), "SetFontTypeArg"),
            new("Reported Value", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.ReportedValue), "SetIntegerArg"),
            new("Display Timeout", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.DisplayTimeout), "SetIntegerArg")
        ], []);
    }

    public static Api.NotifyUserIntegerResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
