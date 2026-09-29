using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class GetMeasurementInfoDataOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.get_measurement_info_data", "Get Measurement Info Data",
        "briosa.AnalysisOperations", "GetMeasurementInfoData", "/briosa.AnalysisOperations/GetMeasurementInfoData",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, ["fixture_validation_pending"]);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("info_data", "Info Data", WorkerMpValueKind.Text)];

    public static WorkerMpCommand CreateCommand(Api.GetMeasurementInfoDataRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.PointName, "point_name"), "SetPointNameArg")],
            [new("Info Data", WorkerMpValueKind.Text, "GetStringArg")]);
    }

    public static Api.GetMeasurementInfoDataResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        InfoData = completed.Execution.OutputValues[0].RequireValue<WorkerTextValue>().Value,
        Execution = completed.Details
    };
}
