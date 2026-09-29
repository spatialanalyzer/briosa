using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class SetMultiplyInstrumentScaleFactorOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.set_multiply_instrument_scale_factor",
        "Set (multiply) Instrument Scale Factor (CAUTION!)", "SetMultiplyInstrumentScaleFactor");

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetMultiplyInstrumentScaleFactorRequest request)
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

    public static Api.SetMultiplyInstrumentScaleFactorResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
