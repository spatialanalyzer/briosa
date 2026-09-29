using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class GetRelationshipSubSamplingOptionsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.get_relationship_sub_sampling_options", "Get Relationship Sub Sampling Options",
        "briosa.RelationshipOperations", "GetRelationshipSubSamplingOptions",
        "/briosa.RelationshipOperations/GetRelationshipSubSamplingOptions",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("use_every_ith_point", "Use every i-th point", WorkerMpValueKind.Logical),
        new("i_value", "i value", WorkerMpValueKind.WholeNumber),
        new("use_no_more_than_n_points", "Use no more than n points", WorkerMpValueKind.Logical),
        new("n_value", "n value", WorkerMpValueKind.WholeNumber)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetRelationshipSubSamplingOptionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2")],
            [
                new("Use every i-th point", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("i value", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
                new("Use no more than n points", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("n value", WorkerMpValueKind.WholeNumber, "GetIntegerArg")
            ]);
    }

    public static Api.GetRelationshipSubSamplingOptionsResult CreateResult(SuccessfulOperationExecution completed)
    {
        var outputs = completed.Execution.OutputValues;
        return new()
        {
            UseEveryIthPoint = outputs[0].RequireValue<WorkerBooleanValue>().Value,
            IValue = outputs[1].RequireValue<WorkerIntegerValue>().Value,
            UseNoMoreThanNPoints = outputs[2].RequireValue<WorkerBooleanValue>().Value,
            NValue = outputs[3].RequireValue<WorkerIntegerValue>().Value,
            Execution = completed.Details
        };
    }
}
