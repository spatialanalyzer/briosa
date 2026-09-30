using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class SetToolkitVisibilityOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.set_toolkit_visibility", "Set Toolkit Visibility", "briosa.ViewControl",
        "SetToolkitVisibility", "/briosa.ViewControl/SetToolkitVisibility", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetToolkitVisibilityRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [new("Show Toolkit?", WorkerMpValueKind.Logical,
            new WorkerBooleanValue(request.ShowToolkit), "SetBoolArg")], []);
    }

    public static Api.SetToolkitVisibilityResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
