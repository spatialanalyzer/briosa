using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Variables;

internal static class GetCollectionObjectRefListVariableOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "variables.get_collection_object_ref_list_variable", "Get Collection Object Ref List Variable",
        "briosa.Variables", "GetCollectionObjectRefListVariable", "/briosa.Variables/GetCollectionObjectRefListVariable",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("value", "Value", WorkerMpValueKind.CollectionObjectNameList)];

    public static WorkerMpCommand CreateCommand(Api.GetCollectionObjectRefListVariableRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Name", WorkerMpValueKind.Text, new WorkerTextValue(request.Name), "SetStringArg")],
            [new("Value", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg")]);
    }

    public static Api.GetCollectionObjectRefListVariableResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.GetCollectionObjectRefListVariableResult { Execution = completed.Details };
        foreach (var value in completed.Execution.OutputValues[0].RequireValue<WorkerCollectionObjectNameListValue>().Values)
        {
            result.Value.Add(CollectionObjectNameMapper.ToProtocol(value));
        }
        return result;
    }
}
