using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotCalibrationApplianceNodeOperations;

internal static class ClearCalibrationApplianceNodeTrapManagerRequestsOperation
{
    public static OperationDescriptor Descriptor { get; } = CalibrationApplianceNodeOperation.MutatingDescriptor(
        "robot_calibration_appliance_node_operations.clear_calibration_appliance_node_trap_manager_requests",
        "Clear Calibration Appliance Node Trap Manager Requests", "ClearCalibrationApplianceNodeTrapManagerRequests");

    public static WorkerMpCommand CreateCommand(Api.ClearCalibrationApplianceNodeTrapManagerRequestsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return CalibrationApplianceNodeOperation.MutatingCommand(Descriptor,
            CalibrationApplianceNodeOperation.NodeArgument(request.CalibrationApplianceNode));
    }

    public static Api.ClearCalibrationApplianceNodeTrapManagerRequestsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
