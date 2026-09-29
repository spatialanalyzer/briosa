using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class SetRelationshipDormantStatusOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.set_relationship_dormant_status", "Set Relationship Dormant Status",
        "briosa.RelationshipOperations", "SetRelationshipDormantStatus",
        "/briosa.RelationshipOperations/SetRelationshipDormantStatus",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetRelationshipDormantStatusRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationships", WorkerMpValueKind.CollectionItemNameList,
                    CollectionItemNameMapper.RequiredList(request.Relationships, "relationships"), "SetCollectionObjectNameRefListArg"),
                new("Dormant Status", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasDormantStatus && request.DormantStatus), "SetBoolArg")
            ], []);
    }

    public static Api.SetRelationshipDormantStatusResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
