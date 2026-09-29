using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class ShowHidePointsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.show_hide_points", "Show/Hide Points", "briosa.ViewControl",
        "ShowHidePoints", "/briosa.ViewControl/ShowHidePoints", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ShowHidePointsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Point Names", WorkerMpValueKind.PointNameList,
                PointNameMapper.RequiredList(request.PointNames, "point_names"), "SetPointNameRefListArg"),
            new("Show? (Hide = FALSE)", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.Show), "SetBoolArg")
        ], []);
    }

    public static Api.ShowHidePointsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
