using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Variables;

internal static class SetRelationshipRefListVariableOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "variables.set_relationship_ref_list_variable", "Set Relationship Ref List Variable",
        "briosa.Variables", "SetRelationshipRefListVariable", "/briosa.Variables/SetRelationshipRefListVariable",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetRelationshipRefListVariableRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Name", WorkerMpValueKind.Text, new WorkerTextValue(request.Name), "SetStringArg"),
             new("Value", WorkerMpValueKind.CollectionItemNameList, CollectionItemNameMapper.RequiredList(request.Value, "value"), "SetCollectionObjectNameRefListArg")], []);
    }

    public static Api.SetRelationshipRefListVariableResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
