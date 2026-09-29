using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class SetLadarFeatureMeasSphereOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.set_ladar_feature_meas_sphere", "Set LADAR FeatureMeas Sphere", "SetLadarFeatureMeasSphere");

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetLadarFeatureMeasSphereRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Scan Line Spacing", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasScanLineSpacing ? request.ScanLineSpacing : 0.05), "SetDoubleArg")
            ], []);
    }

    public static Api.SetLadarFeatureMeasSphereResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
