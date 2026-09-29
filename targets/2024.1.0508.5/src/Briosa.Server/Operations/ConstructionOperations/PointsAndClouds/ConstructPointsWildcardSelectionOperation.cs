using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointsWildcardSelectionOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_points_wildcard_selection", "Construct Points WildCard Selection",
        "briosa.ConstructionOperations", "ConstructPointsWildcardSelection", "/briosa.ConstructionOperations/ConstructPointsWildcardSelection",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointsWildcardSelectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Groups to Select From", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.GroupsToSelectFrom, "groups_to_select_from"), "SetCollectionObjectNameRefListArg"),
            new("WildCard Selection Names", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.WildcardSelectionNames, "wildcard_selection_names"), "SetPointNameArg"),
            new("Group for New Points", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.GroupForNewPoints, "group_for_new_points", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
            new("Include prior complete name", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasIncludePriorCompleteName && request.IncludePriorCompleteName), "SetBoolArg")
        ], []);
    }

    public static Api.ConstructPointsWildcardSelectionResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
