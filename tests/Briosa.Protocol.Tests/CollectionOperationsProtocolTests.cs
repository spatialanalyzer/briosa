using TargetProtocol = Briosa.Sa.V2026_1_0529_7.V1Alpha1;

namespace Briosa.Protocol.Tests;

public sealed partial class ProtocolSchemaTests
{
    [Fact]
    public void CollectionOperationsHaveStableExactTargetIdentities()
    {
        Assert.Equal(
            "briosa.sa.v2026_1_0529_7.v1alpha1.CollectionOperations",
            TargetProtocol.CollectionOperations.Descriptor.FullName);
        var methods = TargetProtocol.CollectionOperations.Descriptor.Methods
            .ToDictionary(method => method.Name, StringComparer.Ordinal);

        Assert.Equal(
            [
                "GetCollectionCount",
                "GetCollectionNameByIndex",
                "GetPointCountInGroup",
                "ListGroupsInCollection",
                "ListPointsInGroup"
            ],
            methods.Keys.Order(StringComparer.Ordinal));
        Assert.Equal(
            TargetProtocol.GetCollectionNameByIndexRequest.Descriptor,
            methods["GetCollectionNameByIndex"].InputType);
        Assert.Equal(
            TargetProtocol.ListPointsInGroupResult.Descriptor,
            methods["ListPointsInGroup"].OutputType);
    }

    [Fact]
    public void CollectionScalarFieldsPreservePresenceAtStableFieldNumbers()
    {
        var indexRequest = new TargetProtocol.GetCollectionNameByIndexRequest
        {
            CollectionIndex = 0
        };
        var groupListRequest = new TargetProtocol.ListGroupsInCollectionRequest
        {
            CollectionName = string.Empty
        };
        var countResult = new TargetProtocol.GetCollectionCountResult
        {
            CollectionCount = 0
        };

        Assert.True(indexRequest.HasCollectionIndex);
        Assert.True(groupListRequest.HasCollectionName);
        Assert.True(countResult.HasCollectionCount);
        Assert.Equal(
            1,
            TargetProtocol.GetCollectionNameByIndexRequest.Descriptor
                .FindFieldByName("collection_index").FieldNumber);
        Assert.Equal(
            1,
            TargetProtocol.GetCollectionCountResult.Descriptor
                .FindFieldByName("collection_count").FieldNumber);
        Assert.Equal(
            1000,
            TargetProtocol.GetCollectionCountResult.Descriptor
                .FindFieldByName("execution").FieldNumber);
    }

    [Fact]
    public void CollectionIdentityListsRetainTheirReviewedMessageFamilies()
    {
        Assert.Equal(
            TargetProtocol.CollectionObjectName.Descriptor,
            TargetProtocol.GetPointCountInGroupRequest.Descriptor
                .FindFieldByName("group").MessageType);
        Assert.Equal(
            TargetProtocol.CollectionObjectNameList.Descriptor,
            TargetProtocol.ListGroupsInCollectionResult.Descriptor
                .FindFieldByName("groups").MessageType);
        Assert.Equal(
            TargetProtocol.PointNameList.Descriptor,
            TargetProtocol.ListPointsInGroupResult.Descriptor
                .FindFieldByName("points").MessageType);
    }
}
