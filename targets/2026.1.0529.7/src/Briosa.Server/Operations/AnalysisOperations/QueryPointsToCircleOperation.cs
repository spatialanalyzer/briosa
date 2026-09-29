using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class QueryPointsToCircleOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.query_points_to_circle", "Query Points to Circle",
        "briosa.AnalysisOperations", "QueryPointsToCircle", "/briosa.AnalysisOperations/QueryPointsToCircle",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.QueryPointsToCircleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Circle Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.CircleName, "circle_name"), "SetCollectionObjectNameArg2"),
                new("Point Group Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.PointGroupName, "point_group_name"), "SetCollectionObjectNameArg2"),
                new("Is Inside Measurement", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasIsInsideMeasurement || request.IsInsideMeasurement), "SetBoolArg"),
                new("Auto Scale Vectors to % of Radius", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.HasAutoScaleVectorsToOfRadius ? request.AutoScaleVectorsToOfRadius : 40), "SetIntegerArg"),
                new("Vector Group Name for Radial", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.VectorGroupNameForRadial, "vector_group_name_for_radial"), "SetCollectionObjectNameArg2"),
                new("Vector Group Name for Planar", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.VectorGroupNameForPlanar, "vector_group_name_for_planar"), "SetCollectionObjectNameArg2"),
                new("Vector Group Name for Combined", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.VectorGroupNameForCombined, "vector_group_name_for_combined"), "SetCollectionObjectNameArg2")
            ], []);
    }

    public static Api.QueryPointsToCircleResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
