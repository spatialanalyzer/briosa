using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class QueryPointToPointAlongCurveOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.query_point_to_point_along_curve", "Query Point to Point Along Curve",
        "briosa.AnalysisOperations", "QueryPointToPointAlongCurve", "/briosa.AnalysisOperations/QueryPointToPointAlongCurve",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("distance_along_curve", "Distance Along Curve", WorkerMpValueKind.FloatingPoint)];

    public static WorkerMpCommand CreateCommand(Api.QueryPointToPointAlongCurveRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("1st Point", WorkerMpValueKind.PointName,
                    PointNameMapper.Required(request.Value1StPoint, "value_1st_point"), "SetPointNameArg"),
                new("2nd Point", WorkerMpValueKind.PointName,
                    PointNameMapper.Required(request.Value2NdPoint, "value_2nd_point"), "SetPointNameArg"),
                new("Curve", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.Curve, "curve"), "SetCollectionObjectNameArg2")
            ],
            [new("Distance Along Curve", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")]);
    }

    public static Api.QueryPointToPointAlongCurveResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        DistanceAlongCurve = completed.Execution.OutputValues[0].RequireValue<WorkerDoubleValue>().Value,
        Execution = completed.Details
    };
}
