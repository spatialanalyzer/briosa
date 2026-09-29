using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class GetPointsToObjectsRelationshipStatisticsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.get_points_to_objects_relationship_statistics", "Get Points to Objects Relationship Statistics",
        "briosa.RelationshipOperations", "GetPointsToObjectsRelationshipStatistics",
        "/briosa.RelationshipOperations/GetPointsToObjectsRelationshipStatistics",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("absolute_max_deviation", "Absolute Max Deviation", WorkerMpValueKind.FloatingPoint),
        new("max_deviation", "Max Deviation", WorkerMpValueKind.FloatingPoint),
        new("min_deviation", "Min Deviation", WorkerMpValueKind.FloatingPoint),
        new("rms", "RMS", WorkerMpValueKind.FloatingPoint),
        new("candidate_point_count", "# of Candidate Points", WorkerMpValueKind.WholeNumber),
        new("sampled_point_count", "# of Points Sampled", WorkerMpValueKind.WholeNumber),
        new("rejected_point_count", "# of Points Rejected", WorkerMpValueKind.WholeNumber),
        new("used_point_count", "# of Points Used", WorkerMpValueKind.WholeNumber),
        new("out_of_tolerance_point_count", "# of Points Out of Tolerance", WorkerMpValueKind.WholeNumber)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetPointsToObjectsRelationshipStatisticsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var relationship = CollectionItemNameMapper.Required(
            request.RelationshipName, "relationship_name", WorkerItemTypeValue.Relationship);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Relationship Name", WorkerMpValueKind.CollectionItemName,
                relationship, "SetCollectionObjectNameArg2")],
            [
                new("Absolute Max Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Max Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Min Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("RMS", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("# of Candidate Points", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
                new("# of Points Sampled", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
                new("# of Points Rejected", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
                new("# of Points Used", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
                new("# of Points Out of Tolerance", WorkerMpValueKind.WholeNumber, "GetIntegerArg")
            ]);
    }

    public static Api.GetPointsToObjectsRelationshipStatisticsResult CreateResult(SuccessfulOperationExecution completed)
    {
        var outputs = completed.Execution.OutputValues;
        var result = new Api.GetPointsToObjectsRelationshipStatisticsResult { Execution = completed.Details };
        result.AbsoluteMaxDeviation = outputs[0].RequireValue<WorkerDoubleValue>().Value;
        result.MaxDeviation = outputs[1].RequireValue<WorkerDoubleValue>().Value;
        result.MinDeviation = outputs[2].RequireValue<WorkerDoubleValue>().Value;
        result.Rms = outputs[3].RequireValue<WorkerDoubleValue>().Value;
        result.CandidatePointCount = outputs[4].RequireValue<WorkerIntegerValue>().Value;
        result.SampledPointCount = outputs[5].RequireValue<WorkerIntegerValue>().Value;
        result.RejectedPointCount = outputs[6].RequireValue<WorkerIntegerValue>().Value;
        result.UsedPointCount = outputs[7].RequireValue<WorkerIntegerValue>().Value;
        result.OutOfTolerancePointCount = outputs[8].RequireValue<WorkerIntegerValue>().Value;
        return result;
    }
}
