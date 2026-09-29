using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class SaveChartToJPegFileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.save_chart_to_jpeg_file", "Save Chart to JPeg file", "briosa.ReportingOperations",
        "SaveChartToJPegFile", "/briosa.ReportingOperations/SaveChartToJPegFile", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SaveChartToJPegFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Chart to Save", WorkerMpValueKind.ChartName,
                ChartNameMapper.Required(request.ChartToSave, "chart_to_save"), "SetChartNameArg"),
            new("File to save to", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.FileToSaveTo, "file_to_save_to"), "SetFilePathArg")
        ], []);
    }

    public static Api.SaveChartToJPegFileResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
