using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class MakeObjectToObjectDirectionRelationshipOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.make_object_to_object_direction_relationship", "Make Object to Object Direction Relationship",
        "briosa.RelationshipOperations", "MakeObjectToObjectDirectionRelationship",
        "/briosa.RelationshipOperations/MakeObjectToObjectDirectionRelationship",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MakeObjectToObjectDirectionRelationshipRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionItemName,
                    CollectionItemNameMapper.Required(request.RelationshipName, "relationship_name", WorkerItemTypeValue.Relationship), "SetCollectionObjectNameArg2"),
                new("First Object in Relationship", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.FirstObjectInRelationship, "first_object_in_relationship"), "SetCollectionObjectNameArg2"),
                new("Second Object in Relationship", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.SecondObjectInRelationship, "second_object_in_relationship"), "SetCollectionObjectNameArg2"),
                new("Nominal Angle", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasNominalAngle ? request.NominalAngle : 0d), "SetDoubleArg")
            ], []);
    }

    public static Api.MakeObjectToObjectDirectionRelationshipResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
