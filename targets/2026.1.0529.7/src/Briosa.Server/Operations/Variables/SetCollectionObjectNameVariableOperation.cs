using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Variables;

internal static class SetCollectionObjectNameVariableOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "variables.set_collection_object_name_variable", "Set Collection Object Name Variable",
        "briosa.Variables", "SetCollectionObjectNameVariable", "/briosa.Variables/SetCollectionObjectNameVariable",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetCollectionObjectNameVariableRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Name", WorkerMpValueKind.Text, new WorkerTextValue(request.Name), "SetStringArg"),
             new("Value", WorkerMpValueKind.CollectionObjectName, CollectionObjectNameMapper.Required(request.Value, "value"), "SetCollectionObjectNameArg2")], []);
    }

    public static Api.SetCollectionObjectNameVariableResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
