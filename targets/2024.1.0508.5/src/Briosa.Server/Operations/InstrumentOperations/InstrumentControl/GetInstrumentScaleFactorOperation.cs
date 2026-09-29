using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class GetInstrumentScaleFactorOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.ReadOnly(
        "instrument_operations.get_instrument_scale_factor", "Get Instrument Scale Factor", "GetInstrumentScaleFactor");

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("scale_factor", "Scale Factor", WorkerMpValueKind.FloatingPoint)];

    public static WorkerMpCommand CreateCommand(Api.GetInstrumentScaleFactorRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Instrument's ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg")],
            [new("Scale Factor", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")]);
    }

    public static Api.GetInstrumentScaleFactorResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        ScaleFactor = completed.Execution.OutputValues[0].RequireValue<WorkerDoubleValue>().Value,
        Execution = completed.Details
    };
}
