using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class SetAbsoluteInstrumentScaleFactorOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.set_absolute_instrument_scale_factor",
        "Set (absolute) Instrument Scale Factor (CAUTION!)", "SetAbsoluteInstrumentScaleFactor");

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetAbsoluteInstrumentScaleFactorRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument's ID", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Scale Factor", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.ScaleFactor), "SetDoubleArg")
            ], []);
    }

    public static Api.SetAbsoluteInstrumentScaleFactorResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
