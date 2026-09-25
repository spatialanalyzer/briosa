using Briosa.Worker.Control;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ComSdk = Briosa.SpatialAnalyzer.Interop.ISpatialAnalyzerSDK;
using ComSdkClass = Briosa.SpatialAnalyzer.Interop.SpatialAnalyzerSDKClass;

namespace Briosa.Worker.Sdk;

/// <summary>
/// Adapts the generated SpatialAnalyzer COM interface to the worker-owned SDK boundary.
/// </summary>
internal sealed partial class SpatialAnalyzerSdkAdapter : ISpatialAnalyzerSdk
{
    private readonly ISpatialAnalyzerSdkProcessMonitor _processMonitor;
    private ISpatialAnalyzerSdkCalls? _sdk;

    internal SpatialAnalyzerSdkAdapter(ISpatialAnalyzerSdkCalls sdk)
        : this(sdk, AlwaysAliveSpatialAnalyzerSdkProcessMonitor.Instance)
    {
    }

    private SpatialAnalyzerSdkAdapter(
        ISpatialAnalyzerSdkCalls sdk,
        ISpatialAnalyzerSdkProcessMonitor processMonitor)
    {
        _sdk = sdk;
        _processMonitor = processMonitor;
    }

    public static ISpatialAnalyzerSdk Create()
    {
        var activation = SpatialAnalyzerSdkProcessMonitor.Activate(
            static () => new ComSdkCalls(new ComSdkClass()));
        return new SpatialAnalyzerSdkAdapter(
            activation.Sdk,
            activation.ProcessMonitor);
    }

    public SdkLivenessStatus GetLiveness() => _processMonitor.GetLiveness();
    public string? GetActivatedSdkVersion() => _processMonitor.GetVersion();

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

