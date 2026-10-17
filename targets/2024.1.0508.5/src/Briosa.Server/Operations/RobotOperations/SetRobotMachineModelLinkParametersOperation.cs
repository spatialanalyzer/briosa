using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotOperations;

internal static class SetRobotMachineModelLinkParametersOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "robot_operations.set_robot_machine_model_link_parameters", "Set Robot/Machine Model Link Parameters",
        "briosa.RobotOperations", "SetRobotMachineModelLinkParameters", "/briosa.RobotOperations/SetRobotMachineModelLinkParameters",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetRobotMachineModelLinkParametersRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.MachineId is null || string.IsNullOrWhiteSpace(request.MachineId.CollectionName))
            throw new ArgumentException("Machine ID is required.", nameof(request));
        if (request.SegmentCgInSegment is null)
            throw new ArgumentException("Segment CG In Segment is required.", nameof(request));

        var linkType = request.HasLinkType ? request.LinkType : Api.RobotModelLinkType.Dh;
        var activeJointComponent = request.HasActiveJointComponent
            ? request.ActiveJointComponent
            : Api.RobotActiveJointComponent.None;

        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Machine ID", WorkerMpValueKind.CollectionMachineId,
                    new WorkerCollectionMachineIdValue(request.MachineId.CollectionName, request.MachineId.MachineId),
                    "SetColMachineIdArg"),
                new("Link Name", WorkerMpValueKind.Text, new WorkerTextValue(request.LinkName), "SetStringArg"),
                new("Link Type", WorkerMpValueKind.Text, new WorkerTextValue(LinkType(linkType)), "SetStringArg"),
                new("DH ALPHA Component", WorkerMpValueKind.FloatingPoint,
                    RequiredValueMapper.Double(request.HasDhAlphaComponent, request.DhAlphaComponent, "dh_alpha_component"), "SetDoubleArg"),
                new("DH A Component", WorkerMpValueKind.FloatingPoint,
                    RequiredValueMapper.Double(request.HasDhAComponent, request.DhAComponent, "dh_a_component"), "SetDoubleArg"),
                new("DH D Component", WorkerMpValueKind.FloatingPoint,
                    RequiredValueMapper.Double(request.HasDhDComponent, request.DhDComponent, "dh_d_component"), "SetDoubleArg"),
                new("DH THETA Component", WorkerMpValueKind.FloatingPoint,
                    RequiredValueMapper.Double(request.HasDhThetaComponent, request.DhThetaComponent, "dh_theta_component"), "SetDoubleArg"),
                new("DH X-Axis Deflection Factor", WorkerMpValueKind.FloatingPoint,
                    RequiredValueMapper.Double(request.HasDhXAxisDeflectionFactor, request.DhXAxisDeflectionFactor, "dh_x_axis_deflection_factor"), "SetDoubleArg"),
                new("DH Y-Axis Deflection Factor", WorkerMpValueKind.FloatingPoint,
                    RequiredValueMapper.Double(request.HasDhYAxisDeflectionFactor, request.DhYAxisDeflectionFactor, "dh_y_axis_deflection_factor"), "SetDoubleArg"),
                new("DH Z-Axis Deflection Factor", WorkerMpValueKind.FloatingPoint,
                    RequiredValueMapper.Double(request.HasDhZAxisDeflectionFactor, request.DhZAxisDeflectionFactor, "dh_z_axis_deflection_factor"), "SetDoubleArg"),
                new("6DOF X Component", WorkerMpValueKind.FloatingPoint,
                    RequiredValueMapper.Double(request.HasSixDofXComponent, request.SixDofXComponent, "six_dof_x_component"), "SetDoubleArg"),
                new("6DOF Y Component", WorkerMpValueKind.FloatingPoint,
                    RequiredValueMapper.Double(request.HasSixDofYComponent, request.SixDofYComponent, "six_dof_y_component"), "SetDoubleArg"),
                new("6DPF Z Component", WorkerMpValueKind.FloatingPoint,
                    RequiredValueMapper.Double(request.HasSixDofZComponent, request.SixDofZComponent, "six_dof_z_component"), "SetDoubleArg"),
                new("6DOF RX Component", WorkerMpValueKind.FloatingPoint,
                    RequiredValueMapper.Double(request.HasSixDofRxComponent, request.SixDofRxComponent, "six_dof_rx_component"), "SetDoubleArg"),
                new("6DOF RY Component", WorkerMpValueKind.FloatingPoint,
                    RequiredValueMapper.Double(request.HasSixDofRyComponent, request.SixDofRyComponent, "six_dof_ry_component"), "SetDoubleArg"),
                new("6DOF RZ Component", WorkerMpValueKind.FloatingPoint,
                    RequiredValueMapper.Double(request.HasSixDofRzComponent, request.SixDofRzComponent, "six_dof_rz_component"), "SetDoubleArg"),
                new("Active Joint Component", WorkerMpValueKind.Text, new WorkerTextValue(ActiveJoint(activeJointComponent)), "SetStringArg"),
                new("Encoder Offset Value", WorkerMpValueKind.FloatingPoint,
                    RequiredValueMapper.Double(request.HasEncoderOffsetValue, request.EncoderOffsetValue, "encoder_offset_value"), "SetDoubleArg"),
                new("Minimum Encoder Limit", WorkerMpValueKind.FloatingPoint,
                    RequiredValueMapper.Double(request.HasMinimumEncoderLimit, request.MinimumEncoderLimit, "minimum_encoder_limit"), "SetDoubleArg"),
                new("Maximum Encoder Limit", WorkerMpValueKind.FloatingPoint,
                    RequiredValueMapper.Double(request.HasMaximumEncoderLimit, request.MaximumEncoderLimit, "maximum_encoder_limit"), "SetDoubleArg"),
                new("Encoder Sense Negative", WorkerMpValueKind.Logical,
                    RequiredValueMapper.Boolean(request.HasEncoderSenseNegative, request.EncoderSenseNegative, "encoder_sense_negative"), "SetBoolArg"),
                new("Include Additional Encoder", WorkerMpValueKind.Logical,
                    RequiredValueMapper.Boolean(request.HasIncludeAdditionalEncoder, request.IncludeAdditionalEncoder, "include_additional_encoder"), "SetBoolArg"),
                new("Additional Encoder Index Offset", WorkerMpValueKind.WholeNumber,
                    RequiredValueMapper.Integer(request.HasAdditionalEncoderIndexOffset, request.AdditionalEncoderIndexOffset, "additional_encoder_index_offset"), "SetIntegerArg"),
                new("Additional Encoder Sense Negative", WorkerMpValueKind.Logical,
                    RequiredValueMapper.Boolean(request.HasAdditionalEncoderSenseNegative, request.AdditionalEncoderSenseNegative, "additional_encoder_sense_negative"), "SetBoolArg"),
                new("Segment Origin Mass in Kg", WorkerMpValueKind.FloatingPoint,
                    RequiredValueMapper.Double(request.HasSegmentOriginMassKg, request.SegmentOriginMassKg, "segment_origin_mass_kg"), "SetDoubleArg"),
                new("Segment CG Mass in Kg", WorkerMpValueKind.FloatingPoint,
                    RequiredValueMapper.Double(request.HasSegmentCgMassKg, request.SegmentCgMassKg, "segment_cg_mass_kg"), "SetDoubleArg"),
                new("Segment CG In Segment", WorkerMpValueKind.Vector,
                    new WorkerVectorValue(request.SegmentCgInSegment.X, request.SegmentCgInSegment.Y, request.SegmentCgInSegment.Z),
                    "SetVectorArg")
            ], []);
    }

    public static Api.SetRobotMachineModelLinkParametersResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };

    private static string LinkType(Api.RobotModelLinkType value) => value switch
    {
        Api.RobotModelLinkType.Dh => "DH",
        Api.RobotModelLinkType.SixDof => "6DOF",
        _ => throw new ArgumentException("Link Type must be a supported exact-target choice.")
    };

    private static string ActiveJoint(Api.RobotActiveJointComponent value) => value switch
    {
        Api.RobotActiveJointComponent.None => "NONE",
        Api.RobotActiveJointComponent.X => "X",
        Api.RobotActiveJointComponent.Y => "Y",
        Api.RobotActiveJointComponent.Z => "Z",
        Api.RobotActiveJointComponent.Rx => "Rx",
        Api.RobotActiveJointComponent.Ry => "Ry",
        Api.RobotActiveJointComponent.Rz => "Rz",
        Api.RobotActiveJointComponent.Alpha => "Alpha",
        Api.RobotActiveJointComponent.A => "A",
        Api.RobotActiveJointComponent.D => "D",
        Api.RobotActiveJointComponent.Theta => "THETA",
        _ => throw new ArgumentException("Active Joint Component must be a supported exact-target choice.")
    };
}
