using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class OutputSaReportToPdfOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.output_sa_report_to_pdf", "Output SA Report to PDF", "briosa.ReportingOperations",
        "OutputSaReportToPdf", "/briosa.ReportingOperations/OutputSaReportToPdf", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.OutputSaReportToPdfRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Report Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ReportName, "report_name"), "SetCollectionObjectNameArg2"),
            new("File Name", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.FileName, "file_name"), "SetFilePathArg"),
            new("Show PDF?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.ShowPdf), "SetBoolArg")
        ], []);
    }

    public static Api.OutputSaReportToPdfResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
