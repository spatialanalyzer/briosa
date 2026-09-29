using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointAtIntersectionOfPlanesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_point_at_intersection_of_planes", "Construct Point at Intersection of Planes",
        "briosa.ConstructionOperations", "ConstructPointAtIntersectionOfPlanes", "/briosa.ConstructionOperations/ConstructPointAtIntersectionOfPlanes",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointAtIntersectionOfPlanesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Plane 1 Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.Plane1Name, "plane_1_name", WorkerObjectTypeValue.Plane), "SetCollectionObjectNameArg2"),
            new("Plane 2 Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.Plane2Name, "plane_2_name", WorkerObjectTypeValue.Plane), "SetCollectionObjectNameArg2"),
            new("Plane 3 Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.Plane3Name, "plane_3_name", WorkerObjectTypeValue.Plane), "SetCollectionObjectNameArg2"),
            new("Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.PointName, "point_name"), "SetPointNameArg")
        ], []);
    }

    public static Api.ConstructPointAtIntersectionOfPlanesResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
