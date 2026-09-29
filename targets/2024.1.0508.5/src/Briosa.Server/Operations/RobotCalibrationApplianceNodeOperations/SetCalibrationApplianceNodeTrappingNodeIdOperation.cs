using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotCalibrationApplianceNodeOperations;

internal static class SetCalibrationApplianceNodeTrappingNodeIdOperation
{
    public static OperationDescriptor Descriptor { get; } = CalibrationApplianceNodeOperation.MutatingDescriptor(
        "robot_calibration_appliance_node_operations.set_calibration_appliance_node_trapping_node_id",
        "Set Calibration Appliance Node Trapping Node ID", "SetCalibrationApplianceNodeTrappingNodeId");

    public static WorkerMpCommand CreateCommand(Api.SetCalibrationApplianceNodeTrappingNodeIdRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var nodeId = request.HasTrappingNodeId ? request.TrappingNodeId : 0;
        return CalibrationApplianceNodeOperation.MutatingCommand(Descriptor,
            CalibrationApplianceNodeOperation.NodeArgument(request.CalibrationApplianceNode),
            new WorkerMpInputArgument("Trapping Node ID", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(nodeId), "SetIntegerArg"));
    }

    public static Api.SetCalibrationApplianceNodeTrappingNodeIdResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
