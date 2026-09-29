using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class SetLadarFeatureMeasCircleOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.set_ladar_feature_meas_circle", "Set LADAR FeatureMeas Circle", "SetLadarFeatureMeasCircle");

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetLadarFeatureMeasCircleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Scan Line Spacing", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasScanLineSpacing ? request.ScanLineSpacing : 0.05), "SetDoubleArg"),
                new("Width of Extra Area Around Scan", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasWidthOfExtraAreaAroundScan ? request.WidthOfExtraAreaAroundScan : 0), "SetDoubleArg")
            ], []);
    }

    public static Api.SetLadarFeatureMeasCircleResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
