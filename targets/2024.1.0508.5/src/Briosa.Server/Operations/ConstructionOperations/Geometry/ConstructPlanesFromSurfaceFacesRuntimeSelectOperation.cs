using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPlanesFromSurfaceFacesRuntimeSelectOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_planes_from_surface_faces_runtime_select", "Construct Planes From Surface Faces - Runtime Select",
        "briosa.ConstructionOperations", "ConstructPlanesFromSurfaceFacesRuntimeSelect",
        "/briosa.ConstructionOperations/ConstructPlanesFromSurfaceFacesRuntimeSelect",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.ConstructPlanesFromSurfaceFacesRuntimeSelectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep, [], []);
    }
    public static Api.ConstructPlanesFromSurfaceFacesRuntimeSelectResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
