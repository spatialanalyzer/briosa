using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class SetObjectToObjectDirectionRelationshipTolerancesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.set_object_to_object_direction_relationship_tolerances",
        "Set Object to Object Direction Relationship Tolerances",
        "briosa.RelationshipOperations", "SetObjectToObjectDirectionRelationshipTolerances",
        "/briosa.RelationshipOperations/SetObjectToObjectDirectionRelationshipTolerances",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetObjectToObjectDirectionRelationshipTolerancesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionItemName,
                    CollectionItemNameMapper.Required(request.RelationshipName, "relationship_name", WorkerItemTypeValue.Relationship), "SetCollectionObjectNameArg2"),
                new("Angle Between Vectors Tolerances", WorkerMpValueKind.ToleranceScalarOptions,
                    ToleranceScalarOptionsMapper.ToWorker(request.AngleBetweenVectorsTolerances ??
                        throw new ArgumentException("Request field 'angle_between_vectors_tolerances' is required.", nameof(request))), "SetToleranceScalarOptionsArg"),
                new("Mutual Perpendicular Length Tolerances", WorkerMpValueKind.ToleranceScalarOptions,
                    ToleranceScalarOptionsMapper.ToWorker(request.MutualPerpendicularLengthTolerances ??
                        throw new ArgumentException("Request field 'mutual_perpendicular_length_tolerances' is required.", nameof(request))), "SetToleranceScalarOptionsArg")
            ], []);
    }

    public static Api.SetObjectToObjectDirectionRelationshipTolerancesResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
