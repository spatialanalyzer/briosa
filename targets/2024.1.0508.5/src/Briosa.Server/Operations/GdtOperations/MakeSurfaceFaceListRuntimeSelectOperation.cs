using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.GdtOperations;

internal static class MakeSurfaceFaceListRuntimeSelectOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "gdt_operations.make_surface_face_list_runtime_select", "Make Surface Face List - Runtime Select",
        "briosa.GdtOperations", "MakeSurfaceFaceListRuntimeSelect", "/briosa.GdtOperations/MakeSurfaceFaceListRuntimeSelect",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("surface_faces", "Selected Surface Faces", WorkerMpValueKind.Text)];

    public static WorkerMpCommand CreateCommand(Api.MakeSurfaceFaceListRuntimeSelectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep, [],
            [new("Selected Surface Faces", WorkerMpValueKind.Text, "GetStringArg")]);
    }

    public static Api.MakeSurfaceFaceListRuntimeSelectResult CreateResult(SuccessfulOperationExecution completed) =>
        new()
        {
            SurfaceFaces = new Api.SurfaceFaceList
            {
                Value = completed.Execution.OutputValues[0].RequireValue<WorkerTextValue>().Value
            },
            Execution = completed.Details
        };
}
