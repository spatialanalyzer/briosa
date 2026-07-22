using Briosa.Worker.Sdk;

namespace Briosa.Worker.Tests;

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

    [Fact]
    public void MpFailureDoesNotAttemptResultOnlyArgumentGetters()
    {
        using var calls = new RecordingSdkCalls
        {
            MpSucceeded = false,
            MpResultCode = 42
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
        Assert.Equal(42, result.MpResult.ResultCode);
        Assert.Empty(result.OutputValues);
        Assert.Equal("mp-command-failed", result.DiagnosticCode);
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
    private sealed class RecordingSdkCalls : ISpatialAnalyzerSdkCalls
    {
        public List<string> Events { get; } = [];

        public bool MpSucceeded { get; init; } = true;

        public int MpResultCode { get; init; }

        public string? FailedOutputName { get; init; }

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

        public bool ExecuteStep()
        {
            Events.Add("ExecuteStep");
            return true;
        }

        public bool GetMPStepResult(ref int resultCode)
        {
            Events.Add("GetMPStepResult");
            resultCode = MpResultCode;
            return MpSucceeded;
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

        public void Dispose()
        {
        }

        private bool RecordSetter(string method, string name)
        {
            Events.Add($"{method}:{name}");
            return true;
        }
    }
}
