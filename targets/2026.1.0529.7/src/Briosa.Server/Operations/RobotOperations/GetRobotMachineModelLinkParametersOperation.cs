using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotOperations;

internal static class GetRobotMachineModelLinkParametersOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "robot_operations.get_robot_machine_model_link_parameters", "Get Robot/Machine Model Link Parameters",
        "briosa.RobotOperations", "GetRobotMachineModelLinkParameters", "/briosa.RobotOperations/GetRobotMachineModelLinkParameters",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("link_type", "Link Type", WorkerMpValueKind.Text),
        new("dh_alpha_component", "DH ALPHA Component", WorkerMpValueKind.FloatingPoint),
        new("dh_a_component", "DH A Component", WorkerMpValueKind.FloatingPoint),
        new("dh_d_component", "DH D Component", WorkerMpValueKind.FloatingPoint),
        new("dh_theta_component", "DH THETA Component", WorkerMpValueKind.FloatingPoint),
        new("dh_x_axis_deflection_factor", "DH X-Axis Deflection Factor", WorkerMpValueKind.FloatingPoint),
        new("dh_y_axis_deflection_factor", "DH Y-Axis Deflection Factor", WorkerMpValueKind.FloatingPoint),
        new("dh_z_axis_deflection_factor", "DH Z-Axis Deflection Factor", WorkerMpValueKind.FloatingPoint),
        new("six_dof_x_component", "6DOF X Component", WorkerMpValueKind.FloatingPoint),
        new("six_dof_y_component", "6DOF Y Component", WorkerMpValueKind.FloatingPoint),
        new("six_dof_z_component", "6DPF Z Component", WorkerMpValueKind.FloatingPoint),
        new("six_dof_rx_component", "6DOF RX Component", WorkerMpValueKind.FloatingPoint),
        new("six_dof_ry_component", "6DOF RY Component", WorkerMpValueKind.FloatingPoint),
        new("six_dof_rz_component", "6DOF RZ Component", WorkerMpValueKind.FloatingPoint),
        new("active_joint_component", "Active Joint Component", WorkerMpValueKind.Text),
        new("encoder_value", "Encoder Value", WorkerMpValueKind.FloatingPoint),
        new("encoder_offset_value", "Encoder Offset Value", WorkerMpValueKind.FloatingPoint),
        new("minimum_encoder_limit", "Minimum Encoder Limit", WorkerMpValueKind.FloatingPoint),
        new("maximum_encoder_limit", "Maximum Encoder Limit", WorkerMpValueKind.FloatingPoint),
        new("encoder_sense_negative", "Encoder Sense Negative", WorkerMpValueKind.Logical),
        new("include_additional_encoder", "Include Additional Encoder", WorkerMpValueKind.Logical),
        new("additional_encoder_index_offset", "Additional Encoder Index Offset", WorkerMpValueKind.WholeNumber),
        new("additional_encoder_sense_negative", "Additional Encoder Sense Negative", WorkerMpValueKind.Logical),
        new("segment_origin_mass_kg", "Segment Origin Mass in Kg", WorkerMpValueKind.FloatingPoint),
        new("segment_cg_mass_kg", "Segment CG Mass in Kg", WorkerMpValueKind.FloatingPoint),
        new("segment_cg_in_segment", "Segment CG In Segment", WorkerMpValueKind.Vector)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetRobotMachineModelLinkParametersRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.MachineId is null || string.IsNullOrWhiteSpace(request.MachineId.CollectionName))
            throw new ArgumentException("Machine ID is required.", nameof(request));

        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Machine ID", WorkerMpValueKind.CollectionMachineId,
                    new WorkerCollectionMachineIdValue(request.MachineId.CollectionName, request.MachineId.MachineId),
                    "SetColMachineIdArg"),
                new("Link Name", WorkerMpValueKind.Text, new WorkerTextValue(request.LinkName), "SetStringArg")
            ],
            [
                new("Link Type", WorkerMpValueKind.Text, "GetStringArg"),
                new("DH ALPHA Component", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("DH A Component", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("DH D Component", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("DH THETA Component", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("DH X-Axis Deflection Factor", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("DH Y-Axis Deflection Factor", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("DH Z-Axis Deflection Factor", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("6DOF X Component", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("6DOF Y Component", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("6DPF Z Component", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("6DOF RX Component", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("6DOF RY Component", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("6DOF RZ Component", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Active Joint Component", WorkerMpValueKind.Text, "GetStringArg"),
                new("Encoder Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Encoder Offset Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Minimum Encoder Limit", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Maximum Encoder Limit", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Encoder Sense Negative", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Include Additional Encoder", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Additional Encoder Index Offset", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
                new("Additional Encoder Sense Negative", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Segment Origin Mass in Kg", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Segment CG Mass in Kg", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Segment CG In Segment", WorkerMpValueKind.Vector, "GetVectorArg")
            ]);
    }

    public static Api.GetRobotMachineModelLinkParametersResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            LinkType = ParseLinkType(values[0].RequireValue<WorkerTextValue>().Value),
            DhAlphaComponent = Double(values[1]),
            DhAComponent = Double(values[2]),
            DhDComponent = Double(values[3]),
            DhThetaComponent = Double(values[4]),
            DhXAxisDeflectionFactor = Double(values[5]),
            DhYAxisDeflectionFactor = Double(values[6]),
            DhZAxisDeflectionFactor = Double(values[7]),
            SixDofXComponent = Double(values[8]),
            SixDofYComponent = Double(values[9]),
            SixDofZComponent = Double(values[10]),
            SixDofRxComponent = Double(values[11]),
            SixDofRyComponent = Double(values[12]),
            SixDofRzComponent = Double(values[13]),
            ActiveJointComponent = ParseActiveJointComponent(values[14].RequireValue<WorkerTextValue>().Value),
            EncoderValue = Double(values[15]),
            EncoderOffsetValue = Double(values[16]),
            MinimumEncoderLimit = Double(values[17]),
            MaximumEncoderLimit = Double(values[18]),
            EncoderSenseNegative = Boolean(values[19]),
            IncludeAdditionalEncoder = Boolean(values[20]),
            AdditionalEncoderIndexOffset = Integer(values[21]),
            AdditionalEncoderSenseNegative = Boolean(values[22]),
            SegmentOriginMassKg = Double(values[23]),
            SegmentCgMassKg = Double(values[24]),
            SegmentCgInSegment = Vector(values[25]),
            Execution = completed.Details
        };
    }

    private static double Double(WorkerMpOutputValue value) => value.RequireValue<WorkerDoubleValue>().Value;
    private static bool Boolean(WorkerMpOutputValue value) => value.RequireValue<WorkerBooleanValue>().Value;
    private static int Integer(WorkerMpOutputValue value) => value.RequireValue<WorkerIntegerValue>().Value;

    private static Api.Vector Vector(WorkerMpOutputValue value)
    {
        var vector = value.RequireValue<WorkerVectorValue>();
        return new() { X = vector.X, Y = vector.Y, Z = vector.Z };
    }

    private static Api.RobotModelLinkType ParseLinkType(string value) => value switch
    {
        "DH" => Api.RobotModelLinkType.Dh,
        "6DOF" => Api.RobotModelLinkType.SixDof,
        _ => throw new InvalidOperationException("SpatialAnalyzer returned an unsupported robot model link type.")
    };

    private static Api.RobotActiveJointComponent ParseActiveJointComponent(string value) => value switch
    {
        "NONE" => Api.RobotActiveJointComponent.None,
        "X" => Api.RobotActiveJointComponent.X,
        "Y" => Api.RobotActiveJointComponent.Y,
        "Z" => Api.RobotActiveJointComponent.Z,
        "RX" => Api.RobotActiveJointComponent.Rx,
        "RY" => Api.RobotActiveJointComponent.Ry,
        "RZ" => Api.RobotActiveJointComponent.Rz,
        "ALPHA" => Api.RobotActiveJointComponent.Alpha,
        "A" => Api.RobotActiveJointComponent.A,
        "D" => Api.RobotActiveJointComponent.D,
        "THETA" => Api.RobotActiveJointComponent.Theta,
        _ => throw new InvalidOperationException("SpatialAnalyzer returned an unsupported active robot joint component.")
    };
}
