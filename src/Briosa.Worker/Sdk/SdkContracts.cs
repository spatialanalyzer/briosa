namespace Briosa.Worker.Sdk;

internal enum SdkConnectionStatus
{
    Connected,
    Unavailable
}

internal sealed record SdkConnectionResult(
    SdkConnectionStatus Status,
    int? StatusCode,
    string? DiagnosticCode);

internal enum SdkConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Faulted,
    Stopping
}

internal sealed record SdkConnectionSnapshot(
    SdkConnectionState State,
    string TargetHost,
    int? StatusCode,
    int Attempt,
    int MaximumAttempts,
    string DiagnosticCode,
    DateTimeOffset TransitionedAt);

internal sealed class SdkConnectionPolicy
{
    public SdkConnectionPolicy(int maximumAttempts, TimeSpan retryDelay)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumAttempts, 1);
        if (retryDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(retryDelay),
                retryDelay,
                "The connection retry delay cannot be negative.");
        }

        MaximumAttempts = maximumAttempts;
        RetryDelay = retryDelay;
    }

    public int MaximumAttempts { get; }

    public TimeSpan RetryDelay { get; }
}

internal enum SdkRequestStatus
{
    Completed,
    Unavailable
}

internal sealed record SdkRequestResult(
    SdkRequestStatus Status,
    SdkExecutionResult? Execution,
    SdkConnectionSnapshot Connection,
    string? DiagnosticCode);

internal enum SdkValueKind
{
    Logical,
    WholeNumber,
    FloatingPoint,
    Text,
    DoubleArray,
    EditText,
    Transform,
    WorldTransform,
    RgbColor,
    FileReference,
    AngularUnit,
    DistanceUnit,
    TemperatureUnit,
    Font,
    AsciiImportFileFormat,
    AsciiFrameSetFormat,
    AutoFilterProximitySettings,
    AxisIdentifier,
    WcfAxisIdentifier,
    BaseColorType,
    BaseMidColorType,
    ChartType,
    CloudThinningOptions,
    CollimationBaselineType,
    CollimationType,
    ColorRangeMethod,
    ColorizationOptions,
    CoordinateSystemType,
    VectorComponent,
    DynamicCircleMode,
    DynamicEllipseMode,
    DynamicLineMode,
    DynamicPlaneMode,
    DynamicPointMode,
    EdgeMode,
    ExportDataDelimiterType,
    ExportTargetNameFormat,
    ExportVectorNameFormat,
    FitConstraintScalarOptions,
    FitDegreeOfFreedomOptions,
    GeometryType,
    InstrumentType,
    ObjectType,
    OffsetDirectionType,
    PointFilterInputType,
    RelationshipWeightingMode,
    RenderModeType,
    ReportOutputOptions,
    ReportPageOrientation,
    ReportViewOptions,
    SaturationLimitType,
    ShowUsmnDialogType,
    SurfaceAnalysisMode,
    SurfaceDissectionModeType,
    TargetComputationMethod,
    ToleranceScalarOptions,
    TranslucencyType,
    PointName,
    Vector,
    ToleranceVectorOptions,
    ChartName,
    CloudName,
    CollectionGroupNameList,
    CollectionInstrumentId,
    CollectionInstrumentIdList,
    CollectionMachineId,
    CollectionName,
    CollectionObjectName,
    CollectionObjectNameList,
    CollectionVectorGroupName,
    CollectionVectorGroupNameList,
    FrameName,
    PointNameList,
    StringList,
    VectorGroupName,
    VectorNameList,
    ViewName
}

internal enum SdkAngularUnitValue
{
    Unspecified,
    Degrees,
    DegreesMinutesSeconds,
    Radians,
    Milliradians,
    GonsGrad,
    Mils,
    Arcseconds,
    DegreesMinutes
}

internal enum SdkDistanceUnitValue
{
    Unspecified,
    Meters,
    Centimeters,
    Millimeters,
    Feet,
    Inches,
    UsSurveyFeet
}

internal enum SdkTemperatureUnitValue
{
    Unspecified,
    Fahrenheit,
    Celsius
}

internal sealed record SdkPointNameValue(
    string CollectionName,
    string GroupName,
    string TargetName);

