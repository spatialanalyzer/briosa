using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.CloudAndMeshOperations;

internal static class MeshVolumeOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "cloud_and_mesh_operations.mesh_volume", "Mesh Volume",
        "briosa.CloudAndMeshOperations", "MeshVolume", "/briosa.CloudAndMeshOperations/MeshVolume",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("above", "Above", WorkerMpValueKind.FloatingPoint),
        new("below", "Below", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.MeshVolumeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Mesh", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.Mesh, "mesh", WorkerObjectTypeValue.ScanStripeMesh), "SetCollectionObjectNameArg2"),
            new("Plane", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.Plane, "plane", WorkerObjectTypeValue.Plane), "SetCollectionObjectNameArg2")
        ],
        [
            new("Above", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Below", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
        ]);
    }

    public static Api.MeshVolumeResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details,
        Above = completed.Execution.OutputValues[0].RequireValue<WorkerDoubleValue>().Value,
        Below = completed.Execution.OutputValues[1].RequireValue<WorkerDoubleValue>().Value
    };
}
