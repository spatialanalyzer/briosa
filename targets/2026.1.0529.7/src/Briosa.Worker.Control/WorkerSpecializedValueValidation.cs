namespace Briosa.Worker.Control;

internal static class WorkerSpecializedValueValidation
{
    public static bool HasInputValueForKind(WorkerMpInputArgument argument) =>
        argument.Value is IWorkerChoiceValue choice
            ? choice.IsDefined && argument.Value is not WorkerChoiceValue<WorkerObjectTypeValue> { Value: WorkerObjectTypeValue.Unspecified }
            : argument.Kind switch
        {
            WorkerMpValueKind.AutoFilterProximitySettings =>
                IsValid((argument.Value as WorkerAutoFilterProximitySettingsValue)),
            WorkerMpValueKind.BSplineFitOptions =>
                IsValid((argument.Value as WorkerBSplineFitOptionsValue)),
            WorkerMpValueKind.CloudThinningOptions =>
                IsValid((argument.Value as WorkerCloudThinningOptionsValue)),
            WorkerMpValueKind.ColorizationOptions =>
                IsValid((argument.Value as WorkerColorizationOptionsValue)),
            WorkerMpValueKind.FitConstraintScalarOptions =>
                IsValid((argument.Value as WorkerFitConstraintScalarOptionsValue)),
            WorkerMpValueKind.FitDegreeOfFreedomOptions =>
                (argument.Value as WorkerFitDegreeOfFreedomOptionsValue) is not null,

            WorkerMpValueKind.ReportOutputOptions =>
                IsValid((argument.Value as WorkerReportOutputOptionsValue)),
            WorkerMpValueKind.ReportViewOptions =>
                IsValid((argument.Value as WorkerReportViewOptionsValue)),
            WorkerMpValueKind.ToleranceScalarOptions =>
                IsValid((argument.Value as WorkerToleranceScalarOptionsValue)),
            WorkerMpValueKind.ProjectionOptions =>
                IsValid((argument.Value as WorkerProjectionOptionsValue)),
            WorkerMpValueKind.PointDeltaReportOptions =>
                IsValid((argument.Value as WorkerPointDeltaReportOptionsValue)),
            WorkerMpValueKind.UdpTransmitSettings =>
                (argument.Value as WorkerUdpTransmitSettingsValue) is { IpAddress: not null, Port: >= 0 and <= 65535 },
            _ => false
        };

    public static bool HasOutputValueForKind(WorkerMpOutputValue output) =>
        output.Kind switch
        {
            WorkerMpValueKind.FitConstraintScalarOptions =>
                IsValid((output.ReadValue() as WorkerFitConstraintScalarOptionsValue)),
            WorkerMpValueKind.ToleranceScalarOptions =>
                IsValid((output.ReadValue() as WorkerToleranceScalarOptionsValue)),
            _ => false
        };


    private static bool IsValid(WorkerAutoFilterProximitySettingsValue? value) =>
        value is not null &&
        IsEnumValue(value.SurfaceProximityMode, 3) &&
        IsEnumValue(value.PlanarProximityMode, 3) &&
        IsEnumValue(value.RadialProximityMode, 3);

    private static bool IsValid(WorkerCloudThinningOptionsValue? value) =>
        value is not null && IsEnumValue(value.Mode, 3);

    private static bool IsValid(WorkerBSplineFitOptionsValue? value) =>
        value is not null && IsEnumValue(value.SortMethod, 3) &&
        IsEnumValue(value.TerminateMethod, 2);

    private static bool IsValid(WorkerColorizationOptionsValue? value) =>
        value is not null &&
        IsEnumValue(value.ColorRangeMethod, 6) &&
        IsEnumValue(value.BaseHighColor, 3) &&
        IsEnumValue(value.BaseMidColor, 4) &&
        IsEnumValue(value.BaseLowColor, 3);

    private static bool IsValid(WorkerFitConstraintScalarOptionsValue? value) =>
        value is { High: not null, Low: not null };


    private static bool IsValid(WorkerReportOutputOptionsValue? value) =>
        value is not null &&
        IsEnumValue(value.OutputType, 5) &&
        (value.ExternalPath is not null) != (value.EmbeddedFile is not null) &&
        (value.EmbeddedFile is null ||
            value.EmbeddedFile.CollectionName is not null &&
            value.EmbeddedFile.FileName is not null);

    private static bool IsValid(WorkerReportViewOptionsValue? value) =>
        value is not null &&
        IsEnumValue(value.ViewType, 3) &&
        value.CollectionName is not null &&
        value.CalloutName is not null;

    private static bool IsValid(WorkerToleranceScalarOptionsValue? value) =>
        value is { High: not null, Low: not null };

    private static bool IsValid(WorkerProjectionOptionsValue? value) =>
        value?.ProjectionType is not null;

    private static bool IsValid(WorkerPointDeltaReportOptionsValue? value) =>
        value is not null &&
        IsEnumValue(value.CoordinateSystem, 3) &&
        value.DetailsFormat is not null;

    private static bool IsEnumValue(int value, int valueCount) =>
        (uint)value < (uint)valueCount;
}
