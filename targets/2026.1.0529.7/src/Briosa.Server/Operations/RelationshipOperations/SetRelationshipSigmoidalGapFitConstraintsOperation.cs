using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class SetRelationshipSigmoidalGapFitConstraintsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.set_relationship_sigmoidal_gap_fit_constraints",
        "Set Relationship Sigmoidal Gap Fit Constraints",
        "briosa.RelationshipOperations", "SetRelationshipSigmoidalGapFitConstraints",
        "/briosa.RelationshipOperations/SetRelationshipSigmoidalGapFitConstraints",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetRelationshipSigmoidalGapFitConstraintsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2"),
                new("Use Sigmoidal Gap Constraints", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasUseSigmoidalGapConstraints ? request.UseSigmoidalGapConstraints : true), "SetBoolArg")
            ], []);
    }

    public static Api.SetRelationshipSigmoidalGapFitConstraintsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
