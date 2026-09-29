using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class MakeEventRefListWildcardSelectionOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.make_event_ref_list_wildcard_selection",
        "Make an Event Reference List- WildCard Selection", "briosa.ConstructionOperations",
        "MakeEventRefListWildcardSelection", "/briosa.ConstructionOperations/MakeEventRefListWildcardSelection",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("resultant_event_ref_list", "Resultant Event Reference List", WorkerMpValueKind.CollectionItemNameList)];

    public static WorkerMpCommand CreateCommand(Api.MakeEventRefListWildcardSelectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Collection Wildcard Criteria", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasCollectionWildcardCriteria ? request.CollectionWildcardCriteria : "*"), "SetStringArg"),
            new("Event Wildcard Criteria", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasEventWildcardCriteria ? request.EventWildcardCriteria : "*"), "SetStringArg")
        ], [new("Resultant Event Reference List", WorkerMpValueKind.CollectionItemNameList,
            "GetCollectionObjectNameRefListArg")]);
    }

    public static Api.MakeEventRefListWildcardSelectionResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.MakeEventRefListWildcardSelectionResult { Execution = completed.Details };
        foreach (var value in completed.Execution.OutputValues[0].RequireValue<WorkerCollectionItemNameListValue>().Values)
            result.ResultantEventRefList.Add(CollectionItemNameMapper.ToProtocol(value));
        return result;
    }
}
