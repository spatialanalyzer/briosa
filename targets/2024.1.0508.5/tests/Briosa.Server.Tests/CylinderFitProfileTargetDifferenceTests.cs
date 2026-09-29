using Briosa.Server.Operations.AnalysisOperations;

namespace Briosa.Server.Tests;

public sealed class CylinderFitProfileTargetDifferenceTests
{
    [Fact]
    public void Sa2024DoesNotExposeTheFiveSa2026CylinderProfileBindings()
    {
        var command = MakeCylinderFitProfileOperation.CreateCommand(new());

        Assert.Equal(11, command.InputArguments.Count);
        Assert.Equal("Cylinder Computation Technique", command.InputArguments[5].Name);
        Assert.Equal("SetCompTechniqueArg", command.InputArguments[5].SdkBinding);
    }
}
