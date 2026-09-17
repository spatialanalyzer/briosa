using Google.Protobuf.Reflection;
using Api = global::Briosa;

namespace Briosa.Protocol.Tests;

public sealed partial class ProtocolSchemaTests
{
    [Fact]
    public void GetIthCollectionNameRetainsMpCompatibleIdentityAndPresence()
    {
        Assert.Equal(
            "briosa.AnalysisOperations",
            Api.AnalysisOperations.Descriptor.FullName);
        var method = Assert.Single(
            Api.AnalysisOperations.Descriptor.Methods,
            candidate => candidate.Name == "GetIthCollectionName");
        Assert.Equal(
            Api.GetIthCollectionNameRequest.Descriptor,
            method.InputType);
        Assert.Equal(
            Api.GetIthCollectionNameResult.Descriptor,
            method.OutputType);

        var collectionIndex = Api.GetIthCollectionNameRequest.Descriptor
            .FindFieldByName("collection_index");
        Assert.Equal(FieldType.Int32, collectionIndex.FieldType);
        Assert.True(collectionIndex.HasPresence);

        var request = new Api.GetIthCollectionNameRequest
        {
            CollectionIndex = 0
        };
        Assert.True(request.HasCollectionIndex);
        Assert.Equal(0, request.CollectionIndex);
        request.ClearCollectionIndex();
        Assert.False(request.HasCollectionIndex);

        var resultantName = Api.GetIthCollectionNameResult.Descriptor
            .FindFieldByName("resultant_name");
        Assert.Equal(FieldType.String, resultantName.FieldType);
        Assert.True(resultantName.HasPresence);
        Assert.Equal(
            1000,
            Api.GetIthCollectionNameResult.Descriptor
                .FindFieldByName("execution").FieldNumber);
    }
}
