using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class SetReportBarVisibilityOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.set_report_bar_visibility", "Set Report Bar Visibility", "briosa.ReportingOperations",
        "SetReportBarVisibility", "/briosa.ReportingOperations/SetReportBarVisibility", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetReportBarVisibilityRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [new("Show Report Bar?", WorkerMpValueKind.Logical,
            new WorkerBooleanValue(request.ShowReportBar), "SetBoolArg")], []);
    }

    public static Api.SetReportBarVisibilityResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
