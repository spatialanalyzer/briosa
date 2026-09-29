using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class SetReportTagValueFromDoubleOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.set_report_tag_value_from_double", "Set Report Tag Value From Double", "briosa.ReportingOperations",
        "SetReportTagValueFromDouble", "/briosa.ReportingOperations/SetReportTagValueFromDouble", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetReportTagValueFromDoubleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Tag Name", WorkerMpValueKind.Text, new WorkerTextValue(request.TagName), "SetStringArg"),
            new("Tag Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.TagValue), "SetDoubleArg")
        ], []);
    }

    public static Api.SetReportTagValueFromDoubleResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
