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
                new("DH ALPHA Component", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.DhAlphaComponent), "SetDoubleArg"),
                new("DH A Component", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.DhAComponent), "SetDoubleArg"),
                new("DH D Component", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.DhDComponent), "SetDoubleArg"),
                new("DH THETA Component", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.DhThetaComponent), "SetDoubleArg"),
                new("DH X-Axis Deflection Factor", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.DhXAxisDeflectionFactor), "SetDoubleArg"),
                new("DH Y-Axis Deflection Factor", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.DhYAxisDeflectionFactor), "SetDoubleArg"),
                new("DH Z-Axis Deflection Factor", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.DhZAxisDeflectionFactor), "SetDoubleArg"),
                new("6DOF X Component", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.SixDofXComponent), "SetDoubleArg"),
                new("6DOF Y Component", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.SixDofYComponent), "SetDoubleArg"),
                new("6DPF Z Component", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.SixDofZComponent), "SetDoubleArg"),
                new("6DOF RX Component", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.SixDofRxComponent), "SetDoubleArg"),
                new("6DOF RY Component", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.SixDofRyComponent), "SetDoubleArg"),
                new("6DOF RZ Component", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.SixDofRzComponent), "SetDoubleArg"),
                new("Active Joint Component", WorkerMpValueKind.Text, new WorkerTextValue(ActiveJoint(activeJointComponent)), "SetStringArg"),
                new("Encoder Offset Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.EncoderOffsetValue), "SetDoubleArg"),
                new("Minimum Encoder Limit", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.MinimumEncoderLimit), "SetDoubleArg"),
                new("Maximum Encoder Limit", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.MaximumEncoderLimit), "SetDoubleArg"),
                new("Encoder Sense Negative", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.EncoderSenseNegative), "SetBoolArg"),
                new("Include Additional Encoder", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.IncludeAdditionalEncoder), "SetBoolArg"),
                new("Additional Encoder Index Offset", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.AdditionalEncoderIndexOffset), "SetIntegerArg"),
                new("Additional Encoder Sense Negative", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.AdditionalEncoderSenseNegative), "SetBoolArg"),
                new("Segment Origin Mass in Kg", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.SegmentOriginMassKg), "SetDoubleArg"),
                new("Segment CG Mass in Kg", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.SegmentCgMassKg), "SetDoubleArg"),
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
