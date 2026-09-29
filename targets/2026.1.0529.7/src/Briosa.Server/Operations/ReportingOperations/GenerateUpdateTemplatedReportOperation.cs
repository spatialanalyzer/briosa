using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class GenerateUpdateTemplatedReportOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.generate_update_templated_report", "Generate/Update Templated Report",
        "briosa.ReportingOperations", "GenerateUpdateTemplatedReport",
        "/briosa.ReportingOperations/GenerateUpdateTemplatedReport", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.GenerateUpdateTemplatedReportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Report Template", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ReportTemplate, "report_template"),
                "SetCollectionObjectNameArg2")], []);
    }

    public static Api.GenerateUpdateTemplatedReportResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
