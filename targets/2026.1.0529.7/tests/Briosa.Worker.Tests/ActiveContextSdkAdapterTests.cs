using Briosa.Worker.Control;
using Briosa.Worker.Sdk;

namespace Briosa.Worker.Tests;

public sealed partial class SpatialAnalyzerSdkAdapterTests
{
    [Fact]
    public void GetActiveCollectionNameUsesExactTargetStringGetterOrder()
    {
        using var calls = new RecordingSdkCalls();
        using var adapter = new SpatialAnalyzerSdkAdapter(calls);
        var command = new SdkCommand(
            "construction_operations.get_active_collection_name",
            "Get Active Collection Name",
            [],
            [
                new WorkerMpOutputArgument(
                    "Currently Active Collection Name",
                    WorkerMpValueKind.Text,
                    "GetStringArg")
            ]);

        var result = adapter.Execute(command);

        Assert.Equal(
            [
                "SetStep:Get Active Collection Name",
                "ExecuteStep",
                "GetMPStepResult",
                "GetStringArg:Currently Active Collection Name"
            ],
            calls.Events);
        Assert.Equal("scripted-output", ((Assert.Single(result.OutputValues).ReadValue() as WorkerTextValue)?.Value));
    }

    [Fact]
    public void GetActiveUnitsUsesExactTargetGetterOrder()
    {
        using var calls = new RecordingSdkCalls();
        using var adapter = new SpatialAnalyzerSdkAdapter(calls);
        var command = new SdkCommand(
            "utility_operations.get_active_units",
            "Get Active Units",
            [],
            [
                new WorkerMpOutputArgument("Length", WorkerMpValueKind.Text, "GetStringArg"),
                new WorkerMpOutputArgument("Angular", WorkerMpValueKind.Text, "GetStringArg"),
                new WorkerMpOutputArgument("Temperature", WorkerMpValueKind.Text, "GetStringArg")
            ]);

        var result = adapter.Execute(command);

        Assert.Equal(
            [
                "SetStep:Get Active Units",
                "ExecuteStep",
                "GetMPStepResult",
                "GetStringArg:Length",
                "GetStringArg:Angular",
                "GetStringArg:Temperature"
            ],
            calls.Events);
        Assert.All(result.OutputValues, output =>
        {
            Assert.True(output.Retrieved);
            Assert.Equal("scripted-output", ((output.ReadValue() as WorkerTextValue)?.Value));
        });
    }

    [Fact]
    public void GetWorkingFramePropertiesUsesExactTargetGetterOrder()
    {
        using var calls = new RecordingSdkCalls();
        using var adapter = new SpatialAnalyzerSdkAdapter(calls);
        var command = new SdkCommand(
            "utility_operations.get_working_frame_properties",
            "Get Working Frame Properties",
            [],
            [
                new WorkerMpOutputArgument("Frame Name", WorkerMpValueKind.Text, "GetStringArg"),
                new WorkerMpOutputArgument("Collection Name", WorkerMpValueKind.Text, "GetStringArg"),
                new WorkerMpOutputArgument(
                    "Working Frame",
                    WorkerMpValueKind.CollectionObjectName,
                    "GetCollectionObjectNameArg",
                    WorkerObjectTypeValue.Frame)
            ]);

        var result = adapter.Execute(command);

        Assert.Equal(
            [
                "SetStep:Get Working Frame Properties",
                "ExecuteStep",
                "GetMPStepResult",
                "GetStringArg:Frame Name",
                "GetStringArg:Collection Name",
                "GetCollectionObjectNameArg:Working Frame"
            ],
            calls.Events);
        Assert.Equal(WorkerObjectTypeValue.PointGroup, (result.OutputValues[2].ReadValue() as WorkerCollectionObjectNameValue)!.ObjectType);
    }
}
