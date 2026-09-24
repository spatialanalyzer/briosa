using Briosa.Server.Operations.WaveA;
using Briosa.Worker.Control;
using Google.Protobuf;
using Api = global::Briosa;

namespace Briosa.Server.Operations.WaveB;

internal static class RobotWaveBOperationCatalog
{
    private const string Service = "briosa.RobotOperations";

    public static IReadOnlyList<MpOperationContract> Operations { get; } =
    [
        Mutating(
            "robot_operations.add_robot_machine_manip_kin",
            "Add Robot/Machine (.ManipKin)",
            "AddRobotMachineManipKin",
            [
                new("manip_kin_file", ".ManipKin File", WorkerMpValueKind.FileReference, "SetFilePathArg", "Omitted", false, null, null, true)]),
        Mutating(
            "robot_operations.add_robot_machine_sa_machine",
            "Add Robot/Machine (.SAMachine)",
            "AddRobotMachineSaMachine",
            [
                new("sa_machine_file", ".SAMachine File", WorkerMpValueKind.FileReference, "SetFilePathArg", "Omitted", false, null, null, true)]),
        Mutating(
            "robot_operations.compute_robot_machine_adjusted_goal_frame",
            "Compute Robot/Machine Adjusted Goal Frame",
            "ComputeRobotMachineAdjustedGoalFrame",
            [
                new("original_goal_frame", "Original Goal Frame", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Frame),
                new("last_adjusted_goal_frame", "Last Adjusted Goal Frame", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Frame),
                new("actual_measured_frame", "Actual Measured Frame", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Frame),
                new("modified_goal_frame", "Modified Goal Frame", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Frame)],
            [
                new("transform_value", "Transform Value", WorkerMpValueKind.Transform, "GetTransformArg", "—", false)]),
        Mutating(
            "robot_operations.create_robot_calibration",
            "Create Robot Calibration",
            "CreateRobotCalibration",
            [
                new("machine_id", "Machine ID", WorkerMpValueKind.CollectionMachineId, "SetColMachineIdArg", "Required", true),
                new("calibration_name", "Calibration Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false)]),
        Mutating(
            "robot_operations.delete_robot_calibration",
            "Delete Robot Calibration",
            "DeleteRobotCalibration",
            [
                new("machine_id", "Machine ID", WorkerMpValueKind.CollectionMachineId, "SetColMachineIdArg", "Required", true),
                new("calibration_name", "Calibration Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false)], riskFlags: ["destructive"]),
        Mutating(
            "robot_operations.delete_robot_machine",
            "Delete Robot/Machine",
            "DeleteRobotMachine",
            [
                new("machine_id", "Machine ID", WorkerMpValueKind.CollectionMachineId, "SetColMachineIdArg", "Required", true)], riskFlags: ["destructive"]),
        ReadOnly(
            "robot_operations.get_calibration_appliance_data",
            "Get Calibration Appliance Data",
            "GetCalibrationApplianceData",
            [],
            [
                new("real_values", "Real Values", WorkerMpValueKind.DoubleArray, "GetDoubleArrayArg", "—", false, ArraySizeFieldName: "real_value_count")]),
        ReadOnly(
            "robot_operations.get_calibration_appliance_integer_value",
            "Get Calibration Appliance Integer Value",
            "GetCalibrationApplianceIntegerValue",
            [
                new("index_offset", "Index Offset", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false)],
            [
                new("integer_value", "Integer Value", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false)]),
        ReadOnly(
            "robot_operations.get_calibration_appliance_real_value",
            "Get Calibration Appliance Real Value",
            "GetCalibrationApplianceRealValue",
            [
                new("index_offset", "Index Offset", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false)],
            [
                new("real_value", "Real Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false)]),
        ReadOnly(
            "robot_operations.get_robot_machine_model_link_parameters",
            "Get Robot/Machine Model Link Parameters",
            "GetRobotMachineModelLinkParameters",
            [
                new("machine_id", "Machine ID", WorkerMpValueKind.CollectionMachineId, "SetColMachineIdArg", "Required", true),
                new("link_name", "Link Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false)],
            [
                new("link_type", "Link Type", WorkerMpValueKind.Text, "GetStringArg", "—", false, null, ["DH", "6DOF"]),
                new("dh_alpha_component", "DH ALPHA Component", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("dh_a_component", "DH A Component", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("dh_d_component", "DH D Component", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("dh_theta_component", "DH THETA Component", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("dh_x_axis_deflection_factor", "DH X-Axis Deflection Factor", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("dh_y_axis_deflection_factor", "DH Y-Axis Deflection Factor", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("dh_z_axis_deflection_factor", "DH Z-Axis Deflection Factor", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("six_dof_x_component", "6DOF X Component", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("six_dof_y_component", "6DOF Y Component", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("six_dof_z_component", "6DPF Z Component", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("six_dof_rx_component", "6DOF RX Component", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("six_dof_ry_component", "6DOF RY Component", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("six_dof_rz_component", "6DOF RZ Component", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("active_joint_component", "Active Joint Component", WorkerMpValueKind.Text, "GetStringArg", "—", false, null, ["NONE", "X", "Y", "Z", "RX", "RY", "RZ", "ALPHA", "A", "D", "THETA"]),
                new("encoder_value", "Encoder Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("encoder_offset_value", "Encoder Offset Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("minimum_encoder_limit", "Minimum Encoder Limit", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("maximum_encoder_limit", "Maximum Encoder Limit", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("encoder_sense_negative", "Encoder Sense Negative", WorkerMpValueKind.Logical, "GetBoolArg", "—", false),
                new("include_additional_encoder", "Include Additional Encoder", WorkerMpValueKind.Logical, "GetBoolArg", "—", false),
                new("additional_encoder_index_offset", "Additional Encoder Index Offset", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false),
                new("additional_encoder_sense_negative", "Additional Encoder Sense Negative", WorkerMpValueKind.Logical, "GetBoolArg", "—", false),
                new("segment_origin_mass_kg", "Segment Origin Mass in Kg", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("segment_cg_mass_kg", "Segment CG Mass in Kg", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("segment_cg_in_segment", "Segment CG In Segment", WorkerMpValueKind.Vector, "GetVectorArg", "—", false)]),
        ReadOnly(
            "robot_operations.get_robot_machine_parameter",
            "Get Robot/Machine Parameter",
            "GetRobotMachineParameter",
            [
                new("machine_id", "Machine ID", WorkerMpValueKind.CollectionMachineId, "SetColInstIdArg", "Required", true),
                new("parameter_name", "Parameter Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false)],
            [
                new("parameter_value", "Parameter Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false)]),
        Mutating(
            "robot_operations.import_poses_match_to_frames",
            "Import Poses Match to Frames",
            "ImportPosesMatchToFrames",
            [
                new("machine_id", "Machine ID", WorkerMpValueKind.CollectionMachineId, "SetColMachineIdArg", "Required", true),
                new("calibration_name", "Calibration Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("frame_names", "Frame Names", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("csv_joint_set_file", "FilePath for CSV Joint Set File", WorkerMpValueKind.FileReference, "SetFilePathArg", "Omitted", false, null, null, true)]),
        Mutating(
            "robot_operations.import_poses_match_to_measurements",
            "Import Poses Match to Measurements",
            "ImportPosesMatchToMeasurements",
            [
                new("machine_id", "Machine ID", WorkerMpValueKind.CollectionMachineId, "SetColMachineIdArg", "Required", true),
                new("calibration_name", "Calibration Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("point_names", "Point Names", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true),
                new("csv_joint_set_file", "FilePath for CSV Joint Set File", WorkerMpValueKind.FileReference, "SetFilePathArg", "Omitted", false, null, null, true)]),
        Mutating(
            "robot_operations.move_robot_machine_through_path",
            "Move Robot/Machine through Path",
            "MoveRobotMachineThroughPath",
            [
                new("machine_id", "Machine ID", WorkerMpValueKind.CollectionMachineId, "SetColMachineIdArg", "Required", true),
                new("path_frames", "Path Frames", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("use_sa_kinematics", "Use SA Kinematics", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("linear_segments", "Linear Segments", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("acknowledge_arrival", "Acknowledge Arrival", WorkerMpValueKind.Logical, "SetBoolArg", "true", false)]),
        Mutating(
            "robot_operations.move_robot_machine_to_frame",
            "Move Robot/Machine to Frame",
            "MoveRobotMachineToFrame",
            [
                new("machine_id", "Machine ID", WorkerMpValueKind.CollectionMachineId, "SetColMachineIdArg", "Required", true),
                new("destination_frame", "Destination Frame", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Frame),
                new("use_sa_kinematics", "Use SA Kinematics", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("acknowledge_arrival", "Acknowledge Arrival", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)],
            [
                new("actual_transform_in_working", "Actual Transform In Working (result)", WorkerMpValueKind.Transform, "GetTransformArg", "—", false)]),
        Mutating(
            "robot_operations.move_robot_machine_to_joint_pose_six_dof",
            "Move Robot/Machine to Joint Pose (6DOF)",
            "MoveRobotMachineToJointPoseSixDof",
            [
                new("machine_id", "Machine ID", WorkerMpValueKind.CollectionMachineId, "SetColMachineIdArg", "Required", true),
                new("joint_1", "Joint 1", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("joint_2", "Joint 2", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("joint_3", "Joint 3", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("joint_4", "Joint 4", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("joint_5", "Joint 5", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("joint_6", "Joint 6", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false)]),
        Mutating(
            "robot_operations.move_robot_machine_to_named_destination",
            "Move Robot/Machine to Named Destination",
            "MoveRobotMachineToNamedDestination",
            [
                new("machine_id", "Machine ID", WorkerMpValueKind.CollectionMachineId, "SetColMachineIdArg", "Required", true),
                new("destination_name", "Destination Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("acknowledge_arrival", "Acknowledge Arrival", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)],
            [
                new("actual_transform_in_working", "Actual Transform In Working (result)", WorkerMpValueKind.Transform, "GetTransformArg", "—", false)]),
        Mutating(
            "robot_operations.perform_robot_calibration_alternate",
            "Perform Robot Calibration (Alternate)",
            "PerformRobotCalibrationAlternate",
            [
                new("machine_id", "Machine ID", WorkerMpValueKind.CollectionMachineId, "SetColMachineIdArg", "Required", true),
                new("calibration_name", "Calibration Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("set_current_base_as_nominal", "Set Current Base as Nominal?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("base_degrees_of_freedom", "BASE Degrees of Freedom", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("robot_degrees_of_freedom", "ROBOT Degrees of Freedom", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("tool_degrees_of_freedom", "TOOL Degrees of Freedom", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("show_interface", "Show Interface", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("allowed_outlier_rejection_count", "Allowed Outlier Rejection Count", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("allowable_maximum_error", "Allowable Maximum Error", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("allowable_average_error", "Allowable Average Error", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false)],
            [
                new("metrics", "XYZ Max", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "xyz_max"),
                new("metrics", "XYZ Average", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "xyz_average"),
                new("metrics", "XYZ RMS", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "xyz_rms"),
                new("metrics", "Orient Max", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "orient_max"),
                new("metrics", "Orient Average", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "orient_average"),
                new("metrics", "Orient RMS", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "orient_rms"),
                new("metrics", "Robustness", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "robustness")]),
        Mutating(
            "robot_operations.set_active_robot_calibration",
            "Set Active Robot Calibration",
            "SetActiveRobotCalibration",
            [
                new("machine_id", "Machine ID", WorkerMpValueKind.CollectionMachineId, "SetColMachineIdArg", "Required", true),
                new("calibration_name", "Calibration Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false)]),
        Mutating(
            "robot_operations.set_calibration_appliance_integer_value",
            "Set Calibration Appliance Integer Value",
            "SetCalibrationApplianceIntegerValue",
            [
                new("index_offset", "Index Offset", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("integer_value", "Integer Value", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false)]),
        Mutating(
            "robot_operations.set_calibration_appliance_real_value",
            "Set Calibration Appliance Real Value",
            "SetCalibrationApplianceRealValue",
            [
                new("index_offset", "Index Offset", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("real_value", "Real Value", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false)]),
        Mutating(
            "robot_operations.set_robot_calibration_measurement_offset_in_tool_frame",
            "Set Robot Calibration Measurement Offset In Tool Frame",
            "SetRobotCalibrationMeasurementOffsetInToolFrame",
            [
                new("machine_id", "Machine ID", WorkerMpValueKind.CollectionMachineId, "SetColMachineIdArg", "Required", true),
                new("calibration_name", "Calibration Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("measurement_frame", "Measurement Frame (relative to tool)", WorkerMpValueKind.Transform, "SetTransformArg", "Required", true)]),
        Mutating(
            "robot_operations.set_robot_calibration_tool_frame",
            "Set Robot Calibration Tool Frame",
            "SetRobotCalibrationToolFrame",
            [
                new("machine_id", "Machine ID", WorkerMpValueKind.CollectionMachineId, "SetColMachineIdArg", "Required", true),
                new("calibration_name", "Calibration Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("tool_frame", "Tool Frame (relative to flange)", WorkerMpValueKind.Transform, "SetTransformArg", "Required", true)]),
        Mutating(
            "robot_operations.set_robot_machine_base_transform",
            "Set Robot/Machine Base Transform",
            "SetRobotMachineBaseTransform",
            [
                new("machine_id", "Machine ID", WorkerMpValueKind.CollectionMachineId, "SetColMachineIdArg", "Required", true),
                new("destination_transform", "Destination Transform", WorkerMpValueKind.Transform, "SetTransformArg", "Required", true),
                new("reference_frame", "Reference Frame", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Frame),
                new("number_of_steps", "Number of Steps", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false)]),
        Mutating(
            "robot_operations.set_robot_machine_model_link_parameters",
            "Set Robot/Machine Model Link Parameters",
            "SetRobotMachineModelLinkParameters",
            [
                new("machine_id", "Machine ID", WorkerMpValueKind.CollectionMachineId, "SetColMachineIdArg", "Required", true),
                new("link_name", "Link Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("link_type", "Link Type", WorkerMpValueKind.Text, "SetStringArg", "DH", false, null, ["DH", "6DOF"]),
                new("dh_alpha_component", "DH ALPHA Component", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("dh_a_component", "DH A Component", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("dh_d_component", "DH D Component", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("dh_theta_component", "DH THETA Component", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("dh_x_axis_deflection_factor", "DH X-Axis Deflection Factor", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("dh_y_axis_deflection_factor", "DH Y-Axis Deflection Factor", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("dh_z_axis_deflection_factor", "DH Z-Axis Deflection Factor", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("six_dof_x_component", "6DOF X Component", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("six_dof_y_component", "6DOF Y Component", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("six_dof_z_component", "6DPF Z Component", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("six_dof_rx_component", "6DOF RX Component", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("six_dof_ry_component", "6DOF RY Component", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("six_dof_rz_component", "6DOF RZ Component", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("active_joint_component", "Active Joint Component", WorkerMpValueKind.Text, "SetStringArg", "NONE", false, null, ["NONE", "X", "Y", "Z", "Rx", "Ry", "Rz", "Alpha", "A", "D", "THETA"]),
                new("encoder_offset_value", "Encoder Offset Value", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("minimum_encoder_limit", "Minimum Encoder Limit", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("maximum_encoder_limit", "Maximum Encoder Limit", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("encoder_sense_negative", "Encoder Sense Negative", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("include_additional_encoder", "Include Additional Encoder", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("additional_encoder_index_offset", "Additional Encoder Index Offset", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("additional_encoder_sense_negative", "Additional Encoder Sense Negative", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("segment_origin_mass_kg", "Segment Origin Mass in Kg", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("segment_cg_mass_kg", "Segment CG Mass in Kg", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("segment_cg_in_segment", "Segment CG In Segment", WorkerMpValueKind.Vector, "SetVectorArg", "Required", true)]),
        Mutating(
            "robot_operations.set_robot_machine_parameter",
            "Set Robot/Machine Parameter",
            "SetRobotMachineParameter",
            [
                new("machine_id", "Machine ID", WorkerMpValueKind.CollectionMachineId, "SetColMachineIdArg", "Required", true),
                new("parameter_name", "Parameter Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("parameter_value", "Parameter Value", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false)]),
        Mutating(
            "robot_operations.simulate_robot_machine_path_output_csv_file",
            "Simulate Robot/Machine Path, Output CSV File",
            "SimulateRobotMachinePathOutputCsvFile",
            [
                new("machine_id", "Machine ID", WorkerMpValueKind.CollectionMachineId, "SetColMachineIdArg", "Required", true),
                new("path_frames", "Path Frames", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("output_csv_file", "Output CSV File", WorkerMpValueKind.FileReference, "SetFilePathArg", "Omitted", false, null, null, true)]),
        Mutating(
            "robot_operations.start_robot_machine_interface",
            "Start Robot/Machine Interface",
            "StartRobotMachineInterface",
            [
                new("machine_id", "Machine ID", WorkerMpValueKind.CollectionMachineId, "SetColInstIdArg", "Required", true),
                new("interface_type", "Interface Type", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("run_in_simulation", "Run in Simulation", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "robot_operations.start_stop_robot_calibration_trapping",
            "Start/Stop Robot Calibration Trapping",
            "StartStopRobotCalibrationTrapping",
            [
                new("machine_id", "Machine ID", WorkerMpValueKind.CollectionMachineId, "SetColMachineIdArg", "Required", true),
                new("calibration_name", "Calibration Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("instrument_id", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("start_trapping", "Start Trapping (FALSE = Stop)", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "robot_operations.stop_robot_machine_interface",
            "Stop Robot/Machine Interface",
            "StopRobotMachineInterface",
            [
                new("machine_id", "Machine ID", WorkerMpValueKind.CollectionMachineId, "SetColInstIdArg", "Required", true)]),
        ReadOnly(
            "robot_operations.get_robot_pose_for_a_frame",
            "Get Robot Pose for a Frame",
            "GetRobotPoseForAFrame",
            [
                new("machine_id", "Machine ID", WorkerMpValueKind.CollectionMachineId, "SetColMachineIdArg", "Required", true),
                new("goal_frame", "Goal Frame", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Frame),
                new("reference_pose", "Reference Pose", WorkerMpValueKind.DoubleArray, "SetDoubleArrayArg", "Empty array", false)],
            [
                new("goal_pose", "Goal Pose", WorkerMpValueKind.DoubleArray, "GetDoubleArrayArg", "—", false, ArraySizeFieldName: "goal_pose_count")]),
        Mutating(
            "robot_operations.perform_robot_calibration",
            "Perform Robot Calibration",
            "PerformRobotCalibration",
            [
                new("machine_id", "Machine ID", WorkerMpValueKind.CollectionMachineId, "SetColMachineIdArg", "Required", true),
                new("calibration_name", "Calibration Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("set_current_base_as_nominal", "Set Current Base as Nominal?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("show_interface", "Show Interface", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("allowed_outlier_rejection_count", "Allowed Outlier Rejection Count", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("allowable_maximum_error", "Allowable Maximum Error", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("allowable_average_error", "Allowable Average Error", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false)],
            [
                new("metrics", "XYZ Max", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, NestedFieldName: "xyz_max"),
                new("metrics", "XYZ Average", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, NestedFieldName: "xyz_average"),
                new("metrics", "XYZ RMS", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, NestedFieldName: "xyz_rms"),
                new("metrics", "Orient Max", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, NestedFieldName: "orient_max"),
                new("metrics", "Orient Average", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, NestedFieldName: "orient_average"),
                new("metrics", "Orient RMS", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, NestedFieldName: "orient_rms"),
                new("metrics", "Robustness", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, NestedFieldName: "robustness")]),
        Mutating(
            "robot_operations.set_calibration_appliance_data",
            "Set Calibration Appliance Data",
            "SetCalibrationApplianceData",
            [
                new("real_values", "Real Values", WorkerMpValueKind.DoubleArray, "SetDoubleArrayArg", "Empty array", false)]),
    ];

    private static MpOperationContract Mutating(string operationId, string step, string rpc, IReadOnlyList<MpArgumentContract> inputs, IReadOnlyList<MpArgumentContract>? outputs = null, IReadOnlyList<string>? riskFlags = null, Action<IMessage>? validateRequest = null) =>
        new(operationId, step, Service, rpc, "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, riskFlags ?? [], inputs, outputs ?? [], validateRequest);

    private static MpOperationContract ReadOnly(string operationId, string step, string rpc, IReadOnlyList<MpArgumentContract> inputs, IReadOnlyList<MpArgumentContract> outputs) =>
        new(operationId, step, Service, rpc, "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, [], inputs, outputs);
}
