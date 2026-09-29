using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class GetInstrumentPartTemperatureOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.get_instrument_part_temperature", "Get Instrument Part Temperature",
        "briosa.InstrumentOperations", "GetInstrumentPartTemperature",
        "/briosa.InstrumentOperations/GetInstrumentPartTemperature",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("part_temperature", "Part Temperature (F)", WorkerMpValueKind.FloatingPoint)];

    public static WorkerMpCommand CreateCommand(Api.GetInstrumentPartTemperatureRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Instrument's ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg")],
            [new("Part Temperature (F)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")]);
    }

    public static Api.GetInstrumentPartTemperatureResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        PartTemperature = completed.Execution.OutputValues[0].RequireValue<WorkerDoubleValue>().Value,
        Execution = completed.Details
    };
}
