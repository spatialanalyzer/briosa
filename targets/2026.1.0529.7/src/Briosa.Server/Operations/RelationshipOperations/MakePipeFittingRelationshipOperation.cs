using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class MakePipeFittingRelationshipOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.make_pipe_fitting_relationship", "Make Pipe Fitting Relationship",
        "briosa.RelationshipOperations", "MakePipeFittingRelationship",
        "/briosa.RelationshipOperations/MakePipeFittingRelationship",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MakePipeFittingRelationshipRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2"),
                new("Pipe 1 - Object Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.Pipe1ObjectName, "pipe_1_object_name"), "SetCollectionObjectNameArg2"),
                new("Pipe 2 - Object Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.Pipe2ObjectName, "pipe_2_object_name"), "SetCollectionObjectNameArg2")
            ], []);
    }

    public static Api.MakePipeFittingRelationshipResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
