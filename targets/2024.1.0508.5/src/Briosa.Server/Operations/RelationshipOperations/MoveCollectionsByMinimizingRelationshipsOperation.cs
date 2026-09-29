using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class MoveCollectionsByMinimizingRelationshipsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.move_collections_by_minimizing_relationships",
        "Move Collections by Minimizing Relationships",
        "briosa.RelationshipOperations", "MoveCollectionsByMinimizingRelationships",
        "/briosa.RelationshipOperations/MoveCollectionsByMinimizingRelationships",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MoveCollectionsByMinimizingRelationshipsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Collections To Move", WorkerMpValueKind.StringList,
                    new WorkerStringListValue(request.CollectionsToMove), "SetStringRefListArg"),
                new("Relationships To Minimize", WorkerMpValueKind.CollectionItemNameList,
                    RelationshipOperationValueMapper.RequiredRelationships(request.RelationshipsToMinimize, "relationships_to_minimize"), "SetCollectionObjectNameRefListArg"),
                new("Solver Mode", WorkerMpValueKind.Text,
                    RelationshipOperationValueMapper.Solver(request.HasSolverMode ? request.SolverMode : null), "SetStringArg"),
                new("Motion to allow", WorkerMpValueKind.FitDegreeOfFreedomOptions,
                    RelationshipOperationValueMapper.RequiredMotion(request.MotionToAllow, "motion_to_allow"), "SetFitDofOptionsArg"),
                new("Use Fit Dialog", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasUseFitDialog && request.UseFitDialog), "SetBoolArg"),
                new("Convergence Threshold", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasConvergenceThreshold ? request.ConvergenceThreshold : 0d), "SetDoubleArg")
            ], []);
    }

    public static Api.MoveCollectionsByMinimizingRelationshipsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
