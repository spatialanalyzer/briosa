using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.GdtOperations;

internal static class SetGlobalForceSimultaneousEvaluationOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "gdt_operations.set_global_force_simultaneous_evaluation", "Set Global Force Simultaneous Evaluation",
        "briosa.GdtOperations", "SetGlobalForceSimultaneousEvaluation",
        "/briosa.GdtOperations/SetGlobalForceSimultaneousEvaluation",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetGlobalForceSimultaneousEvaluationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Global Simultaneous Evaluation?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.GlobalSimultaneousEvaluation), "SetBoolArg")
        ], []);
    }

    public static Api.SetGlobalForceSimultaneousEvaluationResult CreateResult(
        SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}
