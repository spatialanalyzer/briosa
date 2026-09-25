using Briosa.Worker.Control;
using Briosa.Worker.Sdk;

namespace Briosa.Worker.Tests;

public sealed partial class SpatialAnalyzerSdkAdapterTests
{
    [Fact]
    public void GetWorkingDirectoryUsesExactResultOnlyGetterAfterMpSuccess()
    {
        using var calls = new RecordingSdkCalls();
        using var adapter = new SpatialAnalyzerSdkAdapter(calls);
        var command = new SdkCommand(
            "file_operations.get_working_directory",
            "Get Working Directory",
            inputArguments: [],
            [new WorkerMpOutputArgument("Directory", WorkerMpValueKind.Text, "GetStringArg")]);

        var result = adapter.Execute(command);

        Assert.Equal(
            [
                "SetStep:Get Working Directory",
                "ExecuteStep",
                "GetMPStepResult",
                "GetStringArg:Directory"
            ],
            calls.Events);
        Assert.True(result.ExecuteStepReturned);
        Assert.True(result.MpSucceeded);
        var output = Assert.Single(result.OutputValues);
        Assert.True(output.Retrieved);
        Assert.Equal("scripted-output", ((output.ReadValue() as WorkerTextValue)?.Value));
    }
}
