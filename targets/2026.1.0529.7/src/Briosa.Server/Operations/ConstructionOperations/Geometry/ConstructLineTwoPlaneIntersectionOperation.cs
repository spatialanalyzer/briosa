using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructLineTwoPlaneIntersectionOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_line_two_plane_intersection", "Construct Line 2 Plane Intersection",
        "briosa.ConstructionOperations", "ConstructLineTwoPlaneIntersection", "/briosa.ConstructionOperations/ConstructLineTwoPlaneIntersection",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructLineTwoPlaneIntersectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Line Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.LineName, "line_name", WorkerObjectTypeValue.Line), "SetCollectionObjectNameArg2"),
            new("First Plane", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.FirstPlane, "first_plane", WorkerObjectTypeValue.Plane), "SetCollectionObjectNameArg2"),
            new("Second Plane", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.SecondPlane, "second_plane", WorkerObjectTypeValue.Plane), "SetCollectionObjectNameArg2")
        ], []);
    }

    public static Api.ConstructLineTwoPlaneIntersectionResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
