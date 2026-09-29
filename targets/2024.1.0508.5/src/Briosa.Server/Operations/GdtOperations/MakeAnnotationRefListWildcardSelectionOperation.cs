using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.GdtOperations;

internal static class MakeAnnotationRefListWildcardSelectionOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "gdt_operations.make_annotation_ref_list_wildcard_selection",
        "Make Annotation Ref List- WildCard Selection", "briosa.GdtOperations",
        "MakeAnnotationRefListWildcardSelection", "/briosa.GdtOperations/MakeAnnotationRefListWildcardSelection",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("annotations", "Resultant Annotation Reference List", WorkerMpValueKind.CollectionItemNameList)];
    public static WorkerMpCommand CreateCommand(Api.MakeAnnotationRefListWildcardSelectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Collection Wildcard Criteria", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasCollectionWildcardCriteria ? request.CollectionWildcardCriteria : "*"),
                "SetStringArg"),
            new("Annotation Wildcard Criteria", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasAnnotationWildcardCriteria ? request.AnnotationWildcardCriteria : "*"),
                "SetStringArg")
        ],
        [new("Resultant Annotation Reference List", WorkerMpValueKind.CollectionItemNameList,
            "GetCollectionObjectNameRefListArg")]);
    }
    public static Api.MakeAnnotationRefListWildcardSelectionResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.MakeAnnotationRefListWildcardSelectionResult { Execution = completed.Details };
        foreach (var item in completed.Execution.OutputValues[0].RequireValue<WorkerCollectionItemNameListValue>().Values)
            result.Annotations.Add(CollectionItemNameMapper.ToProtocol(item));
        return result;
    }
}
