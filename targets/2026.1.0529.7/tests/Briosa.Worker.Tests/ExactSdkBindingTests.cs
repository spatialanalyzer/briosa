using Briosa.Worker.Sdk;

namespace Briosa.Worker.Tests;

public sealed partial class SpatialAnalyzerSdkAdapterTests
{
    [Fact]
    public void InputBindingVariantIsNotReducedToItsBroadValueKind()
    {
        using var calls = new RecordingSdkCalls();
        using var adapter = new SpatialAnalyzerSdkAdapter(calls);
        var command = new SdkCommand(
            "binding-mismatch",
            "Binding Mismatch",
            [
                new SdkInputArgument(
                    "Value",
                    SdkValueKind.Text,
                    StringValue: "sensitive-value",
                    SdkBinding: "SetStringArg2")
            ],
            outputArguments: []);

        var result = adapter.Execute(command);

        Assert.Equal(["SetStep:Binding Mismatch"], calls.Events);
        Assert.False(result.ExecuteStepReturned);
        Assert.Equal("sdk-argument-rejected", result.DiagnosticCode);
    }

    [Fact]
    public void OutputBindingVariantIsNotReducedToItsBroadValueKind()
    {
        using var calls = new RecordingSdkCalls();
        using var adapter = new SpatialAnalyzerSdkAdapter(calls);
        var command = new SdkCommand(
            "binding-mismatch",
            "Binding Mismatch",
            inputArguments: [],
            [new SdkOutputArgument("Value", SdkValueKind.Text, "GetStringArg2")]);

        var result = adapter.Execute(command);

        Assert.Equal(
            ["SetStep:Binding Mismatch", "ExecuteStep", "GetMPStepResult"],
            calls.Events);
        Assert.False(Assert.Single(result.OutputValues).Retrieved);
        Assert.Equal("sdk-output-retrieval-failed", result.DiagnosticCode);
    }
}
