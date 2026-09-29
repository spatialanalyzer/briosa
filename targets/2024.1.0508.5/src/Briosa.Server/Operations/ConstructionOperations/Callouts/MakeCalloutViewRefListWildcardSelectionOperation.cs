using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class MakeCalloutViewRefListWildcardSelectionOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.make_callout_view_ref_list_wildcard_selection",
        "Make a Callout View Ref List - WildCard Selection", "briosa.ConstructionOperations",
        "MakeCalloutViewRefListWildcardSelection", "/briosa.ConstructionOperations/MakeCalloutViewRefListWildcardSelection",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("callout_views", "Resultant Callout View List", WorkerMpValueKind.CollectionItemNameList)];

    public static WorkerMpCommand CreateCommand(Api.MakeCalloutViewRefListWildcardSelectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Collection Wildcard Criteria", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasCollectionWildcardCriteria ? request.CollectionWildcardCriteria : "*"), "SetStringArg"),
            new("Callout View Wildcard Criteria", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasCalloutViewWildcardCriteria ? request.CalloutViewWildcardCriteria : "*"), "SetStringArg")
        ], [new("Resultant Callout View List", WorkerMpValueKind.CollectionItemNameList,
            "GetCollectionObjectNameRefListArg")]);
    }

    public static Api.MakeCalloutViewRefListWildcardSelectionResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.MakeCalloutViewRefListWildcardSelectionResult { Execution = completed.Details };
        foreach (var value in completed.Execution.OutputValues[0].RequireValue<WorkerCollectionItemNameListValue>().Values)
            result.CalloutViews.Add(CollectionItemNameMapper.ToProtocol(value));
        return result;
    }
}
