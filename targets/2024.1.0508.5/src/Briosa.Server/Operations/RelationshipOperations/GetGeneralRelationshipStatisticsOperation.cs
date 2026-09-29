using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class GetGeneralRelationshipStatisticsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.get_general_relationship_statistics", "Get General Relationship Statistics",
        "briosa.RelationshipOperations", "GetGeneralRelationshipStatistics",
        "/briosa.RelationshipOperations/GetGeneralRelationshipStatistics",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("max_deviation", "Max Deviation", WorkerMpValueKind.FloatingPoint),
        new("rms", "RMS", WorkerMpValueKind.FloatingPoint),
        new("has_signed_deviation", "Has Signed Deviation?", WorkerMpValueKind.Logical),
        new("signed_max_deviation", "Signed Max Deviation", WorkerMpValueKind.FloatingPoint),
        new("signed_min_deviation", "Signed Min Deviation", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetGeneralRelationshipStatisticsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var relationship = CollectionItemNameMapper.Required(
            request.RelationshipName, "relationship_name", WorkerItemTypeValue.Relationship);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Relationship Name", WorkerMpValueKind.CollectionItemName,
                relationship, "SetCollectionObjectNameArg2")],
            [
                new("Max Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("RMS", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Has Signed Deviation?", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Signed Max Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Signed Min Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
            ]);
    }

    public static Api.GetGeneralRelationshipStatisticsResult CreateResult(SuccessfulOperationExecution completed)
    {
        var outputs = completed.Execution.OutputValues;
        var result = new Api.GetGeneralRelationshipStatisticsResult { Execution = completed.Details };
        result.MaxDeviation = outputs[0].RequireValue<WorkerDoubleValue>().Value;
        result.Rms = outputs[1].RequireValue<WorkerDoubleValue>().Value;
        result.HasSignedDeviation = outputs[2].RequireValue<WorkerBooleanValue>().Value;
        result.SignedMaxDeviation = outputs[3].RequireValue<WorkerDoubleValue>().Value;
        result.SignedMinDeviation = outputs[4].RequireValue<WorkerDoubleValue>().Value;
        return result;
    }
}
