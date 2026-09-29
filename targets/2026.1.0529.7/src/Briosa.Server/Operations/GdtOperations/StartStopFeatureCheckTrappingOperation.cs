using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.GdtOperations;

internal static class StartStopFeatureCheckTrappingOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "gdt_operations.start_stop_feature_check_trapping", "Start/Stop Feature Check Trapping",
        "briosa.GdtOperations", "StartStopFeatureCheckTrapping",
        "/briosa.GdtOperations/StartStopFeatureCheckTrapping",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.StartStopFeatureCheckTrappingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var featureCheck = CollectionItemNameMapper.Required(request.FeatureCheck, "feature_check");
        if (request.InstrumentId is null || string.IsNullOrWhiteSpace(request.InstrumentId.CollectionName))
            throw new ArgumentException("Request field 'instrument_id' is required.", nameof(request));

        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Feature Check", WorkerMpValueKind.CollectionItemName, featureCheck, "SetCollectionObjectNameArg2"),
            new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                new WorkerCollectionInstrumentIdValue(request.InstrumentId.CollectionName, request.InstrumentId.InstrumentId),
                "SetColInstIdArg"),
            new("Start Trapping (FALSE = Stop)", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.StartTrapping), "SetBoolArg")
        ], []);
    }

    public static Api.StartStopFeatureCheckTrappingResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
