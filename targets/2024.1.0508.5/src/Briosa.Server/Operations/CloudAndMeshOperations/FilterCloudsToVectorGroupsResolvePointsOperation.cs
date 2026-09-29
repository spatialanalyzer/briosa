using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.CloudAndMeshOperations;

internal static class FilterCloudsToVectorGroupsResolvePointsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "cloud_and_mesh_operations.filter_clouds_to_vector_groups_resolve_points", "Filter Clouds to Vector Groups - Resolve points",
        "briosa.CloudAndMeshOperations", "FilterCloudsToVectorGroupsResolvePoints", "/briosa.CloudAndMeshOperations/FilterCloudsToVectorGroupsResolvePoints",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.FilterCloudsToVectorGroupsResolvePointsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var outputType = request.HasOutputType ? request.OutputType : Api.PointOutputType.Points;
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Cloud Names", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.CloudNames, "cloud_names", WorkerObjectTypeValue.Cloud), "SetCollectionObjectNameRefListArg"),
            new("Vector Group Names", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.VectorGroupNames, "vector_group_names", WorkerObjectTypeValue.VectorGroup), "SetCollectionObjectNameRefListArg"),
            new("Output Group Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.OutputGroupName, "output_group_name", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
            new("Minimum Proximity", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasMinimumProximity ? request.MinimumProximity : 0), "SetDoubleArg"),
            new("Maximum Proximity", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasMaximumProximity ? request.MaximumProximity : 0), "SetDoubleArg"),
            new("Maximum Distance From Vector Begin", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasMaximumDistanceFromVectorBegin ? request.MaximumDistanceFromVectorBegin : 0), "SetDoubleArg"),
            new("Minimum number of required points", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.HasMinimumNumberOfRequiredPoints ? request.MinimumNumberOfRequiredPoints : 0), "SetIntegerArg"),
            new("Output Type", WorkerMpValueKind.Text, PointOutputTypeMapper.ToMpValue(outputType), "SetStringArg")
        ], []);
    }

    public static Api.FilterCloudsToVectorGroupsResolvePointsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
