using Briosa.Server.Operations.WaveA;
using Briosa.Worker.Control;
using Google.Protobuf;
using Api = global::Briosa;

namespace Briosa.Server.Operations.WaveB;

internal static class RobotCalibrationApplianceNodeWaveBOperationCatalog
{
    private const string Service = "briosa.RobotCalibrationApplianceNodeOperations";

    public static IReadOnlyList<MpOperationContract> Operations { get; } =
    [
        Mutating(
            "robot_calibration_appliance_node_operations.add_calibration_appliance_node",
            "Add Calibration Appliance Node",
            "AddCalibrationApplianceNode",
            [
                new("calibration_appliance_node_to_add", "Calibration Appliance Node to Add", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true)]),
        Mutating(
            "robot_calibration_appliance_node_operations.clear_calibration_appliance_node_trap_manager_requests",
            "Clear Calibration Appliance Node Trap Manager Requests",
            "ClearCalibrationApplianceNodeTrapManagerRequests",
            [
                new("calibration_appliance_node", "Calibration Appliance Node", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true)]),
        Mutating(
            "robot_calibration_appliance_node_operations.connect_disconnect_calibration_appliance_node",
            "Connect/Disconnect Calibration Appliance Node",
            "ConnectDisconnectCalibrationApplianceNode",
            [
                new("calibration_appliance_node", "Calibration Appliance Node", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("connect", "Connect(TRUE) or Disconnect(FALSE)?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false)]),
        Mutating(
            "robot_calibration_appliance_node_operations.delete_calibration_appliance_node",
            "Delete Calibration Appliance Node",
            "DeleteCalibrationApplianceNode",
            [
                new("calibration_appliance_node_to_delete", "Calibration Appliance Node to Delete", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true)], riskFlags: ["destructive"]),
        Mutating(
            "robot_calibration_appliance_node_operations.enable_disable_calibration_appliance_node_instrument_auto_point",
            "Enable/Disable Calibration Appliance Node Instrument Auto Point",
            "EnableDisableCalibrationApplianceNodeInstrumentAutoPoint",
            [
                new("calibration_appliance_node", "Calibration Appliance Node", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("enable_instrument_auto_point", "Enable Instrument Auto Point?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false)]),
        Mutating(
            "robot_calibration_appliance_node_operations.enable_disable_calibration_appliance_node_trap_manager",
            "Enable/Disable Calibration Appliance Node Trap Manager",
            "EnableDisableCalibrationApplianceNodeTrapManager",
            [
                new("calibration_appliance_node", "Calibration Appliance Node", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("enable", "Enable(TRUE), Disable(FALSE)?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false)]),
        ReadOnly(
            "robot_calibration_appliance_node_operations.get_calibration_appliance_node_data",
            "Get Calibration Appliance Node Data",
            "GetCalibrationApplianceNodeData",
            [
                new("calibration_appliance_node", "Calibration Appliance Node", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true)],
            [
                new("real_values", "Real Values", WorkerMpValueKind.DoubleArray, "GetDoubleArrayArg", "—", false, ArraySizeFieldName: "real_value_count")]),
        ReadOnly(
            "robot_calibration_appliance_node_operations.get_calibration_appliance_node_integer_value",
            "Get Calibration Appliance Node Integer Value",
            "GetCalibrationApplianceNodeIntegerValue",
            [
                new("calibration_appliance_node", "Calibration Appliance Node", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("index_offset", "Index Offset", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false)],
            [
                new("integer_value", "Integer Value", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false)]),
        ReadOnly(
            "robot_calibration_appliance_node_operations.get_calibration_appliance_node_real_value",
            "Get Calibration Appliance Node Real Value",
            "GetCalibrationApplianceNodeRealValue",
            [
                new("calibration_appliance_node", "Calibration Appliance Node", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("index_offset", "Index Offset", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false)],
            [
                new("real_value", "Real Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false)]),
        ReadOnly(
            "robot_calibration_appliance_node_operations.get_calibration_appliance_node_status",
            "Get Calibration Appliance Node Status",
            "GetCalibrationApplianceNodeStatus",
            [
                new("calibration_appliance_node", "Calibration Appliance Node", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true)],
            [
                new("instrument_connected", "Instrument Connected?", WorkerMpValueKind.Logical, "GetBoolArg", "—", false),
                new("calibration_appliance_connected", "Calibration Appliance Connected?", WorkerMpValueKind.Logical, "GetBoolArg", "—", false)]),
        Mutating(
            "robot_calibration_appliance_node_operations.set_calibration_appliance_node_calibration_appliance_ip_address",
            "Set Calibration Appliance Node Calibration Appliance IP Address",
            "SetCalibrationApplianceNodeCalibrationApplianceIpAddress",
            [
                new("calibration_appliance_node", "Calibration Appliance Node", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("calibration_appliance_ip_address", "Calibration Appliance IP Address", WorkerMpValueKind.Text, "SetStringArg", "0.0.0.0", false)]),
        Mutating(
            "robot_calibration_appliance_node_operations.set_calibration_appliance_node_display_robot",
            "Set Calibration Appliance Node Display Robot",
            "SetCalibrationApplianceNodeDisplayRobot",
            [
                new("calibration_appliance_node", "Calibration Appliance Node", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("machine_id", "Machine ID", WorkerMpValueKind.CollectionMachineId, "SetColMachineIdArg", "Required", true)]),
        Mutating(
            "robot_calibration_appliance_node_operations.set_calibration_appliance_node_instrument",
            "Set Calibration Appliance Node Instrument",
            "SetCalibrationApplianceNodeInstrument",
            [
                new("calibration_appliance_node", "Calibration Appliance Node", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true)]),
        Mutating(
            "robot_calibration_appliance_node_operations.set_calibration_appliance_node_instrument_dwell_time",
            "Set Calibration Appliance Node Instrument Dwell Time",
            "SetCalibrationApplianceNodeInstrumentDwellTime",
            [
                new("calibration_appliance_node", "Calibration Appliance Node", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("measurement_dwell_time", "Measurement Dwell Time (Seconds)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false)]),
        Mutating(
            "robot_calibration_appliance_node_operations.set_calibration_appliance_node_integer_value",
            "Set Calibration Appliance Node Integer Value",
            "SetCalibrationApplianceNodeIntegerValue",
            [
                new("calibration_appliance_node", "Calibration Appliance Node", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("index_offset", "Index Offset", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("integer_value", "Integer Value", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false)]),
        Mutating(
            "robot_calibration_appliance_node_operations.set_calibration_appliance_node_measurement_frame",
            "Set Calibration Appliance Node Measurement Frame",
            "SetCalibrationApplianceNodeMeasurementFrame",
            [
                new("calibration_appliance_node", "Calibration Appliance Node", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("measurement_reference_frame", "Measurement Reference Frame", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Frame)]),
        Mutating(
            "robot_calibration_appliance_node_operations.set_calibration_appliance_node_measurement_offset_transform",
            "Set Calibration Appliance Node Measurement Offset Transform",
            "SetCalibrationApplianceNodeMeasurementOffsetTransform",
            [
                new("calibration_appliance_node", "Calibration Appliance Node", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("measurement_offset_transform", "Measurement Offset Transform", WorkerMpValueKind.Transform, "SetTransformArg", "Required", true)]),
        Mutating(
            "robot_calibration_appliance_node_operations.set_calibration_appliance_node_measurement_point_group",
            "Set Calibration Appliance Node Measurement Point Group",
            "SetCalibrationApplianceNodeMeasurementPointGroup",
            [
                new("calibration_appliance_node", "Calibration Appliance Node", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("point_group_name", "Point Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup)]),
        Mutating(
            "robot_calibration_appliance_node_operations.set_calibration_appliance_node_measurement_profile",
            "Set Calibration Appliance Node Measurement Profile",
            "SetCalibrationApplianceNodeMeasurementProfile",
            [
                new("calibration_appliance_node", "Calibration Appliance Node", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("measurement_profile", "Measurement Profile", WorkerMpValueKind.Text, "SetStringArg", "Empty", false)]),
        Mutating(
            "robot_calibration_appliance_node_operations.set_calibration_appliance_node_measurement_target",
            "Set Calibration Appliance Node Measurement Target",
            "SetCalibrationApplianceNodeMeasurementTarget",
            [
                new("calibration_appliance_node", "Calibration Appliance Node", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("measurement_target", "Measurement Target", WorkerMpValueKind.Text, "SetStringArg", "Empty", false)]),
        Mutating(
            "robot_calibration_appliance_node_operations.set_calibration_appliance_node_real_value",
            "Set Calibration Appliance Node Real Value",
            "SetCalibrationApplianceNodeRealValue",
            [
                new("calibration_appliance_node", "Calibration Appliance Node", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("index_offset", "Index Offset", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("real_value", "Real Value", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false)]),
        Mutating(
            "robot_calibration_appliance_node_operations.set_calibration_appliance_node_trapping_node_id",
            "Set Calibration Appliance Node Trapping Node ID",
            "SetCalibrationApplianceNodeTrappingNodeId",
            [
                new("calibration_appliance_node", "Calibration Appliance Node", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("trapping_node_id", "Trapping Node ID", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false)]),
        Mutating(
            "robot_calibration_appliance_node_operations.skip_calibration_appliance_node_measurement",
            "Skip Calibration Appliance Node Measurement",
            "SkipCalibrationApplianceNodeMeasurement",
            [
                new("calibration_appliance_node", "Calibration Appliance Node", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true)]),
        Mutating(
            "robot_calibration_appliance_node_operations.update_calibration_appliance_node_display_robot_joints",
            "Update Calibration Appliance Node Display Robot Joints",
            "UpdateCalibrationApplianceNodeDisplayRobotJoints",
            [
                new("calibration_appliance_node", "Calibration Appliance Node", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("enable_display_robot_joint_updates", "Enable Display Robot Joint Updates?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false)]),
        Mutating(
            "robot_calibration_appliance_node_operations.set_calibration_appliance_node_data",
            "Set Calibration Appliance Node Data",
            "SetCalibrationApplianceNodeData",
            [
                new("calibration_appliance_node", "Calibration Appliance Node", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("real_values", "Real Values", WorkerMpValueKind.DoubleArray, "SetDoubleArrayArg", "Empty array", false)]),
    ];

    private static MpOperationContract Mutating(string operationId, string step, string rpc, IReadOnlyList<MpArgumentContract> inputs, IReadOnlyList<MpArgumentContract>? outputs = null, IReadOnlyList<string>? riskFlags = null, Action<IMessage>? validateRequest = null) =>
        new(operationId, step, Service, rpc, "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, riskFlags ?? [], inputs, outputs ?? [], validateRequest);

    private static MpOperationContract ReadOnly(string operationId, string step, string rpc, IReadOnlyList<MpArgumentContract> inputs, IReadOnlyList<MpArgumentContract> outputs) =>
        new(operationId, step, Service, rpc, "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, [], inputs, outputs);
}
