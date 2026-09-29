using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Variables;

internal static class SetCollectionObjectRefListVariableOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "variables.set_collection_object_ref_list_variable", "Set Collection Object Ref List Variable",
        "briosa.Variables", "SetCollectionObjectRefListVariable", "/briosa.Variables/SetCollectionObjectRefListVariable",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetCollectionObjectRefListVariableRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Name", WorkerMpValueKind.Text, new WorkerTextValue(request.Name), "SetStringArg"),
             new("Value", WorkerMpValueKind.CollectionObjectNameList, CollectionObjectNameMapper.RequiredList(request.Value, "value"), "SetCollectionObjectNameRefListArg")], []);
    }

    public static Api.SetCollectionObjectRefListVariableResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
