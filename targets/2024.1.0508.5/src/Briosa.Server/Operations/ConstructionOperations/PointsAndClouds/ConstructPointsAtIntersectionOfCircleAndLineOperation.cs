using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointsAtIntersectionOfCircleAndLineOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_points_at_intersection_of_circle_and_line", "Construct Points at Intersection of Circle and Line",
        "briosa.ConstructionOperations", "ConstructPointsAtIntersectionOfCircleAndLine", "/briosa.ConstructionOperations/ConstructPointsAtIntersectionOfCircleAndLine",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointsAtIntersectionOfCircleAndLineRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Circle Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.CircleName, "circle_name", WorkerObjectTypeValue.Circle), "SetCollectionObjectNameArg2"),
            new("Line Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.LineName, "line_name", WorkerObjectTypeValue.Line), "SetCollectionObjectNameArg2"),
            new("Base Point Name for results", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.BasePointNameForResults, "base_point_name_for_results"), "SetPointNameArg")
        ], []);
    }

    public static Api.ConstructPointsAtIntersectionOfCircleAndLineResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
