using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class FitGeometryToPointsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.fit_geometry_to_points", "Fit Geometry to Points",
        "briosa.AnalysisOperations", "FitGeometryToPoints", "/briosa.AnalysisOperations/FitGeometryToPoints",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.FitGeometryToPointsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Geometry Type", WorkerMpValueKind.GeometryType,
                    GeometryTypeMapper.Required(request.GeometryType, "geometry_type"), "SetGeometryTypeArg"),
                new("Points to Fit", WorkerMpValueKind.PointNameList,
                    PointNameMapper.RequiredList(request.PointsToFit, "points_to_fit"), "SetPointNameRefListArg"),
                new("Resulting Object Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.ResultingObjectName, "resulting_object_name"), "SetCollectionObjectNameArg2"),
                new("Fit Profile Name", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.HasFitProfileName ? request.FitProfileName : string.Empty), "SetStringArg"),
                new("Report Deviations", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasReportDeviations && request.ReportDeviations), "SetBoolArg"),
                new("Fit Interface Tolerance (-1.0 use profile)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasFitInterfaceTolerance ? request.FitInterfaceTolerance : -1d), "SetDoubleArg"),
                new("Ignore Out of Tolerance Points", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasIgnoreOutOfTolerancePoints && request.IgnoreOutOfTolerancePoints), "SetBoolArg"),
                new("Starting Condition Geometry (optional)", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.StartingConditionGeometry, "starting_condition_geometry"), "SetCollectionObjectNameArg2")
            ], []);
    }

    public static Api.FitGeometryToPointsResult CreateResult(SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}
