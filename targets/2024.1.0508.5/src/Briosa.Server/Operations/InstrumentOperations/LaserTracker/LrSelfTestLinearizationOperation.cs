using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class LrSelfTestLinearizationOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.lr_self_test_linearization", "LR Self Test - Linearization", "LrSelfTestLinearization");

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("linearity", "Linearity (kHz)", WorkerMpValueKind.FloatingPoint)];

    public static WorkerMpCommand CreateCommand(Api.LrSelfTestLinearizationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg")],
            [new("Linearity (kHz)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")]);
    }

    public static Api.LrSelfTestLinearizationResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Linearity = completed.Execution.OutputValues[0].RequireValue<WorkerDoubleValue>().Value,
        Execution = completed.Details
    };
}
