using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class EdgeScanMeasurementOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.edge_scan_measurement", "Edge Scan Measurement",
        "briosa.InstrumentOperations", "EdgeScanMeasurement",
        "/briosa.InstrumentOperations/EdgeScanMeasurement",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.EdgeScanMeasurementRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument to scan", WorkerMpValueKind.CollectionInstrumentId,
                    CollectionInstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Point near edge", WorkerMpValueKind.PointName,
                    PointNameMapper.Required(request.PointNearEdge, "point_near_edge"), "SetPointNameArg"),
                new("Point in edge search direction", WorkerMpValueKind.PointName,
                    PointNameMapper.Required(request.EdgeSearchDirectionPoint, "edge_search_direction_point"),
                    "SetPointNameArg"),
                new("Parameter set name", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.HasParameterSetName ? request.ParameterSetName : string.Empty),
                    "SetStringArg"),
                new("Group Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.PointGroup, "point_group",
                        WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
                new("Target Name", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.HasTargetName ? request.TargetName : string.Empty), "SetStringArg")
            ], []);
    }

    public static Api.EdgeScanMeasurementResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
