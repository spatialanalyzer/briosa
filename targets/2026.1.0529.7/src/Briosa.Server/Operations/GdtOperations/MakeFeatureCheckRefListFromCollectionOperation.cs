using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.GdtOperations;

internal static class MakeFeatureCheckRefListFromCollectionOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "gdt_operations.make_feature_check_ref_list_from_collection",
        "Make a Feature Check Ref List from a Collection", "briosa.GdtOperations",
        "MakeFeatureCheckRefListFromCollection", "/briosa.GdtOperations/MakeFeatureCheckRefListFromCollection",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("feature_checks", "Feature Check Ref List", WorkerMpValueKind.CollectionItemNameList)];
    public static WorkerMpCommand CreateCommand(Api.MakeFeatureCheckRefListFromCollectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Collection Name", WorkerMpValueKind.CollectionName,
                GdtCollectionNameMapper.Required(request.Collection), "SetCollectionNameArg")],
            [new("Feature Check Ref List", WorkerMpValueKind.CollectionItemNameList,
                "GetCollectionObjectNameRefListArg")]);
    }
    public static Api.MakeFeatureCheckRefListFromCollectionResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.MakeFeatureCheckRefListFromCollectionResult { Execution = completed.Details };
        foreach (var item in completed.Execution.OutputValues[0].RequireValue<WorkerCollectionItemNameListValue>().Values)
            result.FeatureChecks.Add(CollectionItemNameMapper.ToProtocol(item));
        return result;
    }
}
