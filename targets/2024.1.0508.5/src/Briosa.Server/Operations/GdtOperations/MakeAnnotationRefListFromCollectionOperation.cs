using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.GdtOperations;

internal static class MakeAnnotationRefListFromCollectionOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "gdt_operations.make_annotation_ref_list_from_collection",
        "Make Annotation Ref List from a Collection", "briosa.GdtOperations",
        "MakeAnnotationRefListFromCollection", "/briosa.GdtOperations/MakeAnnotationRefListFromCollection",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("annotations", "Resultant Annotation Reference List", WorkerMpValueKind.CollectionItemNameList)];
    public static WorkerMpCommand CreateCommand(Api.MakeAnnotationRefListFromCollectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Collection Name", WorkerMpValueKind.CollectionName,
                GdtCollectionNameMapper.Required(request.Collection), "SetCollectionNameArg")],
            [new("Resultant Annotation Reference List", WorkerMpValueKind.CollectionItemNameList,
                "GetCollectionObjectNameRefListArg")]);
    }
    public static Api.MakeAnnotationRefListFromCollectionResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.MakeAnnotationRefListFromCollectionResult { Execution = completed.Details };
        foreach (var item in completed.Execution.OutputValues[0].RequireValue<WorkerCollectionItemNameListValue>().Values)
            result.Annotations.Add(CollectionItemNameMapper.ToProtocol(item));
        return result;
    }
}
