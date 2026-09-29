using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructCylindersFromSurfaceFacesRuntimeSelectOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_cylinders_from_surface_faces_runtime_select", "Construct Cylinders From Surface Faces - Runtime Select",
        "briosa.ConstructionOperations", "ConstructCylindersFromSurfaceFacesRuntimeSelect",
        "/briosa.ConstructionOperations/ConstructCylindersFromSurfaceFacesRuntimeSelect",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.ConstructCylindersFromSurfaceFacesRuntimeSelectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep, [], []);
    }
    public static Api.ConstructCylindersFromSurfaceFacesRuntimeSelectResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
