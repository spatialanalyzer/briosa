using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class MakeUtilityChartOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.make_utility_chart", "Make Utility Chart", "briosa.ReportingOperations",
        "MakeUtilityChart", "/briosa.ReportingOperations/MakeUtilityChart", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["fixture_validation_pending"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("is_point_inside", "Is Point Inside?", WorkerMpValueKind.Logical)];

    public static WorkerMpCommand CreateCommand(Api.MakeUtilityChartRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("ASCII File Path", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.AsciiFilePath, "ascii_file_path"), "SetFilePathArg"),
            new("Chart Title Override", WorkerMpValueKind.Text,
                new WorkerTextValue(request.ChartTitleOverride), "SetStringArg"),
            new("Output Picture Name", WorkerMpValueKind.CollectionItemName,
                CollectionItemNameMapper.Required(request.OutputPictureName, "output_picture_name"), "SetCollectionObjectNameArg2"),
            new("Show Chart Dialog?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.ShowChartDialog), "SetBoolArg"),
            new("Plot Additional XY Value?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.PlotAdditionalXyValue), "SetBoolArg"),
            new("X Value", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.XValue), "SetDoubleArg"),
            new("Y Value", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.YValue), "SetDoubleArg")
        ], [new("Is Point Inside?", WorkerMpValueKind.Logical, "GetBoolArg")]);
    }

    public static Api.MakeUtilityChartResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details,
        IsPointInside = completed.Execution.OutputValues[0].RequireValue<WorkerBooleanValue>().Value
    };
}
