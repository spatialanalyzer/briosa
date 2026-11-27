using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class CollimationOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.collimation", "Collimation",
        "briosa.InstrumentOperations", "Collimation",
        "/briosa.InstrumentOperations/Collimation",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.CollimationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.HasTiltMode)
            throw RequestFieldViolation.Required("tilt_mode");
        if (request.TiltMode == Api.CollimationTiltMode.Unspecified || !Enum.IsDefined(request.TiltMode))
            throw RequestFieldViolation.SupportedChoice("tilt_mode");
        if (!request.HasBaselineMethod)
            throw RequestFieldViolation.Required("baseline_method");
        if (request.BaselineMethod == Api.CollimationBaselineMethod.Unspecified ||
            !Enum.IsDefined(request.BaselineMethod))
            throw RequestFieldViolation.SupportedChoice("baseline_method");
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Stationary Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                    CollectionInstrumentIdMapper.Required(request.StationaryInstrument, "stationary_instrument"),
                    "SetColInstIdArg"),
                new("Moving Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                    CollectionInstrumentIdMapper.Required(request.MovingInstrument, "moving_instrument"),
                    "SetColInstIdArg"),
                new("Collimation Point", WorkerMpValueKind.PointName,
                    PointNameMapper.Required(request.CollimationPoint, "collimation_point"), "SetPointNameArg"),
                new("Zero Moving Instrument", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasZeroMovingInstrument && request.ZeroMovingInstrument), "SetBoolArg"),
                new("Collimation Tilt Mode", WorkerMpValueKind.CollimationType,
                    WorkerChoiceFactory.FromOrdinal(WorkerMpValueKind.CollimationType, (int)request.TiltMode - 1),
                    "SetCollimationTypeArg"),
                new("Collimation Baseline Mode", WorkerMpValueKind.CollimationBaselineType,
                    WorkerChoiceFactory.FromOrdinal(WorkerMpValueKind.CollimationBaselineType,
                        (int)request.BaselineMethod - 1), "SetCollimationBaselineTypeArg"),
                new("Baseline Distance", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasBaselineDistance ? request.BaselineDistance : 0), "SetDoubleArg"),
                new("Scale Point 1", WorkerMpValueKind.PointName,
                    PointNameMapper.Required(request.ScalePoint1, "scale_point_1"), "SetPointNameArg"),
                new("Scale Point 2", WorkerMpValueKind.PointName,
                    PointNameMapper.Required(request.ScalePoint2, "scale_point_2"), "SetPointNameArg"),
                new("Not Measured By Moving Instrument", WorkerMpValueKind.PointName,
                    PointNameMapper.Required(request.NotMeasuredByMovingInstrument,
                        "not_measured_by_moving_instrument"), "SetPointNameArg"),
                new("As Measured By Moving Instrument", WorkerMpValueKind.PointName,
                    PointNameMapper.Required(request.AsMeasuredByMovingInstrument,
                        "as_measured_by_moving_instrument"), "SetPointNameArg")
            ], []);
    }

    public static Api.CollimationResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
