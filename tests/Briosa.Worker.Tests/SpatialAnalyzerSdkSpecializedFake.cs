namespace Briosa.Worker.Tests;

public sealed partial class SpatialAnalyzerSdkAdapterTests
{
    private sealed partial class RecordingSdkCalls
    {
        public Dictionary<string, object?[]> SpecializedArguments { get; } = [];

        public bool SetAsciiFileFormatArg(string name, string value) => RecordStringSetter(nameof(SetAsciiFileFormatArg), name, value);
        public bool SetAxisNameArg(string name, string value) => RecordStringSetter(nameof(SetAxisNameArg), name, value);
        public bool SetBaseColorTypeArg(string name, string value) => RecordStringSetter(nameof(SetBaseColorTypeArg), name, value);
        public bool SetBaseMidColorTypeArg(string name, string value) => RecordStringSetter(nameof(SetBaseMidColorTypeArg), name, value);
        public bool SetChartTypeArg(string name, string value) => RecordStringSetter(nameof(SetChartTypeArg), name, value);
        public bool SetCollimationBaselineTypeArg(string name, string value) => RecordStringSetter(nameof(SetCollimationBaselineTypeArg), name, value);
        public bool SetCollimationTypeArg(string name, string value) => RecordStringSetter(nameof(SetCollimationTypeArg), name, value);
        public bool SetColorRangeMethodArg(string name, string value) => RecordStringSetter(nameof(SetColorRangeMethodArg), name, value);
        public bool SetCoordinateSystemTypeArg(string name, string value) => RecordStringSetter(nameof(SetCoordinateSystemTypeArg), name, value);
        public bool SetDatasetTypeArg(string name, string value) => RecordStringSetter(nameof(SetDatasetTypeArg), name, value);
        public bool SetDynamicCircleModeArg(string name, string value) => RecordStringSetter(nameof(SetDynamicCircleModeArg), name, value);
        public bool SetDynamicEllipseModeArg(string name, string value) => RecordStringSetter(nameof(SetDynamicEllipseModeArg), name, value);
        public bool SetDynamicLineModeArg(string name, string value) => RecordStringSetter(nameof(SetDynamicLineModeArg), name, value);
        public bool SetDynamicPlaneModeArg(string name, string value) => RecordStringSetter(nameof(SetDynamicPlaneModeArg), name, value);
        public bool SetDynamicPointModeArg(string name, string value) => RecordStringSetter(nameof(SetDynamicPointModeArg), name, value);
        public bool SetEdgeModeArg(string name, string value) => RecordStringSetter(nameof(SetEdgeModeArg), name, value);
        public bool SetExportDataDelimeterTypeArg(string name, string value) => RecordStringSetter(nameof(SetExportDataDelimeterTypeArg), name, value);
        public bool SetExportTargetNameFormatArg(string name, string value) => RecordStringSetter(nameof(SetExportTargetNameFormatArg), name, value);
        public bool SetExportVectorNameFormatArg(string name, string value) => RecordStringSetter(nameof(SetExportVectorNameFormatArg), name, value);
        public bool SetGeometryTypeArg(string name, string value) => RecordStringSetter(nameof(SetGeometryTypeArg), name, value);
        public bool SetInstTypeNameArg(string name, string value) => RecordStringSetter(nameof(SetInstTypeNameArg), name, value);
        public bool SetObjectTypeArg(string name, string value) => RecordStringSetter(nameof(SetObjectTypeArg), name, value);
        public bool SetOffsetDirectionTypeArg(string name, string value) => RecordStringSetter(nameof(SetOffsetDirectionTypeArg), name, value);
        public bool SetPointFilterInputTypeArg(string name, string value) => RecordStringSetter(nameof(SetPointFilterInputTypeArg), name, value);
        public bool SetRelWeightingModeArg(string name, string value) => RecordStringSetter(nameof(SetRelWeightingModeArg), name, value);
        public bool SetRenderModeTypeArg(string name, string value) => RecordStringSetter(nameof(SetRenderModeTypeArg), name, value);
        public bool SetReportPageSettingsArg(string name, string value) => RecordStringSetter(nameof(SetReportPageSettingsArg), name, value);
        public bool SetSaturationLimitTypeArg(string name, string value) => RecordStringSetter(nameof(SetSaturationLimitTypeArg), name, value);
        public bool SetShowUsmnDialogTypeArg(string name, string value) => RecordStringSetter(nameof(SetShowUsmnDialogTypeArg), name, value);
        public bool SetSurfaceAnalysisModeArg(string name, string value) => RecordStringSetter(nameof(SetSurfaceAnalysisModeArg), name, value);
        public bool SetSurfDissectModeTypeArg(string name, string value) => RecordStringSetter(nameof(SetSurfDissectModeTypeArg), name, value);
        public bool SetTargetComputationMethodArg(string name, string value) => RecordStringSetter(nameof(SetTargetComputationMethodArg), name, value);
        public bool SetTranslucencyTypeArg(string name, string value) => RecordStringSetter(nameof(SetTranslucencyTypeArg), name, value);

