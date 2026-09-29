using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotOperations;

internal static class GetCalibrationApplianceDataOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "robot_operations.get_calibration_appliance_data", "Get Calibration Appliance Data",
        "briosa.RobotOperations", "GetCalibrationApplianceData", "/briosa.RobotOperations/GetCalibrationApplianceData",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("real_values", "Real Values", WorkerMpValueKind.DoubleArray)];

    public static WorkerMpCommand CreateCommand(Api.GetCalibrationApplianceDataRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var arraySize = request.HasRealValueCount ? request.RealValueCount : (int?)null;
        return new(Descriptor.OperationId, Descriptor.MpStep, [],
            [new("Real Values", WorkerMpValueKind.DoubleArray, "GetDoubleArrayArg", ArraySize: arraySize)]);
    }

    public static Api.GetCalibrationApplianceDataResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.GetCalibrationApplianceDataResult { Execution = completed.Details };
        result.RealValues.AddRange(completed.Execution.OutputValues[0].RequireValue<WorkerDoubleArrayValue>().Values);
        return result;
    }
}
