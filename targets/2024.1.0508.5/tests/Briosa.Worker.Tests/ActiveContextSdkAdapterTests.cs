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
                new SdkOutputArgument(
                    "Currently Active Collection Name",
                    SdkValueKind.Text,
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
        Assert.Equal("scripted-output", Assert.Single(result.OutputValues).StringValue);
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
                new SdkOutputArgument("Length", SdkValueKind.Text, "GetStringArg"),
                new SdkOutputArgument("Angular", SdkValueKind.Text, "GetStringArg"),
                new SdkOutputArgument("Temperature", SdkValueKind.Text, "GetStringArg")
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
            Assert.Equal("scripted-output", output.StringValue);
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
                new SdkOutputArgument("Frame Name", SdkValueKind.Text, "GetStringArg"),
                new SdkOutputArgument("Collection Name", SdkValueKind.Text, "GetStringArg"),
                new SdkOutputArgument(
                    "Working Frame",
                    SdkValueKind.CollectionObjectName,
                    "GetCollectionObjectNameArg",
                    SdkObjectTypeValue.Frame)
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
        Assert.Equal(SdkObjectTypeValue.PointGroup, result.OutputValues[2]
            .CollectionObjectNameValue!.ObjectType);
    }
}
