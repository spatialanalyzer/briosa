using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.CloudAndMeshOperations;

internal static class DeleteCloudPointsByRadialDistanceFromPointsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "cloud_and_mesh_operations.delete_cloud_points_by_radial_distance_from_points", "Delete Cloud Points by Radial Distance from Points",
        "briosa.CloudAndMeshOperations", "DeleteCloudPointsByRadialDistanceFromPoints", "/briosa.CloudAndMeshOperations/DeleteCloudPointsByRadialDistanceFromPoints",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["destructive"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.DeleteCloudPointsByRadialDistanceFromPointsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Cloud Names", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.CloudNames, "cloud_names", WorkerObjectTypeValue.Cloud), "SetCollectionObjectNameRefListArg"),
            new("Points", WorkerMpValueKind.PointNameList,
                PointNameMapper.RequiredList(request.Points, "points"), "SetPointNameRefListArg"),
            new("Radius", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasRadius ? request.Radius : 0), "SetDoubleArg"),
            new("Delete Inside", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasDeleteInside && request.DeleteInside), "SetBoolArg")
        ], []);
    }

    public static Api.DeleteCloudPointsByRadialDistanceFromPointsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
