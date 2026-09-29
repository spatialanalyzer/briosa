using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class SetObjectToObjectDirectionRelationshipFitConstraintsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.set_object_to_object_direction_relationship_fit_constraints",
        "Set Object to Object Direction Relationship Fit Constraints",
        "briosa.RelationshipOperations", "SetObjectToObjectDirectionRelationshipFitConstraints",
        "/briosa.RelationshipOperations/SetObjectToObjectDirectionRelationshipFitConstraints",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetObjectToObjectDirectionRelationshipFitConstraintsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var relationship = CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name");
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                    relationship, "SetCollectionObjectNameArg2"),
                new("Angle Between Vectors Fit Constraints", WorkerMpValueKind.FitConstraintScalarOptions,
                    FitConstraintScalarOptionsMapper.ToWorker(request.AngleBetweenVectorsFitConstraints), "SetFitConstraintScalarOptionsArg"),
                new("Mutual Perpendicular Length Fit Constraints", WorkerMpValueKind.FitConstraintScalarOptions,
                    FitConstraintScalarOptionsMapper.ToWorker(request.MutualPerpendicularLengthFitConstraints), "SetFitConstraintScalarOptionsArg")
            ], []);
    }

    public static Api.SetObjectToObjectDirectionRelationshipFitConstraintsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}