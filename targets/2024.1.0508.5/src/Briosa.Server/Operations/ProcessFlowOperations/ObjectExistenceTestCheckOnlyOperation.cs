using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ProcessFlowOperations;

internal static class ObjectExistenceTestCheckOnlyOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "process_flow_operations.object_existence_test_check_only", "Object Existence Test (Check Only)",
        "briosa.ProcessFlowOperations", "ObjectExistenceTestCheckOnly",
        "/briosa.ProcessFlowOperations/ObjectExistenceTestCheckOnly",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("exists", "Exists?", WorkerMpValueKind.Logical)];

    public static WorkerMpCommand CreateCommand(Api.ObjectExistenceTestCheckOnlyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Object Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ObjectName, "object_name"), "SetCollectionObjectNameArg2")
        ], [new("Exists?", WorkerMpValueKind.Logical, "GetBoolArg")]);
    }

    public static Api.ObjectExistenceTestCheckOnlyResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Exists = completed.Execution.OutputValues[0].RequireValue<WorkerBooleanValue>().Value,
            Execution = completed.Details };
}
