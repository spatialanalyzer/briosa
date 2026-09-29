using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class SetRelationshipProjectionOptionsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.set_relationship_projection_options", "Set Relationship Projection Options",
        "briosa.RelationshipOperations", "SetRelationshipProjectionOptions",
        "/briosa.RelationshipOperations/SetRelationshipProjectionOptions",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetRelationshipProjectionOptionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2"),
                new("Projection Options", WorkerMpValueKind.ProjectionOptions,
                    ProjectionOptionsMapper.FromRequest(request.ProjectionOptions), "SetProjectionOptionsArg")
            ], []);
    }

    public static Api.SetRelationshipProjectionOptionsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
