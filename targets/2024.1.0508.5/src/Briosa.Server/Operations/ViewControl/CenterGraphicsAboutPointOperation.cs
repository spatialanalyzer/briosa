using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class CenterGraphicsAboutPointOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.center_graphics_about_point", "Center Graphics About Point", "briosa.ViewControl",
        "CenterGraphicsAboutPoint", "/briosa.ViewControl/CenterGraphicsAboutPoint", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.CenterGraphicsAboutPointRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [new("Point Name", WorkerMpValueKind.PointName,
            PointNameMapper.Required(request.PointName, "point_name"), "SetPointNameArg")], []);
    }

    public static Api.CenterGraphicsAboutPointResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
