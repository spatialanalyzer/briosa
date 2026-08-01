using Google.Protobuf.Reflection;
using Api = global::Briosa;

namespace Briosa.Protocol.Tests;

public sealed partial class ProtocolSchemaTests
{
    [Fact]
    public void GetNumberOfCollectionsRetainsMpCompatibleIdentityAndPresence()
    {
        Assert.Equal(
            "briosa.AnalysisOperations",
            Api.AnalysisOperations.Descriptor.FullName);
        var method = Assert.Single(
            Api.AnalysisOperations.Descriptor.Methods,
            candidate => candidate.Name == "GetNumberOfCollections");
        Assert.Equal(
            Api.GetNumberOfCollectionsRequest.Descriptor,
            method.InputType);
        Assert.Equal(
            Api.GetNumberOfCollectionsResult.Descriptor,
            method.OutputType);
        Assert.Empty(Api.GetNumberOfCollectionsRequest.Descriptor.Fields.InFieldNumberOrder());

        var totalCount = Api.GetNumberOfCollectionsResult.Descriptor
            .FindFieldByName("total_count");
        Assert.Equal(FieldType.Int32, totalCount.FieldType);
        Assert.True(totalCount.HasPresence);
        Assert.Equal(
            1000,
            Api.GetNumberOfCollectionsResult.Descriptor
                .FindFieldByName("execution").FieldNumber);
    }
}
