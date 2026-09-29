using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class GetSurfacePhysicalStatsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.get_surface_physical_stats", "Get Surface Physical Stats",
        "briosa.AnalysisOperations", "GetSurfacePhysicalStats", "/briosa.AnalysisOperations/GetSurfacePhysicalStats",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("volume", "Volume", WorkerMpValueKind.FloatingPoint),
        new("area", "Area", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetSurfacePhysicalStatsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Surface Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.SurfaceName, "surface_name"), "SetCollectionObjectNameArg2")],
            [
                new("Volume", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Area", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
            ]);
    }

    public static Api.GetSurfacePhysicalStatsResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            Volume = values[0].RequireValue<WorkerDoubleValue>().Value,
            Area = values[1].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }
}
