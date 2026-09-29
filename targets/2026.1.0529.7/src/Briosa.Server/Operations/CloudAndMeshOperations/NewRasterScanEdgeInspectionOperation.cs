using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.CloudAndMeshOperations;

internal static class NewRasterScanEdgeInspectionOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "cloud_and_mesh_operations.new_raster_scan_edge_inspection", "New Raster Scan Edge Inspection",
        "briosa.CloudAndMeshOperations", "NewRasterScanEdgeInspection", "/briosa.CloudAndMeshOperations/NewRasterScanEdgeInspection",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("summary_result", "Summary Result", WorkerMpValueKind.Text)];

    public static WorkerMpCommand CreateCommand(Api.NewRasterScanEdgeInspectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var inputs = new List<WorkerMpInputArgument>
        {
            new("Edge Cloud Names", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.EdgeCloudNames, "edge_cloud_names", WorkerObjectTypeValue.Cloud), "SetCollectionObjectNameRefListArg"),
            new("Edge Surface Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.EdgeSurfaceName, "edge_surface_name", WorkerObjectTypeValue.Surface), "SetCollectionObjectNameArg2"),
            new("Edge BSpline Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.EdgeBSplineName, "edge_b_spline_name", WorkerObjectTypeValue.BSpline), "SetCollectionObjectNameArg2"),
            new("Output Prefix", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.OutputPrefix, "output_prefix", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
            new("Inspection Increment", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasInspectionIncrement ? request.InspectionIncrement : 0), "SetDoubleArg"),
            new("Proximity Filter Distance", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasProximityFilterDistance ? request.ProximityFilterDistance : 0), "SetDoubleArg"),
            new("Edge Bias Value", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasEdgeBiasValue ? request.EdgeBiasValue : 0), "SetDoubleArg"),
            new("Error Tolerance", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasErrorTolerance ? request.ErrorTolerance : 0), "SetDoubleArg"),
            new("Use Cosine Projection Method", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasUseCosineProjectionMethod && request.UseCosineProjectionMethod), "SetBoolArg"),
            new("Minimum Number of Edge Points per segment", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.HasMinimumEdgePointsPerSegment ? request.MinimumEdgePointsPerSegment : 0), "SetIntegerArg")
        };
        if (request.IntermediateCalculationResultsFile is not null)
        {
            inputs.Add(new("Intermediate Calculation Results File(optional)", WorkerMpValueKind.FileReference,
                new WorkerFileReferenceValue(request.IntermediateCalculationResultsFile.Path,
                    request.IntermediateCalculationResultsFile.EmbeddedFile), "SetFilePathArg"));
        }

        return new(Descriptor.OperationId, Descriptor.MpStep, inputs,
            [new("Summary Result", WorkerMpValueKind.Text, "GetStringArg")]);
    }

    public static Api.NewRasterScanEdgeInspectionResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details,
        SummaryResult = completed.Execution.OutputValues[0].RequireValue<WorkerTextValue>().Value
    };
}
