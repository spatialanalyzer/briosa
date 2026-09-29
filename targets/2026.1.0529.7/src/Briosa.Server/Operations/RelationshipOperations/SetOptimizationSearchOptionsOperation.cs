using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class SetOptimizationSearchOptionsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.set_optimization_search_options",
        "Set Optimization Search Options",
        "briosa.RelationshipOperations", "SetOptimizationSearchOptions",
        "/briosa.RelationshipOperations/SetOptimizationSearchOptions",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetOptimizationSearchOptionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var maxStepSizeReduction = request.HasMaxNumberOfStepSizeReduction
            ? request.MaxNumberOfStepSizeReduction : 5;
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Max Number of Step Size Reduction", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(maxStepSizeReduction), "SetIntegerArg")], []);
    }

    public static Api.SetOptimizationSearchOptionsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}