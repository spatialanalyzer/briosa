using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class MakeNewSaReportOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.make_new_sa_report", "Make New SA Report", "briosa.ReportingOperations",
        "MakeNewSaReport", "/briosa.ReportingOperations/MakeNewSaReport", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MakeNewSaReportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var inputArguments = new List<WorkerMpInputArgument>
        {
            new("New SA Report Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.NewSaReportName, "new_sa_report_name"),
                "SetCollectionObjectNameArg2")
        };
        if (request.SaReportTemplate is not null)
        {
            inputArguments.Add(new("SA Report Template (optional)", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.SaReportTemplate, "sa_report_template"),
                "SetCollectionObjectNameArg2"));
        }

        return new(Descriptor.OperationId, Descriptor.MpStep, inputArguments, []);
    }

    public static Api.MakeNewSaReportResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
