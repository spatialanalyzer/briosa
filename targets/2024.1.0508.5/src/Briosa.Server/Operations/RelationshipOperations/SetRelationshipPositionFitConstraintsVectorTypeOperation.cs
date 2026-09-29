using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class SetRelationshipPositionFitConstraintsVectorTypeOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.set_relationship_position_fit_constraints_vector_type",
        "Set Relationship Position Fit Constraints (Vector Type)",
        "briosa.RelationshipOperations", "SetRelationshipPositionFitConstraintsVectorType",
        "/briosa.RelationshipOperations/SetRelationshipPositionFitConstraintsVectorType",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetRelationshipPositionFitConstraintsVectorTypeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2"),
                new("Position Vector Constraint", WorkerMpValueKind.ToleranceVectorOptions,
                    ToleranceVectorOptionsMapper.Required(request.PositionVectorConstraint, "position_vector_constraint"),
                    "SetToleranceVectorOptionsArg")
            ], []);
    }

    public static Api.SetRelationshipPositionFitConstraintsVectorTypeResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
