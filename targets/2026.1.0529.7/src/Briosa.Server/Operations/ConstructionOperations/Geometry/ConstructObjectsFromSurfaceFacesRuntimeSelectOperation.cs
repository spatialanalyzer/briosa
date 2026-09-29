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
        var type = request.ObjectType switch
        {
            Api.ConstructObjectType.Any => "Any",
            Api.ConstructObjectType.Circles => "Circles",
            Api.ConstructObjectType.Cones => "Cones",
            Api.ConstructObjectType.Cylinders => "Cylinders",
            Api.ConstructObjectType.Lines => "Lines",
            Api.ConstructObjectType.Planes => "Planes",
            Api.ConstructObjectType.Slots => "Slots",
            Api.ConstructObjectType.Spheres => "Spheres",
            Api.ConstructObjectType.CenterPoints => "Center Points",
            Api.ConstructObjectType.SurfacePoints => "Surface Points",
            Api.ConstructObjectType.VertexPoints => "Vertex Points",
            _ => throw new ArgumentException("A supported object_type is required.", nameof(request))
        };
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Object Type", WorkerMpValueKind.Text, new WorkerTextValue(type), "SetStringArg"),
            new("Point Offset", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasPointOffset ? request.PointOffset : 0), "SetDoubleArg")
        ], []);
    }

    public static Api.ConstructObjectsFromSurfaceFacesRuntimeSelectResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
