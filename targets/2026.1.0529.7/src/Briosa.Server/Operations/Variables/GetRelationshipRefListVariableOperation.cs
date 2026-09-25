using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Variables;

internal static class GetRelationshipRefListVariableOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "variables.get_relationship_ref_list_variable", "Get Relationship Ref List Variable",
        "briosa.Variables", "GetRelationshipRefListVariable", "/briosa.Variables/GetRelationshipRefListVariable",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("value", "Value", WorkerMpValueKind.CollectionItemNameList)];

    public static WorkerMpCommand CreateCommand(Api.GetRelationshipRefListVariableRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Name", WorkerMpValueKind.Text, new WorkerTextValue(request.Name), "SetStringArg")],
            [new("Value", WorkerMpValueKind.CollectionItemNameList, "GetCollectionObjectNameRefListArg")]);
    }

    public static Api.GetRelationshipRefListVariableResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.GetRelationshipRefListVariableResult { Execution = completed.Details };
        foreach (var value in completed.Execution.OutputValues[0].RequireValue<WorkerCollectionItemNameListValue>().Values)
        {
            result.Value.Add(CollectionItemNameMapper.ToProtocol(value));
        }
        return result;
    }
}
