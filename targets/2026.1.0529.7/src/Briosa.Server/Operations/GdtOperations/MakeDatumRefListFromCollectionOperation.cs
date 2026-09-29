using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.GdtOperations;

internal static class MakeDatumRefListFromCollectionOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "gdt_operations.make_datum_ref_list_from_collection",
        "Make a Datum Ref List from a Collection", "briosa.GdtOperations",
        "MakeDatumRefListFromCollection", "/briosa.GdtOperations/MakeDatumRefListFromCollection",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("datums", "Datum Ref List", WorkerMpValueKind.CollectionObjectNameList)];
    public static WorkerMpCommand CreateCommand(Api.MakeDatumRefListFromCollectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Collection Name", WorkerMpValueKind.CollectionName,
                GdtCollectionNameMapper.Required(request.Collection), "SetCollectionNameArg")],
            [new("Datum Ref List", WorkerMpValueKind.CollectionObjectNameList,
                "GetCollectionObjectNameRefListArg")]);
    }
    public static Api.MakeDatumRefListFromCollectionResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.MakeDatumRefListFromCollectionResult { Execution = completed.Details };
        foreach (var item in completed.Execution.OutputValues[0].RequireValue<WorkerCollectionObjectNameListValue>().Values)
            result.Datums.Add(CollectionObjectNameMapper.ToProtocol(item));
        return result;
    }
}
