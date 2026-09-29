using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.CloudAndMeshOperations;

internal static class FilterCloudsToGroupOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "cloud_and_mesh_operations.filter_clouds_to_group", "Filter Clouds to Group",
        "briosa.CloudAndMeshOperations", "FilterCloudsToGroup", "/briosa.CloudAndMeshOperations/FilterCloudsToGroup",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.FilterCloudsToGroupRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var outputType = request.HasOutputType ? request.OutputType : Api.PointOutputType.Points;
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Cloud Names", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.CloudNames, "cloud_names", WorkerObjectTypeValue.Cloud), "SetCollectionObjectNameRefListArg"),
            new("Filter Group's Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.FilterGroupName, "filter_group_name", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
            new("Output Group Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.OutputGroupName, "output_group_name", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
            new("Proximity (0 for Closest Point only)", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasProximity ? request.Proximity : 0), "SetDoubleArg"),
            new("Maximum Number of Points (0 for Unlimited)", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.HasMaximumNumberOfPoints ? request.MaximumNumberOfPoints : 0), "SetIntegerArg"),
            new("Output Type", WorkerMpValueKind.Text, PointOutputTypeMapper.ToMpValue(outputType), "SetStringArg")
        ], []);
    }

    public static Api.FilterCloudsToGroupResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
