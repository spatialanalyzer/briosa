using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.CloudAndMeshOperations;

internal static class FilterCloudsToPlaneOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "cloud_and_mesh_operations.filter_clouds_to_plane", "Filter Clouds to Plane",
        "briosa.CloudAndMeshOperations", "FilterCloudsToPlane", "/briosa.CloudAndMeshOperations/FilterCloudsToPlane",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.FilterCloudsToPlaneRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var direction = request.HasAllowableOffsetDirection
            ? request.AllowableOffsetDirection
            : Api.OffsetDirectionType.Both;
        var outputType = request.HasOutputType ? request.OutputType : Api.PointOutputType.Points;
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Cloud Names", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.CloudNames, "cloud_names", WorkerObjectTypeValue.Cloud), "SetCollectionObjectNameRefListArg"),
            new("Filter Plane's Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.FilterPlaneName, "filter_plane_name", WorkerObjectTypeValue.Plane), "SetCollectionObjectNameArg2"),
            new("Output Group Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.OutputGroupName, "output_group_name", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
            new("Proximity", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasProximity ? request.Proximity : 0), "SetDoubleArg"),
            new("Allowable Offset Dir", WorkerMpValueKind.OffsetDirectionType,
                OffsetDirectionTypeMapper.ToMpValue(direction), "SetOffsetDirectionTypeArg"),
            new("Output Type", WorkerMpValueKind.Text, PointOutputTypeMapper.ToMpValue(outputType), "SetStringArg")
        ], []);
    }

    public static Api.FilterCloudsToPlaneResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
