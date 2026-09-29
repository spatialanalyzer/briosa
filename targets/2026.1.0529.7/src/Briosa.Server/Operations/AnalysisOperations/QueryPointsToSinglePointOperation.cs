using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class QueryPointsToSinglePointOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.query_points_to_single_point", "Query Points to Single Point",
        "briosa.AnalysisOperations", "QueryPointsToSinglePoint", "/briosa.AnalysisOperations/QueryPointsToSinglePoint",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.QueryPointsToSinglePointRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Point Names", WorkerMpValueKind.PointNameList,
                    PointNameMapper.RequiredList(request.PointNames, "point_names"), "SetPointNameRefListArg"),
                new("Single Point", WorkerMpValueKind.PointName,
                    PointNameMapper.Required(request.SinglePoint, "single_point"), "SetPointNameArg"),
                new("Show Vector Properties?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasShowVectorProperties && request.ShowVectorProperties), "SetBoolArg")
            ], []);
    }

    public static Api.QueryPointsToSinglePointResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
