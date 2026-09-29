using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class EnableDisableRelationshipsForOptimizationOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.enable_disable_relationships_for_optimization",
        "Enable/Disable Relationships for Optimization",
        "briosa.RelationshipOperations", "EnableDisableRelationshipsForOptimization",
        "/briosa.RelationshipOperations/EnableDisableRelationshipsForOptimization",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.EnableDisableRelationshipsForOptimizationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationships", WorkerMpValueKind.CollectionItemNameList,
                    CollectionItemNameMapper.RequiredList(request.Relationships, "relationships"), "SetCollectionObjectNameRefListArg"),
                new("Enable?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.Enable), "SetBoolArg")
            ], []);
    }

    public static Api.EnableDisableRelationshipsForOptimizationResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}