    public WorkerMpExecutionResult Execute(SdkCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        ObjectDisposedException.ThrowIf(_sdk is null, this);

        var started = Stopwatch.GetTimestamp();
        _sdk.SetStep(command.StepName);
        foreach (var argument in command.InputArguments)
        {
            if (!SetInputArgument(_sdk, argument))
            {
                return new WorkerArgumentsRejected(
                    (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                    "sdk-argument-rejected");
            }
        }

        var executeStepReturned = _sdk.ExecuteStep();
        var mpResultRetrieved = false;
        var resultCode = 0;
        if (executeStepReturned)
        {
            mpResultRetrieved = _sdk.GetMPStepResult(ref resultCode);
        }

        var mpSucceeded = mpResultRetrieved && resultCode == 2;
        IReadOnlyList<WorkerMpOutputValue> outputValues = [];
        var diagnosticCode = executeStepReturned switch
        {
            false => "execute-step-rejected",
            true when !mpResultRetrieved => "sdk-mp-result-retrieval-failed",
            true when !mpSucceeded => "mp-command-failed",
            _ => null
        };
        if (mpSucceeded)
        {
            outputValues = [.. command.OutputArguments.Select(argument => GetOutputValue(_sdk, argument))];
            if (outputValues.Any(output => !output.Retrieved))
            {
                diagnosticCode = outputValues
                    .First(output => !output.Retrieved)
                    .DiagnosticCode ?? "sdk-output-retrieval-failed";
            }
        }

        var durationMilliseconds = (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        if (!executeStepReturned) return new WorkerExecuteRejected(durationMilliseconds, diagnosticCode);
        if (!mpResultRetrieved) return new WorkerMpResultUnavailable(durationMilliseconds, diagnosticCode);
        return new WorkerMpResultAvailable(resultCode, durationMilliseconds, outputValues, diagnosticCode);
    }

    public void Dispose()
    {
        var sdk = _sdk;
        _sdk = null;
        try
        {
            sdk?.Dispose();
        }
        finally
        {
            _processMonitor.Dispose();
        }
    }

    private static bool SetInputArgument(ISpatialAnalyzerSdkCalls sdk, SdkInputArgument argument) =>
        HasExpectedBinding(argument.SdkBinding, ExpectedSetter(argument.Kind)) && argument.Kind switch
        {
            WorkerMpValueKind.Logical when argument.BooleanValue is { } value =>
                sdk.SetBoolArg(argument.Name, value),
            WorkerMpValueKind.WholeNumber when argument.IntegerValue is { } value =>
                sdk.SetIntegerArg(argument.Name, value),
            WorkerMpValueKind.FloatingPoint when argument.DoubleValue is { } value =>
                sdk.SetDoubleArg(argument.Name, value),
            WorkerMpValueKind.Text when argument.StringValue is { } value =>
                sdk.SetStringArg(argument.Name, value),
            WorkerMpValueKind.InstrumentTypeName when argument.StringValue is { } value &&
                Control.WorkerInstrumentTypeNames.IsSupported(value) =>
                sdk.SetInstTypeNameArg(argument.Name, value),
            WorkerMpValueKind.DoubleArray when argument.DoubleArrayValue is { } value =>
                SetDoubleArray(sdk, argument.Name, value),
            WorkerMpValueKind.EditText when argument.StringListValue is { } value =>
                SetEditText(sdk, argument.Name, value),
            WorkerMpValueKind.Transform when argument.TransformValue is { } value =>
                SetTransform(sdk, argument.Name, value),
            WorkerMpValueKind.WorldTransform when argument.WorldTransformValue is { } value =>
                SetWorldTransform(sdk, argument.Name, value),
            WorkerMpValueKind.RgbColor when argument.RgbColorValue is { } value =>
                sdk.SetColorArg(argument.Name, value.Red, value.Green, value.Blue),
            WorkerMpValueKind.FileReference when argument.FileReferenceValue is { } value =>
                sdk.SetFilePathArg(argument.Name, value.Path, value.EmbeddedFile),
            WorkerMpValueKind.AngularUnit when argument.AngularUnitValue is { } value =>
                SetAngularUnit(sdk, argument.Name, value),
            WorkerMpValueKind.DistanceUnit when argument.DistanceUnitValue is { } value =>
                SetDistanceUnit(sdk, argument.Name, value),
            WorkerMpValueKind.TemperatureUnit when argument.TemperatureUnitValue is { } value =>
                SetTemperatureUnit(sdk, argument.Name, value),
            WorkerMpValueKind.Font when argument.FontValue is { } value =>
                sdk.SetFontTypeArg(
                    argument.Name,
                    value.FontName,
                    value.Size,
                    value.Color.Red,
                    value.Color.Green,
                    value.Color.Blue),
            WorkerMpValueKind.ChartName when argument.StringValue is { } value =>
                sdk.SetChartNameArg(argument.Name, value),
            WorkerMpValueKind.CloudName when argument.StringValue is { } value =>
                sdk.SetCloudNameArg(argument.Name, value),
            WorkerMpValueKind.CollectionName when argument.StringValue is { } value =>
                sdk.SetCollectionNameArg(argument.Name, value),
            WorkerMpValueKind.FrameName when argument.StringValue is { } value =>
                sdk.SetFrameNameArg(argument.Name, value),
            WorkerMpValueKind.VectorGroupName when argument.StringValue is { } value =>
                sdk.SetVectorGroupNameArg(argument.Name, value),
            WorkerMpValueKind.ViewName when argument.StringValue is { } value =>
                sdk.SetViewNameArg(argument.Name, value),
            WorkerMpValueKind.PointName when argument.PointNameValue is { } value =>
                sdk.SetPointNameArg(
                    argument.Name,
                    value.CollectionName,
                    value.GroupName,
                    value.TargetName),
            WorkerMpValueKind.Vector when argument.VectorValue is { } value =>
                sdk.SetVectorArg(argument.Name, value.X, value.Y, value.Z),
            WorkerMpValueKind.ToleranceVectorOptions
                when argument.ToleranceVectorOptionsValue is { } value =>
                SetToleranceVectorOptions(sdk, argument.Name, value),
            WorkerMpValueKind.CollectionInstrumentId
                when argument.CollectionInstrumentIdValue is { } value =>
                sdk.SetColInstIdArg(argument.Name, value.CollectionName, value.InstrumentId),
            WorkerMpValueKind.CollectionInstrumentIdList
                when argument.CollectionInstrumentIdListValue is { } value =>
                SetCollectionInstrumentIdList(sdk, argument.Name, value),
            WorkerMpValueKind.CollectionMachineId
                when argument.CollectionMachineIdValue is { } value =>
                sdk.SetColMachineIdArg(argument.Name, value.CollectionName, value.MachineId),
            WorkerMpValueKind.CollectionItemName
                when argument.CollectionItemNameValue is { } value =>
                sdk.SetCollectionObjectNameArg2(
                    argument.Name,
                    value.CollectionName,
                    value.ItemName,
                    SdkSpecializedValueCodec.ToSdkString(value.ItemType)),
            WorkerMpValueKind.CollectionItemNameList
                when argument.CollectionItemNameListValue is { } value =>
                SetCollectionItemNameList(sdk, argument.Name, value),
            WorkerMpValueKind.CollectionObjectName
                when argument.CollectionObjectNameValue is { } value =>
                sdk.SetCollectionObjectNameArg2(
                    argument.Name,
                    value.CollectionName,
                    value.ObjectName,
                    SdkSpecializedValueCodec.ToSdkString(value.ObjectType)),
            WorkerMpValueKind.CollectionObjectNameList
                when argument.CollectionObjectNameListValue is { } value =>
                SetCollectionObjectNameList(sdk, argument.Name, value),
            WorkerMpValueKind.CollectionGroupNameList
                when argument.CollectionGroupNameListValue is { } value =>
                SetCollectionGroupNameList(sdk, argument.Name, value),
            WorkerMpValueKind.CollectionVectorGroupName
                when argument.CollectionVectorGroupNameValue is { } value =>
                sdk.SetColVectorGroupNameArg(
                    argument.Name,
                    value.CollectionName,
                    value.VectorGroupName),
            WorkerMpValueKind.CollectionVectorGroupNameList
                when argument.CollectionVectorGroupNameListValue is { } value =>
                SetCollectionVectorGroupNameList(sdk, argument.Name, value),
            WorkerMpValueKind.PointNameList when argument.PointNameListValue is { } value =>
                SetPointNameList(sdk, argument.Name, value),
            WorkerMpValueKind.StringList when argument.StringListValue is { } value =>
                SetStringList(sdk, argument.Name, value),
            WorkerMpValueKind.VectorNameList when argument.VectorNameListValue is { } value =>
                SetVectorNameList(sdk, argument.Name, value),
            _ => SetSpecializedInputArgument(sdk, argument)
        };

    private static WorkerMpOutputValue GetOutputValue(
        ISpatialAnalyzerSdkCalls sdk,
        WorkerMpOutputArgument argument) =>
        !HasExpectedBinding(argument.SdkBinding, ExpectedGetter(argument.Kind))
            ? new WorkerMpOutputValue(argument.Name, argument.Kind, Retrieved: false)
            : argument.Kind switch
            {
                WorkerMpValueKind.Logical => GetLogical(sdk, argument),
                WorkerMpValueKind.WholeNumber => GetWholeNumber(sdk, argument),
                WorkerMpValueKind.FloatingPoint => GetFloatingPoint(sdk, argument),
                WorkerMpValueKind.Text => GetText(sdk, argument),
                WorkerMpValueKind.DoubleArray => GetDoubleArray(sdk, argument),
                WorkerMpValueKind.EditText => GetEditText(sdk, argument),
                WorkerMpValueKind.Transform => GetTransform(sdk, argument),
                WorkerMpValueKind.WorldTransform => GetWorldTransform(sdk, argument),
                WorkerMpValueKind.FileReference => GetFileReference(sdk, argument),
                WorkerMpValueKind.PointName => GetPointName(sdk, argument),
                WorkerMpValueKind.Vector => GetVector(sdk, argument),
                WorkerMpValueKind.ToleranceVectorOptions =>
                    GetToleranceVectorOptions(sdk, argument),
                WorkerMpValueKind.CollectionInstrumentId =>
                    GetCollectionInstrumentId(sdk, argument),
                WorkerMpValueKind.CollectionInstrumentIdList =>
                    GetCollectionInstrumentIdList(sdk, argument),
                WorkerMpValueKind.CollectionName => GetNamedString(
                    sdk.GetCollectionNameArg,
                    argument),
                WorkerMpValueKind.CollectionItemName =>
                    GetCollectionItemName(sdk, argument),
                WorkerMpValueKind.CollectionItemNameList =>
                    GetCollectionItemNameList(sdk, argument),
                WorkerMpValueKind.CollectionObjectName =>
                    GetCollectionObjectName(sdk, argument),
                WorkerMpValueKind.CollectionObjectNameList =>
                    GetCollectionObjectNameList(sdk, argument),
                WorkerMpValueKind.PointNameList => GetPointNameList(sdk, argument),
                WorkerMpValueKind.StringList => GetStringList(sdk, argument),
                WorkerMpValueKind.VectorNameList => GetVectorNameList(sdk, argument),
                _ => GetSpecializedOutputValue(sdk, argument)
            };

    private static WorkerMpOutputValue GetLogical(ISpatialAnalyzerSdkCalls sdk, WorkerMpOutputArgument argument)
    {
        var value = false;
        var retrieved = sdk.GetBoolArg(argument.Name, ref value);
        return new WorkerMpOutputValue(
            argument.Name,
            argument.Kind,
            retrieved,
            BooleanValue: retrieved ? value : null);
    }

    private static WorkerMpOutputValue GetWholeNumber(ISpatialAnalyzerSdkCalls sdk, WorkerMpOutputArgument argument)
    {
        var value = 0;
        var retrieved = sdk.GetIntegerArg(argument.Name, ref value);
        return new WorkerMpOutputValue(
            argument.Name,
            argument.Kind,
            retrieved,
            IntegerValue: retrieved ? value : null);
    }

    private static WorkerMpOutputValue GetFloatingPoint(ISpatialAnalyzerSdkCalls sdk, WorkerMpOutputArgument argument)
    {
        var value = 0d;
        var retrieved = sdk.GetDoubleArg(argument.Name, ref value);
        return new WorkerMpOutputValue(
            argument.Name,
            argument.Kind,
            retrieved,
            DoubleValue: retrieved ? value : null);
    }

    private static WorkerMpOutputValue GetText(ISpatialAnalyzerSdkCalls sdk, WorkerMpOutputArgument argument)
    {
        var value = string.Empty;
        var retrieved = sdk.GetStringArg(argument.Name, ref value);
        return new WorkerMpOutputValue(
            argument.Name,
            argument.Kind,
            retrieved,
            StringValue: retrieved ? value : null);
    }

    private static WorkerMpOutputValue GetPointName(ISpatialAnalyzerSdkCalls sdk, WorkerMpOutputArgument argument)
    {
        var collectionName = string.Empty;
        var groupName = string.Empty;
        var targetName = string.Empty;
        var retrieved = sdk.GetPointNameArg(
            argument.Name,
            ref collectionName,
            ref groupName,
            ref targetName);
        return new WorkerMpOutputValue(
            argument.Name,
            argument.Kind,
            retrieved,
            PointNameValue: retrieved
                ? new WorkerPointNameValue(collectionName, groupName, targetName)
                : null);
    }

    private static WorkerMpOutputValue GetVector(ISpatialAnalyzerSdkCalls sdk, WorkerMpOutputArgument argument)
    {
        var x = 0d;
        var y = 0d;
        var z = 0d;
        var retrieved = sdk.GetVectorArg(argument.Name, ref x, ref y, ref z);
        return new WorkerMpOutputValue(
            argument.Name,
            argument.Kind,
            retrieved,
            VectorValue: retrieved ? new WorkerVectorValue(x, y, z) : null);
    }

    private static WorkerMpOutputValue GetToleranceVectorOptions(
        ISpatialAnalyzerSdkCalls sdk,
        WorkerMpOutputArgument argument)
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
        return new WorkerMpOutputValue(
            argument.Name,
            argument.Kind,
            retrieved,
            ToleranceVectorOptionsValue: retrieved
                ? new WorkerToleranceVectorOptionsValue(
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

    private static bool SetDoubleArray(
        ISpatialAnalyzerSdkCalls sdk,
        string name,
        WorkerDoubleArrayValue value)
    {
        var sdkValue = SdkContainerValueCodec.ToDoubleArrayComValue(value);
        return sdk.SetDoubleArrayArg(name, value.Values.Count, ref sdkValue);
    }

    private static bool SetEditText(
        ISpatialAnalyzerSdkCalls sdk,
        string name,
        WorkerStringListValue value)
    {
        var sdkValue = SdkContainerValueCodec.ToEditTextComValue(value);
        return sdk.SetEditTextArg(name, ref sdkValue);
    }

    private static bool SetTransform(
        ISpatialAnalyzerSdkCalls sdk,
        string name,
        WorkerTransformValue value)
    {
        try
        {
            var sdkValue = SdkContainerValueCodec.ToTransformComValue(value);
            return sdk.SetTransformArg(name, ref sdkValue);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool SetWorldTransform(
        ISpatialAnalyzerSdkCalls sdk,
        string name,
        WorkerWorldTransformValue value)
    {
        try
        {
            var sdkValue = SdkContainerValueCodec.ToTransformComValue(value.Transform);
            return sdk.SetWorldTransformArg(name, ref sdkValue, value.ScaleFactor);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool SetAngularUnit(
        ISpatialAnalyzerSdkCalls sdk,
        string name,
        WorkerAngularUnitValue value) =>
        AngularUnitSdkString(value) is { } sdkValue &&
        sdk.SetAngularUnitsArg(name, sdkValue);

    private static bool SetDistanceUnit(
        ISpatialAnalyzerSdkCalls sdk,
        string name,
        WorkerDistanceUnitValue value) =>
        DistanceUnitSdkString(value) is { } sdkValue &&
        sdk.SetDistanceUnitsArg(name, sdkValue);

    private static bool SetTemperatureUnit(
        ISpatialAnalyzerSdkCalls sdk,
        string name,
        WorkerTemperatureUnitValue value) =>
        TemperatureUnitSdkString(value) is { } sdkValue &&
        sdk.SetTemperatureUnitsArg(name, sdkValue);

    private static string? AngularUnitSdkString(WorkerAngularUnitValue value) =>
        value switch
        {
            WorkerAngularUnitValue.Degrees => "Degrees",
            WorkerAngularUnitValue.DegreesMinutesSeconds => "Deg:Min:Sec",
            WorkerAngularUnitValue.Radians => "Radians",
            WorkerAngularUnitValue.Milliradians => "Milliradians",
            WorkerAngularUnitValue.GonsGrad => "Gons/Grad",
            WorkerAngularUnitValue.Mils => "Mils",
            WorkerAngularUnitValue.Arcseconds => "Arcseconds",
            WorkerAngularUnitValue.DegreesMinutes => "Deg:Min",
            _ => null
        };

    private static string? DistanceUnitSdkString(WorkerDistanceUnitValue value) =>
        value switch
        {
            WorkerDistanceUnitValue.Meters => "Meters",
            WorkerDistanceUnitValue.Centimeters => "Centimeters",
            WorkerDistanceUnitValue.Millimeters => "Millimeters",
            WorkerDistanceUnitValue.Feet => "Feet",
            WorkerDistanceUnitValue.Inches => "Inches",
            WorkerDistanceUnitValue.UsSurveyFeet => "US Survey Feet",
            _ => null
        };

    private static string? TemperatureUnitSdkString(WorkerTemperatureUnitValue value) =>
        value switch
        {
            WorkerTemperatureUnitValue.Fahrenheit => "Fahrenheit",
            WorkerTemperatureUnitValue.Celsius => "Celsius",
            _ => null
        };

    private static WorkerMpOutputValue GetDoubleArray(
        ISpatialAnalyzerSdkCalls sdk,
        WorkerMpOutputArgument argument)
    {
        var size = argument.ArraySize ?? 0;
        var sdkValue = argument.ArraySize.HasValue
            ? SdkContainerValueCodec.DoubleArrayBuffer(size)
            : SdkContainerValueCodec.EmptyArrayBuffer();
        WorkerDoubleArrayValue? value = null;
        var retrieved = sdk.GetDoubleArrayArg(argument.Name, ref size, ref sdkValue) &&
            SdkContainerValueCodec.TryParseDoubleArray(sdkValue, size, out value);
        return new WorkerMpOutputValue(
            argument.Name,
            argument.Kind,
            retrieved,
            DoubleArrayValue: retrieved ? value : null);
    }

    private static WorkerMpOutputValue GetEditText(
        ISpatialAnalyzerSdkCalls sdk,
        WorkerMpOutputArgument argument)
    {
        var sdkValue = SdkContainerValueCodec.EmptyArrayBuffer();
        WorkerStringListValue? value = null;
        var retrieved = sdk.GetEditTextArg(argument.Name, ref sdkValue) &&
            SdkContainerValueCodec.TryParseEditText(sdkValue, out value);
        return new WorkerMpOutputValue(
            argument.Name,
            argument.Kind,
            retrieved,
            StringListValue: retrieved ? value : null);
    }

    private static WorkerMpOutputValue GetTransform(
        ISpatialAnalyzerSdkCalls sdk,
        WorkerMpOutputArgument argument)
    {
        var sdkValue = SdkContainerValueCodec.TransformBuffer();
        WorkerTransformValue? value = null;
        var retrieved = sdk.GetTransformArg(argument.Name, ref sdkValue) &&
            SdkContainerValueCodec.TryParseTransform(sdkValue, out value);
        return new WorkerMpOutputValue(
            argument.Name,
            argument.Kind,
            retrieved,
            TransformValue: retrieved ? value : null);
    }

    private static WorkerMpOutputValue GetWorldTransform(
        ISpatialAnalyzerSdkCalls sdk,
        WorkerMpOutputArgument argument)
    {
        var sdkValue = SdkContainerValueCodec.TransformBuffer();
        var scaleFactor = 0d;
        WorkerTransformValue? transform = null;
        var retrieved = sdk.GetWorldTransformArg(
            argument.Name,
            ref sdkValue,
            ref scaleFactor) &&
            SdkContainerValueCodec.TryParseTransform(sdkValue, out transform);
        return new WorkerMpOutputValue(
            argument.Name,
            argument.Kind,
            retrieved,
            WorldTransformValue: retrieved
                ? new WorkerWorldTransformValue(transform!, scaleFactor)
                : null);
    }

    private static WorkerMpOutputValue GetFileReference(
        ISpatialAnalyzerSdkCalls sdk,
        WorkerMpOutputArgument argument)
    {
        var path = string.Empty;
        var embeddedFile = false;
        var retrieved = sdk.GetFilePathArg(
            argument.Name,
            ref path,
            ref embeddedFile);
        return new WorkerMpOutputValue(
            argument.Name,
            argument.Kind,
            retrieved,
            FileReferenceValue: retrieved
                ? new WorkerFileReferenceValue(path, embeddedFile)
                : null);
    }

    private static bool SetCollectionInstrumentIdList(
        ISpatialAnalyzerSdkCalls sdk,
        string name,
        WorkerCollectionInstrumentIdListValue value) =>
        SetReferenceList(
            value.Values.Select(SdkReferenceListCodec.Format),
            (ref object values) => sdk.SetColInstIdRefListArg(name, ref values));

    private static bool SetCollectionGroupNameList(
        ISpatialAnalyzerSdkCalls sdk,
        string name,
        WorkerCollectionGroupNameListValue value) =>
        SetReferenceList(
            value.Values.Select(SdkReferenceListCodec.Format),
            (ref object values) => sdk.SetCollectionGroupNameRefListArg(name, ref values));

    private static bool SetCollectionItemNameList(
        ISpatialAnalyzerSdkCalls sdk,
        string name,
        WorkerCollectionItemNameListValue value) =>
        SetReferenceList(
            value.Values.Select(SdkReferenceListCodec.Format),
            (ref object values) => sdk.SetCollectionObjectNameRefListArg(name, ref values));

    private static bool SetCollectionObjectNameList(
        ISpatialAnalyzerSdkCalls sdk,
        string name,
        WorkerCollectionObjectNameListValue value) =>
        SetReferenceList(
            value.Values.Select(SdkReferenceListCodec.Format),
            (ref object values) => sdk.SetCollectionObjectNameRefListArg(name, ref values));

    private static bool SetCollectionVectorGroupNameList(
        ISpatialAnalyzerSdkCalls sdk,
        string name,
        WorkerCollectionVectorGroupNameListValue value) =>
        SetReferenceList(
            value.Values.Select(SdkReferenceListCodec.Format),
            (ref object values) => sdk.SetCollectionVectorGroupNameRefListArg(name, ref values));

    private static bool SetPointNameList(
        ISpatialAnalyzerSdkCalls sdk,
        string name,
        WorkerPointNameListValue value) =>
        SetReferenceList(
            value.Values.Select(SdkReferenceListCodec.Format),
            (ref object values) => sdk.SetPointNameRefListArg(name, ref values));

    private static bool SetStringList(
        ISpatialAnalyzerSdkCalls sdk,
        string name,
        WorkerStringListValue value) =>
        SetReferenceList(
            value.Values,
            (ref object values) => sdk.SetStringRefListArg(name, ref values));

    private static bool SetVectorNameList(
        ISpatialAnalyzerSdkCalls sdk,
        string name,
        WorkerVectorNameListValue value) =>
        SetReferenceList(
            value.Values.Select(SdkReferenceListCodec.Format),
            (ref object values) => sdk.SetVectorNameRefListArg(name, ref values));

    private static bool SetReferenceList(
        IEnumerable<string> values,
        ReferenceListCall call)
    {
        try
        {
            var sdkValue = SdkReferenceListCodec.ToComValue(values);
            return call(ref sdkValue);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static WorkerMpOutputValue GetCollectionInstrumentId(
        ISpatialAnalyzerSdkCalls sdk,
        WorkerMpOutputArgument argument)
    {
        var collectionName = string.Empty;
        var instrumentId = 0;
        var retrieved = sdk.GetColInstIdArg(
            argument.Name,
            ref collectionName,
            ref instrumentId);
        return new WorkerMpOutputValue(
            argument.Name,
            argument.Kind,
            retrieved,
            CollectionInstrumentIdValue: retrieved
                ? new WorkerCollectionInstrumentIdValue(collectionName, instrumentId)
                : null);
    }

    private static WorkerMpOutputValue GetCollectionItemName(
        ISpatialAnalyzerSdkCalls sdk,
        WorkerMpOutputArgument argument)
    {
        var collectionName = string.Empty;
        var itemName = string.Empty;
        var retrieved = sdk.GetCollectionObjectNameArg(
            argument.Name,
            ref collectionName,
            ref itemName);
        WorkerCollectionItemNameValue? parsed = null;
        retrieved = retrieved &&
            SdkReferenceListCodec.TryParseItemNameResult(
                collectionName,
                itemName,
                out parsed);
        return new WorkerMpOutputValue(
            argument.Name,
            argument.Kind,
            retrieved,
            CollectionItemNameValue: retrieved ? parsed : null);
    }

    private static WorkerMpOutputValue GetCollectionObjectName(
        ISpatialAnalyzerSdkCalls sdk,
        WorkerMpOutputArgument argument)
    {
        var collectionName = string.Empty;
        var objectName = string.Empty;
        var retrieved = sdk.GetCollectionObjectNameArg(
            argument.Name,
            ref collectionName,
            ref objectName);
        if (!retrieved)
        {
            return new WorkerMpOutputValue(
                argument.Name,
                argument.Kind,
                Retrieved: false,
                DiagnosticCode: "sdk-output-getter-rejected");
        }

        WorkerCollectionObjectNameValue? parsed = null;
        retrieved = SdkReferenceListCodec.TryParseObjectNameResult(
            collectionName,
            objectName,
            out parsed);
        if (!retrieved &&
            argument.ObjectTypeWhenOmitted is { } objectType &&
            !string.IsNullOrWhiteSpace(collectionName) &&
            !string.IsNullOrWhiteSpace(objectName) &&
            !objectName.Contains(',', StringComparison.Ordinal))
        {
            parsed = new WorkerCollectionObjectNameValue(
                collectionName,
                objectName,
                objectType);
            retrieved = true;
        }

        return new WorkerMpOutputValue(
            argument.Name,
            argument.Kind,
            retrieved,
            CollectionObjectNameValue: retrieved ? parsed : null,
            DiagnosticCode: retrieved
                ? null
                : CollectionObjectNameDiagnostic(collectionName, objectName));
    }

    private static string CollectionObjectNameDiagnostic(
        string collectionName,
        string objectName) =>
        string.IsNullOrWhiteSpace(collectionName)
            ? "sdk-output-collection-object-collection-missing"
            : string.IsNullOrWhiteSpace(objectName)
                ? "sdk-output-collection-object-name-missing"
                : !objectName.Contains(',', StringComparison.Ordinal)
                    ? "sdk-output-collection-object-type-omitted"
                    : "sdk-output-collection-object-type-unrecognized";

    private static WorkerMpOutputValue GetNamedString(
        NamedStringGetter getter,
        WorkerMpOutputArgument argument)
    {
        var value = string.Empty;
        var retrieved = getter(argument.Name, ref value);
        return new WorkerMpOutputValue(
            argument.Name,
            argument.Kind,
            retrieved,
            StringValue: retrieved ? value : null);
    }

    private static WorkerMpOutputValue GetCollectionInstrumentIdList(
        ISpatialAnalyzerSdkCalls sdk,
        WorkerMpOutputArgument argument) =>
        GetReferenceList<WorkerCollectionInstrumentIdListValue>(
            argument,
            sdk.GetColInstIdRefListArg,
            SdkReferenceListCodec.TryParseInstrumentIds,
            (name, kind, value) => new WorkerMpOutputValue(
                name,
                kind,
                true,
                CollectionInstrumentIdListValue: value));

    private static WorkerMpOutputValue GetCollectionItemNameList(
        ISpatialAnalyzerSdkCalls sdk,
        WorkerMpOutputArgument argument) =>
        GetReferenceList<WorkerCollectionItemNameListValue>(
            argument,
            sdk.GetCollectionObjectNameRefListArg,
            SdkReferenceListCodec.TryParseItemNames,
            (name, kind, value) => new WorkerMpOutputValue(
                name,
                kind,
                true,
                CollectionItemNameListValue: value));

    private static WorkerMpOutputValue GetCollectionObjectNameList(
        ISpatialAnalyzerSdkCalls sdk,
        WorkerMpOutputArgument argument) =>
        GetReferenceList<WorkerCollectionObjectNameListValue>(
            argument,
            sdk.GetCollectionObjectNameRefListArg,
            SdkReferenceListCodec.TryParseObjectNames,
            (name, kind, value) => new WorkerMpOutputValue(
                name,
                kind,
                true,
                CollectionObjectNameListValue: value));

    private static WorkerMpOutputValue GetPointNameList(
        ISpatialAnalyzerSdkCalls sdk,
        WorkerMpOutputArgument argument) =>
        GetReferenceList<WorkerPointNameListValue>(
            argument,
            sdk.GetPointNameRefListArg,
            SdkReferenceListCodec.TryParsePointNames,
            (name, kind, value) => new WorkerMpOutputValue(
                name,
                kind,
                true,
                PointNameListValue: value));

    private static WorkerMpOutputValue GetStringList(
        ISpatialAnalyzerSdkCalls sdk,
        WorkerMpOutputArgument argument) =>
        GetReferenceList<WorkerStringListValue>(
            argument,
            sdk.GetStringRefListArg,
            SdkReferenceListCodec.TryParseStrings,
            (name, kind, value) => new WorkerMpOutputValue(
                name,
                kind,
                true,
                StringListValue: value));

    private static WorkerMpOutputValue GetVectorNameList(
        ISpatialAnalyzerSdkCalls sdk,
        WorkerMpOutputArgument argument) =>
        GetReferenceList<WorkerVectorNameListValue>(
            argument,
            sdk.GetVectorNameRefListArg,
            SdkReferenceListCodec.TryParseVectorNames,
            (name, kind, value) => new WorkerMpOutputValue(
                name,
                kind,
                true,
                VectorNameListValue: value));

    private static WorkerMpOutputValue GetReferenceList<T>(
        WorkerMpOutputArgument argument,
        ReferenceListGetter getter,
        TryParseList<T> parser,
        Func<string, WorkerMpValueKind, T, WorkerMpOutputValue> create)
        where T : class
    {
        var value = SdkReferenceListCodec.ToComValue([]);
        if (!getter(argument.Name, ref value) || !parser(value, out var parsed) || parsed is null)
        {
            return new WorkerMpOutputValue(argument.Name, argument.Kind, Retrieved: false);
        }

        return create(argument.Name, argument.Kind, parsed);
    }

    private delegate bool ReferenceListCall(ref object values);

    private delegate bool ReferenceListGetter(string name, ref object values);

    private delegate bool NamedStringGetter(string name, ref string value);

    private delegate bool TryParseList<T>(object value, out T? result) where T : class;
    private static bool HasExpectedBinding(string? actual, string expected) =>
        actual is null || string.Equals(actual, expected, StringComparison.Ordinal);

    private static string ExpectedSetter(WorkerMpValueKind kind) =>
        kind switch
        {
            WorkerMpValueKind.Logical => "SetBoolArg",
            WorkerMpValueKind.WholeNumber => "SetIntegerArg",
            WorkerMpValueKind.FloatingPoint => "SetDoubleArg",
            WorkerMpValueKind.Text => "SetStringArg",
            WorkerMpValueKind.InstrumentTypeName => "SetInstTypeNameArg",
            WorkerMpValueKind.DoubleArray => "SetDoubleArrayArg",
            WorkerMpValueKind.EditText => "SetEditTextArg",
            WorkerMpValueKind.Transform => "SetTransformArg",
            WorkerMpValueKind.WorldTransform => "SetWorldTransformArg",
            WorkerMpValueKind.RgbColor => "SetColorArg",
            WorkerMpValueKind.FileReference => "SetFilePathArg",
            WorkerMpValueKind.AngularUnit => "SetAngularUnitsArg",
            WorkerMpValueKind.DistanceUnit => "SetDistanceUnitsArg",
            WorkerMpValueKind.TemperatureUnit => "SetTemperatureUnitsArg",
            WorkerMpValueKind.Font => "SetFontTypeArg",
            WorkerMpValueKind.PointName => "SetPointNameArg",
            WorkerMpValueKind.Vector => "SetVectorArg",
            WorkerMpValueKind.ToleranceVectorOptions => "SetToleranceVectorOptionsArg",
            WorkerMpValueKind.ChartName => "SetChartNameArg",
            WorkerMpValueKind.CloudName => "SetCloudNameArg",
            WorkerMpValueKind.CollectionGroupNameList => "SetCollectionGroupNameRefListArg",
            WorkerMpValueKind.CollectionInstrumentId => "SetColInstIdArg",
            WorkerMpValueKind.CollectionInstrumentIdList => "SetColInstIdRefListArg",
            WorkerMpValueKind.CollectionMachineId => "SetColMachineIdArg",
            WorkerMpValueKind.CollectionName => "SetCollectionNameArg",
            WorkerMpValueKind.CollectionItemName => "SetCollectionObjectNameArg2",
            WorkerMpValueKind.CollectionItemNameList => "SetCollectionObjectNameRefListArg",
            WorkerMpValueKind.CollectionObjectName => "SetCollectionObjectNameArg2",
            WorkerMpValueKind.CollectionObjectNameList => "SetCollectionObjectNameRefListArg",
            WorkerMpValueKind.CollectionVectorGroupName => "SetColVectorGroupNameArg",
            WorkerMpValueKind.CollectionVectorGroupNameList => "SetCollectionVectorGroupNameRefListArg",
            WorkerMpValueKind.FrameName => "SetFrameNameArg",
            WorkerMpValueKind.PointNameList => "SetPointNameRefListArg",
            WorkerMpValueKind.StringList => "SetStringRefListArg",
            WorkerMpValueKind.VectorGroupName => "SetVectorGroupNameArg",
            WorkerMpValueKind.VectorNameList => "SetVectorNameRefListArg",
            WorkerMpValueKind.ViewName => "SetViewNameArg",
            _ => SpecializedExpectedSetter(kind)
        };

    private static string ExpectedGetter(WorkerMpValueKind kind) =>
        kind switch
        {
            WorkerMpValueKind.Logical => "GetBoolArg",
            WorkerMpValueKind.WholeNumber => "GetIntegerArg",
            WorkerMpValueKind.FloatingPoint => "GetDoubleArg",
            WorkerMpValueKind.Text => "GetStringArg",
            WorkerMpValueKind.DoubleArray => "GetDoubleArrayArg",
            WorkerMpValueKind.EditText => "GetEditTextArg",
            WorkerMpValueKind.Transform => "GetTransformArg",
            WorkerMpValueKind.WorldTransform => "GetWorldTransformArg",
            WorkerMpValueKind.FileReference => "GetFilePathArg",
            WorkerMpValueKind.RgbColor => "GetColorArg",
            WorkerMpValueKind.AngularUnit => "GetAngularUnitsArg",
            WorkerMpValueKind.DistanceUnit => "GetDistanceUnitsArg",
            WorkerMpValueKind.TemperatureUnit => "GetTemperatureUnitsArg",
            WorkerMpValueKind.Font => "GetFontTypeArg",
            WorkerMpValueKind.PointName => "GetPointNameArg",
            WorkerMpValueKind.Vector => "GetVectorArg",
            WorkerMpValueKind.ToleranceVectorOptions => "GetToleranceVectorOptionsArg",
            WorkerMpValueKind.ChartName => "GetChartNameArg",
            WorkerMpValueKind.CloudName => "GetCloudNameArg",
            WorkerMpValueKind.CollectionGroupNameList => "GetCollectionGroupNameRefListArg",
            WorkerMpValueKind.CollectionInstrumentId => "GetColInstIdArg",
            WorkerMpValueKind.CollectionInstrumentIdList => "GetColInstIdRefListArg",
            WorkerMpValueKind.CollectionMachineId => "GetColMachineIdArg",
            WorkerMpValueKind.CollectionName => "GetCollectionNameArg",
            WorkerMpValueKind.CollectionItemName => "GetCollectionObjectNameArg",
            WorkerMpValueKind.CollectionItemNameList => "GetCollectionObjectNameRefListArg",
            WorkerMpValueKind.CollectionObjectName => "GetCollectionObjectNameArg",
            WorkerMpValueKind.CollectionObjectNameList => "GetCollectionObjectNameRefListArg",
            WorkerMpValueKind.CollectionVectorGroupName => "GetColVectorGroupNameArg",
            WorkerMpValueKind.CollectionVectorGroupNameList => "GetCollectionVectorGroupNameRefListArg",
            WorkerMpValueKind.FrameName => "GetFrameNameArg",
            WorkerMpValueKind.PointNameList => "GetPointNameRefListArg",
            WorkerMpValueKind.StringList => "GetStringRefListArg",
            WorkerMpValueKind.VectorGroupName => "GetVectorGroupNameArg",
            WorkerMpValueKind.VectorNameList => "GetVectorNameRefListArg",
            WorkerMpValueKind.ViewName => "GetViewNameArg",
            _ => SpecializedExpectedGetter(kind)
        };
    private static bool SetToleranceVectorOptions(
        ISpatialAnalyzerSdkCalls sdk,
        string name,
        WorkerToleranceVectorOptionsValue value) =>
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

        public WorkerToleranceLimit ToValue() => new(Enabled, Value);
    }
    private sealed partial class ComSdkCalls(ComSdk sdk) : ISpatialAnalyzerSdkCalls
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

        public bool SetChartNameArg(string name, string chartName) =>
            Sdk.SetChartNameArg(name, chartName);

        public bool SetCloudNameArg(string name, string cloudName) =>
            Sdk.SetCloudNameArg(name, cloudName);

        public bool SetColInstIdArg(string name, string collectionName, int instrumentId) =>
            Sdk.SetColInstIdArg(name, collectionName, instrumentId);

        public bool SetColInstIdRefListArg(string name, ref object values) =>
            Sdk.SetColInstIdRefListArg(name, ref values);

        public bool SetColMachineIdArg(string name, string collectionName, int machineId) =>
            Sdk.SetColMachineIdArg(name, collectionName, machineId);

        public bool SetCollectionGroupNameRefListArg(string name, ref object values) =>
            Sdk.SetCollectionGroupNameRefListArg(name, ref values);

        public bool SetCollectionNameArg(string name, string collectionName) =>
            Sdk.SetCollectionNameArg(name, collectionName);

        public bool SetCollectionObjectNameArg2(
            string name,
            string collectionName,
            string objectName,
            string objectType) =>
            Sdk.SetCollectionObjectNameArg2(name, collectionName, objectName, objectType);

        public bool SetCollectionObjectNameRefListArg(string name, ref object values) =>
            Sdk.SetCollectionObjectNameRefListArg(name, ref values);

        public bool SetCollectionVectorGroupNameRefListArg(string name, ref object values) =>
            Sdk.SetCollectionVectorGroupNameRefListArg(name, ref values);

        public bool SetColVectorGroupNameArg(
            string name,
            string collectionName,
            string vectorGroupName) =>
            Sdk.SetColVectorGroupNameArg(name, collectionName, vectorGroupName);

        public bool SetFrameNameArg(string name, string frameName) =>
            Sdk.SetFrameNameArg(name, frameName);

        public bool SetPointNameRefListArg(string name, ref object values) =>
            Sdk.SetPointNameRefListArg(name, ref values);

        public bool SetStringRefListArg(string name, ref object values) =>
            Sdk.SetStringRefListArg(name, ref values);

        public bool SetVectorGroupNameArg(string name, string vectorGroupName) =>
            Sdk.SetVectorGroupNameArg(name, vectorGroupName);

        public bool SetVectorNameRefListArg(string name, ref object values) =>
            Sdk.SetVectorNameRefListArg(name, ref values);

        public bool SetViewNameArg(string name, string viewName) =>
            Sdk.SetViewNameArg(name, viewName);
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

        public bool SetDoubleArrayArg(string name, int arraySize, ref object values) =>
            Sdk.SetDoubleArrayArg(name, arraySize, ref values);

        public bool SetEditTextArg(string name, ref object values) =>
            Sdk.SetEditTextArg(name, ref values);

        public bool SetTransformArg(string name, ref object transform) =>
            Sdk.SetTransformArg(name, ref transform);

        public bool SetWorldTransformArg(
            string name,
            ref object transform,
            double scaleFactor) =>
            Sdk.SetWorldTransformArg(name, ref transform, scaleFactor);

        public bool SetColorArg(string name, byte red, byte green, byte blue) =>
            Sdk.SetColorArg(name, red, green, blue);

        public bool SetFilePathArg(string name, string path, bool embeddedFile) =>
            Sdk.SetFilePathArg(name, path, embeddedFile);

        public bool SetAngularUnitsArg(string name, string angularUnits) =>
            Sdk.SetAngularUnitsArg(name, angularUnits);

        public bool SetDistanceUnitsArg(string name, string distanceUnits) =>
            Sdk.SetDistanceUnitsArg(name, distanceUnits);

        public bool SetTemperatureUnitsArg(string name, string temperatureUnits) =>
            Sdk.SetTemperatureUnitsArg(name, temperatureUnits);

        public bool SetFontTypeArg(
            string name,
            string fontName,
            byte fontSize,
            byte red,
            byte green,
            byte blue) =>
            Sdk.SetFontTypeArg(name, fontName, fontSize, red, green, blue);

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

        public bool GetColInstIdArg(
            string name,
            ref string collectionName,
            ref int instrumentId) =>
            Sdk.GetColInstIdArg(name, ref collectionName, ref instrumentId);

        public bool GetColInstIdRefListArg(string name, ref object values) =>
            Sdk.GetColInstIdRefListArg(name, ref values);

        public bool GetCollectionNameArg(string name, ref string collectionName) =>
            Sdk.GetCollectionNameArg(name, ref collectionName);

        public bool GetCollectionObjectNameArg(
            string name,
            ref string collectionName,
            ref string objectName) =>
            Sdk.GetCollectionObjectNameArg(name, ref collectionName, ref objectName);

        public bool GetCollectionObjectNameRefListArg(string name, ref object values) =>
            Sdk.GetCollectionObjectNameRefListArg(name, ref values);

        public bool GetPointNameRefListArg(string name, ref object values) =>
            Sdk.GetPointNameRefListArg(name, ref values);

        public bool GetStringRefListArg(string name, ref object values) =>
            Sdk.GetStringRefListArg(name, ref values);

        public bool GetVectorNameRefListArg(string name, ref object values) =>
            Sdk.GetVectorNameRefListArg(name, ref values);
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

        public bool GetDoubleArrayArg(
            string name,
            ref int arraySize,
            ref object values) =>
            Sdk.GetDoubleArrayArg(name, ref arraySize, ref values);

        public bool GetEditTextArg(string name, ref object values) =>
            Sdk.GetEditTextArg(name, ref values);

        public bool GetTransformArg(string name, ref object transform) =>
            Sdk.GetTransformArg(name, ref transform);

        public bool GetWorldTransformArg(
            string name,
            ref object transform,
            ref double scaleFactor) =>
            Sdk.GetWorldTransformArg(name, ref transform, ref scaleFactor);

        public bool GetFilePathArg(
            string name,
            ref string path,
            ref bool embeddedFile) =>
            Sdk.GetFilePathArg(name, ref path, ref embeddedFile);

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
