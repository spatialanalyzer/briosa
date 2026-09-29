using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotCalibrationApplianceNodeOperations;

internal static class GetCalibrationApplianceNodeRealValueOperation
{
    public static OperationDescriptor Descriptor { get; } = CalibrationApplianceNodeOperation.ReadOnlyDescriptor(
        "robot_calibration_appliance_node_operations.get_calibration_appliance_node_real_value",
        "Get Calibration Appliance Node Real Value", "GetCalibrationApplianceNodeRealValue");

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("real_value", "Real Value", WorkerMpValueKind.FloatingPoint)];

    public static WorkerMpCommand CreateCommand(Api.GetCalibrationApplianceNodeRealValueRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var index = request.HasIndexOffset ? request.IndexOffset : 0;
        return CalibrationApplianceNodeOperation.Command(Descriptor,
            [CalibrationApplianceNodeOperation.NodeArgument(request.CalibrationApplianceNode),
             new WorkerMpInputArgument("Index Offset", WorkerMpValueKind.WholeNumber,
                 new WorkerIntegerValue(index), "SetIntegerArg")],
            [new("Real Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")]);
    }

    public static Api.GetCalibrationApplianceNodeRealValueResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        RealValue = completed.Execution.OutputValues[0].RequireValue<WorkerDoubleValue>().Value,
        Execution = completed.Details
    };
}
