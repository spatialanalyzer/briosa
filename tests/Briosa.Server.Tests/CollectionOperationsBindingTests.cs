using Briosa.Core.V1Alpha1;
using Briosa.Server.Generated.Sa.V2026_1_0529_7.V1Alpha1;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using TargetProtocol = Briosa.Sa.V2026_1_0529_7.V1Alpha1;

namespace Briosa.Server.Tests;

public sealed class CollectionOperationsBindingTests
{
    [Fact]
    [OperationTest("collection_operations.get_collection_count")]
    public void CollectionCountMapsTheExactResultBinding()
    {
        var command = CollectionOperationsGetCollectionCountBinding.CreateCommand(
            new TargetProtocol.GetCollectionCountRequest());
        var output = Assert.Single(command.OutputArguments);

        Assert.Equal("Get Number of Collections", command.StepName);
        Assert.Empty(command.InputArguments);
        Assert.Equal("Total Count", output.Name);
        Assert.Equal(WorkerMpValueKind.WholeNumber, output.Kind);
        Assert.Equal("GetIntegerArg", output.SdkBinding);

        var result = CollectionOperationsGetCollectionCountBinding.CreateResult(Completed(
            new WorkerMpOutputValue(
                "Total Count",
                WorkerMpValueKind.WholeNumber,
                Retrieved: true,
                IntegerValue: 3)));
        Assert.True(result.HasCollectionCount);
        Assert.Equal(3, result.CollectionCount);
    }

    [Fact]
    [OperationTest("collection_operations.get_collection_name_by_index")]
    public void CollectionNameByIndexRequiresAndMapsTheExplicitIndex()
    {
        var command = CollectionOperationsGetCollectionNameByIndexBinding.CreateCommand(
            new TargetProtocol.GetCollectionNameByIndexRequest { CollectionIndex = 2 });
        var input = Assert.Single(command.InputArguments);
        var output = Assert.Single(command.OutputArguments);

        Assert.Equal("Collection Index", input.Name);
        Assert.Equal(WorkerMpValueKind.WholeNumber, input.Kind);
        Assert.Equal(2, input.IntegerValue);
        Assert.Equal("SetIntegerArg", input.SdkBinding);
        Assert.Equal("Resultant Name", output.Name);
        Assert.Equal(WorkerMpValueKind.CollectionName, output.Kind);
        Assert.Equal("GetCollectionNameArg", output.SdkBinding);

        var result = CollectionOperationsGetCollectionNameByIndexBinding.CreateResult(Completed(
            new WorkerMpOutputValue(
                "Resultant Name",
                WorkerMpValueKind.CollectionName,
                Retrieved: true,
                StringValue: "Measured")));
        Assert.True(result.HasCollectionName);
        Assert.Equal("Measured", result.CollectionName);
    }

    [Fact]
    [OperationTest("collection_operations.get_point_count_in_group")]
    public void PointCountMapsTheExactGroupIdentityAndCount()
    {
        var command = CollectionOperationsGetPointCountInGroupBinding.CreateCommand(
            new TargetProtocol.GetPointCountInGroupRequest { Group = Group("Actuals") });
        var input = Assert.Single(command.InputArguments);

        Assert.Equal("Group Name", input.Name);
        Assert.Equal(WorkerMpValueKind.CollectionObjectName, input.Kind);
        Assert.Equal("SetCollectionObjectNameArg2", input.SdkBinding);
        Assert.Equal("Measured", input.CollectionObjectNameValue!.CollectionName);
        Assert.Equal("Actuals", input.CollectionObjectNameValue.ObjectName);
        Assert.Equal(WorkerObjectTypeValue.PointGroup, input.CollectionObjectNameValue.ObjectType);

        var result = CollectionOperationsGetPointCountInGroupBinding.CreateResult(Completed(
            new WorkerMpOutputValue(
                "Total Count",
                WorkerMpValueKind.WholeNumber,
                Retrieved: true,
                IntegerValue: 4)));
        Assert.True(result.HasPointCount);
        Assert.Equal(4, result.PointCount);
    }

