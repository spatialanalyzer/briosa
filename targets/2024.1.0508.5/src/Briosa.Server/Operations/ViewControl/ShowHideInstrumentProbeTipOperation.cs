using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class ShowHideInstrumentProbeTipOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.show_hide_instrument_probe_tip", "Show/Hide Instrument Probe Tip", "briosa.ViewControl",
        "ShowHideInstrumentProbeTip", "/briosa.ViewControl/ShowHideInstrumentProbeTip", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ShowHideInstrumentProbeTipRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [new("Show Instrument Probe Tip?", WorkerMpValueKind.Logical,
            new WorkerBooleanValue(request.ShowInstrumentProbeTip), "SetBoolArg")], []);
    }

    public static Api.ShowHideInstrumentProbeTipResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
