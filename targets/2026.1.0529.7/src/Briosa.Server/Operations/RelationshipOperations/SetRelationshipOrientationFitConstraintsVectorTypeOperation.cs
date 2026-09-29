using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class SetRelationshipOrientationFitConstraintsVectorTypeOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.set_relationship_orientation_fit_constraints_vector_type",
        "Set Relationship Orientation Fit Constraints (Vector Type)",
        "briosa.RelationshipOperations", "SetRelationshipOrientationFitConstraintsVectorType",
        "/briosa.RelationshipOperations/SetRelationshipOrientationFitConstraintsVectorType",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetRelationshipOrientationFitConstraintsVectorTypeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2"),
                new("Orientation Vector Constraint", WorkerMpValueKind.ToleranceVectorOptions,
                    ToleranceVectorOptionsMapper.Required(request.OrientationVectorConstraint, "orientation_vector_constraint"),
                    "SetToleranceVectorOptionsArg")
            ], []);
    }

    public static Api.SetRelationshipOrientationFitConstraintsVectorTypeResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
