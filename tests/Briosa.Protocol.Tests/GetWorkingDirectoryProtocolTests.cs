using TargetProtocol = Briosa.Sa.V2026_1_0529_7.V1Alpha1;

namespace Briosa.Protocol.Tests;

public sealed partial class ProtocolSchemaTests
{
    [Fact]
    public void GetWorkingDirectoryHasExactTargetServiceIdentityAndPresence()
    {
        Assert.Equal(
            "briosa.sa.v2026_1_0529_7.v1alpha1.FileOperations",
            TargetProtocol.FileOperations.Descriptor.FullName);
        var method = Assert.Single(TargetProtocol.FileOperations.Descriptor.Methods);
        Assert.Equal("GetWorkingDirectory", method.Name);
        Assert.Equal(
            TargetProtocol.GetWorkingDirectoryRequest.Descriptor,
            method.InputType);
        Assert.Equal(
            TargetProtocol.GetWorkingDirectoryResult.Descriptor,
            method.OutputType);

        var result = new TargetProtocol.GetWorkingDirectoryResult
        {
            Directory = string.Empty
        };
        Assert.True(result.HasDirectory);
        result.ClearDirectory();
        Assert.False(result.HasDirectory);
    }
}
