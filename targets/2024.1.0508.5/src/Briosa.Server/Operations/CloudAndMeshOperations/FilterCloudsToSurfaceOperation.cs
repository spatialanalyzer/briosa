using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.CloudAndMeshOperations;

internal static class FilterCloudsToSurfaceOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "cloud_and_mesh_operations.filter_clouds_to_surface", "Filter Clouds to Surface",
        "briosa.CloudAndMeshOperations", "FilterCloudsToSurface", "/briosa.CloudAndMeshOperations/FilterCloudsToSurface",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.FilterCloudsToSurfaceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var outputType = request.HasOutputType ? request.OutputType : Api.PointOutputType.Points;
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Cloud Names", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.CloudNames, "cloud_names", WorkerObjectTypeValue.Cloud), "SetCollectionObjectNameRefListArg"),
            new("Filter Surface's Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.FilterSurfaceName, "filter_surface_name", WorkerObjectTypeValue.Surface), "SetCollectionObjectNameArg2"),
            new("Output Group Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.OutputGroupName, "output_group_name", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
            new("Low Proximity", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasLowProximity ? request.LowProximity : 0), "SetDoubleArg"),
            new("High Proximity", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasHighProximity ? request.HighProximity : 0), "SetDoubleArg"),
            new("Skip Factor", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.HasSkipFactor ? request.SkipFactor : 0), "SetIntegerArg"),
            new("Output Type", WorkerMpValueKind.Text, PointOutputTypeMapper.ToMpValue(outputType), "SetStringArg")
        ], []);
    }

    public static Api.FilterCloudsToSurfaceResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
