using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class SetReportTagValueFromIntegerOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.set_report_tag_value_from_integer", "Set Report Tag Value From Integer", "briosa.ReportingOperations",
        "SetReportTagValueFromInteger", "/briosa.ReportingOperations/SetReportTagValueFromInteger", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetReportTagValueFromIntegerRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Tag Name", WorkerMpValueKind.Text, new WorkerTextValue(request.TagName), "SetStringArg"),
            new("Tag Value", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.TagValue), "SetIntegerArg")
        ], []);
    }

    public static Api.SetReportTagValueFromIntegerResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
