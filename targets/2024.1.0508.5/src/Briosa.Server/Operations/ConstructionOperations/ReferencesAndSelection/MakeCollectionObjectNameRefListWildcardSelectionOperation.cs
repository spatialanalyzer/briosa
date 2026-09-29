using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class MakeCollectionObjectNameRefListWildcardSelectionOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.make_collection_object_name_ref_list_wildcard_selection", "Make a Collection Object Name Reference List- WildCard Selection",
        "briosa.ConstructionOperations", "MakeCollectionObjectNameRefListWildcardSelection", "/briosa.ConstructionOperations/MakeCollectionObjectNameRefListWildcardSelection",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("resultant_collection_object_name_ref_list", "Resultant Collection Object Name Reference List", WorkerMpValueKind.CollectionObjectNameList)];

    public static WorkerMpCommand CreateCommand(Api.MakeCollectionObjectNameRefListWildcardSelectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Collection Wildcard Criteria", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasCollectionWildcardCriteria ? request.CollectionWildcardCriteria : "*"), "SetStringArg"),
            new("Object Wildcard Criteria", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasObjectWildcardCriteria ? request.ObjectWildcardCriteria : "*"), "SetStringArg"),
            new("Object Type", WorkerMpValueKind.ObjectType, ObjectTypeMapper.Required(request.ObjectType), "SetObjectTypeArg")
        ], [new("Resultant Collection Object Name Reference List", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg")]);
    }

    public static Api.MakeCollectionObjectNameRefListWildcardSelectionResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.MakeCollectionObjectNameRefListWildcardSelectionResult { Execution = completed.Details };
        foreach (var value in completed.Execution.OutputValues[0].RequireValue<WorkerCollectionObjectNameListValue>().Values)
            result.ResultantCollectionObjectNameRefList.Add(CollectionObjectNameMapper.ToProtocol(value));
        return result;
    }
}
