using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class SetXyzInstrumentUncertaintiesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.set_xyz_instrument_uncertainties", "Set XYZ Instrument Uncertainties",
        "briosa.InstrumentOperations", "SetXyzInstrumentUncertainties",
        "/briosa.InstrumentOperations/SetXyzInstrumentUncertainties",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetXyzInstrumentUncertaintiesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument's ID", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("X Uncertainty", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasXUncertainty ? request.XUncertainty : 0.0005), "SetDoubleArg"),
                new("Y Uncertainty", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasYUncertainty ? request.YUncertainty : 0.0005), "SetDoubleArg"),
                new("Z Uncertainty)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasZUncertainty ? request.ZUncertainty : 0.0005), "SetDoubleArg")
            ], []);
    }

    public static Api.SetXyzInstrumentUncertaintiesResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
