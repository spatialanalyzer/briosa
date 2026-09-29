using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class RunCribSheetOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.run_crib_sheet", "Run Crib Sheet", "briosa.InstrumentOperations",
        "RunCribSheet", "/briosa.InstrumentOperations/RunCribSheet", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe,
        ["instrument-control", "long-running", "at-risk-no-runtime-validation"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.RunCribSheetRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.HasCribSheetName || string.IsNullOrWhiteSpace(request.CribSheetName))
            throw new ArgumentException("Request field 'crib_sheet_name' is required.", nameof(request));
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Collection Name", WorkerMpValueKind.CollectionName,
                CollectionNameMapper.Required(request.Collection, "collection"), "SetCollectionNameArg"),
            new("Crib Sheet Name", WorkerMpValueKind.Text,
                new WorkerTextValue(request.CribSheetName), "SetStringArg"),
            new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                CollectionInstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg")
        ], []);
    }

    public static Api.RunCribSheetResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
