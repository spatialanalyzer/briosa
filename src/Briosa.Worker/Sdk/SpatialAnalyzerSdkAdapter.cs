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
                    new SdkMpResult(
                        Retrieved: false,
                        Succeeded: false,
                        ResultCode: null,
                        "sdk-argument-rejected"),
                    Stopwatch.GetElapsedTime(started),
                    OutputValues: [],
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
        IReadOnlyList<SdkOutputValue> outputValues = [];
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
                diagnosticCode = "sdk-output-retrieval-failed";
            }
        }

        return new SdkExecutionResult(
            executeStepReturned,
            new SdkMpResult(
                mpResultRetrieved,
                mpSucceeded,
                mpResultRetrieved ? resultCode : null,
                diagnosticCode),
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
        HasExpectedBinding(argument.SdkBinding, ExpectedSetter(argument.Kind)) && argument.Kind switch
        {
            SdkValueKind.Logical when argument.BooleanValue is { } value =>
                sdk.SetBoolArg(argument.Name, value),
            SdkValueKind.WholeNumber when argument.IntegerValue is { } value =>
                sdk.SetIntegerArg(argument.Name, value),
            SdkValueKind.FloatingPoint when argument.DoubleValue is { } value =>
                sdk.SetDoubleArg(argument.Name, value),
            SdkValueKind.Text when argument.StringValue is { } value =>
                sdk.SetStringArg(argument.Name, value),
            SdkValueKind.DoubleArray when argument.DoubleArrayValue is { } value =>
                SetDoubleArray(sdk, argument.Name, value),
            SdkValueKind.EditText when argument.StringListValue is { } value =>
                SetEditText(sdk, argument.Name, value),
            SdkValueKind.Transform when argument.TransformValue is { } value =>
                SetTransform(sdk, argument.Name, value),
            SdkValueKind.WorldTransform when argument.WorldTransformValue is { } value =>
                SetWorldTransform(sdk, argument.Name, value),
            SdkValueKind.RgbColor when argument.RgbColorValue is { } value =>
                sdk.SetColorArg(argument.Name, value.Red, value.Green, value.Blue),
            SdkValueKind.FileReference when argument.FileReferenceValue is { } value =>
                sdk.SetFilePathArg(argument.Name, value.Path, value.EmbeddedFile),
            SdkValueKind.AngularUnit when argument.AngularUnitValue is { } value =>
                SetAngularUnit(sdk, argument.Name, value),
            SdkValueKind.DistanceUnit when argument.DistanceUnitValue is { } value =>
                SetDistanceUnit(sdk, argument.Name, value),
            SdkValueKind.TemperatureUnit when argument.TemperatureUnitValue is { } value =>
                SetTemperatureUnit(sdk, argument.Name, value),
            SdkValueKind.Font when argument.FontValue is { } value =>
                sdk.SetFontTypeArg(
                    argument.Name,
                    value.FontName,
                    value.Size,
                    value.Color.Red,
                    value.Color.Green,
                    value.Color.Blue),
            SdkValueKind.ChartName when argument.StringValue is { } value =>
                sdk.SetChartNameArg(argument.Name, value),
            SdkValueKind.CloudName when argument.StringValue is { } value =>
                sdk.SetCloudNameArg(argument.Name, value),
            SdkValueKind.CollectionName when argument.StringValue is { } value =>
                sdk.SetCollectionNameArg(argument.Name, value),
            SdkValueKind.FrameName when argument.StringValue is { } value =>
                sdk.SetFrameNameArg(argument.Name, value),
            SdkValueKind.VectorGroupName when argument.StringValue is { } value =>
                sdk.SetVectorGroupNameArg(argument.Name, value),
            SdkValueKind.ViewName when argument.StringValue is { } value =>
                sdk.SetViewNameArg(argument.Name, value),
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
            SdkValueKind.CollectionInstrumentId
                when argument.CollectionInstrumentIdValue is { } value =>
                sdk.SetColInstIdArg(argument.Name, value.CollectionName, value.InstrumentId),
            SdkValueKind.CollectionInstrumentIdList
                when argument.CollectionInstrumentIdListValue is { } value =>
                SetCollectionInstrumentIdList(sdk, argument.Name, value),
            SdkValueKind.CollectionMachineId
                when argument.CollectionMachineIdValue is { } value =>
                sdk.SetColMachineIdArg(argument.Name, value.CollectionName, value.MachineId),
            SdkValueKind.CollectionItemName
                when argument.CollectionItemNameValue is { } value =>
                sdk.SetCollectionObjectNameArg2(
                    argument.Name,
                    value.CollectionName,
                    value.ItemName,
                    SdkSpecializedValueCodec.ToSdkString(value.ItemType)),
            SdkValueKind.CollectionItemNameList
                when argument.CollectionItemNameListValue is { } value =>
                SetCollectionItemNameList(sdk, argument.Name, value),
            SdkValueKind.CollectionObjectName
                when argument.CollectionObjectNameValue is { } value =>
                sdk.SetCollectionObjectNameArg2(
                    argument.Name,
                    value.CollectionName,
                    value.ObjectName,
                    SdkSpecializedValueCodec.ToSdkString(value.ObjectType)),
            SdkValueKind.CollectionObjectNameList
                when argument.CollectionObjectNameListValue is { } value =>
                SetCollectionObjectNameList(sdk, argument.Name, value),
            SdkValueKind.CollectionGroupNameList
                when argument.CollectionGroupNameListValue is { } value =>
                SetCollectionGroupNameList(sdk, argument.Name, value),
            SdkValueKind.CollectionVectorGroupName
                when argument.CollectionVectorGroupNameValue is { } value =>
                sdk.SetColVectorGroupNameArg(
                    argument.Name,
                    value.CollectionName,
                    value.VectorGroupName),
            SdkValueKind.CollectionVectorGroupNameList
                when argument.CollectionVectorGroupNameListValue is { } value =>
                SetCollectionVectorGroupNameList(sdk, argument.Name, value),
            SdkValueKind.PointNameList when argument.PointNameListValue is { } value =>
                SetPointNameList(sdk, argument.Name, value),
            SdkValueKind.StringList when argument.StringListValue is { } value =>
                SetStringList(sdk, argument.Name, value),
            SdkValueKind.VectorNameList when argument.VectorNameListValue is { } value =>
                SetVectorNameList(sdk, argument.Name, value),
            _ => SetSpecializedInputArgument(sdk, argument)
        };

    private static SdkOutputValue GetOutputValue(
        ISpatialAnalyzerSdkCalls sdk,
        SdkOutputArgument argument) =>
        !HasExpectedBinding(argument.SdkBinding, ExpectedGetter(argument.Kind))
            ? new SdkOutputValue(argument.Name, argument.Kind, Retrieved: false)
            : argument.Kind switch
            {
                SdkValueKind.Logical => GetLogical(sdk, argument),
                SdkValueKind.WholeNumber => GetWholeNumber(sdk, argument),
                SdkValueKind.FloatingPoint => GetFloatingPoint(sdk, argument),
                SdkValueKind.Text => GetText(sdk, argument),
                SdkValueKind.DoubleArray => GetDoubleArray(sdk, argument),
                SdkValueKind.EditText => GetEditText(sdk, argument),
                SdkValueKind.Transform => GetTransform(sdk, argument),
                SdkValueKind.WorldTransform => GetWorldTransform(sdk, argument),
                SdkValueKind.FileReference => GetFileReference(sdk, argument),
                SdkValueKind.PointName => GetPointName(sdk, argument),
                SdkValueKind.Vector => GetVector(sdk, argument),
                SdkValueKind.ToleranceVectorOptions =>
                    GetToleranceVectorOptions(sdk, argument),
                SdkValueKind.CollectionInstrumentId =>
                    GetCollectionInstrumentId(sdk, argument),
                SdkValueKind.CollectionInstrumentIdList =>
                    GetCollectionInstrumentIdList(sdk, argument),
                SdkValueKind.CollectionName => GetNamedString(
                    sdk.GetCollectionNameArg,
                    argument),
                SdkValueKind.CollectionItemName =>
                    GetCollectionItemName(sdk, argument),
                SdkValueKind.CollectionItemNameList =>
                    GetCollectionItemNameList(sdk, argument),
                SdkValueKind.CollectionObjectName =>
                    GetCollectionObjectName(sdk, argument),
                SdkValueKind.CollectionObjectNameList =>
                    GetCollectionObjectNameList(sdk, argument),
                SdkValueKind.PointNameList => GetPointNameList(sdk, argument),
                SdkValueKind.StringList => GetStringList(sdk, argument),
                SdkValueKind.VectorNameList => GetVectorNameList(sdk, argument),
                _ => GetSpecializedOutputValue(sdk, argument)
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

    private static bool SetDoubleArray(
        ISpatialAnalyzerSdkCalls sdk,
        string name,
        SdkDoubleArrayValue value)
    {
        var sdkValue = SdkContainerValueCodec.ToDoubleArrayComValue(value);
        return sdk.SetDoubleArrayArg(name, value.Values.Count, ref sdkValue);
    }

    private static bool SetEditText(
        ISpatialAnalyzerSdkCalls sdk,
        string name,
        SdkStringListValue value)
    {
        var sdkValue = SdkContainerValueCodec.ToEditTextComValue(value);
        return sdk.SetEditTextArg(name, ref sdkValue);
    }

    private static bool SetTransform(
        ISpatialAnalyzerSdkCalls sdk,
        string name,
        SdkTransformValue value)
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
        SdkWorldTransformValue value)
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
        SdkAngularUnitValue value) =>
        AngularUnitSdkString(value) is { } sdkValue &&
        sdk.SetAngularUnitsArg(name, sdkValue);

    private static bool SetDistanceUnit(
        ISpatialAnalyzerSdkCalls sdk,
        string name,
        SdkDistanceUnitValue value) =>
        DistanceUnitSdkString(value) is { } sdkValue &&
        sdk.SetDistanceUnitsArg(name, sdkValue);

    private static bool SetTemperatureUnit(
        ISpatialAnalyzerSdkCalls sdk,
        string name,
        SdkTemperatureUnitValue value) =>
        TemperatureUnitSdkString(value) is { } sdkValue &&
        sdk.SetTemperatureUnitsArg(name, sdkValue);

    private static string? AngularUnitSdkString(SdkAngularUnitValue value) =>
        value switch
        {
            SdkAngularUnitValue.Degrees => "Degrees",
            SdkAngularUnitValue.DegreesMinutesSeconds => "Deg:Min:Sec",
            SdkAngularUnitValue.Radians => "Radians",
            SdkAngularUnitValue.Milliradians => "Milliradians",
            SdkAngularUnitValue.GonsGrad => "Gons/Grad",
            SdkAngularUnitValue.Mils => "Mils",
            SdkAngularUnitValue.Arcseconds => "Arcseconds",
            SdkAngularUnitValue.DegreesMinutes => "Deg:Min",
            _ => null
        };

    private static string? DistanceUnitSdkString(SdkDistanceUnitValue value) =>
        value switch
        {
            SdkDistanceUnitValue.Meters => "Meters",
            SdkDistanceUnitValue.Centimeters => "Centimeters",
            SdkDistanceUnitValue.Millimeters => "Millimeters",
            SdkDistanceUnitValue.Feet => "Feet",
            SdkDistanceUnitValue.Inches => "Inches",
            SdkDistanceUnitValue.UsSurveyFeet => "US Survey Feet",
            _ => null
        };

    private static string? TemperatureUnitSdkString(SdkTemperatureUnitValue value) =>
        value switch
        {
            SdkTemperatureUnitValue.Fahrenheit => "Fahrenheit",
            SdkTemperatureUnitValue.Celsius => "Celsius",
            _ => null
        };

    private static SdkOutputValue GetDoubleArray(
        ISpatialAnalyzerSdkCalls sdk,
        SdkOutputArgument argument)
    {
        var size = 0;
        var sdkValue = SdkContainerValueCodec.EmptyArrayBuffer();
        SdkDoubleArrayValue? value = null;
        var retrieved = sdk.GetDoubleArrayArg(argument.Name, ref size, ref sdkValue) &&
            SdkContainerValueCodec.TryParseDoubleArray(sdkValue, size, out value);
        return new SdkOutputValue(
            argument.Name,
            argument.Kind,
            retrieved,
            DoubleArrayValue: retrieved ? value : null);
    }

    private static SdkOutputValue GetEditText(
        ISpatialAnalyzerSdkCalls sdk,
        SdkOutputArgument argument)
    {
        var sdkValue = SdkContainerValueCodec.EmptyArrayBuffer();
        SdkStringListValue? value = null;
        var retrieved = sdk.GetEditTextArg(argument.Name, ref sdkValue) &&
            SdkContainerValueCodec.TryParseEditText(sdkValue, out value);
        return new SdkOutputValue(
            argument.Name,
            argument.Kind,
            retrieved,
            StringListValue: retrieved ? value : null);
    }

    private static SdkOutputValue GetTransform(
        ISpatialAnalyzerSdkCalls sdk,
        SdkOutputArgument argument)
    {
        var sdkValue = SdkContainerValueCodec.TransformBuffer();
        SdkTransformValue? value = null;
        var retrieved = sdk.GetTransformArg(argument.Name, ref sdkValue) &&
            SdkContainerValueCodec.TryParseTransform(sdkValue, out value);
        return new SdkOutputValue(
            argument.Name,
            argument.Kind,
            retrieved,
            TransformValue: retrieved ? value : null);
    }

    private static SdkOutputValue GetWorldTransform(
        ISpatialAnalyzerSdkCalls sdk,
        SdkOutputArgument argument)
    {
        var sdkValue = SdkContainerValueCodec.TransformBuffer();
        var scaleFactor = 0d;
        SdkTransformValue? transform = null;
        var retrieved = sdk.GetWorldTransformArg(
            argument.Name,
            ref sdkValue,
            ref scaleFactor) &&
            SdkContainerValueCodec.TryParseTransform(sdkValue, out transform);
        return new SdkOutputValue(
            argument.Name,
            argument.Kind,
            retrieved,
            WorldTransformValue: retrieved
                ? new SdkWorldTransformValue(transform!, scaleFactor)
                : null);
    }

    private static SdkOutputValue GetFileReference(
        ISpatialAnalyzerSdkCalls sdk,
        SdkOutputArgument argument)
    {
        var path = string.Empty;
        var embeddedFile = false;
        var retrieved = sdk.GetFilePathArg(
            argument.Name,
            ref path,
            ref embeddedFile);
        return new SdkOutputValue(
            argument.Name,
            argument.Kind,
            retrieved,
            FileReferenceValue: retrieved
                ? new SdkFileReferenceValue(path, embeddedFile)
                : null);
    }

    private static bool SetCollectionInstrumentIdList(
        ISpatialAnalyzerSdkCalls sdk,
        string name,
        SdkCollectionInstrumentIdListValue value) =>
        SetReferenceList(
            value.Values.Select(SdkReferenceListCodec.Format),
            (ref object values) => sdk.SetColInstIdRefListArg(name, ref values));

    private static bool SetCollectionGroupNameList(
        ISpatialAnalyzerSdkCalls sdk,
        string name,
        SdkCollectionGroupNameListValue value) =>
        SetReferenceList(
            value.Values.Select(SdkReferenceListCodec.Format),
            (ref object values) => sdk.SetCollectionGroupNameRefListArg(name, ref values));

    private static bool SetCollectionItemNameList(
        ISpatialAnalyzerSdkCalls sdk,
        string name,
        SdkCollectionItemNameListValue value) =>
        SetReferenceList(
            value.Values.Select(SdkReferenceListCodec.Format),
            (ref object values) => sdk.SetCollectionObjectNameRefListArg(name, ref values));

    private static bool SetCollectionObjectNameList(
        ISpatialAnalyzerSdkCalls sdk,
        string name,
        SdkCollectionObjectNameListValue value) =>
        SetReferenceList(
            value.Values.Select(SdkReferenceListCodec.Format),
            (ref object values) => sdk.SetCollectionObjectNameRefListArg(name, ref values));

    private static bool SetCollectionVectorGroupNameList(
        ISpatialAnalyzerSdkCalls sdk,
        string name,
        SdkCollectionVectorGroupNameListValue value) =>
        SetReferenceList(
            value.Values.Select(SdkReferenceListCodec.Format),
            (ref object values) => sdk.SetCollectionVectorGroupNameRefListArg(name, ref values));

    private static bool SetPointNameList(
        ISpatialAnalyzerSdkCalls sdk,
        string name,
        SdkPointNameListValue value) =>
        SetReferenceList(
            value.Values.Select(SdkReferenceListCodec.Format),
            (ref object values) => sdk.SetPointNameRefListArg(name, ref values));

    private static bool SetStringList(
        ISpatialAnalyzerSdkCalls sdk,
        string name,
        SdkStringListValue value) =>
        SetReferenceList(
            value.Values,
            (ref object values) => sdk.SetStringRefListArg(name, ref values));

    private static bool SetVectorNameList(
        ISpatialAnalyzerSdkCalls sdk,
        string name,
        SdkVectorNameListValue value) =>
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

    private static SdkOutputValue GetCollectionInstrumentId(
        ISpatialAnalyzerSdkCalls sdk,
        SdkOutputArgument argument)
    {
        var collectionName = string.Empty;
        var instrumentId = 0;
        var retrieved = sdk.GetColInstIdArg(
            argument.Name,
            ref collectionName,
            ref instrumentId);
        return new SdkOutputValue(
            argument.Name,
            argument.Kind,
            retrieved,
            CollectionInstrumentIdValue: retrieved
                ? new SdkCollectionInstrumentIdValue(collectionName, instrumentId)
                : null);
    }

    private static SdkOutputValue GetCollectionItemName(
        ISpatialAnalyzerSdkCalls sdk,
        SdkOutputArgument argument)
    {
        var collectionName = string.Empty;
        var itemName = string.Empty;
        var retrieved = sdk.GetCollectionObjectNameArg(
            argument.Name,
            ref collectionName,
            ref itemName);
        SdkCollectionItemNameValue? parsed = null;
        retrieved = retrieved &&
            SdkReferenceListCodec.TryParseItemNameResult(
                collectionName,
                itemName,
                out parsed);
        return new SdkOutputValue(
            argument.Name,
            argument.Kind,
            retrieved,
            CollectionItemNameValue: retrieved ? parsed : null);
    }

    private static SdkOutputValue GetCollectionObjectName(
        ISpatialAnalyzerSdkCalls sdk,
        SdkOutputArgument argument)
    {
        var collectionName = string.Empty;
        var objectName = string.Empty;
        var retrieved = sdk.GetCollectionObjectNameArg(
            argument.Name,
            ref collectionName,
            ref objectName);
        SdkCollectionObjectNameValue? parsed = null;
        retrieved = retrieved &&
            SdkReferenceListCodec.TryParseObjectNameResult(
                collectionName,
                objectName,
                out parsed);
        return new SdkOutputValue(
            argument.Name,
            argument.Kind,
            retrieved,
            CollectionObjectNameValue: retrieved ? parsed : null);
    }

    private static SdkOutputValue GetNamedString(
        NamedStringGetter getter,
        SdkOutputArgument argument)
    {
        var value = string.Empty;
        var retrieved = getter(argument.Name, ref value);
        return new SdkOutputValue(
            argument.Name,
            argument.Kind,
            retrieved,
            StringValue: retrieved ? value : null);
    }

    private static SdkOutputValue GetCollectionInstrumentIdList(
        ISpatialAnalyzerSdkCalls sdk,
        SdkOutputArgument argument) =>
        GetReferenceList<SdkCollectionInstrumentIdListValue>(
            argument,
            sdk.GetColInstIdRefListArg,
            SdkReferenceListCodec.TryParseInstrumentIds,
            (name, kind, value) => new SdkOutputValue(
                name,
                kind,
                true,
                CollectionInstrumentIdListValue: value));

    private static SdkOutputValue GetCollectionItemNameList(
        ISpatialAnalyzerSdkCalls sdk,
        SdkOutputArgument argument) =>
        GetReferenceList<SdkCollectionItemNameListValue>(
            argument,
            sdk.GetCollectionObjectNameRefListArg,
            SdkReferenceListCodec.TryParseItemNames,
            (name, kind, value) => new SdkOutputValue(
                name,
                kind,
                true,
                CollectionItemNameListValue: value));

    private static SdkOutputValue GetCollectionObjectNameList(
        ISpatialAnalyzerSdkCalls sdk,
        SdkOutputArgument argument) =>
        GetReferenceList<SdkCollectionObjectNameListValue>(
            argument,
            sdk.GetCollectionObjectNameRefListArg,
            SdkReferenceListCodec.TryParseObjectNames,
            (name, kind, value) => new SdkOutputValue(
                name,
                kind,
                true,
                CollectionObjectNameListValue: value));

    private static SdkOutputValue GetPointNameList(
        ISpatialAnalyzerSdkCalls sdk,
        SdkOutputArgument argument) =>
        GetReferenceList<SdkPointNameListValue>(
            argument,
            sdk.GetPointNameRefListArg,
            SdkReferenceListCodec.TryParsePointNames,
            (name, kind, value) => new SdkOutputValue(
                name,
                kind,
                true,
                PointNameListValue: value));

    private static SdkOutputValue GetStringList(
        ISpatialAnalyzerSdkCalls sdk,
        SdkOutputArgument argument) =>
        GetReferenceList<SdkStringListValue>(
            argument,
            sdk.GetStringRefListArg,
            SdkReferenceListCodec.TryParseStrings,
            (name, kind, value) => new SdkOutputValue(
                name,
                kind,
                true,
                StringListValue: value));

    private static SdkOutputValue GetVectorNameList(
        ISpatialAnalyzerSdkCalls sdk,
        SdkOutputArgument argument) =>
        GetReferenceList<SdkVectorNameListValue>(
            argument,
            sdk.GetVectorNameRefListArg,
            SdkReferenceListCodec.TryParseVectorNames,
            (name, kind, value) => new SdkOutputValue(
                name,
                kind,
                true,
                VectorNameListValue: value));

    private static SdkOutputValue GetReferenceList<T>(
        SdkOutputArgument argument,
        ReferenceListGetter getter,
        TryParseList<T> parser,
        Func<string, SdkValueKind, T, SdkOutputValue> create)
        where T : class
    {
        var value = SdkReferenceListCodec.ToComValue([]);
        if (!getter(argument.Name, ref value) || !parser(value, out var parsed) || parsed is null)
        {
            return new SdkOutputValue(argument.Name, argument.Kind, Retrieved: false);
        }

        return create(argument.Name, argument.Kind, parsed);
    }

    private delegate bool ReferenceListCall(ref object values);

    private delegate bool ReferenceListGetter(string name, ref object values);

    private delegate bool NamedStringGetter(string name, ref string value);

    private delegate bool TryParseList<T>(object value, out T? result) where T : class;
    private static bool HasExpectedBinding(string? actual, string expected) =>
        actual is null || string.Equals(actual, expected, StringComparison.Ordinal);

    private static string ExpectedSetter(SdkValueKind kind) =>
        kind switch
        {
            SdkValueKind.Logical => "SetBoolArg",
            SdkValueKind.WholeNumber => "SetIntegerArg",
            SdkValueKind.FloatingPoint => "SetDoubleArg",
            SdkValueKind.Text => "SetStringArg",
            SdkValueKind.DoubleArray => "SetDoubleArrayArg",
            SdkValueKind.EditText => "SetEditTextArg",
            SdkValueKind.Transform => "SetTransformArg",
            SdkValueKind.WorldTransform => "SetWorldTransformArg",
            SdkValueKind.RgbColor => "SetColorArg",
            SdkValueKind.FileReference => "SetFilePathArg",
            SdkValueKind.AngularUnit => "SetAngularUnitsArg",
            SdkValueKind.DistanceUnit => "SetDistanceUnitsArg",
            SdkValueKind.TemperatureUnit => "SetTemperatureUnitsArg",
            SdkValueKind.Font => "SetFontTypeArg",
            SdkValueKind.PointName => "SetPointNameArg",
            SdkValueKind.Vector => "SetVectorArg",
            SdkValueKind.ToleranceVectorOptions => "SetToleranceVectorOptionsArg",
            SdkValueKind.ChartName => "SetChartNameArg",
            SdkValueKind.CloudName => "SetCloudNameArg",
            SdkValueKind.CollectionGroupNameList => "SetCollectionGroupNameRefListArg",
            SdkValueKind.CollectionInstrumentId => "SetColInstIdArg",
            SdkValueKind.CollectionInstrumentIdList => "SetColInstIdRefListArg",
            SdkValueKind.CollectionMachineId => "SetColMachineIdArg",
            SdkValueKind.CollectionName => "SetCollectionNameArg",
            SdkValueKind.CollectionItemName => "SetCollectionObjectNameArg2",
            SdkValueKind.CollectionItemNameList => "SetCollectionObjectNameRefListArg",
            SdkValueKind.CollectionObjectName => "SetCollectionObjectNameArg2",
            SdkValueKind.CollectionObjectNameList => "SetCollectionObjectNameRefListArg",
            SdkValueKind.CollectionVectorGroupName => "SetColVectorGroupNameArg",
            SdkValueKind.CollectionVectorGroupNameList => "SetCollectionVectorGroupNameRefListArg",
            SdkValueKind.FrameName => "SetFrameNameArg",
            SdkValueKind.PointNameList => "SetPointNameRefListArg",
            SdkValueKind.StringList => "SetStringRefListArg",
            SdkValueKind.VectorGroupName => "SetVectorGroupNameArg",
            SdkValueKind.VectorNameList => "SetVectorNameRefListArg",
            SdkValueKind.ViewName => "SetViewNameArg",
            _ => SpecializedExpectedSetter(kind)
        };

    private static string ExpectedGetter(SdkValueKind kind) =>
        kind switch
        {
            SdkValueKind.Logical => "GetBoolArg",
            SdkValueKind.WholeNumber => "GetIntegerArg",
            SdkValueKind.FloatingPoint => "GetDoubleArg",
            SdkValueKind.Text => "GetStringArg",
            SdkValueKind.DoubleArray => "GetDoubleArrayArg",
            SdkValueKind.EditText => "GetEditTextArg",
            SdkValueKind.Transform => "GetTransformArg",
            SdkValueKind.WorldTransform => "GetWorldTransformArg",
            SdkValueKind.FileReference => "GetFilePathArg",
            SdkValueKind.RgbColor => "GetColorArg",
            SdkValueKind.AngularUnit => "GetAngularUnitsArg",
            SdkValueKind.DistanceUnit => "GetDistanceUnitsArg",
            SdkValueKind.TemperatureUnit => "GetTemperatureUnitsArg",
            SdkValueKind.Font => "GetFontTypeArg",
            SdkValueKind.PointName => "GetPointNameArg",
            SdkValueKind.Vector => "GetVectorArg",
            SdkValueKind.ToleranceVectorOptions => "GetToleranceVectorOptionsArg",
            SdkValueKind.ChartName => "GetChartNameArg",
            SdkValueKind.CloudName => "GetCloudNameArg",
            SdkValueKind.CollectionGroupNameList => "GetCollectionGroupNameRefListArg",
            SdkValueKind.CollectionInstrumentId => "GetColInstIdArg",
            SdkValueKind.CollectionInstrumentIdList => "GetColInstIdRefListArg",
            SdkValueKind.CollectionMachineId => "GetColMachineIdArg",
            SdkValueKind.CollectionName => "GetCollectionNameArg",
            SdkValueKind.CollectionItemName => "GetCollectionObjectNameArg",
            SdkValueKind.CollectionItemNameList => "GetCollectionObjectNameRefListArg",
            SdkValueKind.CollectionObjectName => "GetCollectionObjectNameArg",
            SdkValueKind.CollectionObjectNameList => "GetCollectionObjectNameRefListArg",
            SdkValueKind.CollectionVectorGroupName => "GetColVectorGroupNameArg",
            SdkValueKind.CollectionVectorGroupNameList => "GetCollectionVectorGroupNameRefListArg",
            SdkValueKind.FrameName => "GetFrameNameArg",
            SdkValueKind.PointNameList => "GetPointNameRefListArg",
            SdkValueKind.StringList => "GetStringRefListArg",
            SdkValueKind.VectorGroupName => "GetVectorGroupNameArg",
            SdkValueKind.VectorNameList => "GetVectorNameRefListArg",
            SdkValueKind.ViewName => "GetViewNameArg",
            _ => SpecializedExpectedGetter(kind)
        };
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
