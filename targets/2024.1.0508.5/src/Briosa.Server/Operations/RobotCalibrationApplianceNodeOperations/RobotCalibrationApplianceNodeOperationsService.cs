using Briosa.Server.Operations.WaveA;
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
        MpOperationServiceExecutor.ExecuteAsync<Api.SetCalibrationApplianceNodeMeasurementOffsetTransformRequest, Api.SetCalibrationApplianceNodeMeasurementOffsetTransformResult>(executor, request, context, "robot_calibration_appliance_node_operations.set_calibration_appliance_node_measurement_offset_transform");

    [OperationImplementation("robot_calibration_appliance_node_operations.enable_disable_calibration_appliance_node_trap_manager")]
    public override Task<Api.EnableDisableCalibrationApplianceNodeTrapManagerResult> EnableDisableCalibrationApplianceNodeTrapManager(Api.EnableDisableCalibrationApplianceNodeTrapManagerRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.EnableDisableCalibrationApplianceNodeTrapManagerRequest, Api.EnableDisableCalibrationApplianceNodeTrapManagerResult>(executor, request, context, "robot_calibration_appliance_node_operations.enable_disable_calibration_appliance_node_trap_manager");

    [OperationImplementation("robot_calibration_appliance_node_operations.set_calibration_appliance_node_integer_value")]
    public override Task<Api.SetCalibrationApplianceNodeIntegerValueResult> SetCalibrationApplianceNodeIntegerValue(Api.SetCalibrationApplianceNodeIntegerValueRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetCalibrationApplianceNodeIntegerValueRequest, Api.SetCalibrationApplianceNodeIntegerValueResult>(executor, request, context, "robot_calibration_appliance_node_operations.set_calibration_appliance_node_integer_value");

    [OperationImplementation("robot_calibration_appliance_node_operations.set_calibration_appliance_node_trapping_node_id")]
    public override Task<Api.SetCalibrationApplianceNodeTrappingNodeIdResult> SetCalibrationApplianceNodeTrappingNodeId(Api.SetCalibrationApplianceNodeTrappingNodeIdRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetCalibrationApplianceNodeTrappingNodeIdRequest, Api.SetCalibrationApplianceNodeTrappingNodeIdResult>(executor, request, context, "robot_calibration_appliance_node_operations.set_calibration_appliance_node_trapping_node_id");

    [OperationImplementation("robot_calibration_appliance_node_operations.set_calibration_appliance_node_calibration_appliance_ip_address")]
    public override Task<Api.SetCalibrationApplianceNodeCalibrationApplianceIpAddressResult> SetCalibrationApplianceNodeCalibrationApplianceIpAddress(Api.SetCalibrationApplianceNodeCalibrationApplianceIpAddressRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetCalibrationApplianceNodeCalibrationApplianceIpAddressRequest, Api.SetCalibrationApplianceNodeCalibrationApplianceIpAddressResult>(executor, request, context, "robot_calibration_appliance_node_operations.set_calibration_appliance_node_calibration_appliance_ip_address");

    [OperationImplementation("robot_calibration_appliance_node_operations.get_calibration_appliance_node_real_value")]
    public override Task<Api.GetCalibrationApplianceNodeRealValueResult> GetCalibrationApplianceNodeRealValue(Api.GetCalibrationApplianceNodeRealValueRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetCalibrationApplianceNodeRealValueRequest, Api.GetCalibrationApplianceNodeRealValueResult>(executor, request, context, "robot_calibration_appliance_node_operations.get_calibration_appliance_node_real_value");

    [OperationImplementation("robot_calibration_appliance_node_operations.set_calibration_appliance_node_measurement_frame")]
    public override Task<Api.SetCalibrationApplianceNodeMeasurementFrameResult> SetCalibrationApplianceNodeMeasurementFrame(Api.SetCalibrationApplianceNodeMeasurementFrameRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetCalibrationApplianceNodeMeasurementFrameRequest, Api.SetCalibrationApplianceNodeMeasurementFrameResult>(executor, request, context, "robot_calibration_appliance_node_operations.set_calibration_appliance_node_measurement_frame");

    [OperationImplementation("robot_calibration_appliance_node_operations.skip_calibration_appliance_node_measurement")]
    public override Task<Api.SkipCalibrationApplianceNodeMeasurementResult> SkipCalibrationApplianceNodeMeasurement(Api.SkipCalibrationApplianceNodeMeasurementRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SkipCalibrationApplianceNodeMeasurementRequest, Api.SkipCalibrationApplianceNodeMeasurementResult>(executor, request, context, "robot_calibration_appliance_node_operations.skip_calibration_appliance_node_measurement");

    [OperationImplementation("robot_calibration_appliance_node_operations.set_calibration_appliance_node_measurement_point_group")]
    public override Task<Api.SetCalibrationApplianceNodeMeasurementPointGroupResult> SetCalibrationApplianceNodeMeasurementPointGroup(Api.SetCalibrationApplianceNodeMeasurementPointGroupRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetCalibrationApplianceNodeMeasurementPointGroupRequest, Api.SetCalibrationApplianceNodeMeasurementPointGroupResult>(executor, request, context, "robot_calibration_appliance_node_operations.set_calibration_appliance_node_measurement_point_group");

    [OperationImplementation("robot_calibration_appliance_node_operations.set_calibration_appliance_node_data")]
    public override Task<Api.SetCalibrationApplianceNodeDataResult> SetCalibrationApplianceNodeData(Api.SetCalibrationApplianceNodeDataRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetCalibrationApplianceNodeDataRequest, Api.SetCalibrationApplianceNodeDataResult>(executor, request, context, "robot_calibration_appliance_node_operations.set_calibration_appliance_node_data");

    [OperationImplementation("robot_calibration_appliance_node_operations.connect_disconnect_calibration_appliance_node")]
    public override Task<Api.ConnectDisconnectCalibrationApplianceNodeResult> ConnectDisconnectCalibrationApplianceNode(Api.ConnectDisconnectCalibrationApplianceNodeRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ConnectDisconnectCalibrationApplianceNodeRequest, Api.ConnectDisconnectCalibrationApplianceNodeResult>(executor, request, context, "robot_calibration_appliance_node_operations.connect_disconnect_calibration_appliance_node");

    [OperationImplementation("robot_calibration_appliance_node_operations.get_calibration_appliance_node_status")]
    public override Task<Api.GetCalibrationApplianceNodeStatusResult> GetCalibrationApplianceNodeStatus(Api.GetCalibrationApplianceNodeStatusRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetCalibrationApplianceNodeStatusRequest, Api.GetCalibrationApplianceNodeStatusResult>(executor, request, context, "robot_calibration_appliance_node_operations.get_calibration_appliance_node_status");

    [OperationImplementation("robot_calibration_appliance_node_operations.set_calibration_appliance_node_real_value")]
    public override Task<Api.SetCalibrationApplianceNodeRealValueResult> SetCalibrationApplianceNodeRealValue(Api.SetCalibrationApplianceNodeRealValueRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetCalibrationApplianceNodeRealValueRequest, Api.SetCalibrationApplianceNodeRealValueResult>(executor, request, context, "robot_calibration_appliance_node_operations.set_calibration_appliance_node_real_value");

    [OperationImplementation("robot_calibration_appliance_node_operations.delete_calibration_appliance_node")]
    public override Task<Api.DeleteCalibrationApplianceNodeResult> DeleteCalibrationApplianceNode(Api.DeleteCalibrationApplianceNodeRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.DeleteCalibrationApplianceNodeRequest, Api.DeleteCalibrationApplianceNodeResult>(executor, request, context, "robot_calibration_appliance_node_operations.delete_calibration_appliance_node");

    [OperationImplementation("robot_calibration_appliance_node_operations.update_calibration_appliance_node_display_robot_joints")]
    public override Task<Api.UpdateCalibrationApplianceNodeDisplayRobotJointsResult> UpdateCalibrationApplianceNodeDisplayRobotJoints(Api.UpdateCalibrationApplianceNodeDisplayRobotJointsRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.UpdateCalibrationApplianceNodeDisplayRobotJointsRequest, Api.UpdateCalibrationApplianceNodeDisplayRobotJointsResult>(executor, request, context, "robot_calibration_appliance_node_operations.update_calibration_appliance_node_display_robot_joints");

    [OperationImplementation("robot_calibration_appliance_node_operations.get_calibration_appliance_node_data")]
    public override Task<Api.GetCalibrationApplianceNodeDataResult> GetCalibrationApplianceNodeData(Api.GetCalibrationApplianceNodeDataRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetCalibrationApplianceNodeDataRequest, Api.GetCalibrationApplianceNodeDataResult>(executor, request, context, "robot_calibration_appliance_node_operations.get_calibration_appliance_node_data");

    [OperationImplementation("robot_calibration_appliance_node_operations.set_calibration_appliance_node_instrument")]
    public override Task<Api.SetCalibrationApplianceNodeInstrumentResult> SetCalibrationApplianceNodeInstrument(Api.SetCalibrationApplianceNodeInstrumentRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetCalibrationApplianceNodeInstrumentRequest, Api.SetCalibrationApplianceNodeInstrumentResult>(executor, request, context, "robot_calibration_appliance_node_operations.set_calibration_appliance_node_instrument");

    [OperationImplementation("robot_calibration_appliance_node_operations.set_calibration_appliance_node_measurement_profile")]
    public override Task<Api.SetCalibrationApplianceNodeMeasurementProfileResult> SetCalibrationApplianceNodeMeasurementProfile(Api.SetCalibrationApplianceNodeMeasurementProfileRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetCalibrationApplianceNodeMeasurementProfileRequest, Api.SetCalibrationApplianceNodeMeasurementProfileResult>(executor, request, context, "robot_calibration_appliance_node_operations.set_calibration_appliance_node_measurement_profile");

    [OperationImplementation("robot_calibration_appliance_node_operations.add_calibration_appliance_node")]
    public override Task<Api.AddCalibrationApplianceNodeResult> AddCalibrationApplianceNode(Api.AddCalibrationApplianceNodeRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AddCalibrationApplianceNodeRequest, Api.AddCalibrationApplianceNodeResult>(executor, request, context, "robot_calibration_appliance_node_operations.add_calibration_appliance_node");

    [OperationImplementation("robot_calibration_appliance_node_operations.get_calibration_appliance_node_integer_value")]
    public override Task<Api.GetCalibrationApplianceNodeIntegerValueResult> GetCalibrationApplianceNodeIntegerValue(Api.GetCalibrationApplianceNodeIntegerValueRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetCalibrationApplianceNodeIntegerValueRequest, Api.GetCalibrationApplianceNodeIntegerValueResult>(executor, request, context, "robot_calibration_appliance_node_operations.get_calibration_appliance_node_integer_value");

    [OperationImplementation("robot_calibration_appliance_node_operations.set_calibration_appliance_node_instrument_dwell_time")]
    public override Task<Api.SetCalibrationApplianceNodeInstrumentDwellTimeResult> SetCalibrationApplianceNodeInstrumentDwellTime(Api.SetCalibrationApplianceNodeInstrumentDwellTimeRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetCalibrationApplianceNodeInstrumentDwellTimeRequest, Api.SetCalibrationApplianceNodeInstrumentDwellTimeResult>(executor, request, context, "robot_calibration_appliance_node_operations.set_calibration_appliance_node_instrument_dwell_time");

    [OperationImplementation("robot_calibration_appliance_node_operations.enable_disable_calibration_appliance_node_instrument_auto_point")]
    public override Task<Api.EnableDisableCalibrationApplianceNodeInstrumentAutoPointResult> EnableDisableCalibrationApplianceNodeInstrumentAutoPoint(Api.EnableDisableCalibrationApplianceNodeInstrumentAutoPointRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.EnableDisableCalibrationApplianceNodeInstrumentAutoPointRequest, Api.EnableDisableCalibrationApplianceNodeInstrumentAutoPointResult>(executor, request, context, "robot_calibration_appliance_node_operations.enable_disable_calibration_appliance_node_instrument_auto_point");

    [OperationImplementation("robot_calibration_appliance_node_operations.set_calibration_appliance_node_measurement_target")]
    public override Task<Api.SetCalibrationApplianceNodeMeasurementTargetResult> SetCalibrationApplianceNodeMeasurementTarget(Api.SetCalibrationApplianceNodeMeasurementTargetRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetCalibrationApplianceNodeMeasurementTargetRequest, Api.SetCalibrationApplianceNodeMeasurementTargetResult>(executor, request, context, "robot_calibration_appliance_node_operations.set_calibration_appliance_node_measurement_target");

    [OperationImplementation("robot_calibration_appliance_node_operations.clear_calibration_appliance_node_trap_manager_requests")]
    public override Task<Api.ClearCalibrationApplianceNodeTrapManagerRequestsResult> ClearCalibrationApplianceNodeTrapManagerRequests(Api.ClearCalibrationApplianceNodeTrapManagerRequestsRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ClearCalibrationApplianceNodeTrapManagerRequestsRequest, Api.ClearCalibrationApplianceNodeTrapManagerRequestsResult>(executor, request, context, "robot_calibration_appliance_node_operations.clear_calibration_appliance_node_trap_manager_requests");

    [OperationImplementation("robot_calibration_appliance_node_operations.set_calibration_appliance_node_display_robot")]
    public override Task<Api.SetCalibrationApplianceNodeDisplayRobotResult> SetCalibrationApplianceNodeDisplayRobot(Api.SetCalibrationApplianceNodeDisplayRobotRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetCalibrationApplianceNodeDisplayRobotRequest, Api.SetCalibrationApplianceNodeDisplayRobotResult>(executor, request, context, "robot_calibration_appliance_node_operations.set_calibration_appliance_node_display_robot");

}
