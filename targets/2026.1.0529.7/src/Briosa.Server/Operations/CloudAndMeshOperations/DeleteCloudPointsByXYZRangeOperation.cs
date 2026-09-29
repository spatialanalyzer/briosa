using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.CloudAndMeshOperations;

internal static class DeleteCloudPointsByXYZRangeOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "cloud_and_mesh_operations.delete_cloud_points_by_xyz_range", "Delete Cloud Points by XYZ Range",
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
            new("X Min", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.HasXMin ? request.XMin : 0), "SetDoubleArg"),
            new("X Max", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.HasXMax ? request.XMax : 0), "SetDoubleArg"),
            new("Y Min", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.HasYMin ? request.YMin : 0), "SetDoubleArg"),
            new("Y Max", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.HasYMax ? request.YMax : 0), "SetDoubleArg"),
            new("Z Min", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.HasZMin ? request.ZMin : 0), "SetDoubleArg"),
            new("Z Max", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.HasZMax ? request.ZMax : 0), "SetDoubleArg"),
            new("Delete Inside", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasDeleteInside && request.DeleteInside), "SetBoolArg")
        ], []);
    }

    public static Api.DeleteCloudPointsByXYZRangeResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
