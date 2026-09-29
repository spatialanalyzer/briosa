using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class SetRelationshipSubSamplingOptionsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.set_relationship_sub_sampling_options", "Set Relationship Sub Sampling Options",
        "briosa.RelationshipOperations", "SetRelationshipSubSamplingOptions",
        "/briosa.RelationshipOperations/SetRelationshipSubSamplingOptions",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetRelationshipSubSamplingOptionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2"),
                new("Use every i-th point", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasUseEveryIthPoint && request.UseEveryIthPoint), "SetBoolArg"),
                new("i value", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.HasIValue ? request.IValue : 20), "SetIntegerArg"),
                new("Use no more than n points", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasUseNoMoreThanNPoints ? request.UseNoMoreThanNPoints : true), "SetBoolArg"),
                new("n value", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.HasNValue ? request.NValue : 10000), "SetIntegerArg")
            ], []);
    }

    public static Api.SetRelationshipSubSamplingOptionsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
