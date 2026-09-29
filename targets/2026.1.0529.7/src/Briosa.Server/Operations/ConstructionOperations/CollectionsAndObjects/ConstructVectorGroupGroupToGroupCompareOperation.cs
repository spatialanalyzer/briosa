using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructVectorGroupGroupToGroupCompareOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_vector_group_group_to_group_compare", "Construct a Vector Group - Group to Group Compare",
        "briosa.ConstructionOperations", "ConstructVectorGroupGroupToGroupCompare",
        "/briosa.ConstructionOperations/ConstructVectorGroupGroupToGroupCompare", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("vector_count", "Vector Count", WorkerMpValueKind.WholeNumber),
        new("rms_deviation", "RMS Deviation", WorkerMpValueKind.FloatingPoint),
        new("max_absolute_deviation", "Max Absolute Deviation", WorkerMpValueKind.FloatingPoint),
        new("average_deviation", "Average Deviation", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.ConstructVectorGroupGroupToGroupCompareRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Vector Group Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.VectorGroupName, "vector_group_name", WorkerObjectTypeValue.VectorGroup), "SetCollectionObjectNameArg2"),
            new("Group A", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.GroupA, "group_a", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
            new("Group B", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.GroupB, "group_b", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
            new("RMS Deviation Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.RmsDeviationTolerance), "SetDoubleArg"),
            new("Max Absolute Deviation Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.MaxAbsoluteDeviationTolerance), "SetDoubleArg"),
            new("Average Deviation Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.AverageDeviationTolerance), "SetDoubleArg")
        ],
        [
            new("Vector Count", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
            new("RMS Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Max Absolute Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Average Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
        ]);
    }

    public static Api.ConstructVectorGroupGroupToGroupCompareResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        VectorCount = completed.Execution.OutputValues[0].RequireValue<WorkerIntegerValue>().Value,
        RmsDeviation = completed.Execution.OutputValues[1].RequireValue<WorkerDoubleValue>().Value,
        MaxAbsoluteDeviation = completed.Execution.OutputValues[2].RequireValue<WorkerDoubleValue>().Value,
        AverageDeviation = completed.Execution.OutputValues[3].RequireValue<WorkerDoubleValue>().Value,
        Execution = completed.Details
    };
}
