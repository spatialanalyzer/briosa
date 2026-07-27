namespace Briosa.Worker.Control;

public static class WorkerControlProtocol
{
    public const int CurrentVersion = 8;

    public const int MaximumMessageBytes = 64 * 1024;
}

public enum WorkerControlMessageKind
{
    None = 0,
    Ready,
    Ping,
    Pong,
    Stop,
    Stopped,
    Execute,
    ExecutionResult
}

public enum WorkerMpValueKind
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
    AsciiFileFormat,
    AutoFilterProximitySettings,
    AxisIdentifier,
    BaseColorType,
    BaseMidColorType,
    ChartType,
    CloudThinningOptions,
    CollimationBaselineType,
    CollimationType,
    ColorRangeMethod,
    ColorizationOptions,
    CoordinateSystemType,
    DatasetType,
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
    PointDeltaReportOptions,
    PointFilterInputType,
    ProjectionOptions,
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

public enum WorkerAngularUnitValue
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

public enum WorkerDistanceUnitValue
{
    Unspecified,
    Meters,
    Centimeters,
    Millimeters,
    Feet,
    Inches,
    UsSurveyFeet
}

public enum WorkerTemperatureUnitValue
{
    Unspecified,
    Fahrenheit,
    Celsius
}

public enum WorkerExecutionResponseStatus
{
    Completed,
    Unavailable
}

public enum WorkerConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Faulted,
    Stopping
}

public sealed record WorkerConnectionSnapshot(
    WorkerConnectionState State,
    string TargetHost,
    int? StatusCode,
    int Attempt,
    int MaximumAttempts,
    string DiagnosticCode,
    DateTimeOffset TransitionedAt);

public sealed record WorkerPointNameValue(
    string CollectionName,
    string GroupName,
    string TargetName);

public sealed record WorkerCollectionInstrumentIdValue(
    string CollectionName,
    int InstrumentId);

public sealed record WorkerCollectionMachineIdValue(
    string CollectionName,
    int MachineId);

public sealed record WorkerCollectionObjectNameValue(
    string CollectionName,
    string ObjectName,
    string ObjectType);

public sealed record WorkerCollectionGroupNameValue(
    string CollectionName,
    string GroupName);

public sealed record WorkerCollectionVectorGroupNameValue(
    string CollectionName,
    string VectorGroupName);

public sealed record WorkerVectorNameValue(
    string CollectionName,
    string GroupName,
    string VectorName);

public sealed record WorkerCollectionInstrumentIdListValue(
    IReadOnlyList<WorkerCollectionInstrumentIdValue> Values);

public sealed record WorkerCollectionGroupNameListValue(
    IReadOnlyList<WorkerCollectionGroupNameValue> Values);

public sealed record WorkerCollectionObjectNameListValue(
    IReadOnlyList<WorkerCollectionObjectNameValue> Values);

public sealed record WorkerCollectionVectorGroupNameListValue(
    IReadOnlyList<WorkerCollectionVectorGroupNameValue> Values);

public sealed record WorkerPointNameListValue(
    IReadOnlyList<WorkerPointNameValue> Values);

public sealed record WorkerStringListValue(IReadOnlyList<string> Values);

public sealed record WorkerDoubleArrayValue(IReadOnlyList<double> Values);

public sealed record WorkerTransformValue(IReadOnlyList<double> Values);

public sealed record WorkerWorldTransformValue(
    WorkerTransformValue Transform,
    double ScaleFactor);

public sealed record WorkerRgbColorValue(byte Red, byte Green, byte Blue);

public sealed record WorkerFileReferenceValue(string Path, bool EmbeddedFile);

public sealed record WorkerFontValue(
    string FontName,
    byte Size,
    WorkerRgbColorValue Color);

public sealed record WorkerVectorNameListValue(
    IReadOnlyList<WorkerVectorNameValue> Values);
public sealed record WorkerVectorValue(double X, double Y, double Z);

public sealed record WorkerToleranceLimit(bool Enabled, double Value);

