using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class GetMeasurementAuxiliaryDataOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.get_measurement_auxiliary_data", "Get Measurement Auxiliary Data",
        "briosa.AnalysisOperations", "GetMeasurementAuxiliaryData", "/briosa.AnalysisOperations/GetMeasurementAuxiliaryData",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, ["fixture_validation_pending"]);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("value", "Value", WorkerMpValueKind.FloatingPoint),
        new("units", "Units", WorkerMpValueKind.Text)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetMeasurementAuxiliaryDataRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Point Name", WorkerMpValueKind.PointName,
                    PointNameMapper.Required(request.PointName, "point_name"), "SetPointNameArg"),
                new("Auxiliary Name", WorkerMpValueKind.Text, new WorkerTextValue(request.AuxiliaryName), "SetStringArg")
            ],
            [
                new("Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Units", WorkerMpValueKind.Text, "GetStringArg")
            ]);
    }

    public static Api.GetMeasurementAuxiliaryDataResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            Value = values[0].RequireValue<WorkerDoubleValue>().Value,
            Units = values[1].RequireValue<WorkerTextValue>().Value,
            Execution = completed.Details
        };
    }
}
