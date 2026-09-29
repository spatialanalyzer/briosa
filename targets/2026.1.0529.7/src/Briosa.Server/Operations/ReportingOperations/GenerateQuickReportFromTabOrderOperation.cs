using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class GenerateQuickReportFromTabOrderOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.generate_quick_report_from_tab_order", "Generate Quick Report from Tab Order",
        "briosa.ReportingOperations", "GenerateQuickReportFromTabOrder",
        "/briosa.ReportingOperations/GenerateQuickReportFromTabOrder", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.GenerateQuickReportFromTabOrderRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Report Output Options", WorkerMpValueKind.ReportOutputOptions,
                ReportOutputOptionsMapper.WithDefault(request.ReportOutputOptions), "SetReportOutputOptionsArg"),
            new("Open Report?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.OpenReport), "SetBoolArg")
        ], []);
    }

    public static Api.GenerateQuickReportFromTabOrderResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