public sealed record WorkerToleranceVectorOptionsValue(
    WorkerToleranceLimit HighX,
    WorkerToleranceLimit HighY,
    WorkerToleranceLimit HighZ,
    WorkerToleranceLimit HighMagnitude,
    WorkerToleranceLimit LowX,
    WorkerToleranceLimit LowY,
    WorkerToleranceLimit LowZ,
    WorkerToleranceLimit LowMagnitude);

public sealed record WorkerMpInputArgument(
    string Name,
    WorkerMpValueKind Kind,
    bool? BooleanValue = null,
    int? IntegerValue = null,
    double? DoubleValue = null,
    string? StringValue = null,
    WorkerPointNameValue? PointNameValue = null,
    WorkerVectorValue? VectorValue = null,
    WorkerToleranceVectorOptionsValue? ToleranceVectorOptionsValue = null,
    WorkerCollectionInstrumentIdValue? CollectionInstrumentIdValue = null,
    WorkerCollectionInstrumentIdListValue? CollectionInstrumentIdListValue = null,
    WorkerCollectionMachineIdValue? CollectionMachineIdValue = null,
    WorkerCollectionObjectNameValue? CollectionObjectNameValue = null,
    WorkerCollectionObjectNameListValue? CollectionObjectNameListValue = null,
    WorkerCollectionGroupNameListValue? CollectionGroupNameListValue = null,
    WorkerCollectionVectorGroupNameValue? CollectionVectorGroupNameValue = null,
    WorkerCollectionVectorGroupNameListValue? CollectionVectorGroupNameListValue = null,
    WorkerPointNameListValue? PointNameListValue = null,
    WorkerStringListValue? StringListValue = null,
    WorkerVectorNameListValue? VectorNameListValue = null,
    WorkerDoubleArrayValue? DoubleArrayValue = null,
    WorkerTransformValue? TransformValue = null,
    WorkerWorldTransformValue? WorldTransformValue = null,
    WorkerRgbColorValue? RgbColorValue = null,
    WorkerFileReferenceValue? FileReferenceValue = null,
    WorkerAngularUnitValue? AngularUnitValue = null,
    WorkerDistanceUnitValue? DistanceUnitValue = null,
    WorkerTemperatureUnitValue? TemperatureUnitValue = null,
    WorkerFontValue? FontValue = null,
    WorkerSpecializedEnumValue? SpecializedEnumValue = null,
    WorkerAutoFilterProximitySettingsValue? AutoFilterProximitySettingsValue = null,
    WorkerCloudThinningOptionsValue? CloudThinningOptionsValue = null,
    WorkerColorizationOptionsValue? ColorizationOptionsValue = null,
    WorkerFitConstraintScalarOptionsValue? FitConstraintScalarOptionsValue = null,
    WorkerFitDegreeOfFreedomOptionsValue? FitDegreeOfFreedomOptionsValue = null,
    WorkerPointDeltaReportOptionsValue? PointDeltaReportOptionsValue = null,
    WorkerProjectionOptionsValue? ProjectionOptionsValue = null,
    WorkerReportOutputOptionsValue? ReportOutputOptionsValue = null,
    WorkerReportViewOptionsValue? ReportViewOptionsValue = null,
    WorkerToleranceScalarOptionsValue? ToleranceScalarOptionsValue = null,
    string? SdkBinding = null);
public sealed record WorkerMpOutputArgument(
    string Name,
    WorkerMpValueKind Kind,
    string? SdkBinding = null);

