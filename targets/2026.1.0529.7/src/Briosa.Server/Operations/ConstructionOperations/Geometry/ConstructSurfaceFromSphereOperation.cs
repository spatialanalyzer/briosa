using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructSurfaceFromSphereOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_surface_from_sphere", "Construct Surface From Sphere",
        "briosa.ConstructionOperations", "ConstructSurfaceFromSphere", "/briosa.ConstructionOperations/ConstructSurfaceFromSphere",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructSurfaceFromSphereRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Resulting Surface Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ResultingSurfaceName, "resulting_surface_name", WorkerObjectTypeValue.Surface), "SetCollectionObjectNameArg2"),
            new("Sphere Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.SphereName, "sphere_name", WorkerObjectTypeValue.Sphere), "SetCollectionObjectNameArg2")
        ], []);
    }

    public static Api.ConstructSurfaceFromSphereResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
