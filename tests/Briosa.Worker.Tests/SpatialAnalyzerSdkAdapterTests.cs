using System.Collections;
using System.Runtime.InteropServices;
using Briosa.Worker.Sdk;

namespace Briosa.Worker.Tests;

// These tests intentionally exercise the exact COM rectangular-array contract.
#pragma warning disable CA1814

public sealed partial class SpatialAnalyzerSdkAdapterTests
{
    [Fact]
    public void SuccessfulMpRetrievesRequestedOutputsAfterInspectingMpResult()
    {
        using var calls = new RecordingSdkCalls();
        using var adapter = new SpatialAnalyzerSdkAdapter(calls);
        var command = new SdkCommand(
            "get-point-properties",
            "Get Point Properties",
            [
                new SdkInputArgument(
                    "Point Name",
                    SdkValueKind.PointName,
                    PointNameValue: new SdkPointNameValue("", "", ""))
            ],
            [
                new SdkOutputArgument("Planar Offset", SdkValueKind.FloatingPoint),
                new SdkOutputArgument(
                    "Position Tolerance",
                    SdkValueKind.ToleranceVectorOptions),
                new SdkOutputArgument("Component Weights", SdkValueKind.Vector)
            ]);

        var result = adapter.Execute(command);

        Assert.Equal(
            [
                "SetStep:Get Point Properties",
                "SetPointNameArg:Point Name",
                "ExecuteStep",
                "GetMPStepResult",
                "GetDoubleArg:Planar Offset",
                "GetToleranceVectorOptionsArg:Position Tolerance",
                "GetVectorArg:Component Weights"
            ],
            calls.Events);
        Assert.True(result.ExecuteStepReturned);
        Assert.True(result.MpResult.Succeeded);
        Assert.Equal(3, result.OutputValues.Count);
        Assert.All(result.OutputValues, output => Assert.True(output.Retrieved));
        Assert.Equal(1.25, result.OutputValues[0].DoubleValue);
        Assert.Equal(3, result.OutputValues[2].VectorValue!.Z);
        Assert.True(
            result.OutputValues[1].ToleranceVectorOptionsValue!.HighX.Enabled);
        Assert.Null(result.DiagnosticCode);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(42)]
    public void NonSuccessMpCodesDoNotAttemptResultOnlyArgumentGetters(int resultCode)
    {
        using var calls = new RecordingSdkCalls
        {
            MpResultCode = resultCode
        };
        using var adapter = new SpatialAnalyzerSdkAdapter(calls);
        var command = new SdkCommand(
            "failed-operation",
            "Failed Operation",
            inputArguments: [],
            [new SdkOutputArgument("Result", SdkValueKind.Text)]);

        var result = adapter.Execute(command);

        Assert.Equal(
            ["SetStep:Failed Operation", "ExecuteStep", "GetMPStepResult"],
            calls.Events);
        Assert.False(result.MpResult.Succeeded);
        Assert.True(result.MpResult.Retrieved);
        Assert.Equal(resultCode, result.MpResult.ResultCode);
        Assert.Empty(result.OutputValues);
        Assert.Equal("mp-command-failed", result.DiagnosticCode);
    }

    [Fact]
    public void MpResultRetrievalFailureDoesNotAttemptOutputGetters()
    {
        using var calls = new RecordingSdkCalls { MpResultRetrieved = false };
        using var adapter = new SpatialAnalyzerSdkAdapter(calls);
        var command = new SdkCommand(
            "result-retrieval-failure",
            "Result Retrieval Failure",
            inputArguments: [],
            [new SdkOutputArgument("Result", SdkValueKind.Text)]);

        var result = adapter.Execute(command);

        Assert.Equal(
            ["SetStep:Result Retrieval Failure", "ExecuteStep", "GetMPStepResult"],
            calls.Events);
        Assert.True(result.ExecuteStepReturned);
        Assert.False(result.MpResult.Retrieved);
        Assert.False(result.MpResult.Succeeded);
        Assert.Null(result.MpResult.ResultCode);
        Assert.Empty(result.OutputValues);
        Assert.Equal("sdk-mp-result-retrieval-failed", result.DiagnosticCode);
    }

