using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class DeleteSaReportTemplateOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.delete_sa_report_template", "Delete SA Report Template", "briosa.ReportingOperations",
        "DeleteSaReportTemplate", "/briosa.ReportingOperations/DeleteSaReportTemplate", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.DeleteSaReportTemplateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Report Template Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ReportTemplateName, "report_template_name"),
                "SetCollectionObjectNameArg2")], []);
    }

    public static Api.DeleteSaReportTemplateResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
