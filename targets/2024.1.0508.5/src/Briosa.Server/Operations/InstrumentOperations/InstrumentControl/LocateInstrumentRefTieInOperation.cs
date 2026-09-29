using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class LocateInstrumentRefTieInOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.locate_instrument_ref_tie_in", "Locate Instrument (Ref. Tie-In)",
        "briosa.InstrumentOperations", "LocateInstrumentRefTieIn", "/briosa.InstrumentOperations/LocateInstrumentRefTieIn",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.LocateInstrumentRefTieInRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument to Locate", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Reference Group Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.ReferenceGroup, "reference_group", WorkerObjectTypeValue.PointGroup),
                    "SetCollectionObjectNameArg2"),
                new("Actuals Group Name (to be measured)", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.ActualsGroup, "actuals_group", WorkerObjectTypeValue.PointGroup),
                    "SetCollectionObjectNameArg2"),
                new("Tolerance", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.Tolerance), "SetDoubleArg"),
                new("Auto Survey", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.AutoSurvey), "SetBoolArg")
            ], []);
    }

    public static Api.LocateInstrumentRefTieInResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
