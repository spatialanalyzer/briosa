using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class ReversePlaneNormalsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.reverse_plane_normals", "Reverse Plane Normals",
        "briosa.AnalysisOperations", "ReversePlaneNormals", "/briosa.AnalysisOperations/ReversePlaneNormals",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ReversePlaneNormalsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Plane List", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.PlaneList, "plane_list"),
                "SetCollectionObjectNameRefListArg")], []);
    }

    public static Api.ReversePlaneNormalsResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
