using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class MakeCollectionObjectNameRuntimeSelectOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.make_collection_object_name_runtime_select", "Make a Collection Object Name - Runtime Select",
        "briosa.ConstructionOperations", "MakeCollectionObjectNameRuntimeSelect", "/briosa.ConstructionOperations/MakeCollectionObjectNameRuntimeSelect",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("resultant_collection_object_name", "Resultant Collection Object Name", WorkerMpValueKind.CollectionObjectName)];

    public static WorkerMpCommand CreateCommand(Api.MakeCollectionObjectNameRuntimeSelectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("User Prompt", WorkerMpValueKind.Text, new WorkerTextValue(request.UserPrompt), "SetStringArg"),
            new("Object Type", WorkerMpValueKind.ObjectType, ObjectTypeMapper.Required(request.ObjectType), "SetObjectTypeArg")
        ], [new("Resultant Collection Object Name", WorkerMpValueKind.CollectionObjectName, "GetCollectionObjectNameArg")]);
    }

    public static Api.MakeCollectionObjectNameRuntimeSelectResult CreateResult(SuccessfulOperationExecution completed) =>
        new()
        {
            ResultantCollectionObjectName = CollectionObjectNameMapper.ToProtocol(completed.Execution.OutputValues[0].RequireValue<WorkerCollectionObjectNameValue>()),
            Execution = completed.Details
        };
}
