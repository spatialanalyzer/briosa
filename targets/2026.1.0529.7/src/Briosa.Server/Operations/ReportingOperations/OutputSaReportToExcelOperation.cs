using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class OutputSaReportToExcelOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.output_sa_report_to_excel", "Output SA Report to Excel", "briosa.ReportingOperations",
        "OutputSaReportToExcel", "/briosa.ReportingOperations/OutputSaReportToExcel", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.OutputSaReportToExcelRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Report Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ReportName, "report_name"), "SetCollectionObjectNameArg2"),
            new("File Name", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.FileName, "file_name"), "SetFilePathArg"),
            new("Show File?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.ShowFile), "SetBoolArg")
        ], []);
    }

    public static Api.OutputSaReportToExcelResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
