using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class LrApdisPerformMcmCalibrationOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.lr_apdis_perform_mcm_calibration",
        "LR APDIS Perform MCM Calibration", "LrApdisPerformMcmCalibration");

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.LrApdisPerformMcmCalibrationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Nominal Group Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.NominalGroup, "nominal_group", WorkerObjectTypeValue.PointGroup),
                    "SetCollectionObjectNameArg2"),
                new("Use Matte Tooling Ball?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasUseMatteToolingBall ? request.UseMatteToolingBall : true), "SetBoolArg"),
                new("New Calibration Name", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.HasNewCalibrationName ? request.NewCalibrationName : string.Empty), "SetStringArg")
            ], []);
    }

    public static Api.LrApdisPerformMcmCalibrationResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
