using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.CloudAndMeshOperations;

internal static class ConsolidateMeshOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "cloud_and_mesh_operations.consolidate_mesh", "Consolidate Mesh",
        "briosa.CloudAndMeshOperations", "ConsolidateMesh", "/briosa.CloudAndMeshOperations/ConsolidateMesh",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConsolidateMeshRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Mesh", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.Mesh, "mesh", WorkerObjectTypeValue.ScanStripeMesh), "SetCollectionObjectNameArg2")
        ], []);
    }

    public static Api.ConsolidateMeshResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
