using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class ReverseSurfaceNormalsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.reverse_surface_normals", "Reverse Surface Normals",
        "briosa.AnalysisOperations", "ReverseSurfaceNormals", "/briosa.AnalysisOperations/ReverseSurfaceNormals",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ReverseSurfaceNormalsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Surface List", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.SurfaceList, "surface_list"),
                "SetCollectionObjectNameRefListArg")], []);
    }

    public static Api.ReverseSurfaceNormalsResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
