using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class GenerateStandardHtmlReportOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.generate_standard_html_report", "Generate Standard HTML Report", "briosa.ReportingOperations",
        "GenerateStandardHtmlReport", "/briosa.ReportingOperations/GenerateStandardHtmlReport", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.GenerateStandardHtmlReportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("HTML Output File", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.HtmlOutputFile, "html_output_file"), "SetFilePathArg"),
            new("Decimal Precision", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.DecimalPrecision), "SetIntegerArg")
        ], []);
    }

    public static Api.GenerateStandardHtmlReportResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
