using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructObjectsFromSurfaceFacesRuntimeSelectOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_objects_from_surface_faces_runtime_select",
        "Construct Objects From Surface Faces - Runtime Select", "briosa.ConstructionOperations",
        "ConstructObjectsFromSurfaceFacesRuntimeSelect",
        "/briosa.ConstructionOperations/ConstructObjectsFromSurfaceFacesRuntimeSelect",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructObjectsFromSurfaceFacesRuntimeSelectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        // All seven construction choices are required for this SA target.
        if (!request.HasConstructPlanes)
            throw RequestFieldViolation.Required("construct_planes");
        if (!request.HasConstructCylinders)
            throw RequestFieldViolation.Required("construct_cylinders");
        if (!request.HasConstructSpheres)
            throw RequestFieldViolation.Required("construct_spheres");
        if (!request.HasConstructCones)
            throw RequestFieldViolation.Required("construct_cones");
        if (!request.HasConstructLines)
            throw RequestFieldViolation.Required("construct_lines");
        if (!request.HasConstructPoints)
            throw RequestFieldViolation.Required("construct_points");
        if (!request.HasConstructCircles)
            throw RequestFieldViolation.Required("construct_circles");
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Construct Planes?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.ConstructPlanes), "SetBoolArg"),
            new("Construct Cylinders?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.ConstructCylinders), "SetBoolArg"),
            new("Construct Spheres?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.ConstructSpheres), "SetBoolArg"),
            new("Construct Cones?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.ConstructCones), "SetBoolArg"),
            new("Construct Lines?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.ConstructLines), "SetBoolArg"),
            new("Construct Points?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.ConstructPoints), "SetBoolArg"),
            new("Construct Circles?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.ConstructCircles), "SetBoolArg")
        ], []);
    }

    public static Api.ConstructObjectsFromSurfaceFacesRuntimeSelectResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
