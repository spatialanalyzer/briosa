using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class ConfigureAndMeasureOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.configure_and_measure", "Configure and Measure",
        "briosa.InstrumentOperations", "ConfigureAndMeasure",
        "/briosa.InstrumentOperations/ConfigureAndMeasure",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConfigureAndMeasureRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var measurementMode = request.HasMeasurementMode ? request.MeasurementMode : string.Empty;
        var measureImmediately = request.HasMeasureImmediately && request.MeasureImmediately;
        var waitForCompletion = !request.HasWaitForCompletion || request.WaitForCompletion;
        var timeoutSeconds = request.HasTimeoutSeconds ? request.TimeoutSeconds : 0d;

        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Instrument's ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
            new("Target Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.Target, "target"), "SetPointNameArg"),
            new("Measurement Mode", WorkerMpValueKind.Text,
                new WorkerTextValue(measurementMode), "SetStringArg"),
            new("Measure Immediately", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(measureImmediately), "SetBoolArg"),
            new("Wait for Completion", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(waitForCompletion), "SetBoolArg"),
            new("Timeout in Seconds", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(timeoutSeconds), "SetDoubleArg")
        ], []);
    }

    public static Api.ConfigureAndMeasureResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
