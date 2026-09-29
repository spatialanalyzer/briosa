using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class SetLadarAutoMeasPointOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.set_ladar_auto_meas_point", "Set LADAR AutoMeas Point", "SetLadarAutoMeasPoint");

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetLadarAutoMeasPointRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Sample Time MS (1-2000)", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.SampleTimeMilliseconds), "SetIntegerArg")
            ], []);
    }

    public static Api.SetLadarAutoMeasPointResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
