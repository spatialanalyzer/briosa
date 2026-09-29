using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class GetCurrentInstrumentPositionUpdateOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.get_current_instrument_position_update", "Get Current Instrument Position Update",
        "briosa.InstrumentOperations", "GetCurrentInstrumentPositionUpdate", "/briosa.InstrumentOperations/GetCurrentInstrumentPositionUpdate",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("x_or_r", "X / R", WorkerMpValueKind.FloatingPoint),
        new("y_or_theta", "Y / Theta (Degrees)", WorkerMpValueKind.FloatingPoint),
        new("z_or_phi", "Z / Phi (Degrees)", WorkerMpValueKind.FloatingPoint),
        new("time_since_update", "Time Since Update (sec)", WorkerMpValueKind.FloatingPoint),
        new("timestamp", "Timestamp (Approximate)", WorkerMpValueKind.Text)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetCurrentInstrumentPositionUpdateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var reportingFrame = request.HasReportingFrame
            ? request.ReportingFrame
            : Api.InstrumentPositionReportingFrame.InstrumentBase;
        var frameName = reportingFrame switch
        {
            Api.InstrumentPositionReportingFrame.Unspecified or Api.InstrumentPositionReportingFrame.InstrumentBase => "Instrument Base",
            Api.InstrumentPositionReportingFrame.World => "World",
            Api.InstrumentPositionReportingFrame.Working => "Working",
            _ => throw new ArgumentOutOfRangeException(nameof(request), "Reporting frame is not supported by this SA target.")
        };

        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Reporting Frame", WorkerMpValueKind.Text, new WorkerTextValue(frameName), "SetStringArg"),
                new("Polar Coordinates?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.PolarCoordinates), "SetBoolArg")
            ],
            [
                new("X / R", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Y / Theta (Degrees)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Z / Phi (Degrees)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Time Since Update (sec)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Timestamp (Approximate)", WorkerMpValueKind.Text, "GetStringArg")
            ]);
    }

    public static Api.GetCurrentInstrumentPositionUpdateResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            XOrR = values[0].RequireValue<WorkerDoubleValue>().Value,
            YOrTheta = values[1].RequireValue<WorkerDoubleValue>().Value,
            ZOrPhi = values[2].RequireValue<WorkerDoubleValue>().Value,
            TimeSinceUpdate = values[3].RequireValue<WorkerDoubleValue>().Value,
            Timestamp = values[4].RequireValue<WorkerTextValue>().Value,
            Execution = completed.Details
        };
    }
}
