using Briosa.Server.Operations.WaveA;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotOperations;

internal sealed class RobotOperationsService(OperationExecutor executor)
    : Api.RobotOperations.RobotOperationsBase
{
    [OperationImplementation("robot_operations.get_calibration_appliance_data")]
    public override Task<Api.GetCalibrationApplianceDataResult> GetCalibrationApplianceData(Api.GetCalibrationApplianceDataRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetCalibrationApplianceDataRequest, Api.GetCalibrationApplianceDataResult>(executor, request, context, "robot_operations.get_calibration_appliance_data");

    [OperationImplementation("robot_operations.perform_robot_calibration")]
    public override Task<Api.PerformRobotCalibrationResult> PerformRobotCalibration(Api.PerformRobotCalibrationRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.PerformRobotCalibrationRequest, Api.PerformRobotCalibrationResult>(executor, request, context, "robot_operations.perform_robot_calibration");

    [OperationImplementation("robot_operations.get_calibration_appliance_real_value")]
    public override Task<Api.GetCalibrationApplianceRealValueResult> GetCalibrationApplianceRealValue(Api.GetCalibrationApplianceRealValueRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetCalibrationApplianceRealValueRequest, Api.GetCalibrationApplianceRealValueResult>(executor, request, context, "robot_operations.get_calibration_appliance_real_value");

    [OperationImplementation("robot_operations.move_robot_machine_through_path")]
    public override Task<Api.MoveRobotMachineThroughPathResult> MoveRobotMachineThroughPath(Api.MoveRobotMachineThroughPathRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MoveRobotMachineThroughPathRequest, Api.MoveRobotMachineThroughPathResult>(executor, request, context, "robot_operations.move_robot_machine_through_path");

    [OperationImplementation("robot_operations.get_robot_pose_for_a_frame")]
    public override Task<Api.GetRobotPoseForAFrameResult> GetRobotPoseForAFrame(Api.GetRobotPoseForAFrameRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetRobotPoseForAFrameRequest, Api.GetRobotPoseForAFrameResult>(executor, request, context, "robot_operations.get_robot_pose_for_a_frame");

    [OperationImplementation("robot_operations.set_calibration_appliance_integer_value")]
    public override Task<Api.SetCalibrationApplianceIntegerValueResult> SetCalibrationApplianceIntegerValue(Api.SetCalibrationApplianceIntegerValueRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetCalibrationApplianceIntegerValueRequest, Api.SetCalibrationApplianceIntegerValueResult>(executor, request, context, "robot_operations.set_calibration_appliance_integer_value");

    [OperationImplementation("robot_operations.delete_robot_machine")]
    public override Task<Api.DeleteRobotMachineResult> DeleteRobotMachine(Api.DeleteRobotMachineRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.DeleteRobotMachineRequest, Api.DeleteRobotMachineResult>(executor, request, context, "robot_operations.delete_robot_machine");

    [OperationImplementation("robot_operations.set_robot_machine_base_transform")]
    public override Task<Api.SetRobotMachineBaseTransformResult> SetRobotMachineBaseTransform(Api.SetRobotMachineBaseTransformRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetRobotMachineBaseTransformRequest, Api.SetRobotMachineBaseTransformResult>(executor, request, context, "robot_operations.set_robot_machine_base_transform");

    [OperationImplementation("robot_operations.set_robot_calibration_tool_frame")]
    public override Task<Api.SetRobotCalibrationToolFrameResult> SetRobotCalibrationToolFrame(Api.SetRobotCalibrationToolFrameRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetRobotCalibrationToolFrameRequest, Api.SetRobotCalibrationToolFrameResult>(executor, request, context, "robot_operations.set_robot_calibration_tool_frame");

    [OperationImplementation("robot_operations.set_robot_calibration_measurement_offset_in_tool_frame")]
    public override Task<Api.SetRobotCalibrationMeasurementOffsetInToolFrameResult> SetRobotCalibrationMeasurementOffsetInToolFrame(Api.SetRobotCalibrationMeasurementOffsetInToolFrameRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetRobotCalibrationMeasurementOffsetInToolFrameRequest, Api.SetRobotCalibrationMeasurementOffsetInToolFrameResult>(executor, request, context, "robot_operations.set_robot_calibration_measurement_offset_in_tool_frame");

    [OperationImplementation("robot_operations.import_poses_match_to_frames")]
    public override Task<Api.ImportPosesMatchToFramesResult> ImportPosesMatchToFrames(Api.ImportPosesMatchToFramesRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ImportPosesMatchToFramesRequest, Api.ImportPosesMatchToFramesResult>(executor, request, context, "robot_operations.import_poses_match_to_frames");

    [OperationImplementation("robot_operations.perform_robot_calibration_alternate")]
    public override Task<Api.PerformRobotCalibrationAlternateResult> PerformRobotCalibrationAlternate(Api.PerformRobotCalibrationAlternateRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.PerformRobotCalibrationAlternateRequest, Api.PerformRobotCalibrationAlternateResult>(executor, request, context, "robot_operations.perform_robot_calibration_alternate");

    [OperationImplementation("robot_operations.delete_robot_calibration")]
    public override Task<Api.DeleteRobotCalibrationResult> DeleteRobotCalibration(Api.DeleteRobotCalibrationRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.DeleteRobotCalibrationRequest, Api.DeleteRobotCalibrationResult>(executor, request, context, "robot_operations.delete_robot_calibration");

    [OperationImplementation("robot_operations.import_poses_match_to_measurements")]
    public override Task<Api.ImportPosesMatchToMeasurementsResult> ImportPosesMatchToMeasurements(Api.ImportPosesMatchToMeasurementsRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ImportPosesMatchToMeasurementsRequest, Api.ImportPosesMatchToMeasurementsResult>(executor, request, context, "robot_operations.import_poses_match_to_measurements");

    [OperationImplementation("robot_operations.start_robot_machine_interface")]
    public override Task<Api.StartRobotMachineInterfaceResult> StartRobotMachineInterface(Api.StartRobotMachineInterfaceRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.StartRobotMachineInterfaceRequest, Api.StartRobotMachineInterfaceResult>(executor, request, context, "robot_operations.start_robot_machine_interface");

    [OperationImplementation("robot_operations.set_calibration_appliance_data")]
    public override Task<Api.SetCalibrationApplianceDataResult> SetCalibrationApplianceData(Api.SetCalibrationApplianceDataRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetCalibrationApplianceDataRequest, Api.SetCalibrationApplianceDataResult>(executor, request, context, "robot_operations.set_calibration_appliance_data");

    [OperationImplementation("robot_operations.set_robot_machine_model_link_parameters")]
    public override Task<Api.SetRobotMachineModelLinkParametersResult> SetRobotMachineModelLinkParameters(Api.SetRobotMachineModelLinkParametersRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetRobotMachineModelLinkParametersRequest, Api.SetRobotMachineModelLinkParametersResult>(executor, request, context, "robot_operations.set_robot_machine_model_link_parameters");

    [OperationImplementation("robot_operations.add_robot_machine_manip_kin")]
    public override Task<Api.AddRobotMachineManipKinResult> AddRobotMachineManipKin(Api.AddRobotMachineManipKinRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AddRobotMachineManipKinRequest, Api.AddRobotMachineManipKinResult>(executor, request, context, "robot_operations.add_robot_machine_manip_kin");

    [OperationImplementation("robot_operations.simulate_robot_machine_path_output_csv_file")]
    public override Task<Api.SimulateRobotMachinePathOutputCsvFileResult> SimulateRobotMachinePathOutputCsvFile(Api.SimulateRobotMachinePathOutputCsvFileRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SimulateRobotMachinePathOutputCsvFileRequest, Api.SimulateRobotMachinePathOutputCsvFileResult>(executor, request, context, "robot_operations.simulate_robot_machine_path_output_csv_file");

    [OperationImplementation("robot_operations.get_calibration_appliance_integer_value")]
    public override Task<Api.GetCalibrationApplianceIntegerValueResult> GetCalibrationApplianceIntegerValue(Api.GetCalibrationApplianceIntegerValueRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetCalibrationApplianceIntegerValueRequest, Api.GetCalibrationApplianceIntegerValueResult>(executor, request, context, "robot_operations.get_calibration_appliance_integer_value");

    [OperationImplementation("robot_operations.compute_robot_machine_adjusted_goal_frame")]
    public override Task<Api.ComputeRobotMachineAdjustedGoalFrameResult> ComputeRobotMachineAdjustedGoalFrame(Api.ComputeRobotMachineAdjustedGoalFrameRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ComputeRobotMachineAdjustedGoalFrameRequest, Api.ComputeRobotMachineAdjustedGoalFrameResult>(executor, request, context, "robot_operations.compute_robot_machine_adjusted_goal_frame");

    [OperationImplementation("robot_operations.create_robot_calibration")]
    public override Task<Api.CreateRobotCalibrationResult> CreateRobotCalibration(Api.CreateRobotCalibrationRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.CreateRobotCalibrationRequest, Api.CreateRobotCalibrationResult>(executor, request, context, "robot_operations.create_robot_calibration");

    [OperationImplementation("robot_operations.get_robot_machine_parameter")]
    public override Task<Api.GetRobotMachineParameterResult> GetRobotMachineParameter(Api.GetRobotMachineParameterRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetRobotMachineParameterRequest, Api.GetRobotMachineParameterResult>(executor, request, context, "robot_operations.get_robot_machine_parameter");

    [OperationImplementation("robot_operations.stop_robot_machine_interface")]
    public override Task<Api.StopRobotMachineInterfaceResult> StopRobotMachineInterface(Api.StopRobotMachineInterfaceRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.StopRobotMachineInterfaceRequest, Api.StopRobotMachineInterfaceResult>(executor, request, context, "robot_operations.stop_robot_machine_interface");

    [OperationImplementation("robot_operations.set_calibration_appliance_real_value")]
    public override Task<Api.SetCalibrationApplianceRealValueResult> SetCalibrationApplianceRealValue(Api.SetCalibrationApplianceRealValueRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetCalibrationApplianceRealValueRequest, Api.SetCalibrationApplianceRealValueResult>(executor, request, context, "robot_operations.set_calibration_appliance_real_value");

    [OperationImplementation("robot_operations.move_robot_machine_to_frame")]
    public override Task<Api.MoveRobotMachineToFrameResult> MoveRobotMachineToFrame(Api.MoveRobotMachineToFrameRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MoveRobotMachineToFrameRequest, Api.MoveRobotMachineToFrameResult>(executor, request, context, "robot_operations.move_robot_machine_to_frame");

    [OperationImplementation("robot_operations.set_robot_machine_parameter")]
    public override Task<Api.SetRobotMachineParameterResult> SetRobotMachineParameter(Api.SetRobotMachineParameterRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetRobotMachineParameterRequest, Api.SetRobotMachineParameterResult>(executor, request, context, "robot_operations.set_robot_machine_parameter");

    [OperationImplementation("robot_operations.get_robot_machine_model_link_parameters")]
    public override Task<Api.GetRobotMachineModelLinkParametersResult> GetRobotMachineModelLinkParameters(Api.GetRobotMachineModelLinkParametersRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetRobotMachineModelLinkParametersRequest, Api.GetRobotMachineModelLinkParametersResult>(executor, request, context, "robot_operations.get_robot_machine_model_link_parameters");

    [OperationImplementation("robot_operations.set_active_robot_calibration")]
    public override Task<Api.SetActiveRobotCalibrationResult> SetActiveRobotCalibration(Api.SetActiveRobotCalibrationRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetActiveRobotCalibrationRequest, Api.SetActiveRobotCalibrationResult>(executor, request, context, "robot_operations.set_active_robot_calibration");

    [OperationImplementation("robot_operations.move_robot_machine_to_named_destination")]
    public override Task<Api.MoveRobotMachineToNamedDestinationResult> MoveRobotMachineToNamedDestination(Api.MoveRobotMachineToNamedDestinationRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MoveRobotMachineToNamedDestinationRequest, Api.MoveRobotMachineToNamedDestinationResult>(executor, request, context, "robot_operations.move_robot_machine_to_named_destination");

    [OperationImplementation("robot_operations.start_stop_robot_calibration_trapping")]
    public override Task<Api.StartStopRobotCalibrationTrappingResult> StartStopRobotCalibrationTrapping(Api.StartStopRobotCalibrationTrappingRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.StartStopRobotCalibrationTrappingRequest, Api.StartStopRobotCalibrationTrappingResult>(executor, request, context, "robot_operations.start_stop_robot_calibration_trapping");

    [OperationImplementation("robot_operations.add_robot_machine_sa_machine")]
    public override Task<Api.AddRobotMachineSaMachineResult> AddRobotMachineSaMachine(Api.AddRobotMachineSaMachineRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AddRobotMachineSaMachineRequest, Api.AddRobotMachineSaMachineResult>(executor, request, context, "robot_operations.add_robot_machine_sa_machine");

    [OperationImplementation("robot_operations.move_robot_machine_to_joint_pose_six_dof")]
    public override Task<Api.MoveRobotMachineToJointPoseSixDofResult> MoveRobotMachineToJointPoseSixDof(Api.MoveRobotMachineToJointPoseSixDofRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MoveRobotMachineToJointPoseSixDofRequest, Api.MoveRobotMachineToJointPoseSixDofResult>(executor, request, context, "robot_operations.move_robot_machine_to_joint_pose_six_dof");

}
