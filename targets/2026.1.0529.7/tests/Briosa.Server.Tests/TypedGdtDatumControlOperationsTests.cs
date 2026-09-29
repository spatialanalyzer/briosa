using Briosa.Server.Operations;
using Briosa.Server.Operations.GdtOperations;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedGdtDatumControlOperationsTests
{
    [Fact]
    public void EnableDisableDatumAlignmentAppliesTypesDefaultsAndBindings()
    {
        var request = new Api.EnableDisableDatumAlignmentForFeatureCheckRequest
        {
            FeatureCheck = new() { CollectionName = "C", ItemName = "FC" },
            Alignment = new() { CollectionName = "C", ItemName = "Alignment" }
        };

        var command = EnableDisableDatumAlignmentForFeatureCheckOperation.CreateCommand(request);

        Assert.Equal(["SetCollectionObjectNameArg2", "SetBoolArg", "SetBoolArg", "SetBoolArg", "SetCollectionObjectNameArg2"],
            command.InputArguments.Select(x => x.SdkBinding));
        Assert.Equal(WorkerItemTypeValue.FeatureCheck,
            command.InputArguments[0].RequireValue<WorkerCollectionItemNameValue>().ItemType);
        Assert.Equal([true, false, true], Enumerable.Range(1, 3)
            .Select(i => command.InputArguments[i].RequireValue<WorkerBooleanValue>().Value));
        Assert.Equal(WorkerItemTypeValue.Alignment,
            command.InputArguments[4].RequireValue<WorkerCollectionItemNameValue>().ItemType);
        Assert.Throws<ArgumentException>(() => EnableDisableDatumAlignmentForFeatureCheckOperation.CreateCommand(new()));
        AssertRegisteredAndRemoved(EnableDisableDatumAlignmentForFeatureCheckOperation.Descriptor.OperationId);
    }

    [Fact]
    public void StartStopFeatureCheckTrappingMapsInstrumentAndDefault()
    {
        var request = new Api.StartStopFeatureCheckTrappingRequest
        {
            FeatureCheck = new() { CollectionName = "C", ItemName = "FC" },
            InstrumentId = new() { CollectionName = "Instruments", InstrumentId = 7 }
        };

        var command = StartStopFeatureCheckTrappingOperation.CreateCommand(request);

        Assert.Equal(["SetCollectionObjectNameArg2", "SetColInstIdArg", "SetBoolArg"],
            command.InputArguments.Select(x => x.SdkBinding));
        var instrument = command.InputArguments[1].RequireValue<WorkerCollectionInstrumentIdValue>();
        Assert.Equal("Instruments", instrument.CollectionName);
        Assert.Equal(7, instrument.InstrumentId);
        Assert.False(command.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Throws<ArgumentException>(() => StartStopFeatureCheckTrappingOperation.CreateCommand(new()));
        AssertRegisteredAndRemoved(StartStopFeatureCheckTrappingOperation.Descriptor.OperationId);
    }

    private static void AssertRegisteredAndRemoved(string id)
    {
        Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
    }
}
