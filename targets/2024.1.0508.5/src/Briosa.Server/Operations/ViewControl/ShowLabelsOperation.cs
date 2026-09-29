using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class ShowLabelsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.show_labels", "Show Labels", "briosa.ViewControl",
        "ShowLabels", "/briosa.ViewControl/ShowLabels", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ShowLabelsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Point Labels On?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.PointLabelsOn), "SetBoolArg"),
            new("Objects Labels On?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.ObjectsLabelsOn), "SetBoolArg")
        ], []);
    }

    public static Api.ShowLabelsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
