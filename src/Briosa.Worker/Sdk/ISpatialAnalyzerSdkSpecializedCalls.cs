namespace Briosa.Worker.Sdk;

internal partial interface ISpatialAnalyzerSdkCalls
{
    bool SetAsciiFileFormatArg(string name, string value);
    bool SetAxisNameArg(string name, string value);
    bool SetBaseColorTypeArg(string name, string value);
    bool SetBaseMidColorTypeArg(string name, string value);
    bool SetChartTypeArg(string name, string value);
    bool SetCollimationBaselineTypeArg(string name, string value);
    bool SetCollimationTypeArg(string name, string value);
    bool SetColorRangeMethodArg(string name, string value);
    bool SetCoordinateSystemTypeArg(string name, string value);
    bool SetDatasetTypeArg(string name, string value);
    bool SetDynamicCircleModeArg(string name, string value);
    bool SetDynamicEllipseModeArg(string name, string value);
    bool SetDynamicLineModeArg(string name, string value);
    bool SetDynamicPlaneModeArg(string name, string value);
    bool SetDynamicPointModeArg(string name, string value);
    bool SetEdgeModeArg(string name, string value);
    bool SetExportDataDelimeterTypeArg(string name, string value);
    bool SetExportTargetNameFormatArg(string name, string value);
    bool SetExportVectorNameFormatArg(string name, string value);
    bool SetGeometryTypeArg(string name, string value);
    bool SetInstTypeNameArg(string name, string value);
    bool SetObjectTypeArg(string name, string value);
    bool SetOffsetDirectionTypeArg(string name, string value);
    bool SetPointFilterInputTypeArg(string name, string value);
    bool SetRelWeightingModeArg(string name, string value);
    bool SetRenderModeTypeArg(string name, string value);
    bool SetReportPageSettingsArg(string name, string value);
    bool SetSaturationLimitTypeArg(string name, string value);
    bool SetShowUsmnDialogTypeArg(string name, string value);
    bool SetSurfaceAnalysisModeArg(string name, string value);
    bool SetSurfDissectModeTypeArg(string name, string value);
    bool SetTargetComputationMethodArg(string name, string value);
    bool SetTranslucencyTypeArg(string name, string value);

    bool SetAutoFilterProximitySettingsArg(
        string name,
        double surfaceInclusionProximity,
        double edgeExclusionProximity,
        double planarInclusionProximity,
        double planarExclusionProximity,
        double radialInclusionProximity,
        double geometryExtractionTolerance,
        int surfaceProximityMode,
        int planarProximityMode,
        int radialProximityMode,
        bool projectToPlane,
        bool assertPlaneBoundaries);

    bool SetCloudThinningOptionsArg(
        string name,
        string mode,
        int pointIncrement,
        int minimumNumberOfPoints,
        int maximumNumberOfPoints);

    bool SetColorizationOptionsArg(
        string name,
        string colorRangeMethod,
        string baseHighColor,
        string baseMidColor,
        string baseLowColor,
        bool drawTubes,
        bool drawArrowheads,
        bool indicateValues,
        double vectorMagnification,
        int vectorWidth,
        bool drawBlotches,
        double blotchSize,
        bool showOutOfToleranceOnly,
        bool showColorBarInView,
        bool showColorBarPercentages,
        bool showColorBarFractions,
        double highSaturationLimit,
        double lowSaturationLimit,
        double highTolerance,
        double lowTolerance);

    bool SetFitConstraintScalarOptionsArg(
        string name,
        bool useHigh,
        double highTolerance,
        bool useLow,
        double lowTolerance);

    bool SetFitDofOptionsArg(
        string name,
        bool allowX,
        bool allowY,
        bool allowZ,
        bool allowRx,
        bool allowRy,
        bool allowRz,
        bool rotateAboutCentroid);

    bool SetReportOutputOptionsArg(string name, string outputType, string pathOrEmbeddedName);
    bool SetReportViewOptionsArg(
        string name,
        string viewType,
        string collectionName,
        string calloutName);

    bool SetToleranceScalarOptionsArg(
        string name,
        bool useHigh,
        double highTolerance,
        bool useLow,
        double lowTolerance);

    bool GetFitConstraintScalarOptionsArg(
        string name,
        ref bool useHigh,
        ref double highTolerance,
        ref bool useLow,
        ref double lowTolerance);

    bool GetToleranceScalarOptionsArg(
        string name,
        ref bool useHigh,
        ref double highTolerance,
        ref bool useLow,
        ref double lowTolerance);
}
