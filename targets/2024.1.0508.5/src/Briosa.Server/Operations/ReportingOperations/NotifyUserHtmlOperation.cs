using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class NotifyUserHtmlOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.notify_user_html", "Notify User HTML", "briosa.ReportingOperations",
        "NotifyUserHtml", "/briosa.ReportingOperations/NotifyUserHtml", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.NotifyUserHtmlRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("HTML File", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.HtmlFile, "html_file"), "SetFilePathArg")], []);
    }

    public static Api.NotifyUserHtmlResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
