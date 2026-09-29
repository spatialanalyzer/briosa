using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Google.Protobuf.Collections;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class AutoMeasureBatchOfFeaturesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.auto_measure_batch_of_features", "Auto-Measure Batch of Features",
        "briosa.InstrumentOperations", "AutoMeasureBatchOfFeatures",
        "/briosa.InstrumentOperations/AutoMeasureBatchOfFeatures",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.AutoMeasureBatchOfFeaturesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
            new("Feature List", WorkerMpValueKind.CollectionObjectNameList,
                FeaturesAsObjectNames(request.Features), "SetCollectionObjectNameRefListArg"),
            new("Wait for Complete", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasWaitForComplete ? request.WaitForComplete : true), "SetBoolArg")
        ], []);
    }

    private static WorkerCollectionObjectNameListValue FeaturesAsObjectNames(
        RepeatedField<Api.CollectionItemName> features)
    {
        if (features.Count == 0)
            throw new ArgumentException("Request field 'features' is required.", nameof(features));

        var values = features.Select(feature =>
        {
            if (feature.HasItemType && !Enum.IsDefined(feature.ItemType))
                throw new ArgumentException("Item type is not supported by this SA target.", nameof(features));

            // The MP binding is SetCollectionObjectNameRefListArg, so the legacy contract maps item names to Any objects.
            return new WorkerCollectionObjectNameValue(
                feature.CollectionName, feature.ItemName, WorkerObjectTypeValue.Any);
        }).ToArray();
        return new(values);
    }

    public static Api.AutoMeasureBatchOfFeaturesResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
