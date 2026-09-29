using Briosa.Server.Operations;
using Briosa.Server.Operations.GdtOperations;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedGdtDatumAlignmentTests
{
    [Fact]
    public void MapsRequiredListsAndFeatureCheckDefault()
    {
        var request = new Api.DatumAlignmentRequest
        {
            FeatureCheck = new() { CollectionName = "C", ItemName = "FC" }
        };
        request.ObjectsToMove.Add(new Api.CollectionObjectName { CollectionName = "C", ObjectName = "Part" });
        request.InstrumentsToMove.Add(new Api.CollectionInstrumentId { CollectionName = "I", InstrumentId = 4 });

        var command = DatumAlignmentOperation.CreateCommand(request);

        Assert.Equal(["SetCollectionObjectNameArg2", "SetCollectionObjectNameRefListArg",
            "SetColInstIdRefListArg", "SetBoolArg"], command.InputArguments.Select(x => x.SdkBinding));
        Assert.Equal(WorkerItemTypeValue.FeatureCheck,
            command.InputArguments[0].RequireValue<WorkerCollectionItemNameValue>().ItemType);
        Assert.Equal("Part", Assert.Single(command.InputArguments[1]
            .RequireValue<WorkerCollectionObjectNameListValue>().Values).ObjectName);
        var instrument = Assert.Single(command.InputArguments[2]
            .RequireValue<WorkerCollectionInstrumentIdListValue>().Values);
        Assert.Equal("I", instrument.CollectionName);
        Assert.Equal(4, instrument.InstrumentId);
        Assert.False(command.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.Throws<ArgumentException>(() => DatumAlignmentOperation.CreateCommand(new()));

        var id = DatumAlignmentOperation.Descriptor.OperationId;
        Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
    }
}