    [Fact]
    public void ExecuteStepRejectionDoesNotRequestMpResultOrOutputs()
    {
        using var calls = new RecordingSdkCalls { ExecuteStepReturned = false };
        using var adapter = new SpatialAnalyzerSdkAdapter(calls);
        var command = new SdkCommand(
            "execute-rejected",
            "Execute Rejected",
            inputArguments: [],
            [new SdkOutputArgument("Result", SdkValueKind.Text)]);

        var result = adapter.Execute(command);

        Assert.Equal(["SetStep:Execute Rejected", "ExecuteStep"], calls.Events);
        Assert.False(result.ExecuteStepReturned);
        Assert.False(result.MpResult.Retrieved);
        Assert.False(result.MpResult.Succeeded);
        Assert.Null(result.MpResult.ResultCode);
        Assert.Empty(result.OutputValues);
        Assert.Equal("execute-step-rejected", result.DiagnosticCode);
    }

    [Fact]
    public void OutputGetterFailureIsPreservedAsAResultDiagnostic()
    {
        using var calls = new RecordingSdkCalls
        {
            FailedOutputName = "Result"
        };
        using var adapter = new SpatialAnalyzerSdkAdapter(calls);
        var command = new SdkCommand(
            "output-failure",
            "Output Failure",
            inputArguments: [],
            [new SdkOutputArgument("Result", SdkValueKind.Text)]);

        var result = adapter.Execute(command);

        var output = Assert.Single(result.OutputValues);
        Assert.False(output.Retrieved);
        Assert.Null(output.StringValue);
        Assert.Equal("sdk-output-retrieval-failed", result.DiagnosticCode);
    }
    [Fact]
    public void IdentityAndReferenceValuesUseExactSdkBindings()
    {
        using var calls = new RecordingSdkCalls();
        using var adapter = new SpatialAnalyzerSdkAdapter(calls);
        var command = new SdkCommand(
            "identity-reference-values",
            "Identity Reference Values",
            [
                new("Chart", SdkValueKind.ChartName, StringValue: "Chart A", SdkBinding: "SetChartNameArg"),
                new("Cloud", SdkValueKind.CloudName, StringValue: "Cloud A", SdkBinding: "SetCloudNameArg"),
                new("Instrument", SdkValueKind.CollectionInstrumentId, CollectionInstrumentIdValue: new("Collection", 17), SdkBinding: "SetColInstIdArg"),
                new("Instruments", SdkValueKind.CollectionInstrumentIdList, CollectionInstrumentIdListValue: new([new("Collection", 17)]), SdkBinding: "SetColInstIdRefListArg"),
                new("Machine", SdkValueKind.CollectionMachineId, CollectionMachineIdValue: new("Collection", 4), SdkBinding: "SetColMachineIdArg"),
                new("Groups", SdkValueKind.CollectionGroupNameList, CollectionGroupNameListValue: new([new("Collection", "Group")]), SdkBinding: "SetCollectionGroupNameRefListArg"),
                new("Collection", SdkValueKind.CollectionName, StringValue: "Collection", SdkBinding: "SetCollectionNameArg"),
                new("Object", SdkValueKind.CollectionObjectName, CollectionObjectNameValue: new("Collection", "Object", "Point Group"), SdkBinding: "SetCollectionObjectNameArg2"),
                new("Objects", SdkValueKind.CollectionObjectNameList, CollectionObjectNameListValue: new([new("Collection", "Object", "Point Group")]), SdkBinding: "SetCollectionObjectNameRefListArg"),
                new("Vector Group", SdkValueKind.CollectionVectorGroupName, CollectionVectorGroupNameValue: new("Collection", "Vectors"), SdkBinding: "SetColVectorGroupNameArg"),
                new("Vector Groups", SdkValueKind.CollectionVectorGroupNameList, CollectionVectorGroupNameListValue: new([new("Collection", "Vectors")]), SdkBinding: "SetCollectionVectorGroupNameRefListArg"),
                new("Frame", SdkValueKind.FrameName, StringValue: "Frame A", SdkBinding: "SetFrameNameArg"),
                new("Points", SdkValueKind.PointNameList, PointNameListValue: new([new("Collection", "Group", "Point")]), SdkBinding: "SetPointNameRefListArg"),
                new("Strings", SdkValueKind.StringList, StringListValue: new(["A", "B"]), SdkBinding: "SetStringRefListArg"),
                new("Vector Group Name", SdkValueKind.VectorGroupName, StringValue: "Vectors", SdkBinding: "SetVectorGroupNameArg"),
                new("Vectors", SdkValueKind.VectorNameList, VectorNameListValue: new([new("Collection", "Vectors", "Vector")]), SdkBinding: "SetVectorNameRefListArg"),
                new("View", SdkValueKind.ViewName, StringValue: "View A", SdkBinding: "SetViewNameArg")
            ],
            [
                new("Instrument Result", SdkValueKind.CollectionInstrumentId, "GetColInstIdArg"),
                new("Instrument Results", SdkValueKind.CollectionInstrumentIdList, "GetColInstIdRefListArg"),
                new("Collection Result", SdkValueKind.CollectionName, "GetCollectionNameArg"),
                new("Object Result", SdkValueKind.CollectionObjectName, "GetCollectionObjectNameArg"),
                new("Object Results", SdkValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg"),
                new("Point Results", SdkValueKind.PointNameList, "GetPointNameRefListArg"),
                new("String Results", SdkValueKind.StringList, "GetStringRefListArg"),
                new("Vector Results", SdkValueKind.VectorNameList, "GetVectorNameRefListArg")
            ]);

        var result = adapter.Execute(command);

        Assert.True(result.MpResult.Succeeded);
        Assert.All(result.OutputValues, output => Assert.True(output.Retrieved));
        Assert.Equal(17, result.OutputValues[0].CollectionInstrumentIdValue!.InstrumentId);
        Assert.Equal(2, result.OutputValues[1].CollectionInstrumentIdListValue!.Values.Count);
        Assert.Equal(
            "Point Group",
            result.OutputValues[3].CollectionObjectNameValue!.ObjectType);
        Assert.Equal("Point Group", result.OutputValues[4].CollectionObjectNameListValue!.Values[0].ObjectType);
        Assert.Equal("Point B", result.OutputValues[5].PointNameListValue!.Values[1].TargetName);
        Assert.Equal(["A", "B"], result.OutputValues[6].StringListValue!.Values);
        Assert.Equal("Vector A", result.OutputValues[7].VectorNameListValue!.Values[0].VectorName);
        Assert.Equal(["Collection::17"], calls.ReferenceArguments["Instruments"]);
        Assert.Equal(["Collection::Group::Point"], calls.ReferenceArguments["Points"]);
        Assert.Contains("SetCollectionObjectNameArg2:Object", calls.Events);
        Assert.Contains("GetCollectionObjectNameArg:Object Result", calls.Events);
        Assert.True(calls.ReferenceGettersReceivedVariantWrapper);
    }

    [Fact]
    public void MalformedReferenceListOutputFailsAtomically()
    {
        using var calls = new RecordingSdkCalls { MalformedOutputName = "Points" };
        using var adapter = new SpatialAnalyzerSdkAdapter(calls);
        var command = new SdkCommand(
            "malformed-list",
            "Malformed List",
            [],
            [new SdkOutputArgument("Points", SdkValueKind.PointNameList, "GetPointNameRefListArg")]);

        var result = adapter.Execute(command);

        var output = Assert.Single(result.OutputValues);
        Assert.False(output.Retrieved);
        Assert.Null(output.PointNameListValue);
        Assert.Equal("sdk-output-retrieval-failed", result.DiagnosticCode);
    }

    [Fact]
    public void CollectionObjectOutputWithoutEmbeddedTypeIsNotRetrieved()
    {
        using var calls = new RecordingSdkCalls { MalformedOutputName = "Object" };
        using var adapter = new SpatialAnalyzerSdkAdapter(calls);
        var command = new SdkCommand(
            "malformed-object",
            "Malformed Object",
            [],
            [new SdkOutputArgument(
                "Object",
                SdkValueKind.CollectionObjectName,
                "GetCollectionObjectNameArg")]);

        var result = adapter.Execute(command);

        var output = Assert.Single(result.OutputValues);
        Assert.False(output.Retrieved);
        Assert.Null(output.CollectionObjectNameValue);
        Assert.Equal("sdk-output-retrieval-failed", result.DiagnosticCode);
    }

    [Fact]
    public void AmbiguousReferenceComponentIsRejectedBeforeSdkExecution()
    {
        using var calls = new RecordingSdkCalls();
        using var adapter = new SpatialAnalyzerSdkAdapter(calls);
        var command = new SdkCommand(
            "ambiguous-reference",
            "Ambiguous Reference",
            [new SdkInputArgument(
                "Points",
                SdkValueKind.PointNameList,
                PointNameListValue: new([new("Collection", "Group", "Point::Suffix")]),
                SdkBinding: "SetPointNameRefListArg")],
            []);

        var result = adapter.Execute(command);

        Assert.False(result.ExecuteStepReturned);
        Assert.Equal("sdk-argument-rejected", result.DiagnosticCode);
        Assert.DoesNotContain("ExecuteStep", calls.Events);
    }
    [Fact]
    public void SpecializedIdentifierCannotFallBackToGenericStringSetter()
    {
        using var calls = new RecordingSdkCalls();
        using var adapter = new SpatialAnalyzerSdkAdapter(calls);
        var command = new SdkCommand(
            "wrong-binding",
            "Wrong Binding",
            [new SdkInputArgument(
                "Chart",
                SdkValueKind.ChartName,
                StringValue: "Chart A",
                SdkBinding: "SetStringArg")],
            []);

        var result = adapter.Execute(command);

        Assert.False(result.ExecuteStepReturned);
        Assert.Equal("sdk-argument-rejected", result.DiagnosticCode);
        Assert.DoesNotContain(calls.Events, value => value.StartsWith("SetStringArg", StringComparison.Ordinal));
    }
    [Fact]
    public void ContainerValueFamiliesUseExactSdkBindingsAndRoundTripOutputs()
    {
        using var calls = new RecordingSdkCalls();
        using var adapter = new SpatialAnalyzerSdkAdapter(calls);
        var transform = Enumerable.Range(0, 16).Select(value => (double)value).ToArray();
        var command = new SdkCommand(
            "container-values",
            "Container Values",
            [
                new("Array", SdkValueKind.DoubleArray,
                    DoubleArrayValue: new([]),
                    SdkBinding: "SetDoubleArrayArg"),
                new("Edit", SdkValueKind.EditText,
                    StringListValue: new([]),
                    SdkBinding: "SetEditTextArg"),
                new("Transform", SdkValueKind.Transform,
                    TransformValue: new(transform),
                    SdkBinding: "SetTransformArg"),
                new("World", SdkValueKind.WorldTransform,
                    WorldTransformValue: new(new(transform), 0),
                    SdkBinding: "SetWorldTransformArg"),
                new("Color", SdkValueKind.RgbColor,
                    RgbColorValue: new(0, 127, 255),
                    SdkBinding: "SetColorArg"),
                new("File", SdkValueKind.FileReference,
                    FileReferenceValue: new("", false),
                    SdkBinding: "SetFilePathArg"),
                new("Angle", SdkValueKind.AngularUnit,
                    AngularUnitValue: SdkAngularUnitValue.DegreesMinutesSeconds,
                    SdkBinding: "SetAngularUnitsArg"),
                new("Distance", SdkValueKind.DistanceUnit,
                    DistanceUnitValue: SdkDistanceUnitValue.UsSurveyFeet,
                    SdkBinding: "SetDistanceUnitsArg"),
                new("Temperature", SdkValueKind.TemperatureUnit,
                    TemperatureUnitValue: SdkTemperatureUnitValue.Celsius,
                    SdkBinding: "SetTemperatureUnitsArg"),
                new("Font", SdkValueKind.Font,
                    FontValue: new("Segoe UI", 12, new(10, 20, 30)),
                    SdkBinding: "SetFontTypeArg")
            ],
            [
                new("Array Result", SdkValueKind.DoubleArray, "GetDoubleArrayArg"),
                new("Edit Result", SdkValueKind.EditText, "GetEditTextArg"),
                new("Transform Result", SdkValueKind.Transform, "GetTransformArg"),
                new("World Result", SdkValueKind.WorldTransform, "GetWorldTransformArg"),
                new("File Result", SdkValueKind.FileReference, "GetFilePathArg")
            ]);

        var result = adapter.Execute(command);

        Assert.True(result.MpResult.Succeeded);
        Assert.All(result.OutputValues, output => Assert.True(output.Retrieved));
        Assert.Empty(Assert.IsType<double[]>(calls.ContainerArguments["Array"]));
        Assert.Empty(Assert.IsType<object[]>(calls.ContainerArguments["Edit"]));
        var transformArgument = Assert.IsType<double[,]>(calls.ContainerArguments["Transform"]);
        Assert.Equal(15d, transformArgument[3, 3]);
        Assert.Equal(0, calls.ScaleArguments["World"]);
        Assert.Equal(((byte)0, (byte)127, byte.MaxValue), calls.ColorArguments["Color"]);
        Assert.Equal(("", false), calls.FileArguments["File"]);
        Assert.Equal("Deg:Min:Sec", calls.StringArguments["Angle"]);
        Assert.Equal("US Survey Feet", calls.StringArguments["Distance"]);
        Assert.Equal("Celsius", calls.StringArguments["Temperature"]);
        Assert.Equal(("Segoe UI", (byte)12, (byte)10, (byte)20, (byte)30), calls.FontArgument);
        Assert.Equal([1d, 2d, 3d], result.OutputValues[0].DoubleArrayValue!.Values);
        Assert.Equal(["A", "", "C"], result.OutputValues[1].StringListValue!.Values);
        Assert.Equal(15d, result.OutputValues[2].TransformValue!.Values[15]);
        Assert.Equal(2.5, result.OutputValues[3].WorldTransformValue!.ScaleFactor);
        Assert.Equal(@"C:\sensitive\model.xit", result.OutputValues[4].FileReferenceValue!.Path);
        Assert.True(result.OutputValues[4].FileReferenceValue!.EmbeddedFile);
        Assert.True(calls.ContainerGettersReceivedVariantWrapper);
    }

    [Theory]
    [InlineData("DoubleArray", "GetDoubleArrayArg")]
    [InlineData("EditText", "GetEditTextArg")]
    [InlineData("Transform", "GetTransformArg")]
    [InlineData("WorldTransform", "GetWorldTransformArg")]
    public void MalformedContainerOutputIsReportedAsRetrievalFailure(
        string kindName,
        string binding)
    {
        var kind = Enum.Parse<SdkValueKind>(kindName);
        using var calls = new RecordingSdkCalls { MalformedOutputName = "Result" };
        using var adapter = new SpatialAnalyzerSdkAdapter(calls);
        var command = new SdkCommand(
            "malformed-container",
            "Malformed Container",
            [],
            [new SdkOutputArgument("Result", kind, binding)]);

        var result = adapter.Execute(command);

        var output = Assert.Single(result.OutputValues);
        Assert.False(output.Retrieved);
        Assert.Equal("sdk-output-retrieval-failed", result.DiagnosticCode);
    }

    [Theory]
    [InlineData("DoubleArray", "GetDoubleArrayArg")]
    [InlineData("EditText", "GetEditTextArg")]
    [InlineData("Transform", "GetTransformArg")]
    [InlineData("WorldTransform", "GetWorldTransformArg")]
    [InlineData("FileReference", "GetFilePathArg")]
    public void ContainerGetterFailureDoesNotExposeDefaultLikeValues(
        string kindName,
        string binding)
    {
        using var calls = new RecordingSdkCalls { FailedOutputName = "Result" };
        using var adapter = new SpatialAnalyzerSdkAdapter(calls);
        var command = new SdkCommand(
            "failed-container-getter",
            "Failed Container Getter",
            [],
            [new SdkOutputArgument(
                "Result",
                Enum.Parse<SdkValueKind>(kindName),
                binding)]);

        var result = adapter.Execute(command);

        var output = Assert.Single(result.OutputValues);
        Assert.False(output.Retrieved);
        Assert.Null(output.DoubleArrayValue);
        Assert.Null(output.StringListValue);
        Assert.Null(output.TransformValue);
        Assert.Null(output.WorldTransformValue);
        Assert.Null(output.FileReferenceValue);
        Assert.Equal("sdk-output-retrieval-failed", result.DiagnosticCode);
    }

    [Fact]
    public void TransformWithWrongDimensionsIsRejectedBeforeExecution()
    {
        using var calls = new RecordingSdkCalls();
        using var adapter = new SpatialAnalyzerSdkAdapter(calls);
        var command = new SdkCommand(
            "bad-transform",
            "Bad Transform",
            [new SdkInputArgument(
                "Transform",
                SdkValueKind.Transform,
                TransformValue: new([1d, 2d]),
                SdkBinding: "SetTransformArg")],
            []);

        var result = adapter.Execute(command);

        Assert.False(result.ExecuteStepReturned);
        Assert.Equal("sdk-argument-rejected", result.DiagnosticCode);
        Assert.DoesNotContain("ExecuteStep", calls.Events);
    }
    private sealed class RecordingSdkCalls : ISpatialAnalyzerSdkCalls
    {
        public List<string> Events { get; } = [];

        public bool ExecuteStepReturned { get; init; } = true;

        public bool MpResultRetrieved { get; init; } = true;

        public int MpResultCode { get; init; } = 2;

        public bool ReferenceGettersReceivedVariantWrapper { get; private set; } = true;

        public bool ContainerGettersReceivedVariantWrapper { get; private set; } = true;

        public Dictionary<string, object> ContainerArguments { get; } = [];

        public Dictionary<string, string> StringArguments { get; } = [];

        public Dictionary<string, (byte Red, byte Green, byte Blue)> ColorArguments { get; } = [];

        public Dictionary<string, (string Path, bool Embedded)> FileArguments { get; } = [];

        public Dictionary<string, double> ScaleArguments { get; } = [];

        public (string Name, byte Size, byte Red, byte Green, byte Blue)? FontArgument { get; private set; }

        public string? FailedOutputName { get; init; }

        public string? MalformedOutputName { get; init; }

        public Dictionary<string, IReadOnlyList<string>> ReferenceArguments { get; } = [];

        public bool ConnectEx(string host, ref int statusCode) => true;

        public void SetStep(string stepName) => Events.Add($"SetStep:{stepName}");

        public bool SetBoolArg(string name, bool value) => RecordSetter("SetBoolArg", name);

        public bool SetIntegerArg(string name, int value) =>
            RecordSetter("SetIntegerArg", name);

        public bool SetDoubleArg(string name, double value) =>
            RecordSetter("SetDoubleArg", name);

        public bool SetStringArg(string name, string value) =>
            RecordSetter("SetStringArg", name);

        public bool SetPointNameArg(
            string name,
            string collectionName,
            string groupName,
            string targetName) =>
            RecordSetter("SetPointNameArg", name);

        public bool SetChartNameArg(string name, string chartName) =>
            RecordSetter("SetChartNameArg", name);

        public bool SetCloudNameArg(string name, string cloudName) =>
            RecordSetter("SetCloudNameArg", name);

        public bool SetColInstIdArg(string name, string collectionName, int instrumentId) =>
            RecordSetter("SetColInstIdArg", name);

        public bool SetColInstIdRefListArg(string name, ref object values) =>
            RecordReferenceSetter("SetColInstIdRefListArg", name, values);

        public bool SetColMachineIdArg(string name, string collectionName, int machineId) =>
            RecordSetter("SetColMachineIdArg", name);

        public bool SetCollectionGroupNameRefListArg(string name, ref object values) =>
            RecordReferenceSetter("SetCollectionGroupNameRefListArg", name, values);

        public bool SetCollectionNameArg(string name, string collectionName) =>
            RecordSetter("SetCollectionNameArg", name);

        public bool SetCollectionObjectNameArg2(
            string name,
            string collectionName,
            string objectName,
            string objectType) =>
            RecordSetter("SetCollectionObjectNameArg2", name);

        public bool SetCollectionObjectNameRefListArg(string name, ref object values) =>
            RecordReferenceSetter("SetCollectionObjectNameRefListArg", name, values);

        public bool SetCollectionVectorGroupNameRefListArg(string name, ref object values) =>
            RecordReferenceSetter("SetCollectionVectorGroupNameRefListArg", name, values);

        public bool SetColVectorGroupNameArg(
            string name,
            string collectionName,
            string vectorGroupName) =>
            RecordSetter("SetColVectorGroupNameArg", name);

        public bool SetFrameNameArg(string name, string frameName) =>
            RecordSetter("SetFrameNameArg", name);

        public bool SetPointNameRefListArg(string name, ref object values) =>
            RecordReferenceSetter("SetPointNameRefListArg", name, values);

        public bool SetStringRefListArg(string name, ref object values) =>
            RecordReferenceSetter("SetStringRefListArg", name, values);

        public bool SetVectorGroupNameArg(string name, string vectorGroupName) =>
            RecordSetter("SetVectorGroupNameArg", name);

        public bool SetVectorNameRefListArg(string name, ref object values) =>
            RecordReferenceSetter("SetVectorNameRefListArg", name, values);

        public bool SetViewNameArg(string name, string viewName) =>
            RecordSetter("SetViewNameArg", name);
        public bool SetVectorArg(string name, double x, double y, double z) =>
            RecordSetter("SetVectorArg", name);

        public bool SetToleranceVectorOptionsArg(
            string name,
            bool useHighX,
            double highX,
            bool useHighY,
            double highY,
            bool useHighZ,
            double highZ,
            bool useHighMagnitude,
            double highMagnitude,
            bool useLowX,
            double lowX,
            bool useLowY,
            double lowY,
            bool useLowZ,
            double lowZ,
            bool useLowMagnitude,
            double lowMagnitude) =>
            RecordSetter("SetToleranceVectorOptionsArg", name);

        public bool SetDoubleArrayArg(string name, int arraySize, ref object values)
        {
            var recorded = Unwrap(values);
            Assert.Equal(arraySize, Assert.IsType<double[]>(recorded).Length);
            ContainerArguments[name] = recorded;
            return RecordSetter("SetDoubleArrayArg", name);
        }

        public bool SetEditTextArg(string name, ref object values)
        {
            ContainerArguments[name] = Unwrap(values);
            return RecordSetter("SetEditTextArg", name);
        }

        public bool SetTransformArg(string name, ref object transform)
        {
            ContainerArguments[name] = Unwrap(transform);
            return RecordSetter("SetTransformArg", name);
        }

        public bool SetWorldTransformArg(
            string name,
            ref object transform,
            double scaleFactor)
        {
            ContainerArguments[name] = Unwrap(transform);
            ScaleArguments[name] = scaleFactor;
            return RecordSetter("SetWorldTransformArg", name);
        }

        public bool SetColorArg(string name, byte red, byte green, byte blue)
        {
            ColorArguments[name] = (red, green, blue);
            return RecordSetter("SetColorArg", name);
        }

        public bool SetFilePathArg(string name, string path, bool embeddedFile)
        {
            FileArguments[name] = (path, embeddedFile);
            return RecordSetter("SetFilePathArg", name);
        }

        public bool SetAngularUnitsArg(string name, string angularUnits) =>
            RecordStringSetter("SetAngularUnitsArg", name, angularUnits);

        public bool SetDistanceUnitsArg(string name, string distanceUnits) =>
            RecordStringSetter("SetDistanceUnitsArg", name, distanceUnits);

        public bool SetTemperatureUnitsArg(string name, string temperatureUnits) =>
            RecordStringSetter("SetTemperatureUnitsArg", name, temperatureUnits);

        public bool SetFontTypeArg(
            string name,
            string fontName,
            byte fontSize,
            byte red,
            byte green,
            byte blue)
        {
            FontArgument = (fontName, fontSize, red, green, blue);
            return RecordSetter("SetFontTypeArg", name);
        }

        public bool ExecuteStep()
        {
            Events.Add("ExecuteStep");
            return ExecuteStepReturned;
        }

        public bool GetMPStepResult(ref int resultCode)
        {
            Events.Add("GetMPStepResult");
            resultCode = MpResultCode;
            return MpResultRetrieved;
        }

        public bool GetBoolArg(string name, ref bool value)
        {
            Events.Add($"GetBoolArg:{name}");
            value = true;
            return true;
        }

        public bool GetIntegerArg(string name, ref int value)
        {
            Events.Add($"GetIntegerArg:{name}");
            value = 7;
            return true;
        }

        public bool GetDoubleArg(string name, ref double value)
        {
            Events.Add($"GetDoubleArg:{name}");
            value = 1.25;
            return true;
        }

        public bool GetStringArg(string name, ref string value)
        {
            Events.Add($"GetStringArg:{name}");
            var retrieved = name != FailedOutputName;
            if (retrieved)
            {
                value = "scripted-output";
            }

            return retrieved;
        }

        public bool GetPointNameArg(
            string name,
            ref string collectionName,
            ref string groupName,
            ref string targetName)
        {
            Events.Add($"GetPointNameArg:{name}");
            collectionName = "Collection";
            groupName = "Group";
            targetName = "Point";
            return true;
        }

        public bool GetColInstIdArg(
            string name,
            ref string collectionName,
            ref int instrumentId)
        {
            Events.Add($"GetColInstIdArg:{name}");
            collectionName = "Instruments";
            instrumentId = 17;
            return true;
        }

        public bool GetColInstIdRefListArg(string name, ref object values) =>
            ReturnReferenceList(name, "GetColInstIdRefListArg", ref values,
                "Instruments::17", "Instruments::18::Instrument");

        public bool GetCollectionNameArg(string name, ref string collectionName)
        {
            Events.Add($"GetCollectionNameArg:{name}");
            collectionName = "Collection";
            return true;
        }

        public bool GetCollectionObjectNameArg(
            string name,
            ref string collectionName,
            ref string objectName)
        {
            Events.Add($"GetCollectionObjectNameArg:{name}");
            collectionName = "Collection";
            objectName = name == MalformedOutputName
                ? "Object"
                : "Object,Point Group,";
            return true;
        }

        public bool GetCollectionObjectNameRefListArg(string name, ref object values) =>
            ReturnReferenceList(name, "GetCollectionObjectNameRefListArg", ref values,
                "Collection::Object::Point Group,");

        public bool GetPointNameRefListArg(string name, ref object values) =>
            ReturnReferenceList(name, "GetPointNameRefListArg", ref values,
                "Collection::Group::Point A", "Collection::Group::Point B");

        public bool GetStringRefListArg(string name, ref object values) =>
            ReturnReferenceList(name, "GetStringRefListArg", ref values, "A", "B");

        public bool GetVectorNameRefListArg(string name, ref object values) =>
            ReturnReferenceList(name, "GetVectorNameRefListArg", ref values,
                "Collection::Vectors::Vector A");
        public bool GetVectorArg(
            string name,
            ref double x,
            ref double y,
            ref double z)
        {
            Events.Add($"GetVectorArg:{name}");
            x = 1;
            y = 2;
            z = 3;
            return true;
        }

        public bool GetToleranceVectorOptionsArg(
            string name,
            ref bool useHighX,
            ref double highX,
            ref bool useHighY,
            ref double highY,
            ref bool useHighZ,
            ref double highZ,
            ref bool useHighMagnitude,
            ref double highMagnitude,
            ref bool useLowX,
            ref double lowX,
            ref bool useLowY,
            ref double lowY,
            ref bool useLowZ,
            ref double lowZ,
            ref bool useLowMagnitude,
            ref double lowMagnitude)
        {
            Events.Add($"GetToleranceVectorOptionsArg:{name}");
            useHighX = true;
            highX = 1;
            useHighY = true;
            highY = 2;
            useHighZ = true;
            highZ = 3;
            useHighMagnitude = true;
            highMagnitude = 4;
            useLowX = false;
            lowX = -1;
            useLowY = false;
            lowY = -2;
            useLowZ = false;
            lowZ = -3;
            useLowMagnitude = false;
            lowMagnitude = -4;
            return true;
        }

        public bool GetDoubleArrayArg(
            string name,
            ref int arraySize,
            ref object values)
        {
            ContainerGettersReceivedVariantWrapper &= values is VariantWrapper;
            Events.Add($"GetDoubleArrayArg:{name}");
            values = name == MalformedOutputName
                ? new double[] { 1, 2 }
                : new double[] { 1, 2, 3 };
            arraySize = 3;
            return name != FailedOutputName;
        }

        public bool GetEditTextArg(string name, ref object values)
        {
            ContainerGettersReceivedVariantWrapper &= values is VariantWrapper;
            Events.Add($"GetEditTextArg:{name}");
            values = name == MalformedOutputName
                ? new object[] { "A", 2 }
                : new object[] { "A", "", "C" };
            return name != FailedOutputName;
        }

        public bool GetTransformArg(string name, ref object transform)
        {
            ContainerGettersReceivedVariantWrapper &= transform is VariantWrapper;
            Events.Add($"GetTransformArg:{name}");
            transform = name == MalformedOutputName
                ? new double[3, 4]
                : CreateMatrix();
            return name != FailedOutputName;
        }

        public bool GetWorldTransformArg(
            string name,
            ref object transform,
            ref double scaleFactor)
        {
            ContainerGettersReceivedVariantWrapper &= transform is VariantWrapper;
            Events.Add($"GetWorldTransformArg:{name}");
            transform = name == MalformedOutputName
                ? new double[4, 3]
                : CreateMatrix();
            scaleFactor = 2.5;
            return name != FailedOutputName;
        }

        public bool GetFilePathArg(
            string name,
            ref string path,
            ref bool embeddedFile)
        {
            Events.Add($"GetFilePathArg:{name}");
            path = @"C:\sensitive\model.xit";
            embeddedFile = true;
            return name != FailedOutputName;
        }

        public void Dispose()
        {
        }

        private bool RecordReferenceSetter(string method, string name, object values)
        {
            Events.Add($"{method}:{name}");
            if (values is VariantWrapper wrapper)
            {
                values = wrapper.WrappedObject!;
            }

            ReferenceArguments[name] = ((IEnumerable)values).Cast<object>()
                .Select(value => Assert.IsType<string>(value))
                .ToArray();
            return true;
        }

        private bool ReturnReferenceList(
            string name,
            string method,
            ref object values,
            params string[] result)
        {
            Events.Add($"{method}:{name}");
            ReferenceGettersReceivedVariantWrapper &= values is VariantWrapper;
            if (name == MalformedOutputName)
            {
                values = new object[] { 42 };
                return true;
            }

            values = result.Cast<object>().ToArray();
            return name != FailedOutputName;
        }
        private bool RecordStringSetter(string method, string name, string value)
        {
            StringArguments[name] = value;
            return RecordSetter(method, name);
        }

        private static object Unwrap(object value) =>
            value is VariantWrapper wrapper ? wrapper.WrappedObject! : value;

        private static double[,] CreateMatrix()
        {
            var matrix = new double[4, 4];
            for (var index = 0; index < 16; index++)
            {
                matrix[index / 4, index % 4] = index;
            }

            return matrix;
        }

        private bool RecordSetter(string method, string name)
        {
            Events.Add($"{method}:{name}");
            return true;
        }
    }
}
#pragma warning restore CA1814
