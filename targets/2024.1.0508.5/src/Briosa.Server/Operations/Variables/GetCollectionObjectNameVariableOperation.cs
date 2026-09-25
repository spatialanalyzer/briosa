using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Variables;

internal static class GetCollectionObjectNameVariableOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "variables.get_collection_object_name_variable", "Get Collection Object Name Variable",
        "briosa.Variables", "GetCollectionObjectNameVariable", "/briosa.Variables/GetCollectionObjectNameVariable",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("value", "Value", WorkerMpValueKind.CollectionObjectName)];

    public static WorkerMpCommand CreateCommand(Api.GetCollectionObjectNameVariableRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Name", WorkerMpValueKind.Text, new WorkerTextValue(request.Name), "SetStringArg")],
            [new("Value", WorkerMpValueKind.CollectionObjectName, "GetCollectionObjectNameArg")]);
    }

    public static Api.GetCollectionObjectNameVariableResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details,
        Value = CollectionObjectNameMapper.ToProtocol(completed.Execution.OutputValues[0].RequireValue<WorkerCollectionObjectNameValue>())
    };
}
