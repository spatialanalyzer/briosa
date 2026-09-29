using Briosa.Server.Operations.AnalysisOperations;

namespace Briosa.Server.Tests;

public sealed class ComputeGroupOrientationTargetDifferenceTests
{
    [Fact]
    public void Sa2026PreservesItsExactStepNameSpacing()
    {
        Assert.Equal("Compute Group to Group Orientation (Rx, Ry, Rz)",
            ComputeGroupToGroupOrientationRxRyRzOperation.Descriptor.MpStep);
    }
}
