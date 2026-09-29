using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.CloudAndMeshOperations;

internal static class MeshFillHolesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "cloud_and_mesh_operations.mesh_fill_holes", "Mesh Fill Holes",
        "briosa.CloudAndMeshOperations", "MeshFillHoles", "/briosa.CloudAndMeshOperations/MeshFillHoles",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MeshFillHolesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Mesh", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.Mesh, "mesh", WorkerObjectTypeValue.ScanStripeMesh), "SetCollectionObjectNameArg2"),
            new("Maximum Triangle Length", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasMaximumTriangleLength ? request.MaximumTriangleLength : -1), "SetDoubleArg"),
            new("Tension", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasTension ? request.Tension : 0), "SetDoubleArg"),
            new("Unconditional Filling?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasUnconditionalFilling && request.UnconditionalFilling), "SetBoolArg"),
            new("Fill All Holes?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasFillAllHoles || request.FillAllHoles), "SetBoolArg")
        ], []);
    }

    public static Api.MeshFillHolesResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