        public bool SetAutoFilterProximitySettingsArg(string name, double sip, double eep, double pip, double pep, double rip, double gt, int spm, int ppm, int rpm, bool pp, bool apb) =>
            RecordSpecialized(nameof(SetAutoFilterProximitySettingsArg), name, sip, eep, pip, pep, rip, gt, spm, ppm, rpm, pp, apb);

        public bool SetCloudThinningOptionsArg(string name, string mode, int increment, int minimum, int maximum) =>
            RecordSpecialized(nameof(SetCloudThinningOptionsArg), name, mode, increment, minimum, maximum);

        public bool SetColorizationOptionsArg(string name, string range, string high, string mid, string low, bool tubes, bool arrows, bool values, double magnification, int width, bool blotches, double blotchSize, bool outOfToleranceOnly, bool colorBar, bool percentages, bool fractions, double highSaturation, double lowSaturation, double highTolerance, double lowTolerance) =>
            RecordSpecialized(nameof(SetColorizationOptionsArg), name, range, high, mid, low, tubes, arrows, values, magnification, width, blotches, blotchSize, outOfToleranceOnly, colorBar, percentages, fractions, highSaturation, lowSaturation, highTolerance, lowTolerance);

        public bool SetFitConstraintScalarOptionsArg(string name, bool useHigh, double high, bool useLow, double low) =>
            RecordSpecialized(nameof(SetFitConstraintScalarOptionsArg), name, useHigh, high, useLow, low);

        public bool SetFitDofOptionsArg(string name, bool x, bool y, bool z, bool rx, bool ry, bool rz, bool centroid) =>
            RecordSpecialized(nameof(SetFitDofOptionsArg), name, x, y, z, rx, ry, rz, centroid);

        public bool SetReportOutputOptionsArg(string name, string type, string path) =>
            RecordSpecialized(nameof(SetReportOutputOptionsArg), name, type, path);

        public bool SetReportViewOptionsArg(string name, string type, string collection, string callout) =>
            RecordSpecialized(nameof(SetReportViewOptionsArg), name, type, collection, callout);

        public bool SetToleranceScalarOptionsArg(string name, bool useHigh, double high, bool useLow, double low) =>
            RecordSpecialized(nameof(SetToleranceScalarOptionsArg), name, useHigh, high, useLow, low);

        public bool GetFitConstraintScalarOptionsArg(string name, ref bool useHigh, ref double high, ref bool useLow, ref double low) =>
            ReturnScalarOptions(nameof(GetFitConstraintScalarOptionsArg), name, ref useHigh, ref high, ref useLow, ref low);

        public bool GetToleranceScalarOptionsArg(string name, ref bool useHigh, ref double high, ref bool useLow, ref double low) =>
            ReturnScalarOptions(nameof(GetToleranceScalarOptionsArg), name, ref useHigh, ref high, ref useLow, ref low);

        private bool RecordSpecialized(string method, string name, params object?[] values)
        {
            SpecializedArguments[name] = values;
            return RecordSetter(method, name);
        }

        private bool ReturnScalarOptions(
            string method,
            string name,
            ref bool useHigh,
            ref double high,
            ref bool useLow,
            ref double low)
        {
            Events.Add($"{method}:{name}");
            if (name == FailedOutputName)
            {
                return false;
            }

            useHigh = true;
            high = 1.25;
            useLow = false;
            low = -2.5;
            return true;
        }
    }
}
