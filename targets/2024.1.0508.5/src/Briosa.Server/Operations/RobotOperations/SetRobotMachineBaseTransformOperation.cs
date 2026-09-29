using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotOperations;

internal static class SetRobotMachineBaseTransformOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "robot_operations.set_robot_machine_base_transform", "Set Robot/Machine Base Transform",
        "briosa.RobotOperations", "SetRobotMachineBaseTransform", "/briosa.RobotOperations/SetRobotMachineBaseTransform",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetRobotMachineBaseTransformRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.MachineId is null || string.IsNullOrWhiteSpace(request.MachineId.CollectionName))
            throw new ArgumentException("Machine ID is required.", nameof(request));
        if (request.ReferenceFrame is null || string.IsNullOrWhiteSpace(request.ReferenceFrame.ObjectName))
            throw new ArgumentException("Reference Frame is required.", nameof(request));
        if (!Enum.IsDefined(request.ReferenceFrame.ObjectType))
            throw new ArgumentException("Reference Frame type is not supported by this SA target.", nameof(request));

        var objectType = request.ReferenceFrame.ObjectType == Api.ObjectType.Unspecified
            ? WorkerObjectTypeValue.Frame
            : (WorkerObjectTypeValue)request.ReferenceFrame.ObjectType;

        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Machine ID", WorkerMpValueKind.CollectionMachineId,
                    new WorkerCollectionMachineIdValue(request.MachineId.CollectionName, request.MachineId.MachineId),
                    "SetColMachineIdArg"),
                new("Destination Transform", WorkerMpValueKind.Transform,
                    TransformMapper.Required(request.DestinationTransform, "destination_transform"), "SetTransformArg"),
                new("Reference Frame", WorkerMpValueKind.CollectionObjectName,
                    new WorkerCollectionObjectNameValue(request.ReferenceFrame.CollectionName,
                        request.ReferenceFrame.ObjectName, objectType), "SetCollectionObjectNameArg2"),
                new("Number of Steps", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.NumberOfSteps), "SetIntegerArg")
            ], []);
    }

    public static Api.SetRobotMachineBaseTransformResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
