using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.GdtOperations;

internal static class MakeFeatureCheckReferenceListWildcardSelectionOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "gdt_operations.make_feature_check_reference_list_wildcard_selection",
        "Make a Feature Check Reference List- WildCard Selection", "briosa.GdtOperations",
        "MakeFeatureCheckReferenceListWildcardSelection",
        "/briosa.GdtOperations/MakeFeatureCheckReferenceListWildcardSelection",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("feature_checks", "Resultant Feature Check Reference List", WorkerMpValueKind.CollectionItemNameList)];
    public static WorkerMpCommand CreateCommand(Api.MakeFeatureCheckReferenceListWildcardSelectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Collection Wildcard Criteria", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasCollectionWildcardCriteria ? request.CollectionWildcardCriteria : "*"),
                "SetStringArg"),
            new("Feature Check Wildcard Criteria", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasFeatureCheckWildcardCriteria ? request.FeatureCheckWildcardCriteria : "*"),
                "SetStringArg")
        ],
        [new("Resultant Feature Check Reference List", WorkerMpValueKind.CollectionItemNameList,
            "GetCollectionObjectNameRefListArg")]);
    }
    public static Api.MakeFeatureCheckReferenceListWildcardSelectionResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.MakeFeatureCheckReferenceListWildcardSelectionResult { Execution = completed.Details };
        foreach (var item in completed.Execution.OutputValues[0].RequireValue<WorkerCollectionItemNameListValue>().Values)
            result.FeatureChecks.Add(CollectionItemNameMapper.ToProtocol(item));
        return result;
    }
}
