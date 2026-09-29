using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class SetPcmmInstrumentXyzUncertaintiesOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.set_pcmm_instrument_xyz_uncertainties",
        "Set PCMM Instrument XYZ Uncertainties", "SetPcmmInstrumentXyzUncertainties");

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetPcmmInstrumentXyzUncertaintiesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument's ID", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("X Uncertainty", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasXUncertainty ? request.XUncertainty : 0.001), "SetDoubleArg"),
                new("Y Uncertainty)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasYUncertainty ? request.YUncertainty : 0.001), "SetDoubleArg"),
                new("Z Uncertainty", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasZUncertainty ? request.ZUncertainty : 0.001), "SetDoubleArg")
            ], []);
    }

    public static Api.SetPcmmInstrumentXyzUncertaintiesResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
