using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class ShowHideInstrumentInterfaceOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.show_hide_instrument_interface", "Show/Hide Instrument Interface", "briosa.ViewControl",
        "ShowHideInstrumentInterface", "/briosa.ViewControl/ShowHideInstrumentInterface", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ShowHideInstrumentInterfaceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Instrument's ID", WorkerMpValueKind.CollectionInstrumentId,
                CollectionInstrumentIdMapper.Required(request.InstrumentId, "instrument_id"), "SetColInstIdArg"),
            new("Minimize Interface?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.MinimizeInterface), "SetBoolArg"),
            new("Hide Interface?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HideInterface), "SetBoolArg")
        ], []);
    }

    public static Api.ShowHideInstrumentInterfaceResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
