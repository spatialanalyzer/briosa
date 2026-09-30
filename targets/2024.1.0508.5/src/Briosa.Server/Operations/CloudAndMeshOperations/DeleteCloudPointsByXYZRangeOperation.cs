using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.CloudAndMeshOperations;

internal static class DeleteCloudPointsByXYZRangeOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "cloud_and_mesh_operations.delete_cloud_points_by_xyz_range", "Delete Cloud Points by X Y Z Range",
        "briosa.CloudAndMeshOperations", "DeleteCloudPointsByXYZRange", "/briosa.CloudAndMeshOperations/DeleteCloudPointsByXYZRange",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["destructive"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.DeleteCloudPointsByXYZRangeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Cloud Names", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.CloudNames, "cloud_names", WorkerObjectTypeValue.Cloud), "SetCollectionObjectNameRefListArg"),
            .. OptionalBound("X Min", request.HasXMin, request.XMin),
            .. OptionalBound("X Max", request.HasXMax, request.XMax),
            .. OptionalBound("Y Min", request.HasYMin, request.YMin),
            .. OptionalBound("Y Max", request.HasYMax, request.YMax),
            .. OptionalBound("Z Min", request.HasZMin, request.ZMin),
            .. OptionalBound("Z Max", request.HasZMax, request.ZMax),
            new("Delete Inside", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasDeleteInside && request.DeleteInside), "SetBoolArg")
        ], []);
    }

    public static Api.DeleteCloudPointsByXYZRangeResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };

    // Absent optional range bounds are omitted, never substituted with zero, matching the reviewed v0.8.0 contract.
    private static WorkerMpInputArgument[] OptionalBound(string name, bool hasValue, double value) =>
        hasValue
            ? [new(name, WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(value), "SetDoubleArg")]
            : [];
}
