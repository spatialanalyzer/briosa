using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointAtIntersectionOfPlaneAndLineOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_point_at_intersection_of_plane_and_line", "Construct Point at Intersection of Plane and Line",
        "briosa.ConstructionOperations", "ConstructPointAtIntersectionOfPlaneAndLine", "/briosa.ConstructionOperations/ConstructPointAtIntersectionOfPlaneAndLine",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointAtIntersectionOfPlaneAndLineRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Plane Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.PlaneName, "plane_name", WorkerObjectTypeValue.Plane), "SetCollectionObjectNameArg2"),
            new("Line Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.LineName, "line_name", WorkerObjectTypeValue.Line), "SetCollectionObjectNameArg2"),
            new("Resulting Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.ResultingPointName, "resulting_point_name"), "SetPointNameArg")
        ], []);
    }

    public static Api.ConstructPointAtIntersectionOfPlaneAndLineResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
