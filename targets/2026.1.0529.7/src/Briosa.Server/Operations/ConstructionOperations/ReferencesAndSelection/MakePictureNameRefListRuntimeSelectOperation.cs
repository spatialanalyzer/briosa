using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class MakePictureNameRefListRuntimeSelectOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.make_picture_name_ref_list_runtime_select", "Make a Picture Name Ref List - Runtime Select",
        "briosa.ConstructionOperations", "MakePictureNameRefListRuntimeSelect",
        "/briosa.ConstructionOperations/MakePictureNameRefListRuntimeSelect", "read_only",
        Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("picture_name_list", "Picture Name List", WorkerMpValueKind.CollectionItemNameList)];

    public static WorkerMpCommand CreateCommand(Api.MakePictureNameRefListRuntimeSelectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("User Prompt", WorkerMpValueKind.Text, new WorkerTextValue(request.UserPrompt), "SetStringArg")],
            [new("Picture Name List", WorkerMpValueKind.CollectionItemNameList, "GetCollectionObjectNameRefListArg")]);
    }

    public static Api.MakePictureNameRefListRuntimeSelectResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.MakePictureNameRefListRuntimeSelectResult { Execution = completed.Details };
        foreach (var value in completed.Execution.OutputValues[0].RequireValue<WorkerCollectionItemNameListValue>().Values)
            result.PictureNameList.Add(CollectionItemNameMapper.ToProtocol(value));
        return result;
    }
}
