using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class HtmlDisplayBoardOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.html_display_board", "HTML Display Board", "briosa.ReportingOperations",
        "HtmlDisplayBoard", "/briosa.ReportingOperations/HtmlDisplayBoard", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["fixture_validation_pending"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.HtmlDisplayBoardRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Input HTML File", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.InputHtmlFile, "input_html_file"), "SetFilePathArg"),
            new("Show Board?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasShowBoard ? request.ShowBoard : true), "SetBoolArg")
        ], []);
    }

    public static Api.HtmlDisplayBoardResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
