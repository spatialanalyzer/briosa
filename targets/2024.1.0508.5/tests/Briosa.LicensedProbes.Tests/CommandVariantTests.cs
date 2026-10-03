using Briosa.Worker.Control;

namespace Briosa.LicensedProbes.Tests;

public sealed class CommandVariantTests
{
    private static WorkerMpCommand Shipped() => new(
        "test.operation",
        "Shipped Step",
        [
            new WorkerMpInputArgument("Group To Fit", WorkerMpValueKind.CollectionObjectName,
                new WorkerCollectionObjectNameValue("B277", "G1", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
            new WorkerMpInputArgument("Template Chart Name (optional)", WorkerMpValueKind.ChartName,
                new WorkerTextValue("Chart"), "SetChartNameArg"),
            new WorkerMpInputArgument("Profile File Name (optional)", WorkerMpValueKind.FileReference,
                new WorkerFileReferenceValue(@"C:\profile", EmbeddedFile: true), "SetFilePathArg"),
            new WorkerMpInputArgument("Use Number Suffix?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(true), "SetBoolArg")
        ],
        [new WorkerMpOutputArgument("Result", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")]);

    [Fact]
    public void ShippedVariantReturnsTheIdenticalCommand()
    {
        var shipped = Shipped();

        Assert.Same(shipped, CommandVariant.Shipped.Apply(shipped));
    }

    [Fact]
    public void OmitRemovesOnlyTheNamedSetterAndKeepsSdkOrder()
    {
        var shipped = Shipped();

        var varied = CommandVariant.Omit("Template Chart Name (optional)").Apply(shipped);

        Assert.Equal("Shipped Step", varied.StepName);
        Assert.Equal(["Group To Fit", "Profile File Name (optional)", "Use Number Suffix?"], varied.InputArguments.Select(static a => a.Name));
        Assert.Equal(shipped.OutputArguments, varied.OutputArguments);
        Assert.Same(shipped.InputArguments[0], varied.InputArguments[0]);
    }

    [Fact]
    public void BlankKeepsTheBindingAndEmbeddedObjectType()
    {
        var varied = CommandVariant.Blank("Group To Fit").Apply(Shipped());

        var argument = varied.InputArguments[0];
        Assert.Equal("SetCollectionObjectNameArg2", argument.SdkBinding);
        Assert.Equal(new WorkerCollectionObjectNameValue(string.Empty, string.Empty, WorkerObjectTypeValue.PointGroup), argument.Value);
        Assert.Equal(4, varied.InputArguments.Count);
    }

    [Fact]
    public void BlankFileReferenceAndTextUseEmptyStrings()
    {
        var chart = CommandVariant.Blank("Template Chart Name (optional)").Apply(Shipped());
        var file = CommandVariant.Blank("Profile File Name (optional)").Apply(Shipped());

        Assert.Equal(new WorkerTextValue(string.Empty), chart.InputArguments[1].Value);
        Assert.Equal(new WorkerFileReferenceValue(string.Empty, EmbeddedFile: false), file.InputArguments[2].Value);
    }

    [Fact]
    public void BlankRefusesKindsWithoutAReviewedBlankValue()
    {
        Assert.Throws<InvalidOperationException>(() => CommandVariant.Blank("Use Number Suffix?").Apply(Shipped()));
    }

    [Fact]
    public void RecaseChangesOnlyTheLabelLetterCase()
    {
        var shipped = Shipped();

        var varied = CommandVariant.Recase("Group To Fit", "Group to Fit").Apply(shipped);

        Assert.Equal("Group to Fit", varied.InputArguments[0].Name);
        Assert.Equal(shipped.InputArguments[0].Value, varied.InputArguments[0].Value);
        Assert.Equal(shipped.InputArguments[0].SdkBinding, varied.InputArguments[0].SdkBinding);
    }

    [Theory]
    [InlineData("Group To Fit")]
    [InlineData("Group To  Fit")]
    [InlineData("Group For Fit")]
    public void RecaseRefusesAnythingButACaseOnlyDifference(string replacement)
    {
        Assert.Throws<ArgumentException>(() => CommandVariant.Recase("Group To Fit", replacement));
    }

    [Fact]
    public void StepTextReplacesOnlyTheStep()
    {
        var shipped = Shipped();

        var varied = CommandVariant.StepText("Other Step").Apply(shipped);

        Assert.Equal("Other Step", varied.StepName);
        Assert.Equal(shipped.InputArguments, varied.InputArguments);
        Assert.Equal(shipped.OutputArguments, varied.OutputArguments);
    }

    [Fact]
    public void VariantsMustNameExactlyOneShippedArgument()
    {
        Assert.Throws<InvalidOperationException>(() => CommandVariant.Omit("Missing").Apply(Shipped()));
        Assert.Throws<InvalidOperationException>(() => CommandVariant.Omit("group to fit").Apply(Shipped()));
    }

    [Fact]
    public void TreatmentDescribesEachShippedLabel()
    {
        var omit = CommandVariant.Omit("Group To Fit");

        Assert.Equal("omitted", omit.TreatmentOf("Group To Fit"));
        Assert.Equal("value", omit.TreatmentOf("Use Number Suffix?"));
        Assert.Equal("blank", CommandVariant.Blank("Group To Fit").TreatmentOf("Group To Fit"));
        Assert.Equal("recased", CommandVariant.Recase("Group To Fit", "Group to Fit").TreatmentOf("Group To Fit"));
    }
}
