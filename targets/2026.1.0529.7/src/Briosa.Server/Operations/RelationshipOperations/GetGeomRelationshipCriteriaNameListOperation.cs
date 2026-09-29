using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class GetGeomRelationshipCriteriaNameListOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.get_geom_relationship_criteria_name_list", "Get Geom Relationship Criteria Name List",
        "briosa.RelationshipOperations", "GetGeomRelationshipCriteriaNameList",
        "/briosa.RelationshipOperations/GetGeomRelationshipCriteriaNameList",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [new("criteria_name_list", "Criteria Name List", WorkerMpValueKind.StringList)];

    public static WorkerMpCommand CreateCommand(Api.GetGeomRelationshipCriteriaNameListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var relationshipName = request.RelationshipName ??
            throw new ArgumentException("Request field 'relationship_name' is required.", nameof(request));
        var workerRelationshipName = CollectionItemNameMapper.Required(relationshipName, "relationship_name");
        if (!relationshipName.HasItemType)
        {
            workerRelationshipName = workerRelationshipName with { ItemType = WorkerItemTypeValue.Relationship };
        }

        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionItemName, workerRelationshipName, "SetCollectionObjectNameArg2"),
                new("Include All Criteria?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.IncludeAllCriteria), "SetBoolArg")
            ],
            [new("Criteria Name List", WorkerMpValueKind.StringList, "GetStringRefListArg")]);
    }

    public static Api.GetGeomRelationshipCriteriaNameListResult CreateResult(SuccessfulOperationExecution completed) =>
        new()
        {
            CriteriaNameList = { completed.Execution.OutputValues[0].RequireValue<WorkerStringListValue>().Values },
            Execution = completed.Details
        };
}
