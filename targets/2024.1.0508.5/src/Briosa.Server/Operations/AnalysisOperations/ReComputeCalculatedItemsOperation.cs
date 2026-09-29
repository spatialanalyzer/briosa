using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class ReComputeCalculatedItemsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.re_compute_calculated_items", "Re-Compute Calculated Items",
        "briosa.AnalysisOperations", "ReComputeCalculatedItems", "/briosa.AnalysisOperations/ReComputeCalculatedItems",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ReComputeCalculatedItemsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Targets from Shots", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasTargetsFromShots && request.TargetsFromShots), "SetBoolArg"),
                new("Hidden Points", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasHiddenPoints && request.HiddenPoints), "SetBoolArg"),
                new("Relationships", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasRelationships && request.Relationships), "SetBoolArg")
            ], []);
    }

    public static Api.ReComputeCalculatedItemsResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
