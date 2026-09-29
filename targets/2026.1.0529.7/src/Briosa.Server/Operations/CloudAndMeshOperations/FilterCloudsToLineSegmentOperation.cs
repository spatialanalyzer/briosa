using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.CloudAndMeshOperations;

internal static class FilterCloudsToLineSegmentOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "cloud_and_mesh_operations.filter_clouds_to_line_segment", "Filter Clouds to Line Segment",
        "briosa.CloudAndMeshOperations", "FilterCloudsToLineSegment", "/briosa.CloudAndMeshOperations/FilterCloudsToLineSegment",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.FilterCloudsToLineSegmentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var outputType = request.HasOutputType ? request.OutputType : Api.PointOutputType.Points;
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Cloud Names", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.CloudNames, "cloud_names", WorkerObjectTypeValue.Cloud), "SetCollectionObjectNameRefListArg"),
            new("First Line End Point", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.FirstLineEndPoint, "first_line_end_point"), "SetPointNameArg"),
            new("Second Line End Point", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.SecondLineEndPoint, "second_line_end_point"), "SetPointNameArg"),
            new("Output Group Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.OutputGroupName, "output_group_name", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
            new("Minimum Proximity", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasMinimumProximity ? request.MinimumProximity : 0), "SetDoubleArg"),
            new("Maximum Proximity", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasMaximumProximity ? request.MaximumProximity : 0), "SetDoubleArg"),
            new("Output Type", WorkerMpValueKind.Text, PointOutputTypeMapper.ToMpValue(outputType), "SetStringArg")
        ], []);
    }

    public static Api.FilterCloudsToLineSegmentResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
