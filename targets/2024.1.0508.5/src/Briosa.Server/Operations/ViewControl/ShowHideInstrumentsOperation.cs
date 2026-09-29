using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class ShowHideInstrumentsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.show_hide_instruments", "Show/Hide Instruments", "briosa.ViewControl",
        "ShowHideInstruments", "/briosa.ViewControl/ShowHideInstruments", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ShowHideInstrumentsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Instrument IDs", WorkerMpValueKind.CollectionInstrumentIdList,
                CollectionInstrumentIdMapper.RequiredList(request.InstrumentIDs, "instrument_i_ds"), "SetColInstIdRefListArg"),
            new("Show Instruments?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.ShowInstruments), "SetBoolArg")
        ], []);
    }

    public static Api.ShowHideInstrumentsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
