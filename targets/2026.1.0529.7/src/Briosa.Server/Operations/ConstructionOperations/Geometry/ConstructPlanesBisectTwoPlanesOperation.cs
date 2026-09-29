using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPlanesBisectTwoPlanesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_planes_bisect_two_planes", "Construct Planes, Bisect 2 Planes",
        "briosa.ConstructionOperations", "ConstructPlanesBisectTwoPlanes",
        "/briosa.ConstructionOperations/ConstructPlanesBisectTwoPlanes",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPlanesBisectTwoPlanesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Resultant Plane Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ResultantPlaneName, "resultant_plane_name", WorkerObjectTypeValue.Plane), "SetCollectionObjectNameArg2"),
            new("First Plane", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.FirstPlane, "first_plane", WorkerObjectTypeValue.Plane), "SetCollectionObjectNameArg2"),
            new("Second Plane", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.SecondPlane, "second_plane", WorkerObjectTypeValue.Plane), "SetCollectionObjectNameArg2")
        ], []);
    }

    public static Api.ConstructPlanesBisectTwoPlanesResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
