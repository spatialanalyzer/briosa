using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class NotifyUserDoubleOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.notify_user_double", "Notify User Double", "briosa.ReportingOperations",
        "NotifyUserDouble", "/briosa.ReportingOperations/NotifyUserDouble", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["fixture_validation_pending"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.NotifyUserDoubleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Leading Text", WorkerMpValueKind.Text, new WorkerTextValue(request.LeadingText), "SetStringArg"),
            new("Font", WorkerMpValueKind.Font, FontMapper.WithDefaults(request.Font), "SetFontTypeArg"),
            new("Reported Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.ReportedValue), "SetDoubleArg"),
            new("Decimal Precision", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.DecimalPrecision), "SetIntegerArg"),
            new("Display Timeout", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.DisplayTimeout), "SetIntegerArg")
        ], []);
    }

    public static Api.NotifyUserDoubleResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
