using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.CloudAndMeshOperations;

internal static class RasterScanEdgeInspectionOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "cloud_and_mesh_operations.raster_scan_edge_inspection", "Raster Scan Edge Inspection",
        "briosa.CloudAndMeshOperations", "RasterScanEdgeInspection", "/briosa.CloudAndMeshOperations/RasterScanEdgeInspection",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("summary_result", "Summary Result", WorkerMpValueKind.Text)];

    public static WorkerMpCommand CreateCommand(Api.RasterScanEdgeInspectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Cloud Names", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.CloudNames, "cloud_names", WorkerObjectTypeValue.Cloud), "SetCollectionObjectNameRefListArg"),
            new("Edge Surface Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.EdgeSurfaceName, "edge_surface_name", WorkerObjectTypeValue.Surface), "SetCollectionObjectNameArg2"),
            new("BSpline Edge List", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.BSplineEdgeList, "b_spline_edge_list", WorkerObjectTypeValue.BSpline), "SetCollectionObjectNameRefListArg"),
            new("Prefix for Output Groups", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.PrefixForOutputGroups, "prefix_for_output_groups", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
            new("Tolerance", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasTolerance ? request.Tolerance : 0), "SetDoubleArg"),
            new("Minimum Number of \"Good\" Points per Unit Length", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.HasMinimumGoodPointsPerUnitLength ? request.MinimumGoodPointsPerUnitLength : 0), "SetIntegerArg"),
            new("Maximum Percentage of \"Bad\" Points (0-100)", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasMaximumBadPointsPercentage ? request.MaximumBadPointsPercentage : 0), "SetDoubleArg")
        ], [new("Summary Result", WorkerMpValueKind.Text, "GetStringArg")]);
    }

    public static Api.RasterScanEdgeInspectionResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details,
        SummaryResult = completed.Execution.OutputValues[0].RequireValue<WorkerTextValue>().Value
    };
}
