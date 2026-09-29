using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class MakeSurfaceFaceListFromPointProximityOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.ReadOnly(
        "instrument_operations.make_surface_face_list_from_point_proximity", "Make Surface Face List from Point Proximity", "MakeSurfaceFaceListFromPointProximity");
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("surface_faces", "Selected Surface Faces", WorkerMpValueKind.Text)];

    public static WorkerMpCommand CreateCommand(Api.MakeSurfaceFaceListFromPointProximityRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Measured Points", WorkerMpValueKind.PointNameList,
                PointNameMapper.RequiredList(request.MeasuredPoints, "measured_points"), "SetPointNameRefListArg")],
            [new("Selected Surface Faces", WorkerMpValueKind.Text, "GetStringArg")]);
    }

    public static Api.MakeSurfaceFaceListFromPointProximityResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        SurfaceFaces = new Api.SurfaceFaceList
        {
            Value = completed.Execution.OutputValues[0].RequireValue<WorkerTextValue>().Value
        },
        Execution = completed.Details
    };
}