internal sealed record SdkCollectionInstrumentIdValue(
    string CollectionName,
    int InstrumentId);

internal sealed record SdkCollectionMachineIdValue(
    string CollectionName,
    int MachineId);

internal sealed record SdkCollectionObjectNameValue(
    string CollectionName,
    string ObjectName,
    string ObjectType);

internal sealed record SdkCollectionGroupNameValue(
    string CollectionName,
    string GroupName);

internal sealed record SdkCollectionVectorGroupNameValue(
    string CollectionName,
    string VectorGroupName);

internal sealed record SdkVectorNameValue(
    string CollectionName,
    string GroupName,
    string VectorName);

internal sealed record SdkCollectionInstrumentIdListValue(
    IReadOnlyList<SdkCollectionInstrumentIdValue> Values);

internal sealed record SdkCollectionGroupNameListValue(
    IReadOnlyList<SdkCollectionGroupNameValue> Values);

internal sealed record SdkCollectionObjectNameListValue(
    IReadOnlyList<SdkCollectionObjectNameValue> Values);

internal sealed record SdkCollectionVectorGroupNameListValue(
    IReadOnlyList<SdkCollectionVectorGroupNameValue> Values);

internal sealed record SdkPointNameListValue(IReadOnlyList<SdkPointNameValue> Values);

internal sealed record SdkStringListValue(IReadOnlyList<string> Values);

internal sealed record SdkDoubleArrayValue(IReadOnlyList<double> Values);

internal sealed record SdkTransformValue(IReadOnlyList<double> Values);

internal sealed record SdkWorldTransformValue(
    SdkTransformValue Transform,
    double ScaleFactor);

internal sealed record SdkRgbColorValue(byte Red, byte Green, byte Blue);

internal sealed record SdkFileReferenceValue(string Path, bool EmbeddedFile);

internal sealed record SdkFontValue(
    string FontName,
    byte Size,
    SdkRgbColorValue Color);

internal sealed record SdkVectorNameListValue(IReadOnlyList<SdkVectorNameValue> Values);
internal sealed record SdkVectorValue(double X, double Y, double Z);

internal sealed record SdkToleranceLimit(bool Enabled, double Value);

internal sealed record SdkToleranceVectorOptionsValue(
    SdkToleranceLimit HighX,
    SdkToleranceLimit HighY,
    SdkToleranceLimit HighZ,
    SdkToleranceLimit HighMagnitude,
    SdkToleranceLimit LowX,
    SdkToleranceLimit LowY,
    SdkToleranceLimit LowZ,
    SdkToleranceLimit LowMagnitude);

internal sealed record SdkInputArgument(
    string Name,
    SdkValueKind Kind,
    bool? BooleanValue = null,
    int? IntegerValue = null,
    double? DoubleValue = null,
    string? StringValue = null,
    SdkPointNameValue? PointNameValue = null,
    SdkVectorValue? VectorValue = null,
    SdkToleranceVectorOptionsValue? ToleranceVectorOptionsValue = null,
    SdkCollectionInstrumentIdValue? CollectionInstrumentIdValue = null,
    SdkCollectionInstrumentIdListValue? CollectionInstrumentIdListValue = null,
    SdkCollectionMachineIdValue? CollectionMachineIdValue = null,
    SdkCollectionObjectNameValue? CollectionObjectNameValue = null,
    SdkCollectionObjectNameListValue? CollectionObjectNameListValue = null,
    SdkCollectionGroupNameListValue? CollectionGroupNameListValue = null,
    SdkCollectionVectorGroupNameValue? CollectionVectorGroupNameValue = null,
    SdkCollectionVectorGroupNameListValue? CollectionVectorGroupNameListValue = null,
    SdkPointNameListValue? PointNameListValue = null,
    SdkStringListValue? StringListValue = null,
    SdkVectorNameListValue? VectorNameListValue = null,
    SdkDoubleArrayValue? DoubleArrayValue = null,
    SdkTransformValue? TransformValue = null,
    SdkWorldTransformValue? WorldTransformValue = null,
    SdkRgbColorValue? RgbColorValue = null,
    SdkFileReferenceValue? FileReferenceValue = null,
    SdkAngularUnitValue? AngularUnitValue = null,
    SdkDistanceUnitValue? DistanceUnitValue = null,
    SdkTemperatureUnitValue? TemperatureUnitValue = null,
    SdkFontValue? FontValue = null,
    ISdkSpecializedEnumValue? SpecializedEnumValue = null,
    SdkAutoFilterProximitySettingsValue? AutoFilterProximitySettingsValue = null,
    SdkCloudThinningOptionsValue? CloudThinningOptionsValue = null,
    SdkColorizationOptionsValue? ColorizationOptionsValue = null,
    SdkFitConstraintScalarOptionsValue? FitConstraintScalarOptionsValue = null,
    SdkFitDegreeOfFreedomOptionsValue? FitDegreeOfFreedomOptionsValue = null,
    SdkReportOutputOptionsValue? ReportOutputOptionsValue = null,
    SdkReportViewOptionsValue? ReportViewOptionsValue = null,
    SdkToleranceScalarOptionsValue? ToleranceScalarOptionsValue = null,
    string? SdkBinding = null);
