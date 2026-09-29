using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class MakePipeRelationshipCutOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.make_pipe_relationship_cut", "Make pipe Relationship Cut",
        "briosa.RelationshipOperations", "MakePipeRelationshipCut",
        "/briosa.RelationshipOperations/MakePipeRelationshipCut",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MakePipeRelationshipCutRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2"),
                new("Pipe 1 - Make Cut", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasPipe1MakeCut || request.Pipe1MakeCut), "SetBoolArg"),
                new("Pipe 2 - Make Cut", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasPipe2MakeCut || request.Pipe2MakeCut), "SetBoolArg"),
                new("Pipe 1 - Create Frame", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasPipe1CreateFrame && request.Pipe1CreateFrame), "SetBoolArg"),
                new("Pipe 2 - Create Frame", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasPipe2CreateFrame && request.Pipe2CreateFrame), "SetBoolArg"),
                new("Pipe 1 - Frame Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.Pipe1FrameName, "pipe_1_frame_name"), "SetCollectionObjectNameArg2"),
                new("Pipe 2 - Frame Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.Pipe2FrameName, "pipe_2_frame_name"), "SetCollectionObjectNameArg2")
            ], []);
    }

    public static Api.MakePipeRelationshipCutResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
