using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class ShowHideInspectionBarOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.show_hide_inspection_bar", "Show/Hide Inspection Bar", "briosa.ViewControl",
        "ShowHideInspectionBar", "/briosa.ViewControl/ShowHideInspectionBar", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ShowHideInspectionBarRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [new("Show Inspection Bar?", WorkerMpValueKind.Logical,
            new WorkerBooleanValue(!request.HasShowInspectionBar || request.ShowInspectionBar), "SetBoolArg")], []);
    }

    public static Api.ShowHideInspectionBarResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
