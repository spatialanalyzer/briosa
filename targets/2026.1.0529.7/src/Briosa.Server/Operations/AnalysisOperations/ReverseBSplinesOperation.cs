using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class ReverseBSplinesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.reverse_bsplines", "Reverse B-Splines",
        "briosa.AnalysisOperations", "ReverseBSplines", "/briosa.AnalysisOperations/ReverseBSplines",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ReverseBSplinesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("B-Spline List", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.BSplineList, "b_spline_list"),
                "SetCollectionObjectNameRefListArg")], []);
    }

    public static Api.ReverseBSplinesResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
