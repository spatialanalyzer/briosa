using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class RemoveReportTagOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.remove_report_tag", "Remove Report Tag", "briosa.ReportingOperations",
        "RemoveReportTag", "/briosa.ReportingOperations/RemoveReportTag", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.RemoveReportTagRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Tag Name", WorkerMpValueKind.Text, new WorkerTextValue(request.TagName), "SetStringArg")], []);
    }

    public static Api.RemoveReportTagResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
