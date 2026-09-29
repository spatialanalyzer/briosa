using Briosa.Server.Operations;
using Briosa.Server.Operations.GdtOperations;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedGdtDeleteAndSummaryTests
{
    [Fact]
    public void DeleteUsesReviewedObjectListBindingAndDestructiveRisk()
    {
        var request = new Api.DeleteFeatureChecksRequest();
        request.FeatureChecks.Add(new Api.CollectionItemName
            { CollectionName = "C", ItemName = "FC", ItemType = Api.ItemType.Relationship });

        var command = DeleteFeatureChecksOperation.CreateCommand(request);

        Assert.Equal("SetCollectionObjectNameRefListArg", Assert.Single(command.InputArguments).SdkBinding);
        var featureCheck = Assert.Single(command.InputArguments[0]
            .RequireValue<WorkerCollectionObjectNameListValue>().Values);
        Assert.Equal("FC", featureCheck.ObjectName);
        Assert.Equal(WorkerObjectTypeValue.Any, featureCheck.ObjectType);
        Assert.Contains("destructive", DeleteFeatureChecksOperation.Descriptor.RiskFlags);
        Assert.Throws<ArgumentException>(() => DeleteFeatureChecksOperation.CreateCommand(new()));
        AssertRegisteredAndRemoved(DeleteFeatureChecksOperation.Descriptor.OperationId);
    }

    [Fact]
    public void SummaryUsesReviewedObjectListBindingAndTableNameDefault()
    {
        var request = new Api.GenerateFeatureCheckSummaryRequest();
        request.FeatureCheckList.Add(new Api.CollectionItemName { CollectionName = "C", ItemName = "FC" });

        var command = GenerateFeatureCheckSummaryOperation.CreateCommand(request);

        Assert.Equal(["SetCollectionObjectNameRefListArg", "SetStringArg"],
            command.InputArguments.Select(x => x.SdkBinding));
        var featureCheck = Assert.Single(command.InputArguments[0]
            .RequireValue<WorkerCollectionObjectNameListValue>().Values);
        Assert.Equal("FC", featureCheck.ObjectName);
        Assert.Equal(WorkerObjectTypeValue.Any, featureCheck.ObjectType);
        Assert.Equal("GDT Feature Check Summary", command.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        request.SummaryTableName = "Checks";
        Assert.Equal("Checks", GenerateFeatureCheckSummaryOperation.CreateCommand(request)
            .InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Throws<ArgumentException>(() => GenerateFeatureCheckSummaryOperation.CreateCommand(new()));
        AssertRegisteredAndRemoved(GenerateFeatureCheckSummaryOperation.Descriptor.OperationId);
    }

    private static void AssertRegisteredAndRemoved(string id)
    {
        Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
    }
}
