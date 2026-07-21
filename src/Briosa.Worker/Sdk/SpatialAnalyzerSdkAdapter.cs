using System.Diagnostics;
using System.Runtime.InteropServices;
using ComSdk = Briosa.SpatialAnalyzer.Interop.ISpatialAnalyzerSDK;
using ComSdkClass = Briosa.SpatialAnalyzer.Interop.SpatialAnalyzerSDKClass;

namespace Briosa.Worker.Sdk;

/// <summary>
/// Adapts the generated SpatialAnalyzer COM interface to the worker-owned SDK boundary.
/// </summary>
internal sealed class SpatialAnalyzerSdkAdapter : ISpatialAnalyzerSdk
{
    private ISpatialAnalyzerSdkCalls? _sdk;

    internal SpatialAnalyzerSdkAdapter(ISpatialAnalyzerSdkCalls sdk)
    {
        _sdk = sdk;
    }

    public static ISpatialAnalyzerSdk Create() =>
        new SpatialAnalyzerSdkAdapter(new ComSdkCalls(new ComSdkClass()));

    public SdkConnectionResult Connect(string host)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        ObjectDisposedException.ThrowIf(_sdk is null, this);

        var statusCode = 0;
        var connected = _sdk.ConnectEx(host, ref statusCode);
        return connected
            ? new SdkConnectionResult(SdkConnectionStatus.Connected, statusCode, null)
            : new SdkConnectionResult(
                SdkConnectionStatus.Unavailable,
                statusCode,
                "connect-ex-unavailable");
    }

    public SdkExecutionResult Execute(SdkCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        ObjectDisposedException.ThrowIf(_sdk is null, this);

        var started = Stopwatch.GetTimestamp();
        _sdk.SetStep(command.StepName);
        foreach (var argument in command.InputArguments)
        {
            if (!SetInputArgument(_sdk, argument))
            {
                return new SdkExecutionResult(
                    ExecuteStepReturned: false,
                    new SdkMpResult(false, -1, "sdk-argument-rejected"),
                    Stopwatch.GetElapsedTime(started),
                    OutputValues: [],
                    "sdk-argument-rejected");
            }
        }

        var executeStepReturned = _sdk.ExecuteStep();
        var resultCode = 0;
        var mpSucceeded = _sdk.GetMPStepResult(ref resultCode);
        IReadOnlyList<SdkOutputValue> outputValues = [];
        var diagnosticCode = mpSucceeded ? null : "mp-command-failed";
        if (executeStepReturned && mpSucceeded)
        {
            outputValues = [.. command.OutputArguments.Select(argument => GetOutputValue(_sdk, argument))];
            if (outputValues.Any(output => !output.Retrieved))
            {
                diagnosticCode = "sdk-output-retrieval-failed";
            }
        }
        else if (!executeStepReturned)
        {
            diagnosticCode = "execute-step-rejected";
        }

        return new SdkExecutionResult(
            executeStepReturned,
            new SdkMpResult(
                mpSucceeded,
                resultCode,
                mpSucceeded ? null : "mp-command-failed"),
            Stopwatch.GetElapsedTime(started),
            outputValues,
            diagnosticCode);
    }

    public void Dispose()
    {
        var sdk = _sdk;
        _sdk = null;
        sdk?.Dispose();
    }

    private static bool SetInputArgument(ISpatialAnalyzerSdkCalls sdk, SdkInputArgument argument) =>
        argument.Kind switch
        {
            SdkValueKind.Logical when argument.BooleanValue is { } value =>
                sdk.SetBoolArg(argument.Name, value),
            SdkValueKind.WholeNumber when argument.IntegerValue is { } value =>
                sdk.SetIntegerArg(argument.Name, value),
            SdkValueKind.FloatingPoint when argument.DoubleValue is { } value =>
                sdk.SetDoubleArg(argument.Name, value),
            SdkValueKind.Text when argument.StringValue is { } value =>
                sdk.SetStringArg(argument.Name, value),
            SdkValueKind.PointName when argument.PointNameValue is { } value =>
                sdk.SetPointNameArg(
                    argument.Name,
                    value.CollectionName,
                    value.GroupName,
                    value.TargetName),
            SdkValueKind.Vector when argument.VectorValue is { } value =>
                sdk.SetVectorArg(argument.Name, value.X, value.Y, value.Z),
            SdkValueKind.ToleranceVectorOptions
                when argument.ToleranceVectorOptionsValue is { } value =>
                SetToleranceVectorOptions(sdk, argument.Name, value),
            _ => false
        };

    private static SdkOutputValue GetOutputValue(
        ISpatialAnalyzerSdkCalls sdk,
        SdkOutputArgument argument) =>
        argument.Kind switch
        {
            SdkValueKind.Logical => GetLogical(sdk, argument),
            SdkValueKind.WholeNumber => GetWholeNumber(sdk, argument),
            SdkValueKind.FloatingPoint => GetFloatingPoint(sdk, argument),
            SdkValueKind.Text => GetText(sdk, argument),
            SdkValueKind.PointName => GetPointName(sdk, argument),
            SdkValueKind.Vector => GetVector(sdk, argument),
            SdkValueKind.ToleranceVectorOptions =>
                GetToleranceVectorOptions(sdk, argument),
            _ => new SdkOutputValue(argument.Name, argument.Kind, Retrieved: false)
        };

    private static SdkOutputValue GetLogical(ISpatialAnalyzerSdkCalls sdk, SdkOutputArgument argument)
    {
        var value = false;
        var retrieved = sdk.GetBoolArg(argument.Name, ref value);
        return new SdkOutputValue(
            argument.Name,
            argument.Kind,
            retrieved,
            BooleanValue: retrieved ? value : null);
    }

    private static SdkOutputValue GetWholeNumber(ISpatialAnalyzerSdkCalls sdk, SdkOutputArgument argument)
    {
        var value = 0;
        var retrieved = sdk.GetIntegerArg(argument.Name, ref value);
        return new SdkOutputValue(
            argument.Name,
            argument.Kind,
            retrieved,
            IntegerValue: retrieved ? value : null);
    }

    private static SdkOutputValue GetFloatingPoint(ISpatialAnalyzerSdkCalls sdk, SdkOutputArgument argument)
    {
        var value = 0d;
        var retrieved = sdk.GetDoubleArg(argument.Name, ref value);
        return new SdkOutputValue(
            argument.Name,
            argument.Kind,
            retrieved,
            DoubleValue: retrieved ? value : null);
    }

    private static SdkOutputValue GetText(ISpatialAnalyzerSdkCalls sdk, SdkOutputArgument argument)
    {
        var value = string.Empty;
        var retrieved = sdk.GetStringArg(argument.Name, ref value);
        return new SdkOutputValue(
            argument.Name,
            argument.Kind,
            retrieved,
            StringValue: retrieved ? value : null);
    }

    private static SdkOutputValue GetPointName(ISpatialAnalyzerSdkCalls sdk, SdkOutputArgument argument)
    {
        var collectionName = string.Empty;
        var groupName = string.Empty;
        var targetName = string.Empty;
        var retrieved = sdk.GetPointNameArg(
            argument.Name,
            ref collectionName,
            ref groupName,
            ref targetName);
        return new SdkOutputValue(
            argument.Name,
            argument.Kind,
            retrieved,
            PointNameValue: retrieved
                ? new SdkPointNameValue(collectionName, groupName, targetName)
                : null);
    }

    private static SdkOutputValue GetVector(ISpatialAnalyzerSdkCalls sdk, SdkOutputArgument argument)
    {
        var x = 0d;
        var y = 0d;
        var z = 0d;
        var retrieved = sdk.GetVectorArg(argument.Name, ref x, ref y, ref z);
        return new SdkOutputValue(
            argument.Name,
            argument.Kind,
            retrieved,
            VectorValue: retrieved ? new SdkVectorValue(x, y, z) : null);
    }

    private static SdkOutputValue GetToleranceVectorOptions(
        ISpatialAnalyzerSdkCalls sdk,
        SdkOutputArgument argument)
    {
        var highX = new MutableToleranceLimit();
        var highY = new MutableToleranceLimit();
        var highZ = new MutableToleranceLimit();
        var highMagnitude = new MutableToleranceLimit();
        var lowX = new MutableToleranceLimit();
        var lowY = new MutableToleranceLimit();
        var lowZ = new MutableToleranceLimit();
        var lowMagnitude = new MutableToleranceLimit();
        var retrieved = sdk.GetToleranceVectorOptionsArg(
            argument.Name,
            ref highX.Enabled,
            ref highX.Value,
            ref highY.Enabled,
            ref highY.Value,
            ref highZ.Enabled,
            ref highZ.Value,
            ref highMagnitude.Enabled,
            ref highMagnitude.Value,
            ref lowX.Enabled,
            ref lowX.Value,
            ref lowY.Enabled,
            ref lowY.Value,
            ref lowZ.Enabled,
            ref lowZ.Value,
            ref lowMagnitude.Enabled,
            ref lowMagnitude.Value);
        return new SdkOutputValue(
            argument.Name,
            argument.Kind,
            retrieved,
            ToleranceVectorOptionsValue: retrieved
                ? new SdkToleranceVectorOptionsValue(
                    highX.ToValue(),
                    highY.ToValue(),
                    highZ.ToValue(),
                    highMagnitude.ToValue(),
                    lowX.ToValue(),
                    lowY.ToValue(),
                    lowZ.ToValue(),
                    lowMagnitude.ToValue())
                : null);
    }

    private static bool SetToleranceVectorOptions(
        ISpatialAnalyzerSdkCalls sdk,
        string name,
        SdkToleranceVectorOptionsValue value) =>
        sdk.SetToleranceVectorOptionsArg(
            name,
            value.HighX.Enabled,
            value.HighX.Value,
            value.HighY.Enabled,
            value.HighY.Value,
            value.HighZ.Enabled,
            value.HighZ.Value,
            value.HighMagnitude.Enabled,
            value.HighMagnitude.Value,
            value.LowX.Enabled,
            value.LowX.Value,
            value.LowY.Enabled,
            value.LowY.Value,
            value.LowZ.Enabled,
            value.LowZ.Value,
            value.LowMagnitude.Enabled,
            value.LowMagnitude.Value);

    private sealed class MutableToleranceLimit
    {
        public bool Enabled;

        public double Value;

        public SdkToleranceLimit ToValue() => new(Enabled, Value);
    }
    private sealed class ComSdkCalls(ComSdk sdk) : ISpatialAnalyzerSdkCalls
    {
        private ComSdk? _sdk = sdk;

        private ComSdk Sdk => _sdk ?? throw new ObjectDisposedException(nameof(ComSdkCalls));

        public bool ConnectEx(string host, ref int statusCode) =>
            Sdk.ConnectEx(host, ref statusCode);

        public void SetStep(string stepName) => Sdk.SetStep(stepName);

        public bool SetBoolArg(string name, bool value) => Sdk.SetBoolArg(name, value);

        public bool SetIntegerArg(string name, int value) =>
            Sdk.SetIntegerArg(name, value);

        public bool SetDoubleArg(string name, double value) =>
            Sdk.SetDoubleArg(name, value);

        public bool SetStringArg(string name, string value) =>
            Sdk.SetStringArg(name, value);

        public bool SetPointNameArg(
            string name,
            string collectionName,
            string groupName,
            string targetName) =>
            Sdk.SetPointNameArg(name, collectionName, groupName, targetName);

        public bool SetVectorArg(string name, double x, double y, double z) =>
            Sdk.SetVectorArg(name, x, y, z);

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
            Sdk.SetToleranceVectorOptionsArg(
                name,
                useHighX,
                highX,
                useHighY,
                highY,
                useHighZ,
                highZ,
                useHighMagnitude,
                highMagnitude,
                useLowX,
                lowX,
                useLowY,
                lowY,
                useLowZ,
                lowZ,
                useLowMagnitude,
                lowMagnitude);

        public bool ExecuteStep() => Sdk.ExecuteStep();

        public bool GetMPStepResult(ref int resultCode) =>
            Sdk.GetMPStepResult(ref resultCode);

        public bool GetBoolArg(string name, ref bool value) =>
            Sdk.GetBoolArg(name, ref value);

        public bool GetIntegerArg(string name, ref int value) =>
            Sdk.GetIntegerArg(name, ref value);

        public bool GetDoubleArg(string name, ref double value) =>
            Sdk.GetDoubleArg(name, ref value);

        public bool GetStringArg(string name, ref string value) =>
            Sdk.GetStringArg(name, ref value);

        public bool GetPointNameArg(
            string name,
            ref string collectionName,
            ref string groupName,
            ref string targetName) =>
            Sdk.GetPointNameArg(
                name,
                ref collectionName,
                ref groupName,
                ref targetName);

        public bool GetVectorArg(
            string name,
            ref double x,
            ref double y,
            ref double z) =>
            Sdk.GetVectorArg(name, ref x, ref y, ref z);

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
            ref double lowMagnitude) =>
            Sdk.GetToleranceVectorOptionsArg(
                name,
                ref useHighX,
                ref highX,
                ref useHighY,
                ref highY,
                ref useHighZ,
                ref highZ,
                ref useHighMagnitude,
                ref highMagnitude,
                ref useLowX,
                ref lowX,
                ref useLowY,
                ref lowY,
                ref useLowZ,
                ref lowZ,
                ref useLowMagnitude,
                ref lowMagnitude);

        public void Dispose()
        {
            var sdk = _sdk;
            _sdk = null;
            if (sdk is not null && Marshal.IsComObject(sdk))
            {
                _ = Marshal.FinalReleaseComObject(sdk);
            }
        }
    }
}
