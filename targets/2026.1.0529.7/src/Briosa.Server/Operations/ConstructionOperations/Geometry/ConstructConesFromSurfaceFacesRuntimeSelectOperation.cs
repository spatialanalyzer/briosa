using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructConesFromSurfaceFacesRuntimeSelectOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_cones_from_surface_faces_runtime_select", "Construct Cones From Surface Faces - Runtime Select",
        "briosa.ConstructionOperations", "ConstructConesFromSurfaceFacesRuntimeSelect",
        "/briosa.ConstructionOperations/ConstructConesFromSurfaceFacesRuntimeSelect",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.ConstructConesFromSurfaceFacesRuntimeSelectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep, [], []);
    }
    public static Api.ConstructConesFromSurfaceFacesRuntimeSelectResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
