using System.Collections.Immutable;
using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.GdtOperations;

internal static class DatumAlignmentOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "gdt_operations.datum_alignment", "Datum Alignment",
        "briosa.GdtOperations", "DatumAlignment", "/briosa.GdtOperations/DatumAlignment",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.DatumAlignmentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var featureCheck = CollectionItemNameMapper.Required(request.FeatureCheck, "feature_check");
        if (featureCheck.ItemType == WorkerItemTypeValue.Any)
            featureCheck = featureCheck with { ItemType = WorkerItemTypeValue.FeatureCheck };
        if (request.InstrumentsToMove.Count == 0)
            throw new ArgumentException("Request field 'instruments_to_move' is required.", nameof(request));

        var instruments = new WorkerCollectionInstrumentIdListValue(
            request.InstrumentsToMove.Select(instrument =>
                new WorkerCollectionInstrumentIdValue(instrument.CollectionName, instrument.InstrumentId))
                .ToImmutableArray());
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Feature Check", WorkerMpValueKind.CollectionItemName, featureCheck, "SetCollectionObjectNameArg2"),
            new("Objects to Move", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.ObjectsToMove, "objects_to_move"),
                "SetCollectionObjectNameRefListArg"),
            new("Instruments to Move", WorkerMpValueKind.CollectionInstrumentIdList, instruments,
                "SetColInstIdRefListArg"),
            new("Apply Feature Check Transform?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.ApplyFeatureCheckTransform), "SetBoolArg")
        ], []);
    }

    public static Api.DatumAlignmentResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
