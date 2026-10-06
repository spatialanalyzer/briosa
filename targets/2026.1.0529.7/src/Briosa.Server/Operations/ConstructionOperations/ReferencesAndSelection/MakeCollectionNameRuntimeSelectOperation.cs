using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class MakeCollectionNameRuntimeSelectOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.make_collection_name_runtime_select", "Make a Collection Name - Runtime Select",
        "briosa.ConstructionOperations", "MakeCollectionNameRuntimeSelect", "/briosa.ConstructionOperations/MakeCollectionNameRuntimeSelect",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("resultant_collection_name", "Resultant Collection Name", WorkerMpValueKind.CollectionName)];

    public static WorkerMpCommand CreateCommand(Api.MakeCollectionNameRuntimeSelectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("User Prompt", WorkerMpValueKind.Text, new WorkerTextValue(request.UserPrompt), "SetStringArg")],
            [new("Resultant Collection Name", WorkerMpValueKind.CollectionName, "GetCollectionNameArg")]);
    }

    public static Api.MakeCollectionNameRuntimeSelectResult CreateResult(SuccessfulOperationExecution completed) =>
        new()
        {
            ResultantCollectionName = new() { Name = completed.Execution.OutputValues[0].RequireValue<WorkerTextValue>().Value },
            Execution = completed.Details
        };
}
