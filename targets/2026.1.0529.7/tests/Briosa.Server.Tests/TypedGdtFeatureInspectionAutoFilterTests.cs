using Briosa.Server.Operations;
using Briosa.Server.Operations.GdtOperations;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedGdtFeatureInspectionAutoFilterTests
{
    [Fact]
    public void MapsRequiredListsChoiceAndDefaults()
    {
        var command = FeatureInspectionAutoFilterOperation.CreateCommand(CreateValidRequest());

        Assert.Equal(12, command.InputArguments.Count);
        Assert.Equal(["SetPointNameRefListArg", "SetCollectionObjectNameRefListArg", "SetCollectionObjectNameRefListArg",
            "SetDoubleArg", "SetDoubleArg", "SetOffsetDirectionTypeArg", "SetBoolArg", "SetBoolArg",
            "SetIntegerArg", "SetCollectionObjectNameRefListArg", "SetBoolArg", "SetBoolArg"],
            command.InputArguments.Select(x => x.SdkBinding));
        Assert.Equal("P", Assert.Single(command.InputArguments[0]
            .RequireValue<WorkerPointNameListValue>().Values).TargetName);
        Assert.Equal("Group", Assert.Single(command.InputArguments[1]
            .RequireValue<WorkerCollectionObjectNameListValue>().Values).ObjectName);
        Assert.Equal("Cloud", Assert.Single(command.InputArguments[2]
            .RequireValue<WorkerCollectionObjectNameListValue>().Values).ObjectName);
        Assert.Equal(0.1, command.InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0.1, command.InputArguments[4].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(WorkerOffsetDirectionTypeValue.Both,
            command.InputArguments[5].RequireValue<WorkerChoiceValue<WorkerOffsetDirectionTypeValue>>().Value);
        Assert.Equal([false, false], command.InputArguments.Skip(6).Take(2)
            .Select(x => x.RequireValue<WorkerBooleanValue>().Value));
        Assert.Equal(0, command.InputArguments[8].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal("FC", Assert.Single(command.InputArguments[9]
            .RequireValue<WorkerCollectionObjectNameListValue>().Values).ObjectName);
        Assert.True(command.InputArguments[10].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(command.InputArguments[11].RequireValue<WorkerBooleanValue>().Value);

        var explicitFalse = CreateValidRequest();
        explicitFalse.IncludeDatums = false;
        Assert.False(FeatureInspectionAutoFilterOperation.CreateCommand(explicitFalse)
            .InputArguments[10].RequireValue<WorkerBooleanValue>().Value);
        var missingChoice = CreateValidRequest();
        missingChoice.ClearOffsetDirection();
        Assert.Throws<ArgumentException>(() => FeatureInspectionAutoFilterOperation.CreateCommand(missingChoice));
        var missingGroups = CreateValidRequest();
        missingGroups.GroupNames.Clear();
        Assert.Throws<ArgumentException>(() => FeatureInspectionAutoFilterOperation.CreateCommand(missingGroups));

        var id = FeatureInspectionAutoFilterOperation.Descriptor.OperationId;
        Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
    }

    private static Api.FeatureInspectionAutoFilterRequest CreateValidRequest()
    {
        var request = new Api.FeatureInspectionAutoFilterRequest
            { OffsetDirection = Api.OffsetDirectionType.Both };
        request.PointNames.Add(new Api.PointName { CollectionName = "C", GroupName = "G", TargetName = "P" });
        request.GroupNames.Add(new Api.CollectionObjectName { CollectionName = "C", ObjectName = "Group" });
        request.CloudNames.Add(new Api.CollectionObjectName { CollectionName = "C", ObjectName = "Cloud" });
        request.FeatureCheckNameList.Add(new Api.CollectionItemName { CollectionName = "C", ItemName = "FC" });
        return request;
    }
}
