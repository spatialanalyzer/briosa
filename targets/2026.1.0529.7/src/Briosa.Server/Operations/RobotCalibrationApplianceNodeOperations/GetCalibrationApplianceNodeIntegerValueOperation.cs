using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotCalibrationApplianceNodeOperations;

internal static class GetCalibrationApplianceNodeIntegerValueOperation
{
    public static OperationDescriptor Descriptor { get; } = CalibrationApplianceNodeOperation.ReadOnlyDescriptor(
        "robot_calibration_appliance_node_operations.get_calibration_appliance_node_integer_value",
        "Get Calibration Appliance Node Integer Value", "GetCalibrationApplianceNodeIntegerValue");

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("integer_value", "Integer Value", WorkerMpValueKind.WholeNumber)];

    public static WorkerMpCommand CreateCommand(Api.GetCalibrationApplianceNodeIntegerValueRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var index = request.HasIndexOffset ? request.IndexOffset : 0;
        return CalibrationApplianceNodeOperation.Command(Descriptor,
            [CalibrationApplianceNodeOperation.NodeArgument(request.CalibrationApplianceNode),
             new WorkerMpInputArgument("Index Offset", WorkerMpValueKind.WholeNumber,
                 new WorkerIntegerValue(index), "SetIntegerArg")],
            [new("Integer Value", WorkerMpValueKind.WholeNumber, "GetIntegerArg")]);
    }

    public static Api.GetCalibrationApplianceNodeIntegerValueResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        IntegerValue = completed.Execution.OutputValues[0].RequireValue<WorkerIntegerValue>().Value,
        Execution = completed.Details
    };
}
