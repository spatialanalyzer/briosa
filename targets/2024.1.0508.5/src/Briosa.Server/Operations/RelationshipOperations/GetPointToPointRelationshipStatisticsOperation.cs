using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class GetPointToPointRelationshipStatisticsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.get_point_to_point_relationship_statistics", "Get Point to Point Relationship Statistics",
        "briosa.RelationshipOperations", "GetPointToPointRelationshipStatistics",
        "/briosa.RelationshipOperations/GetPointToPointRelationshipStatistics",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("delta_x", "Delta X", WorkerMpValueKind.FloatingPoint),
        new("delta_y", "Delta Y", WorkerMpValueKind.FloatingPoint),
        new("delta_z", "Delta Z", WorkerMpValueKind.FloatingPoint),
        new("delta_magnitude", "Delta Magnitude", WorkerMpValueKind.FloatingPoint),
        new("reference_frame", "Reference Frame", WorkerMpValueKind.CollectionObjectName)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetPointToPointRelationshipStatisticsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var relationship = CollectionItemNameMapper.Required(
            request.RelationshipName, "relationship_name", WorkerItemTypeValue.Relationship);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Relationship Name", WorkerMpValueKind.CollectionItemName,
                relationship, "SetCollectionObjectNameArg2")],
            [
                new("Delta X", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Delta Y", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Delta Z", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Delta Magnitude", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Reference Frame", WorkerMpValueKind.CollectionObjectName, "GetCollectionObjectNameArg")
            ]);
    }

    public static Api.GetPointToPointRelationshipStatisticsResult CreateResult(SuccessfulOperationExecution completed)
    {
        var outputs = completed.Execution.OutputValues;
        var result = new Api.GetPointToPointRelationshipStatisticsResult { Execution = completed.Details };
        result.DeltaX = outputs[0].RequireValue<WorkerDoubleValue>().Value;
        result.DeltaY = outputs[1].RequireValue<WorkerDoubleValue>().Value;
        result.DeltaZ = outputs[2].RequireValue<WorkerDoubleValue>().Value;
        result.DeltaMagnitude = outputs[3].RequireValue<WorkerDoubleValue>().Value;
        result.ReferenceFrame = CollectionObjectNameMapper.ToProtocol(
            outputs[4].RequireValue<WorkerCollectionObjectNameValue>());
        return result;
    }
}
