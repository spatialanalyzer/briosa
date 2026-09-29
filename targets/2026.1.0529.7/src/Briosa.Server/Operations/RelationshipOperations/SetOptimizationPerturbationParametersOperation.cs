using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class SetOptimizationPerturbationParametersOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.set_optimization_perturbation_parameters",
        "Set Optimization Perturbation Parameters",
        "briosa.RelationshipOperations", "SetOptimizationPerturbationParameters",
        "/briosa.RelationshipOperations/SetOptimizationPerturbationParameters",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetOptimizationPerturbationParametersRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Length Perturbation", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasLengthPerturbation ? request.LengthPerturbation : 0.0001), "SetDoubleArg"),
                new("Angular Perturbation", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasAngularPerturbation ? request.AngularPerturbation : 0.0001), "SetDoubleArg"),
                // Exact SDK evidence includes the trailing space in this argument label.
                new("Damping ", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasDamping ? request.Damping : 1.0), "SetDoubleArg")
            ], []);
    }

    public static Api.SetOptimizationPerturbationParametersResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}