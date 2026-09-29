using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructFrameAtRobotLinkOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_frame_at_robot_link", "Construct Frame at Robot Link",
        "briosa.ConstructionOperations", "ConstructFrameAtRobotLink", "/briosa.ConstructionOperations/ConstructFrameAtRobotLink",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructFrameAtRobotLinkRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.MachineId is null || string.IsNullOrWhiteSpace(request.MachineId.CollectionName))
            throw new ArgumentException("Request field 'machine_id' is required.", nameof(request));
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Machine ID", WorkerMpValueKind.CollectionMachineId,
                new WorkerCollectionMachineIdValue(request.MachineId.CollectionName, request.MachineId.MachineId),
                "SetColMachineIdArg"),
            new("Link Name", WorkerMpValueKind.Text, new WorkerTextValue(request.LinkName), "SetStringArg"),
            new("Resulting Frame", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ResultingFrame, "resulting_frame", WorkerObjectTypeValue.Frame),
                "SetCollectionObjectNameArg2")
        ], []);
    }

    public static Api.ConstructFrameAtRobotLinkResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
