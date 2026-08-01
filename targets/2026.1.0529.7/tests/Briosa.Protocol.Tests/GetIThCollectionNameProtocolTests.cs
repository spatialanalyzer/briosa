using Google.Protobuf.Reflection;
using Api = global::Briosa;

namespace Briosa.Protocol.Tests;

public sealed partial class ProtocolSchemaTests
{
    [Fact]
    public void GetIThCollectionNameRetainsMpCompatibleIdentityAndPresence()
    {
        Assert.Equal(
            "briosa.AnalysisOperations",
            Api.AnalysisOperations.Descriptor.FullName);
        var method = Assert.Single(
            Api.AnalysisOperations.Descriptor.Methods,
            candidate => candidate.Name == "GetIThCollectionName");
        Assert.Equal(
            Api.GetIThCollectionNameRequest.Descriptor,
            method.InputType);
        Assert.Equal(
            Api.GetIThCollectionNameResult.Descriptor,
            method.OutputType);

        var collectionIndex = Api.GetIThCollectionNameRequest.Descriptor
            .FindFieldByName("collection_index");
        Assert.Equal(FieldType.Int32, collectionIndex.FieldType);
        Assert.True(collectionIndex.HasPresence);

        var request = new Api.GetIThCollectionNameRequest
        {
            CollectionIndex = 0
        };
        Assert.True(request.HasCollectionIndex);
        Assert.Equal(0, request.CollectionIndex);
        request.ClearCollectionIndex();
        Assert.False(request.HasCollectionIndex);

        var resultantName = Api.GetIThCollectionNameResult.Descriptor
            .FindFieldByName("resultant_name");
        Assert.Equal(FieldType.String, resultantName.FieldType);
        Assert.True(resultantName.HasPresence);
        Assert.Equal(
            1000,
            Api.GetIThCollectionNameResult.Descriptor
                .FindFieldByName("execution").FieldNumber);
    }
}
