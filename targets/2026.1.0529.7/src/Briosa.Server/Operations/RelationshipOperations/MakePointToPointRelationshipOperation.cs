using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class MakePointToPointRelationshipOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.make_point_to_point_relationship", "Make Point to Point Relationship",
        "briosa.RelationshipOperations", "MakePointToPointRelationship",
        "/briosa.RelationshipOperations/MakePointToPointRelationship",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MakePointToPointRelationshipRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var relationship = CollectionItemNameMapper.Required(
            request.RelationshipName, "relationship_name", WorkerItemTypeValue.Relationship);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionItemName,
                    relationship, "SetCollectionObjectNameArg2"),
                new("First Point Name", WorkerMpValueKind.PointName,
                    PointNameMapper.Required(request.FirstPointName, "first_point_name"), "SetPointNameArg"),
                new("Second Point Name", WorkerMpValueKind.PointName,
                    PointNameMapper.Required(request.SecondPointName, "second_point_name"), "SetPointNameArg"),
                new("Tolerance", WorkerMpValueKind.ToleranceVectorOptions,
                    ToleranceVectorOptionsMapper.Required(request.Tolerance, "tolerance"), "SetToleranceVectorOptionsArg"),
                new("Constraint", WorkerMpValueKind.ToleranceVectorOptions,
                    ToleranceVectorOptionsMapper.Required(request.Constraint, "constraint"), "SetToleranceVectorOptionsArg")
            ], []);
    }

    public static Api.MakePointToPointRelationshipResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