    [Fact]
    [OperationTest("collection_operations.list_groups_in_collection")]
    public void GroupListMapsCollectionNamesAndExactObjectTypes()
    {
        var command = CollectionOperationsListGroupsInCollectionBinding.CreateCommand(
            new TargetProtocol.ListGroupsInCollectionRequest { CollectionName = "Measured" });
        var input = Assert.Single(command.InputArguments);

        Assert.Equal("Collection Name", input.Name);
        Assert.Equal(WorkerMpValueKind.CollectionName, input.Kind);
        Assert.Equal("Measured", input.StringValue);
        Assert.Equal("SetCollectionNameArg", input.SdkBinding);

        var result = CollectionOperationsListGroupsInCollectionBinding.CreateResult(Completed(
            new WorkerMpOutputValue(
                "Collection Object Name List",
                WorkerMpValueKind.CollectionObjectNameList,
                Retrieved: true,
                CollectionObjectNameListValue: new WorkerCollectionObjectNameListValue(
                [
                    new("Measured", "Actuals", WorkerObjectTypeValue.PointGroup),
                    new("Measured", "Nominals", WorkerObjectTypeValue.PointGroup)
                ]))));
        Assert.Equal(2, result.Groups.Values.Count);
        Assert.Equal("Actuals", result.Groups.Values[0].ObjectName);
        Assert.Equal(TargetProtocol.ObjectType.PointGroup, result.Groups.Values[0].ObjectType);
    }

    [Fact]
    [OperationTest("collection_operations.list_points_in_group")]
    public void PointListMapsEveryIdentityComponent()
    {
        var command = CollectionOperationsListPointsInGroupBinding.CreateCommand(
            new TargetProtocol.ListPointsInGroupRequest { Group = Group("Actuals") });
        Assert.Equal(
            "GetPointNameRefListArg",
            Assert.Single(command.OutputArguments).SdkBinding);

        var result = CollectionOperationsListPointsInGroupBinding.CreateResult(Completed(
            new WorkerMpOutputValue(
                "Resultant Point Name List",
                WorkerMpValueKind.PointNameList,
                Retrieved: true,
                PointNameListValue: new WorkerPointNameListValue(
                [
                    new("Measured", "Actuals", "P1"),
                    new("Measured", "Actuals", "P2")
                ]))));
        Assert.Equal(2, result.Points.Values.Count);
        Assert.Equal("Measured", result.Points.Values[1].CollectionName);
        Assert.Equal("Actuals", result.Points.Values[1].GroupName);
        Assert.Equal("P2", result.Points.Values[1].TargetName);
    }

    [Fact]
    public void EveryRequiredInputFailsClosedBeforeACommandCanBeCreated()
    {
        Assert.Throws<ArgumentException>(() =>
            CollectionOperationsGetCollectionNameByIndexBinding.CreateCommand(
                new TargetProtocol.GetCollectionNameByIndexRequest()));
        Assert.Throws<ArgumentException>(() =>
            CollectionOperationsListGroupsInCollectionBinding.CreateCommand(
                new TargetProtocol.ListGroupsInCollectionRequest()));
        Assert.Throws<ArgumentException>(() =>
            CollectionOperationsGetPointCountInGroupBinding.CreateCommand(
                new TargetProtocol.GetPointCountInGroupRequest()));
        Assert.Throws<ArgumentException>(() =>
            CollectionOperationsListPointsInGroupBinding.CreateCommand(
                new TargetProtocol.ListPointsInGroupRequest
                {
                    Group = new TargetProtocol.CollectionObjectName
                    {
                        CollectionName = "Measured",
                        ObjectName = "Actuals"
                    }
                }));
    }

    private static TargetProtocol.CollectionObjectName Group(string name) =>
        new()
        {
            CollectionName = "Measured",
            ObjectName = name,
            ObjectType = TargetProtocol.ObjectType.PointGroup
        };

    private static SuccessfulOperationExecution Completed(WorkerMpOutputValue output) =>
        new(
            new WorkerMpExecutionResult(
                ExecuteStepReturned: true,
                MpResultRetrieved: true,
                MpSucceeded: true,
                MpResultCode: 2,
                DurationMilliseconds: 1,
                OutputValues: [output],
                DiagnosticCode: "completed"),
            new MpExecutionDetails
            {
                State = MpExecutionState.Succeeded,
                MpResultCode = 2
            });
}
