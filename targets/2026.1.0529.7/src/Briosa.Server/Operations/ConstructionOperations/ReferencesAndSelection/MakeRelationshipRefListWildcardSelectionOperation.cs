using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class MakeRelationshipRefListWildcardSelectionOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.make_relationship_ref_list_wildcard_selection",
        "Make a Relationship Reference List- WildCard Selection", "briosa.ConstructionOperations",
        "MakeRelationshipRefListWildcardSelection", "/briosa.ConstructionOperations/MakeRelationshipRefListWildcardSelection",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("resultant_relationship_ref_list", "Resultant Relationship Reference List", WorkerMpValueKind.CollectionItemNameList)];

    public static WorkerMpCommand CreateCommand(Api.MakeRelationshipRefListWildcardSelectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Collection Wildcard Criteria", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasCollectionWildcardCriteria ? request.CollectionWildcardCriteria : "*"), "SetStringArg"),
            new("Relationship Wildcard Criteria", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasRelationshipWildcardCriteria ? request.RelationshipWildcardCriteria : "*"), "SetStringArg")
        ], [new("Resultant Relationship Reference List", WorkerMpValueKind.CollectionItemNameList,
            "GetCollectionObjectNameRefListArg")]);
    }

    public static Api.MakeRelationshipRefListWildcardSelectionResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.MakeRelationshipRefListWildcardSelectionResult { Execution = completed.Details };
        foreach (var value in completed.Execution.OutputValues[0].RequireValue<WorkerCollectionItemNameListValue>().Values)
            result.ResultantRelationshipRefList.Add(CollectionItemNameMapper.ToProtocol(value));
        return result;
    }
}
