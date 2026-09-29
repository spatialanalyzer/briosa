using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.CloudAndMeshOperations;

internal static class GetCloudPointCountOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "cloud_and_mesh_operations.get_cloud_point_count", "Get Cloud Point Count",
        "briosa.CloudAndMeshOperations", "GetCloudPointCount", "/briosa.CloudAndMeshOperations/GetCloudPointCount",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("points_count", "Points Count", WorkerMpValueKind.WholeNumber),
        new("planar_offset", "Planar Offset", WorkerMpValueKind.FloatingPoint),
        new("radial_offset", "Radial Offset", WorkerMpValueKind.FloatingPoint),
        new("active_clipping_planes", "Active Clipping Planes", WorkerMpValueKind.WholeNumber)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetCloudPointCountRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [new("Cloud Name", WorkerMpValueKind.CollectionObjectName,
            CollectionObjectNameMapper.Required(request.CloudName, "cloud_name", WorkerObjectTypeValue.Cloud), "SetCollectionObjectNameArg2")],
        [
            new("Points Count", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
            new("Planar Offset", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Radial Offset", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Active Clipping Planes", WorkerMpValueKind.WholeNumber, "GetIntegerArg")
        ]);
    }

    public static Api.GetCloudPointCountResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details,
        PointsCount = completed.Execution.OutputValues[0].RequireValue<WorkerIntegerValue>().Value,
        PlanarOffset = completed.Execution.OutputValues[1].RequireValue<WorkerDoubleValue>().Value,
        RadialOffset = completed.Execution.OutputValues[2].RequireValue<WorkerDoubleValue>().Value,
        ActiveClippingPlanes = completed.Execution.OutputValues[3].RequireValue<WorkerIntegerValue>().Value
    };
}
