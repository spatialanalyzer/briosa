using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.GdtOperations;

internal static class MakeSurfaceFaceListFromSurfaceOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "gdt_operations.make_surface_face_list_from_surface", "Make Surface Face List From Surface",
        "briosa.GdtOperations", "MakeSurfaceFaceListFromSurface", "/briosa.GdtOperations/MakeSurfaceFaceListFromSurface",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("surface_faces", "Selected Surface Faces", WorkerMpValueKind.Text)];

    public static WorkerMpCommand CreateCommand(Api.MakeSurfaceFaceListFromSurfaceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var surface = CollectionObjectNameMapper.Required(request.Surface, "surface");
        if (surface.ObjectType == WorkerObjectTypeValue.Any)
            surface = surface with { ObjectType = WorkerObjectTypeValue.Surface };
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Surface Name", WorkerMpValueKind.CollectionObjectName,
                surface, "SetCollectionObjectNameArg2")],
            [new("Selected Surface Faces", WorkerMpValueKind.Text, "GetStringArg")]);
    }

    public static Api.MakeSurfaceFaceListFromSurfaceResult CreateResult(SuccessfulOperationExecution completed) =>
        new()
        {
            SurfaceFaces = new Api.SurfaceFaceList
            {
                Value = completed.Execution.OutputValues[0].RequireValue<WorkerTextValue>().Value
            },
            Execution = completed.Details
        };
}
