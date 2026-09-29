using Briosa.Server.Security;
using Briosa.Server.Services;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotCalibrationApplianceNodeOperations;

internal sealed class RobotCalibrationApplianceNodeOperationsService(OperationExecutor executor)
    : Api.RobotCalibrationApplianceNodeOperations.RobotCalibrationApplianceNodeOperationsBase
{
    [OperationImplementation("robot_calibration_appliance_node_operations.set_calibration_appliance_node_measurement_offset_transform")]
    public override Task<Api.SetCalibrationApplianceNodeMeasurementOffsetTransformResult> SetCalibrationApplianceNodeMeasurementOffsetTransform(Api.SetCalibrationApplianceNodeMeasurementOffsetTransformRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetCalibrationApplianceNodeMeasurementOffsetTransformOperation.Descriptor,
            SetCalibrationApplianceNodeMeasurementOffsetTransformOperation.CreateCommand, CalibrationApplianceNodeOperation.NoOutputs,
            SetCalibrationApplianceNodeMeasurementOffsetTransformOperation.CreateResult);

    [OperationImplementation("robot_calibration_appliance_node_operations.enable_disable_calibration_appliance_node_trap_manager")]
    public override Task<Api.EnableDisableCalibrationApplianceNodeTrapManagerResult> EnableDisableCalibrationApplianceNodeTrapManager(Api.EnableDisableCalibrationApplianceNodeTrapManagerRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, EnableDisableCalibrationApplianceNodeTrapManagerOperation.Descriptor,
            EnableDisableCalibrationApplianceNodeTrapManagerOperation.CreateCommand, CalibrationApplianceNodeOperation.NoOutputs,
            EnableDisableCalibrationApplianceNodeTrapManagerOperation.CreateResult);

    [OperationImplementation("robot_calibration_appliance_node_operations.set_calibration_appliance_node_integer_value")]
    public override Task<Api.SetCalibrationApplianceNodeIntegerValueResult> SetCalibrationApplianceNodeIntegerValue(Api.SetCalibrationApplianceNodeIntegerValueRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetCalibrationApplianceNodeIntegerValueOperation.Descriptor,
            SetCalibrationApplianceNodeIntegerValueOperation.CreateCommand, CalibrationApplianceNodeOperation.NoOutputs,
            SetCalibrationApplianceNodeIntegerValueOperation.CreateResult);

    [OperationImplementation("robot_calibration_appliance_node_operations.set_calibration_appliance_node_trapping_node_id")]
    public override Task<Api.SetCalibrationApplianceNodeTrappingNodeIdResult> SetCalibrationApplianceNodeTrappingNodeId(Api.SetCalibrationApplianceNodeTrappingNodeIdRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetCalibrationApplianceNodeTrappingNodeIdOperation.Descriptor,
            SetCalibrationApplianceNodeTrappingNodeIdOperation.CreateCommand, CalibrationApplianceNodeOperation.NoOutputs,
            SetCalibrationApplianceNodeTrappingNodeIdOperation.CreateResult);

    [OperationImplementation("robot_calibration_appliance_node_operations.set_calibration_appliance_node_calibration_appliance_ip_address")]
    public override Task<Api.SetCalibrationApplianceNodeCalibrationApplianceIpAddressResult> SetCalibrationApplianceNodeCalibrationApplianceIpAddress(Api.SetCalibrationApplianceNodeCalibrationApplianceIpAddressRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetCalibrationApplianceNodeCalibrationApplianceIpAddressOperation.Descriptor,
            SetCalibrationApplianceNodeCalibrationApplianceIpAddressOperation.CreateCommand, CalibrationApplianceNodeOperation.NoOutputs,
            SetCalibrationApplianceNodeCalibrationApplianceIpAddressOperation.CreateResult);

    [OperationImplementation("robot_calibration_appliance_node_operations.get_calibration_appliance_node_real_value")]
    public override Task<Api.GetCalibrationApplianceNodeRealValueResult> GetCalibrationApplianceNodeRealValue(Api.GetCalibrationApplianceNodeRealValueRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetCalibrationApplianceNodeRealValueOperation.Descriptor,
            GetCalibrationApplianceNodeRealValueOperation.CreateCommand, GetCalibrationApplianceNodeRealValueOperation.OutputContracts,
            GetCalibrationApplianceNodeRealValueOperation.CreateResult);

    [OperationImplementation("robot_calibration_appliance_node_operations.set_calibration_appliance_node_measurement_frame")]
    public override Task<Api.SetCalibrationApplianceNodeMeasurementFrameResult> SetCalibrationApplianceNodeMeasurementFrame(Api.SetCalibrationApplianceNodeMeasurementFrameRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetCalibrationApplianceNodeMeasurementFrameOperation.Descriptor,
            SetCalibrationApplianceNodeMeasurementFrameOperation.CreateCommand, CalibrationApplianceNodeOperation.NoOutputs,
            SetCalibrationApplianceNodeMeasurementFrameOperation.CreateResult);

    [OperationImplementation("robot_calibration_appliance_node_operations.skip_calibration_appliance_node_measurement")]
    public override Task<Api.SkipCalibrationApplianceNodeMeasurementResult> SkipCalibrationApplianceNodeMeasurement(Api.SkipCalibrationApplianceNodeMeasurementRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SkipCalibrationApplianceNodeMeasurementOperation.Descriptor,
            SkipCalibrationApplianceNodeMeasurementOperation.CreateCommand, CalibrationApplianceNodeOperation.NoOutputs,
            SkipCalibrationApplianceNodeMeasurementOperation.CreateResult);

    [OperationImplementation("robot_calibration_appliance_node_operations.set_calibration_appliance_node_measurement_point_group")]
    public override Task<Api.SetCalibrationApplianceNodeMeasurementPointGroupResult> SetCalibrationApplianceNodeMeasurementPointGroup(Api.SetCalibrationApplianceNodeMeasurementPointGroupRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetCalibrationApplianceNodeMeasurementPointGroupOperation.Descriptor,
            SetCalibrationApplianceNodeMeasurementPointGroupOperation.CreateCommand, CalibrationApplianceNodeOperation.NoOutputs,
            SetCalibrationApplianceNodeMeasurementPointGroupOperation.CreateResult);

    [OperationImplementation("robot_calibration_appliance_node_operations.set_calibration_appliance_node_data")]
    public override Task<Api.SetCalibrationApplianceNodeDataResult> SetCalibrationApplianceNodeData(Api.SetCalibrationApplianceNodeDataRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetCalibrationApplianceNodeDataOperation.Descriptor,
            SetCalibrationApplianceNodeDataOperation.CreateCommand, CalibrationApplianceNodeOperation.NoOutputs,
            SetCalibrationApplianceNodeDataOperation.CreateResult);

    [OperationImplementation("robot_calibration_appliance_node_operations.connect_disconnect_calibration_appliance_node")]
    public override Task<Api.ConnectDisconnectCalibrationApplianceNodeResult> ConnectDisconnectCalibrationApplianceNode(Api.ConnectDisconnectCalibrationApplianceNodeRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ConnectDisconnectCalibrationApplianceNodeOperation.Descriptor,
            ConnectDisconnectCalibrationApplianceNodeOperation.CreateCommand, CalibrationApplianceNodeOperation.NoOutputs,
            ConnectDisconnectCalibrationApplianceNodeOperation.CreateResult);

    [OperationImplementation("robot_calibration_appliance_node_operations.get_calibration_appliance_node_status")]
    public override Task<Api.GetCalibrationApplianceNodeStatusResult> GetCalibrationApplianceNodeStatus(Api.GetCalibrationApplianceNodeStatusRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetCalibrationApplianceNodeStatusOperation.Descriptor,
            GetCalibrationApplianceNodeStatusOperation.CreateCommand, GetCalibrationApplianceNodeStatusOperation.OutputContracts,
            GetCalibrationApplianceNodeStatusOperation.CreateResult);

    [OperationImplementation("robot_calibration_appliance_node_operations.set_calibration_appliance_node_real_value")]
    public override Task<Api.SetCalibrationApplianceNodeRealValueResult> SetCalibrationApplianceNodeRealValue(Api.SetCalibrationApplianceNodeRealValueRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetCalibrationApplianceNodeRealValueOperation.Descriptor,
            SetCalibrationApplianceNodeRealValueOperation.CreateCommand, CalibrationApplianceNodeOperation.NoOutputs,
            SetCalibrationApplianceNodeRealValueOperation.CreateResult);

    [OperationImplementation("robot_calibration_appliance_node_operations.delete_calibration_appliance_node")]
    public override Task<Api.DeleteCalibrationApplianceNodeResult> DeleteCalibrationApplianceNode(Api.DeleteCalibrationApplianceNodeRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, DeleteCalibrationApplianceNodeOperation.Descriptor,
            DeleteCalibrationApplianceNodeOperation.CreateCommand, CalibrationApplianceNodeOperation.NoOutputs,
            DeleteCalibrationApplianceNodeOperation.CreateResult);

    [OperationImplementation("robot_calibration_appliance_node_operations.update_calibration_appliance_node_display_robot_joints")]
    public override Task<Api.UpdateCalibrationApplianceNodeDisplayRobotJointsResult> UpdateCalibrationApplianceNodeDisplayRobotJoints(Api.UpdateCalibrationApplianceNodeDisplayRobotJointsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, UpdateCalibrationApplianceNodeDisplayRobotJointsOperation.Descriptor,
            UpdateCalibrationApplianceNodeDisplayRobotJointsOperation.CreateCommand, CalibrationApplianceNodeOperation.NoOutputs,
            UpdateCalibrationApplianceNodeDisplayRobotJointsOperation.CreateResult);

    [OperationImplementation("robot_calibration_appliance_node_operations.get_calibration_appliance_node_data")]
    public override Task<Api.GetCalibrationApplianceNodeDataResult> GetCalibrationApplianceNodeData(Api.GetCalibrationApplianceNodeDataRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetCalibrationApplianceNodeDataOperation.Descriptor,
            GetCalibrationApplianceNodeDataOperation.CreateCommand, GetCalibrationApplianceNodeDataOperation.OutputContracts,
            GetCalibrationApplianceNodeDataOperation.CreateResult);

    [OperationImplementation("robot_calibration_appliance_node_operations.set_calibration_appliance_node_instrument")]
    public override Task<Api.SetCalibrationApplianceNodeInstrumentResult> SetCalibrationApplianceNodeInstrument(Api.SetCalibrationApplianceNodeInstrumentRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetCalibrationApplianceNodeInstrumentOperation.Descriptor,
            SetCalibrationApplianceNodeInstrumentOperation.CreateCommand, CalibrationApplianceNodeOperation.NoOutputs,
            SetCalibrationApplianceNodeInstrumentOperation.CreateResult);

    [OperationImplementation("robot_calibration_appliance_node_operations.set_calibration_appliance_node_measurement_profile")]
    public override Task<Api.SetCalibrationApplianceNodeMeasurementProfileResult> SetCalibrationApplianceNodeMeasurementProfile(Api.SetCalibrationApplianceNodeMeasurementProfileRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetCalibrationApplianceNodeMeasurementProfileOperation.Descriptor,
            SetCalibrationApplianceNodeMeasurementProfileOperation.CreateCommand, CalibrationApplianceNodeOperation.NoOutputs,
            SetCalibrationApplianceNodeMeasurementProfileOperation.CreateResult);

    [OperationImplementation("robot_calibration_appliance_node_operations.add_calibration_appliance_node")]
    public override Task<Api.AddCalibrationApplianceNodeResult> AddCalibrationApplianceNode(Api.AddCalibrationApplianceNodeRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AddCalibrationApplianceNodeOperation.Descriptor,
            AddCalibrationApplianceNodeOperation.CreateCommand, CalibrationApplianceNodeOperation.NoOutputs,
            AddCalibrationApplianceNodeOperation.CreateResult);

    [OperationImplementation("robot_calibration_appliance_node_operations.get_calibration_appliance_node_integer_value")]
    public override Task<Api.GetCalibrationApplianceNodeIntegerValueResult> GetCalibrationApplianceNodeIntegerValue(Api.GetCalibrationApplianceNodeIntegerValueRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetCalibrationApplianceNodeIntegerValueOperation.Descriptor,
            GetCalibrationApplianceNodeIntegerValueOperation.CreateCommand, GetCalibrationApplianceNodeIntegerValueOperation.OutputContracts,
            GetCalibrationApplianceNodeIntegerValueOperation.CreateResult);

    [OperationImplementation("robot_calibration_appliance_node_operations.set_calibration_appliance_node_instrument_dwell_time")]
    public override Task<Api.SetCalibrationApplianceNodeInstrumentDwellTimeResult> SetCalibrationApplianceNodeInstrumentDwellTime(Api.SetCalibrationApplianceNodeInstrumentDwellTimeRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetCalibrationApplianceNodeInstrumentDwellTimeOperation.Descriptor,
            SetCalibrationApplianceNodeInstrumentDwellTimeOperation.CreateCommand, CalibrationApplianceNodeOperation.NoOutputs,
            SetCalibrationApplianceNodeInstrumentDwellTimeOperation.CreateResult);

    [OperationImplementation("robot_calibration_appliance_node_operations.enable_disable_calibration_appliance_node_instrument_auto_point")]
    public override Task<Api.EnableDisableCalibrationApplianceNodeInstrumentAutoPointResult> EnableDisableCalibrationApplianceNodeInstrumentAutoPoint(Api.EnableDisableCalibrationApplianceNodeInstrumentAutoPointRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, EnableDisableCalibrationApplianceNodeInstrumentAutoPointOperation.Descriptor,
            EnableDisableCalibrationApplianceNodeInstrumentAutoPointOperation.CreateCommand, CalibrationApplianceNodeOperation.NoOutputs,
            EnableDisableCalibrationApplianceNodeInstrumentAutoPointOperation.CreateResult);

    [OperationImplementation("robot_calibration_appliance_node_operations.set_calibration_appliance_node_measurement_target")]
    public override Task<Api.SetCalibrationApplianceNodeMeasurementTargetResult> SetCalibrationApplianceNodeMeasurementTarget(Api.SetCalibrationApplianceNodeMeasurementTargetRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetCalibrationApplianceNodeMeasurementTargetOperation.Descriptor,
            SetCalibrationApplianceNodeMeasurementTargetOperation.CreateCommand, CalibrationApplianceNodeOperation.NoOutputs,
            SetCalibrationApplianceNodeMeasurementTargetOperation.CreateResult);

    [OperationImplementation("robot_calibration_appliance_node_operations.clear_calibration_appliance_node_trap_manager_requests")]
    public override Task<Api.ClearCalibrationApplianceNodeTrapManagerRequestsResult> ClearCalibrationApplianceNodeTrapManagerRequests(Api.ClearCalibrationApplianceNodeTrapManagerRequestsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ClearCalibrationApplianceNodeTrapManagerRequestsOperation.Descriptor,
            ClearCalibrationApplianceNodeTrapManagerRequestsOperation.CreateCommand, CalibrationApplianceNodeOperation.NoOutputs,
            ClearCalibrationApplianceNodeTrapManagerRequestsOperation.CreateResult);

    [OperationImplementation("robot_calibration_appliance_node_operations.set_calibration_appliance_node_display_robot")]
    public override Task<Api.SetCalibrationApplianceNodeDisplayRobotResult> SetCalibrationApplianceNodeDisplayRobot(Api.SetCalibrationApplianceNodeDisplayRobotRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetCalibrationApplianceNodeDisplayRobotOperation.Descriptor,
            SetCalibrationApplianceNodeDisplayRobotOperation.CreateCommand, CalibrationApplianceNodeOperation.NoOutputs,
            SetCalibrationApplianceNodeDisplayRobotOperation.CreateResult);

}