public sealed record WorkerMpOutputValue(
    string Name,
    WorkerMpValueKind Kind,
    bool Retrieved,
    bool? BooleanValue = null,
    int? IntegerValue = null,
    double? DoubleValue = null,
    string? StringValue = null,
    WorkerPointNameValue? PointNameValue = null,
    WorkerVectorValue? VectorValue = null,
    WorkerToleranceVectorOptionsValue? ToleranceVectorOptionsValue = null,
    WorkerCollectionInstrumentIdValue? CollectionInstrumentIdValue = null,
    WorkerCollectionInstrumentIdListValue? CollectionInstrumentIdListValue = null,
    WorkerCollectionMachineIdValue? CollectionMachineIdValue = null,
    WorkerCollectionObjectNameValue? CollectionObjectNameValue = null,
    WorkerCollectionObjectNameListValue? CollectionObjectNameListValue = null,
    WorkerCollectionGroupNameListValue? CollectionGroupNameListValue = null,
    WorkerCollectionVectorGroupNameValue? CollectionVectorGroupNameValue = null,
    WorkerCollectionVectorGroupNameListValue? CollectionVectorGroupNameListValue = null,
    WorkerPointNameListValue? PointNameListValue = null,
    WorkerStringListValue? StringListValue = null,
    WorkerVectorNameListValue? VectorNameListValue = null,
    WorkerDoubleArrayValue? DoubleArrayValue = null,
    WorkerTransformValue? TransformValue = null,
    WorkerWorldTransformValue? WorldTransformValue = null,
    WorkerFileReferenceValue? FileReferenceValue = null,
    WorkerFitConstraintScalarOptionsValue? FitConstraintScalarOptionsValue = null,
    WorkerToleranceScalarOptionsValue? ToleranceScalarOptionsValue = null);
public sealed record WorkerMpCommand(
    string OperationId,
    string StepName,
    IReadOnlyList<WorkerMpInputArgument> InputArguments,
    IReadOnlyList<WorkerMpOutputArgument> OutputArguments);

public sealed record WorkerMpExecutionResult(
    bool ExecuteStepReturned,
    bool MpResultRetrieved,
    bool MpSucceeded,
    int? MpResultCode,
    long DurationMilliseconds,
    IReadOnlyList<WorkerMpOutputValue> OutputValues,
    string? DiagnosticCode);

public sealed record WorkerExecutionResponse(
    WorkerExecutionResponseStatus Status,
    WorkerMpExecutionResult? Execution,
    WorkerConnectionSnapshot Connection,
    string? DiagnosticCode);

public sealed record WorkerControlMessage(
    int ProtocolVersion,
    WorkerControlMessageKind Kind,
    Guid CorrelationId,
    int? ProcessId = null,
    string? DiagnosticCode = null,
    WorkerConnectionSnapshot? Connection = null,
    WorkerMpCommand? Command = null,
    WorkerExecutionResponse? ExecutionResponse = null)
{
    public static WorkerControlMessage Ready(
        int processId,
        WorkerConnectionSnapshot connection) =>
        new(
            WorkerControlProtocol.CurrentVersion,
            WorkerControlMessageKind.Ready,
            Guid.Empty,
            processId,
            Connection: connection ?? throw new ArgumentNullException(nameof(connection)));

    public static WorkerControlMessage Ping(Guid correlationId) =>
        new(WorkerControlProtocol.CurrentVersion, WorkerControlMessageKind.Ping, correlationId);

    public static WorkerControlMessage Pong(Guid correlationId) =>
        new(WorkerControlProtocol.CurrentVersion, WorkerControlMessageKind.Pong, correlationId);

    public static WorkerControlMessage Stop(Guid correlationId) =>
        new(WorkerControlProtocol.CurrentVersion, WorkerControlMessageKind.Stop, correlationId);

    public static WorkerControlMessage Stopped(Guid correlationId) =>
        new(WorkerControlProtocol.CurrentVersion, WorkerControlMessageKind.Stopped, correlationId);

    public static WorkerControlMessage Execute(Guid correlationId, WorkerMpCommand command) =>
        new(
            WorkerControlProtocol.CurrentVersion,
            WorkerControlMessageKind.Execute,
            correlationId,
            Command: command ?? throw new ArgumentNullException(nameof(command)));

    public static WorkerControlMessage ExecutionResult(
        Guid correlationId,
        WorkerExecutionResponse response) =>
        new(
            WorkerControlProtocol.CurrentVersion,
            WorkerControlMessageKind.ExecutionResult,
            correlationId,
            ExecutionResponse: response ?? throw new ArgumentNullException(nameof(response)));
}
