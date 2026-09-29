using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class DeleteHiddenPointRodOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.delete_hidden_point_rod", "Delete Hidden Point Rod",
        "briosa.ConstructionOperations", "DeleteHiddenPointRod", "/briosa.ConstructionOperations/DeleteHiddenPointRod",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["destructive"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.DeleteHiddenPointRodRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Hidden Point Rod Index", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.HasHiddenPointRodIndex ? request.HiddenPointRodIndex : 0), "SetIntegerArg")
        ], []);
    }

    public static Api.DeleteHiddenPointRodResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
