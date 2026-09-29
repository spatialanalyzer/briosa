using Briosa.Server.Operations.AnalysisOperations;

namespace Briosa.Server.Tests;

public sealed class CylinderFitProfileTargetDifferenceTests
{
    private static readonly string[] Sa2026OnlyBindings =
    [
        "Constrain to Nominal Axis?",
        "Constrain to Nominal Orientation?",
        "Align with Nominal?",
        "Reverse Axis?",
        "Set Axis First to Last Point?"
    ];

    [Fact]
    public void Sa2026PreservesTheFiveTargetSpecificCylinderProfileBindings()
    {
        var command = MakeCylinderFitProfileOperation.CreateCommand(new());

        Assert.Equal(16, command.InputArguments.Count);
        Assert.Equal(Sa2026OnlyBindings,
            command.InputArguments.Skip(5).Take(Sa2026OnlyBindings.Length).Select(argument => argument.Name));
        Assert.All(command.InputArguments.Skip(5).Take(Sa2026OnlyBindings.Length), argument =>
            Assert.False(argument.RequireValue<Briosa.Worker.Control.WorkerBooleanValue>().Value));
    }
}
