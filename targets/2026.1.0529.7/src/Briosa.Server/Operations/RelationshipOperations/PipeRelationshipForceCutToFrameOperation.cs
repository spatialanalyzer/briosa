using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class PipeRelationshipForceCutToFrameOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.pipe_relationship_force_cut_to_frame", "Pipe Relationship Force Cut to Frame",
        "briosa.RelationshipOperations", "PipeRelationshipForceCutToFrame",
        "/briosa.RelationshipOperations/PipeRelationshipForceCutToFrame",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.PipeRelationshipForceCutToFrameRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2"),
                new("Pipe 1 - Force Cut to Frame?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasPipe1ForceCutToFrame || request.Pipe1ForceCutToFrame), "SetBoolArg"),
                new("Pipe 1 - Frame Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.Pipe1FrameName, "pipe_1_frame_name"), "SetCollectionObjectNameArg2"),
                new("Pipe 2 - Force Cut to Frame?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasPipe2ForceCutToFrame || request.Pipe2ForceCutToFrame), "SetBoolArg"),
                new("Pipe 2 - Frame Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.Pipe2FrameName, "pipe_2_frame_name"), "SetCollectionObjectNameArg2")
            ], []);
    }

    public static Api.PipeRelationshipForceCutToFrameResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
