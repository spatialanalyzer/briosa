using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class SetInstrumentAxesOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.set_instrument_axes", "Set Instrument Axes", "SetInstrumentAxes");
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetInstrumentAxesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.AxisValues.Count == 0)
            throw new ArgumentException("Request field 'axis_values' is required.", nameof(request));
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument to Adjust", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.InstrumentToAdjust, "instrument_to_adjust"), "SetColInstIdArg"),
                new("Axis Values", WorkerMpValueKind.DoubleArray,
                    new WorkerDoubleArrayValue(request.AxisValues.ToArray()), "SetDoubleArrayArg"),
                new("Number of Steps", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.HasNumberOfSteps ? request.NumberOfSteps : 0), "SetIntegerArg")
            ], []);
    }

    public static Api.SetInstrumentAxesResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
