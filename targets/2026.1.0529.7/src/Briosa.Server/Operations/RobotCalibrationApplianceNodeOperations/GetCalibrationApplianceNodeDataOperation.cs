using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotCalibrationApplianceNodeOperations;

internal static class GetCalibrationApplianceNodeDataOperation
{
    public static OperationDescriptor Descriptor { get; } = CalibrationApplianceNodeOperation.ReadOnlyDescriptor(
        "robot_calibration_appliance_node_operations.get_calibration_appliance_node_data",
        "Get Calibration Appliance Node Data", "GetCalibrationApplianceNodeData");

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("real_values", "Real Values", WorkerMpValueKind.DoubleArray)];

    public static WorkerMpCommand CreateCommand(Api.GetCalibrationApplianceNodeDataRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var arraySize = request.HasRealValueCount ? request.RealValueCount : (int?)null;
        return CalibrationApplianceNodeOperation.Command(Descriptor,
            [CalibrationApplianceNodeOperation.NodeArgument(request.CalibrationApplianceNode)],
            [new("Real Values", WorkerMpValueKind.DoubleArray, "GetDoubleArrayArg", ArraySize: arraySize)]);
    }

    public static Api.GetCalibrationApplianceNodeDataResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.GetCalibrationApplianceNodeDataResult { Execution = completed.Details };
        result.RealValues.AddRange(completed.Execution.OutputValues[0].RequireValue<WorkerDoubleArrayValue>().Values);
        return result;
    }
}
