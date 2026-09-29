using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class SetReportTagValueFromStringOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.set_report_tag_value_from_string", "Set Report Tag Value From String", "briosa.ReportingOperations",
        "SetReportTagValueFromString", "/briosa.ReportingOperations/SetReportTagValueFromString", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetReportTagValueFromStringRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Tag Name", WorkerMpValueKind.Text, new WorkerTextValue(request.TagName), "SetStringArg"),
            new("Tag Value", WorkerMpValueKind.Text, new WorkerTextValue(request.TagValue), "SetStringArg")
        ], []);
    }

    public static Api.SetReportTagValueFromStringResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
