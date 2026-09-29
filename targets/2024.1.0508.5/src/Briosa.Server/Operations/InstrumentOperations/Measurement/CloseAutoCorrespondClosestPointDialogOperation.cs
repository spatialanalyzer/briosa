using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class CloseAutoCorrespondClosestPointDialogOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.close_auto_correspond_closest_point_dialog", "Close Auto-Correspond Closest Point Dialog", "CloseAutoCorrespondClosestPointDialog");
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.CloseAutoCorrespondClosestPointDialogRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg")], []);
    }

    public static Api.CloseAutoCorrespondClosestPointDialogResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
