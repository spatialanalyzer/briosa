using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class GetInstrumentModelOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.ReadOnly(
        "instrument_operations.get_instrument_model", "Get Instrument Model", "GetInstrumentModel");
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("name", "Name", WorkerMpValueKind.Text),
        new("model", "Model", WorkerMpValueKind.Text)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetInstrumentModelRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg")],
            [
                new("Name", WorkerMpValueKind.Text, "GetStringArg"),
                new("Model", WorkerMpValueKind.Text, "GetStringArg")
            ]);
    }

    public static Api.GetInstrumentModelResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            Name = values[0].RequireValue<WorkerTextValue>().Value,
            Model = values[1].RequireValue<WorkerTextValue>().Value,
            Execution = completed.Details
        };
    }
}
