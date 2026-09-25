using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class SetRelationshipFitConstraintsScalarTypeOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.set_relationship_fit_constraints_scalar_type",
        "Set Relationship Fit Constraints (Scalar Type)",
        "briosa.RelationshipOperations", "SetRelationshipFitConstraintsScalarType",
        "/briosa.RelationshipOperations/SetRelationshipFitConstraintsScalarType",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static WorkerMpCommand CreateCommand(Api.SetRelationshipFitConstraintsScalarTypeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var relationship = CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name");
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new WorkerMpInputArgument("Relationship Name", WorkerMpValueKind.CollectionObjectName, relationship, sdkBinding: "SetCollectionObjectNameArg2"),
                new WorkerMpInputArgument("Fit Constraint Options", WorkerMpValueKind.FitConstraintScalarOptions, FitConstraintScalarOptionsMapper.ToWorker(request.FitConstraintOptions), sdkBinding: "SetFitConstraintScalarOptionsArg")
            ], []);
    }

    public static Api.SetRelationshipFitConstraintsScalarTypeResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
