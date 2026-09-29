using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointAtIntersectionOfTwoBSplinesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_point_at_intersection_of_two_b_splines", "Construct Point at Intersection of 2 B-Splines",
        "briosa.ConstructionOperations", "ConstructPointAtIntersectionOfTwoBSplines", "/briosa.ConstructionOperations/ConstructPointAtIntersectionOfTwoBSplines",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointAtIntersectionOfTwoBSplinesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("First B-Spline Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.FirstBSplineName, "first_b_spline_name", WorkerObjectTypeValue.BSpline), "SetCollectionObjectNameArg2"),
            new("Second B-Spline Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.SecondBSplineName, "second_b_spline_name", WorkerObjectTypeValue.BSpline), "SetCollectionObjectNameArg2"),
            new("Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.PointName, "point_name"), "SetPointNameArg")
        ], []);
    }

    public static Api.ConstructPointAtIntersectionOfTwoBSplinesResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
