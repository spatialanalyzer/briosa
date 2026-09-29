using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class LrSetRedLaserIntensityOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.lr_set_red_laser_intensity", "LR Set Red Laser Intensity", "LrSetRedLaserIntensity");

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.LrSetRedLaserIntensityRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Intensity (0-100)", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.Intensity), "SetIntegerArg")
            ], []);
    }

    public static Api.LrSetRedLaserIntensityResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
