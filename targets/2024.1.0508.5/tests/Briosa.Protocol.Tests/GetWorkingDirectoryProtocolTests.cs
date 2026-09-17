using Api = global::Briosa;

namespace Briosa.Protocol.Tests;

public sealed partial class ProtocolSchemaTests
{
    [Fact]
    public void GetWorkingDirectoryHasStableServiceIdentityAndPresence()
    {
        Assert.Equal(
            "briosa.FileOperations",
            Api.FileOperations.Descriptor.FullName);
        var method = Assert.Single(
            Api.FileOperations.Descriptor.Methods,
            candidate => candidate.Name == "GetWorkingDirectory");
        Assert.Equal("GetWorkingDirectory", method.Name);
        Assert.Equal(
            Api.GetWorkingDirectoryRequest.Descriptor,
            method.InputType);
        Assert.Equal(
            Api.GetWorkingDirectoryResult.Descriptor,
            method.OutputType);

        var result = new Api.GetWorkingDirectoryResult
        {
            Directory = string.Empty
        };
        Assert.True(result.HasDirectory);
        result.ClearDirectory();
        Assert.False(result.HasDirectory);
    }
}
