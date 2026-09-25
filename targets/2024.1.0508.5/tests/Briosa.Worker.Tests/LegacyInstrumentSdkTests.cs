using Briosa.Worker.Control;
using Briosa.Worker.Sdk;

namespace Briosa.Worker.Tests;

public sealed partial class SpatialAnalyzerSdkAdapterTests
{
    [Theory]
    [InlineData("Run Crib Sheet")]
    [InlineData("Project Objects")]
    [InlineData("Stop Projection")]
    public void LegacyInstrumentCommandsExecuteOnlyAfterAllExactBindings(string step)
    {
        using var calls = new RecordingSdkCalls();
        using var adapter = new SpatialAnalyzerSdkAdapter(calls);
        var instrument = new SdkInputArgument("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
            CollectionInstrumentIdValue: new("fixture", 1), SdkBinding: "SetColInstIdArg");
        SdkInputArgument[] inputs = step switch
        {
            "Run Crib Sheet" =>
            [
                new("Collection Name", WorkerMpValueKind.CollectionName, StringValue: "fixture", SdkBinding: "SetCollectionNameArg"),
                new("Crib Sheet Name", WorkerMpValueKind.Text, StringValue: "reviewed-crib", SdkBinding: "SetStringArg"),
                instrument
            ],
            "Project Objects" =>
            [
                instrument,
                new("Objects To Project", WorkerMpValueKind.CollectionObjectNameList,
                    CollectionObjectNameListValue: new([new("fixture", "line", WorkerObjectTypeValue.Line)]),
                    SdkBinding: "SetCollectionObjectNameRefListArg")
            ],
            _ => [instrument]
        };
        var result = adapter.Execute(new SdkCommand("instrument-test", step, inputs, []));
        var expected = new[] { $"SetStep:{step}" }
            .Concat(inputs.Select(input => $"{input.SdkBinding}:{input.Name}"))
            .Concat(["ExecuteStep", "GetMPStepResult"]);
        Assert.Equal(expected, calls.Events);
        Assert.True(result.MpSucceeded);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(42)]
    public void ProjectionRetainsEveryNonSuccessCodeWithoutRetry(int code)
    {
        using var calls = new RecordingSdkCalls { MpResultCode = code };
        using var adapter = new SpatialAnalyzerSdkAdapter(calls);
        var result = adapter.Execute(new SdkCommand("instrument_operations.project_objects", "Project Objects",
            [
                new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                    CollectionInstrumentIdValue: new("fixture", 1), SdkBinding: "SetColInstIdArg"),
                new("Objects To Project", WorkerMpValueKind.CollectionObjectNameList,
                    CollectionObjectNameListValue: new([new("fixture", "line", WorkerObjectTypeValue.Line)]),
                    SdkBinding: "SetCollectionObjectNameRefListArg")
            ], []));
        Assert.True(result.MpResultRetrieved);
        Assert.Equal(code, result.MpResultCode);
        Assert.False(result.MpSucceeded);
        Assert.Single(calls.Events, entry => entry == "ExecuteStep");
    }

    [Theory]
    [InlineData("PMT Arm 4m 7 dof", true)]
    [InlineData("ScAlert Temperature Probe", true)]
    [InlineData("Leica ATS800", false)]
    [InlineData("API iLT", false)]
    [InlineData("Generic Aux Device", false)]
    [InlineData("Unknown Instrument", false)]
    public void InstrumentChoiceValidationPreventsUnsupportedSdkCalls(string name, bool supported)
    {
        using var calls = new RecordingSdkCalls();
        using var adapter = new SpatialAnalyzerSdkAdapter(calls);
        var result = adapter.Execute(new SdkCommand("instrument_operations.add_new_instrument", "Add New Instrument",
            [new("Instrument Type", WorkerMpValueKind.InstrumentTypeName, StringValue: name, SdkBinding: "SetInstTypeNameArg")], []));
        Assert.Equal(supported, result.ExecuteStepReturned);
        Assert.Equal(supported, calls.Events.Contains("SetInstTypeNameArg:Instrument Type", StringComparer.Ordinal));
        Assert.Equal(supported, WorkerInstrumentTypeNames.IsSupported(name));
    }

    [Fact]
    public void ReservedEnhancedCloudSlotsCannotBecomeAnotherObjectOrItemType()
    {
        Assert.Equal("User Name", SdkSpecializedValueCodec.ToSdkString(SdkSystemStringValue.UserName));
        Assert.Equal(6, (int)WorkerObjectTypeValue.ScanStripeCloud);
        Assert.Equal(11, (int)WorkerItemTypeValue.ScanStripeCloud);
        Assert.False(Enum.IsDefined((WorkerObjectTypeValue)5));
        Assert.False(Enum.IsDefined((WorkerItemTypeValue)10));
        Assert.Throws<ArgumentOutOfRangeException>(() => SdkSpecializedValueCodec.ToSdkString((WorkerObjectTypeValue)5));
        Assert.Throws<ArgumentOutOfRangeException>(() => SdkSpecializedValueCodec.ToSdkString((WorkerItemTypeValue)10));
    }
}
