using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class GetInstrumentTargetStatusOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.get_instrument_target_status", "Get Instrument Target Status",
        "briosa.InstrumentOperations", "GetInstrumentTargetStatus",
        "/briosa.InstrumentOperations/GetInstrumentTargetStatus",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("status", "Is Locked?", WorkerMpValueKind.Logical),
        new("status", "Name", WorkerMpValueKind.Text),
        new("status", "Number of Faces", WorkerMpValueKind.WholeNumber),
        new("status", "Locked Face", WorkerMpValueKind.WholeNumber)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetInstrumentTargetStatusRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Instrument's ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg")],
        [
            new("Is Locked?", WorkerMpValueKind.Logical, "GetBoolArg"),
            new("Name", WorkerMpValueKind.Text, "GetStringArg"),
            new("Number of Faces", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
            new("Locked Face", WorkerMpValueKind.WholeNumber, "GetIntegerArg")
        ]);
    }

    public static Api.GetInstrumentTargetStatusResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            Status = new Api.InstrumentTargetStatus
            {
                IsLocked = values[0].RequireValue<WorkerBooleanValue>().Value,
                Name = values[1].RequireValue<WorkerTextValue>().Value,
                NumberOfFaces = values[2].RequireValue<WorkerIntegerValue>().Value,
                LockedFace = values[3].RequireValue<WorkerIntegerValue>().Value
            },
            Execution = completed.Details
        };
    }
}
