using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class CloseHtmlDisplayBoardOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.close_html_display_board", "Close HTML Display Board", "briosa.ReportingOperations",
        "CloseHtmlDisplayBoard", "/briosa.ReportingOperations/CloseHtmlDisplayBoard", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["fixture_validation_pending"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.CloseHtmlDisplayBoardRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep, [], []);
    }

    public static Api.CloseHtmlDisplayBoardResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
