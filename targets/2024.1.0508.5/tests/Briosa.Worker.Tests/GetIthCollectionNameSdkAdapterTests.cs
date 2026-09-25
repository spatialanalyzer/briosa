using Briosa.Worker.Control;
using Briosa.Worker.Sdk;

namespace Briosa.Worker.Tests;

public sealed partial class SpatialAnalyzerSdkAdapterTests
{
    [Fact]
    public void GetIthCollectionNameUsesExactSetterAndResultGetterOrder()
    {
        using var calls = new RecordingSdkCalls();
        using var adapter = new SpatialAnalyzerSdkAdapter(calls);
        var command = new SdkCommand(
            "analysis_operations.get_ith_collection_name",
            "Get i-th Collection Name",
            [
                new SdkInputArgument(
                    "Collection Index",
                    WorkerMpValueKind.WholeNumber,
                    IntegerValue: 0,
                    SdkBinding: "SetIntegerArg")
            ],
            [
                new WorkerMpOutputArgument(
                    "Resultant Name",
                    WorkerMpValueKind.CollectionName,
                    "GetCollectionNameArg")
            ]);

        var result = adapter.Execute(command);

        Assert.Equal(
            [
                "SetStep:Get i-th Collection Name",
                "SetIntegerArg:Collection Index",
                "ExecuteStep",
                "GetMPStepResult",
                "GetCollectionNameArg:Resultant Name"
            ],
            calls.Events);
        Assert.True(result.ExecuteStepReturned);
        Assert.True(result.MpSucceeded);
        var output = Assert.Single(result.OutputValues);
        Assert.True(output.Retrieved);
        Assert.Equal("Collection", ((output.ReadValue() as WorkerTextValue)?.Value));
    }
}
