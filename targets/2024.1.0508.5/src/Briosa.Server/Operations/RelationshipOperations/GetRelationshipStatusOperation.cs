using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class GetRelationshipStatusOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.get_relationship_status", "Get Relationship Status",
        "briosa.RelationshipOperations", "GetRelationshipStatus",
        "/briosa.RelationshipOperations/GetRelationshipStatus",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("dormant", "Dormant", WorkerMpValueKind.Logical),
        new("success", "Success", WorkerMpValueKind.Logical),
        new("measured", "Measured", WorkerMpValueKind.Logical),
        new("failed", "Failed", WorkerMpValueKind.Logical),
        new("unmeasured", "Unmeasured", WorkerMpValueKind.Logical)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetRelationshipStatusRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var relationship = CollectionItemNameMapper.Required(
            request.RelationshipName, "relationship_name", WorkerItemTypeValue.Relationship);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Relationship Name", WorkerMpValueKind.CollectionItemName,
                relationship, "SetCollectionObjectNameArg2")],
            [
                new("Dormant", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Success", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Measured", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Failed", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Unmeasured", WorkerMpValueKind.Logical, "GetBoolArg")
            ]);
    }

    public static Api.GetRelationshipStatusResult CreateResult(SuccessfulOperationExecution completed)
    {
        var outputs = completed.Execution.OutputValues;
        return new()
        {
            Status = new()
            {
                Dormant = outputs[0].RequireValue<WorkerBooleanValue>().Value,
                Success = outputs[1].RequireValue<WorkerBooleanValue>().Value,
                Measured = outputs[2].RequireValue<WorkerBooleanValue>().Value,
                Failed = outputs[3].RequireValue<WorkerBooleanValue>().Value,
                Unmeasured = outputs[4].RequireValue<WorkerBooleanValue>().Value
            },
            Execution = completed.Details
        };
    }
}
