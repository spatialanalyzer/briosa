using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.CloudAndMeshOperations;

internal static class FilterCloudsToVectorGroupsResolveCloudsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "cloud_and_mesh_operations.filter_clouds_to_vector_groups_resolve_clouds", "Filter Clouds to Vector Groups - Resolve Clouds",
        "briosa.CloudAndMeshOperations", "FilterCloudsToVectorGroupsResolveClouds", "/briosa.CloudAndMeshOperations/FilterCloudsToVectorGroupsResolveClouds",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("filtered_clouds", "Filtered Clouds", WorkerMpValueKind.CollectionObjectNameList)];

    public static WorkerMpCommand CreateCommand(Api.FilterCloudsToVectorGroupsResolveCloudsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.HasOutputCollectionName || string.IsNullOrWhiteSpace(request.OutputCollectionName))
            throw new ArgumentException("Request field 'output_collection_name' is required.", nameof(request));

        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Cloud Names", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.CloudNames, "cloud_names", WorkerObjectTypeValue.Cloud), "SetCollectionObjectNameRefListArg"),
            new("Vector Group Names", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.VectorGroupNames, "vector_group_names", WorkerObjectTypeValue.VectorGroup), "SetCollectionObjectNameRefListArg"),
            new("Radial Cutoff", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasRadialCutoff ? request.RadialCutoff : 0.1), "SetDoubleArg"),
            new("Lower Cutoff", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasLowerCutoff ? request.LowerCutoff : -0.1), "SetDoubleArg"),
            new("Upper Cutoff", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasUpperCutoff ? request.UpperCutoff : 0.1), "SetDoubleArg"),
            new("Output Collection Name", WorkerMpValueKind.CollectionName,
                new WorkerTextValue(request.OutputCollectionName), "SetCollectionNameArg")
        ], [new("Filtered Clouds", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg")]);
    }

    public static Api.FilterCloudsToVectorGroupsResolveCloudsResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.FilterCloudsToVectorGroupsResolveCloudsResult { Execution = completed.Details };
        foreach (var value in completed.Execution.OutputValues[0]
                     .RequireValue<WorkerCollectionObjectNameListValue>().Values)
        {
            var cloud = value.ObjectType == WorkerObjectTypeValue.Unspecified
                ? value with { ObjectType = WorkerObjectTypeValue.Cloud }
                : value;
            result.FilteredClouds.Add(CollectionObjectNameMapper.ToProtocol(cloud));
        }

        return result;
    }
}
