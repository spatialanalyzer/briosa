using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class DeletePointsWildcardSelectionOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.delete_points_wildcard_selection", "Delete Points WildCard Selection",
        "briosa.ConstructionOperations", "DeletePointsWildcardSelection", "/briosa.ConstructionOperations/DeletePointsWildcardSelection",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["destructive"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.DeletePointsWildcardSelectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Groups to Delete From", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.GroupsToDeleteFrom, "groups_to_delete_from"), "SetCollectionObjectNameRefListArg"),
            new("WildCard Selection Names", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.WildcardSelectionNames, "wildcard_selection_names"), "SetPointNameArg")
        ], []);
    }

    public static Api.DeletePointsWildcardSelectionResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
