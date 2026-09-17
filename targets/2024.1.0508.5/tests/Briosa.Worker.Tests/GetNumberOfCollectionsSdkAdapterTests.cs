using Briosa.Worker.Sdk;

namespace Briosa.Worker.Tests;

public sealed partial class SpatialAnalyzerSdkAdapterTests
{
    [Fact]
    public void GetNumberOfCollectionsUsesExactResultGetterOrder()
    {
        using var calls = new RecordingSdkCalls();
        using var adapter = new SpatialAnalyzerSdkAdapter(calls);
        var command = new SdkCommand(
            "analysis_operations.get_number_of_collections",
            "Get Number of Collections",
            [],
            [
                new SdkOutputArgument(
                    "Total Count",
                    SdkValueKind.WholeNumber,
                    "GetIntegerArg")
            ]);

        var result = adapter.Execute(command);

        Assert.Equal(
            [
                "SetStep:Get Number of Collections",
                "ExecuteStep",
                "GetMPStepResult",
                "GetIntegerArg:Total Count"
            ],
            calls.Events);
        Assert.True(result.ExecuteStepReturned);
        Assert.True(result.MpResult.Succeeded);
        var output = Assert.Single(result.OutputValues);
        Assert.True(output.Retrieved);
        Assert.Equal(7, output.IntegerValue);
    }
}
