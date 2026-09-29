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
        executor.ExecuteAsync(request, context, GetCalibrationApplianceDataOperation.Descriptor,
            GetCalibrationApplianceDataOperation.CreateCommand, GetCalibrationApplianceDataOperation.OutputContracts,
            GetCalibrationApplianceDataOperation.CreateResult);

    [OperationImplementation("robot_operations.perform_robot_calibration")]
    public override Task<Api.PerformRobotCalibrationResult> PerformRobotCalibration(Api.PerformRobotCalibrationRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, PerformRobotCalibrationOperation.Descriptor,
            PerformRobotCalibrationOperation.CreateCommand, PerformRobotCalibrationOperation.OutputContracts,
            PerformRobotCalibrationOperation.CreateResult);

    [OperationImplementation("robot_operations.get_calibration_appliance_real_value")]
    public override Task<Api.GetCalibrationApplianceRealValueResult> GetCalibrationApplianceRealValue(Api.GetCalibrationApplianceRealValueRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetCalibrationApplianceRealValueOperation.Descriptor,
            GetCalibrationApplianceRealValueOperation.CreateCommand, GetCalibrationApplianceRealValueOperation.OutputContracts,
            GetCalibrationApplianceRealValueOperation.CreateResult);

    [OperationImplementation("robot_operations.move_robot_machine_through_path")]
    public override Task<Api.MoveRobotMachineThroughPathResult> MoveRobotMachineThroughPath(Api.MoveRobotMachineThroughPathRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MoveRobotMachineThroughPathOperation.Descriptor,
            MoveRobotMachineThroughPathOperation.CreateCommand, MoveRobotMachineThroughPathOperation.OutputContracts,
            MoveRobotMachineThroughPathOperation.CreateResult);

    [OperationImplementation("robot_operations.get_robot_pose_for_a_frame")]
    public override Task<Api.GetRobotPoseForAFrameResult> GetRobotPoseForAFrame(Api.GetRobotPoseForAFrameRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetRobotPoseForAFrameOperation.Descriptor,
            GetRobotPoseForAFrameOperation.CreateCommand, GetRobotPoseForAFrameOperation.OutputContracts,
            GetRobotPoseForAFrameOperation.CreateResult);

    [OperationImplementation("robot_operations.set_calibration_appliance_integer_value")]
    public override Task<Api.SetCalibrationApplianceIntegerValueResult> SetCalibrationApplianceIntegerValue(Api.SetCalibrationApplianceIntegerValueRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetCalibrationApplianceIntegerValueOperation.Descriptor,
            SetCalibrationApplianceIntegerValueOperation.CreateCommand, SetCalibrationApplianceIntegerValueOperation.OutputContracts,
            SetCalibrationApplianceIntegerValueOperation.CreateResult);

    [OperationImplementation("robot_operations.delete_robot_machine")]
    public override Task<Api.DeleteRobotMachineResult> DeleteRobotMachine(Api.DeleteRobotMachineRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, DeleteRobotMachineOperation.Descriptor,
            DeleteRobotMachineOperation.CreateCommand, DeleteRobotMachineOperation.OutputContracts,
            DeleteRobotMachineOperation.CreateResult);

    [OperationImplementation("robot_operations.set_robot_machine_base_transform")]
    public override Task<Api.SetRobotMachineBaseTransformResult> SetRobotMachineBaseTransform(Api.SetRobotMachineBaseTransformRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetRobotMachineBaseTransformOperation.Descriptor,
            SetRobotMachineBaseTransformOperation.CreateCommand, SetRobotMachineBaseTransformOperation.OutputContracts,
            SetRobotMachineBaseTransformOperation.CreateResult);

    [OperationImplementation("robot_operations.set_robot_calibration_tool_frame")]
    public override Task<Api.SetRobotCalibrationToolFrameResult> SetRobotCalibrationToolFrame(Api.SetRobotCalibrationToolFrameRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetRobotCalibrationToolFrameOperation.Descriptor,
            SetRobotCalibrationToolFrameOperation.CreateCommand, SetRobotCalibrationToolFrameOperation.OutputContracts,
            SetRobotCalibrationToolFrameOperation.CreateResult);

    [OperationImplementation("robot_operations.set_robot_calibration_measurement_offset_in_tool_frame")]
    public override Task<Api.SetRobotCalibrationMeasurementOffsetInToolFrameResult> SetRobotCalibrationMeasurementOffsetInToolFrame(Api.SetRobotCalibrationMeasurementOffsetInToolFrameRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetRobotCalibrationMeasurementOffsetInToolFrameOperation.Descriptor,
            SetRobotCalibrationMeasurementOffsetInToolFrameOperation.CreateCommand, SetRobotCalibrationMeasurementOffsetInToolFrameOperation.OutputContracts,
            SetRobotCalibrationMeasurementOffsetInToolFrameOperation.CreateResult);

    [OperationImplementation("robot_operations.import_poses_match_to_frames")]
    public override Task<Api.ImportPosesMatchToFramesResult> ImportPosesMatchToFrames(Api.ImportPosesMatchToFramesRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ImportPosesMatchToFramesOperation.Descriptor,
            ImportPosesMatchToFramesOperation.CreateCommand, ImportPosesMatchToFramesOperation.OutputContracts,
            ImportPosesMatchToFramesOperation.CreateResult);

    [OperationImplementation("robot_operations.perform_robot_calibration_alternate")]
    public override Task<Api.PerformRobotCalibrationAlternateResult> PerformRobotCalibrationAlternate(Api.PerformRobotCalibrationAlternateRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, PerformRobotCalibrationAlternateOperation.Descriptor,
            PerformRobotCalibrationAlternateOperation.CreateCommand, PerformRobotCalibrationAlternateOperation.OutputContracts,
            PerformRobotCalibrationAlternateOperation.CreateResult);

    [OperationImplementation("robot_operations.delete_robot_calibration")]
    public override Task<Api.DeleteRobotCalibrationResult> DeleteRobotCalibration(Api.DeleteRobotCalibrationRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, DeleteRobotCalibrationOperation.Descriptor,
            DeleteRobotCalibrationOperation.CreateCommand, DeleteRobotCalibrationOperation.OutputContracts,
            DeleteRobotCalibrationOperation.CreateResult);

    [OperationImplementation("robot_operations.import_poses_match_to_measurements")]
    public override Task<Api.ImportPosesMatchToMeasurementsResult> ImportPosesMatchToMeasurements(Api.ImportPosesMatchToMeasurementsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ImportPosesMatchToMeasurementsOperation.Descriptor,
            ImportPosesMatchToMeasurementsOperation.CreateCommand, ImportPosesMatchToMeasurementsOperation.OutputContracts,
            ImportPosesMatchToMeasurementsOperation.CreateResult);

    [OperationImplementation("robot_operations.start_robot_machine_interface")]
    public override Task<Api.StartRobotMachineInterfaceResult> StartRobotMachineInterface(Api.StartRobotMachineInterfaceRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, StartRobotMachineInterfaceOperation.Descriptor,
            StartRobotMachineInterfaceOperation.CreateCommand, StartRobotMachineInterfaceOperation.OutputContracts,
            StartRobotMachineInterfaceOperation.CreateResult);

    [OperationImplementation("robot_operations.set_calibration_appliance_data")]
    public override Task<Api.SetCalibrationApplianceDataResult> SetCalibrationApplianceData(Api.SetCalibrationApplianceDataRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetCalibrationApplianceDataOperation.Descriptor,
            SetCalibrationApplianceDataOperation.CreateCommand, SetCalibrationApplianceDataOperation.OutputContracts,
            SetCalibrationApplianceDataOperation.CreateResult);

    [OperationImplementation("robot_operations.set_robot_machine_model_link_parameters")]
    public override Task<Api.SetRobotMachineModelLinkParametersResult> SetRobotMachineModelLinkParameters(Api.SetRobotMachineModelLinkParametersRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetRobotMachineModelLinkParametersOperation.Descriptor,
            SetRobotMachineModelLinkParametersOperation.CreateCommand, SetRobotMachineModelLinkParametersOperation.OutputContracts,
            SetRobotMachineModelLinkParametersOperation.CreateResult);

    [OperationImplementation("robot_operations.add_robot_machine_manip_kin")]
    public override Task<Api.AddRobotMachineManipKinResult> AddRobotMachineManipKin(Api.AddRobotMachineManipKinRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AddRobotMachineManipKinOperation.Descriptor,
            AddRobotMachineManipKinOperation.CreateCommand, AddRobotMachineManipKinOperation.OutputContracts,
            AddRobotMachineManipKinOperation.CreateResult);

    [OperationImplementation("robot_operations.simulate_robot_machine_path_output_csv_file")]
    public override Task<Api.SimulateRobotMachinePathOutputCsvFileResult> SimulateRobotMachinePathOutputCsvFile(Api.SimulateRobotMachinePathOutputCsvFileRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SimulateRobotMachinePathOutputCsvFileOperation.Descriptor,
            SimulateRobotMachinePathOutputCsvFileOperation.CreateCommand, SimulateRobotMachinePathOutputCsvFileOperation.OutputContracts,
            SimulateRobotMachinePathOutputCsvFileOperation.CreateResult);

    [OperationImplementation("robot_operations.get_calibration_appliance_integer_value")]
    public override Task<Api.GetCalibrationApplianceIntegerValueResult> GetCalibrationApplianceIntegerValue(Api.GetCalibrationApplianceIntegerValueRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetCalibrationApplianceIntegerValueOperation.Descriptor,
            GetCalibrationApplianceIntegerValueOperation.CreateCommand, GetCalibrationApplianceIntegerValueOperation.OutputContracts,
            GetCalibrationApplianceIntegerValueOperation.CreateResult);

    [OperationImplementation("robot_operations.compute_robot_machine_adjusted_goal_frame")]
    public override Task<Api.ComputeRobotMachineAdjustedGoalFrameResult> ComputeRobotMachineAdjustedGoalFrame(Api.ComputeRobotMachineAdjustedGoalFrameRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ComputeRobotMachineAdjustedGoalFrameOperation.Descriptor,
            ComputeRobotMachineAdjustedGoalFrameOperation.CreateCommand, ComputeRobotMachineAdjustedGoalFrameOperation.OutputContracts,
            ComputeRobotMachineAdjustedGoalFrameOperation.CreateResult);

    [OperationImplementation("robot_operations.create_robot_calibration")]
    public override Task<Api.CreateRobotCalibrationResult> CreateRobotCalibration(Api.CreateRobotCalibrationRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, CreateRobotCalibrationOperation.Descriptor,
            CreateRobotCalibrationOperation.CreateCommand, CreateRobotCalibrationOperation.OutputContracts,
            CreateRobotCalibrationOperation.CreateResult);

    [OperationImplementation("robot_operations.get_robot_machine_parameter")]
    public override Task<Api.GetRobotMachineParameterResult> GetRobotMachineParameter(Api.GetRobotMachineParameterRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetRobotMachineParameterOperation.Descriptor,
            GetRobotMachineParameterOperation.CreateCommand, GetRobotMachineParameterOperation.OutputContracts,
            GetRobotMachineParameterOperation.CreateResult);

    [OperationImplementation("robot_operations.stop_robot_machine_interface")]
    public override Task<Api.StopRobotMachineInterfaceResult> StopRobotMachineInterface(Api.StopRobotMachineInterfaceRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, StopRobotMachineInterfaceOperation.Descriptor,
            StopRobotMachineInterfaceOperation.CreateCommand, StopRobotMachineInterfaceOperation.OutputContracts,
            StopRobotMachineInterfaceOperation.CreateResult);

    [OperationImplementation("robot_operations.set_calibration_appliance_real_value")]
    public override Task<Api.SetCalibrationApplianceRealValueResult> SetCalibrationApplianceRealValue(Api.SetCalibrationApplianceRealValueRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetCalibrationApplianceRealValueOperation.Descriptor,
            SetCalibrationApplianceRealValueOperation.CreateCommand, SetCalibrationApplianceRealValueOperation.OutputContracts,
            SetCalibrationApplianceRealValueOperation.CreateResult);

    [OperationImplementation("robot_operations.move_robot_machine_to_frame")]
    public override Task<Api.MoveRobotMachineToFrameResult> MoveRobotMachineToFrame(Api.MoveRobotMachineToFrameRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MoveRobotMachineToFrameOperation.Descriptor,
            MoveRobotMachineToFrameOperation.CreateCommand, MoveRobotMachineToFrameOperation.OutputContracts,
            MoveRobotMachineToFrameOperation.CreateResult);

    [OperationImplementation("robot_operations.set_robot_machine_parameter")]
    public override Task<Api.SetRobotMachineParameterResult> SetRobotMachineParameter(Api.SetRobotMachineParameterRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetRobotMachineParameterOperation.Descriptor,
            SetRobotMachineParameterOperation.CreateCommand, SetRobotMachineParameterOperation.OutputContracts,
            SetRobotMachineParameterOperation.CreateResult);

    [OperationImplementation("robot_operations.get_robot_machine_model_link_parameters")]
    public override Task<Api.GetRobotMachineModelLinkParametersResult> GetRobotMachineModelLinkParameters(Api.GetRobotMachineModelLinkParametersRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetRobotMachineModelLinkParametersOperation.Descriptor,
            GetRobotMachineModelLinkParametersOperation.CreateCommand, GetRobotMachineModelLinkParametersOperation.OutputContracts,
            GetRobotMachineModelLinkParametersOperation.CreateResult);

    [OperationImplementation("robot_operations.set_active_robot_calibration")]
    public override Task<Api.SetActiveRobotCalibrationResult> SetActiveRobotCalibration(Api.SetActiveRobotCalibrationRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetActiveRobotCalibrationOperation.Descriptor,
            SetActiveRobotCalibrationOperation.CreateCommand, SetActiveRobotCalibrationOperation.OutputContracts,
            SetActiveRobotCalibrationOperation.CreateResult);

    [OperationImplementation("robot_operations.move_robot_machine_to_named_destination")]
    public override Task<Api.MoveRobotMachineToNamedDestinationResult> MoveRobotMachineToNamedDestination(Api.MoveRobotMachineToNamedDestinationRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MoveRobotMachineToNamedDestinationOperation.Descriptor,
            MoveRobotMachineToNamedDestinationOperation.CreateCommand, MoveRobotMachineToNamedDestinationOperation.OutputContracts,
            MoveRobotMachineToNamedDestinationOperation.CreateResult);

    [OperationImplementation("robot_operations.start_stop_robot_calibration_trapping")]
    public override Task<Api.StartStopRobotCalibrationTrappingResult> StartStopRobotCalibrationTrapping(Api.StartStopRobotCalibrationTrappingRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, StartStopRobotCalibrationTrappingOperation.Descriptor,
            StartStopRobotCalibrationTrappingOperation.CreateCommand, StartStopRobotCalibrationTrappingOperation.OutputContracts,
            StartStopRobotCalibrationTrappingOperation.CreateResult);

    [OperationImplementation("robot_operations.add_robot_machine_sa_machine")]
    public override Task<Api.AddRobotMachineSaMachineResult> AddRobotMachineSaMachine(Api.AddRobotMachineSaMachineRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AddRobotMachineSaMachineOperation.Descriptor,
            AddRobotMachineSaMachineOperation.CreateCommand, AddRobotMachineSaMachineOperation.OutputContracts,
            AddRobotMachineSaMachineOperation.CreateResult);

    [OperationImplementation("robot_operations.move_robot_machine_to_joint_pose_six_dof")]
    public override Task<Api.MoveRobotMachineToJointPoseSixDofResult> MoveRobotMachineToJointPoseSixDof(Api.MoveRobotMachineToJointPoseSixDofRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MoveRobotMachineToJointPoseSixDofOperation.Descriptor,
            MoveRobotMachineToJointPoseSixDofOperation.CreateCommand, MoveRobotMachineToJointPoseSixDofOperation.OutputContracts,
            MoveRobotMachineToJointPoseSixDofOperation.CreateResult);

}