internal sealed record SdkOutputArgument(
    string Name,
    SdkValueKind Kind,
    string? SdkBinding = null);

internal sealed record SdkOutputValue(
    string Name,
    SdkValueKind Kind,
    bool Retrieved,
    bool? BooleanValue = null,
    int? IntegerValue = null,
    double? DoubleValue = null,
    string? StringValue = null,
    SdkPointNameValue? PointNameValue = null,
    SdkVectorValue? VectorValue = null,
    SdkToleranceVectorOptionsValue? ToleranceVectorOptionsValue = null,
    SdkCollectionInstrumentIdValue? CollectionInstrumentIdValue = null,
    SdkCollectionInstrumentIdListValue? CollectionInstrumentIdListValue = null,
    SdkCollectionMachineIdValue? CollectionMachineIdValue = null,
    SdkCollectionObjectNameValue? CollectionObjectNameValue = null,
    SdkCollectionObjectNameListValue? CollectionObjectNameListValue = null,
    SdkCollectionGroupNameListValue? CollectionGroupNameListValue = null,
    SdkCollectionVectorGroupNameValue? CollectionVectorGroupNameValue = null,
    SdkCollectionVectorGroupNameListValue? CollectionVectorGroupNameListValue = null,
    SdkPointNameListValue? PointNameListValue = null,
    SdkStringListValue? StringListValue = null,
    SdkVectorNameListValue? VectorNameListValue = null,
    SdkDoubleArrayValue? DoubleArrayValue = null,
    SdkTransformValue? TransformValue = null,
    SdkWorldTransformValue? WorldTransformValue = null,
    SdkFileReferenceValue? FileReferenceValue = null,
    SdkFitConstraintScalarOptionsValue? FitConstraintScalarOptionsValue = null,
    SdkToleranceScalarOptionsValue? ToleranceScalarOptionsValue = null);
internal sealed class SdkCommand
{
    public SdkCommand(string operationId)
        : this(operationId, operationId, [], [])
    {
    }

    public SdkCommand(
        string operationId,
        string stepName,
        IReadOnlyList<SdkInputArgument> inputArguments,
        IReadOnlyList<SdkOutputArgument> outputArguments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(stepName);
        ArgumentNullException.ThrowIfNull(inputArguments);
        ArgumentNullException.ThrowIfNull(outputArguments);
        OperationId = operationId;
        StepName = stepName;
        InputArguments = [.. inputArguments];
        OutputArguments = [.. outputArguments];
    }

    public string OperationId { get; }

    public string StepName { get; }

    public IReadOnlyList<SdkInputArgument> InputArguments { get; }

    public IReadOnlyList<SdkOutputArgument> OutputArguments { get; }
}

internal sealed record SdkMpResult(
    bool Retrieved,
    bool Succeeded,
    int? ResultCode,
    string? DiagnosticCode);

internal sealed record SdkExecutionResult(
    bool ExecuteStepReturned,
    SdkMpResult MpResult,
    TimeSpan Duration,
    IReadOnlyList<SdkOutputValue> OutputValues,
    string? DiagnosticCode);
