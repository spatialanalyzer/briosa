namespace Briosa.Worker.Sdk;

internal sealed partial class SpatialAnalyzerSdkAdapter
{
    private sealed partial class ComSdkCalls
    {
        public bool SetAsciiFileFormatArg(string name, string value) =>
            Sdk.SetAsciiFileFormatArg(name, value);
        public bool SetAxisNameArg(string name, string value) =>
            Sdk.SetAxisNameArg(name, value);
        public bool SetBaseColorTypeArg(string name, string value) =>
            Sdk.SetBaseColorTypeArg(name, value);
        public bool SetBaseMidColorTypeArg(string name, string value) =>
            Sdk.SetBaseMidColorTypeArg(name, value);
        public bool SetChartTypeArg(string name, string value) =>
            Sdk.SetChartTypeArg(name, value);
        public bool SetCollimationBaselineTypeArg(string name, string value) =>
            Sdk.SetCollimationBaselineTypeArg(name, value);
        public bool SetCollimationTypeArg(string name, string value) =>
            Sdk.SetCollimationTypeArg(name, value);
        public bool SetColorRangeMethodArg(string name, string value) =>
            Sdk.SetColorRangeMethodArg(name, value);
        public bool SetCoordinateSystemTypeArg(string name, string value) =>
            Sdk.SetCoordinateSystemTypeArg(name, value);
        public bool SetDatasetTypeArg(string name, string value) =>
            Sdk.SetDatasetTypeArg(name, value);
        public bool SetDynamicCircleModeArg(string name, string value) =>
            Sdk.SetDynamicCircleModeArg(name, value);
        public bool SetDynamicEllipseModeArg(string name, string value) =>
            Sdk.SetDynamicEllipseModeArg(name, value);
        public bool SetDynamicLineModeArg(string name, string value) =>
            Sdk.SetDynamicLineModeArg(name, value);
        public bool SetDynamicPlaneModeArg(string name, string value) =>
            Sdk.SetDynamicPlaneModeArg(name, value);
        public bool SetDynamicPointModeArg(string name, string value) =>
            Sdk.SetDynamicPointModeArg(name, value);
        public bool SetEdgeModeArg(string name, string value) =>
            Sdk.SetEdgeModeArg(name, value);
        public bool SetExportDataDelimeterTypeArg(string name, string value) =>
            Sdk.SetExportDataDelimeterTypeArg(name, value);
        public bool SetExportTargetNameFormatArg(string name, string value) =>
            Sdk.SetExportTargetNameFormatArg(name, value);
        public bool SetExportVectorNameFormatArg(string name, string value) =>
            Sdk.SetExportVectorNameFormatArg(name, value);
        public bool SetGeometryTypeArg(string name, string value) =>
            Sdk.SetGeometryTypeArg(name, value);
        public bool SetInstTypeNameArg(string name, string value) =>
            Sdk.SetInstTypeNameArg(name, value);
        public bool SetObjectTypeArg(string name, string value) =>
            Sdk.SetObjectTypeArg(name, value);
        public bool SetOffsetDirectionTypeArg(string name, string value) =>
            Sdk.SetOffsetDirectionTypeArg(name, value);
        public bool SetPointFilterInputTypeArg(string name, string value) =>
            Sdk.SetPointFilterInputTypeArg(name, value);
        public bool SetRelWeightingModeArg(string name, string value) =>
            Sdk.SetRelWeightingModeArg(name, value);
        public bool SetRenderModeTypeArg(string name, string value) =>
            Sdk.SetRenderModeTypeArg(name, value);
        public bool SetReportPageSettingsArg(string name, string value) =>
            Sdk.SetReportPageSettingsArg(name, value);
        public bool SetSaturationLimitTypeArg(string name, string value) =>
            Sdk.SetSaturationLimitTypeArg(name, value);
        public bool SetShowUsmnDialogTypeArg(string name, string value) =>
            Sdk.SetShowUsmnDialogTypeArg(name, value);
        public bool SetSurfaceAnalysisModeArg(string name, string value) =>
            Sdk.SetSurfaceAnalysisModeArg(name, value);
        public bool SetSurfDissectModeTypeArg(string name, string value) =>
            Sdk.SetSurfDissectModeTypeArg(name, value);
        public bool SetTargetComputationMethodArg(string name, string value) =>
            Sdk.SetTargetComputationMethodArg(name, value);
        public bool SetTranslucencyTypeArg(string name, string value) =>
            Sdk.SetTranslucencyTypeArg(name, value);
        public bool SetCompTechniqueArg(string name, string value) =>
            Sdk.SetCompTechniqueArg(name, value);
        public bool SetDegreeOfFreedomArg(string name, string value) =>
            Sdk.SetDegreeOfFreedomArg(name, value);
        public bool SetFitMethodArg(string name, string value) =>
            Sdk.SetFitMethodArg(name, value);
        public bool SetMeasuredSideForPlanarOffsetArg(string name, string value) =>
            Sdk.SetMeasuredSideForPlanarOffsetArg(name, value);
        public bool SetMeasuredSideForRadialOffsetArg(string name, string value) =>
            Sdk.SetMeasuredSideForRadialOffsetArg(name, value);
        public bool SetMPDialogInteractionModeArg(string name, string value) =>
            Sdk.SetMPDialogInteractionModeArg(name, value);
        public bool SetMPInteractionModeArg(string name, string value) =>
            Sdk.SetMPInteractionModeArg(name, value);
        public bool SetNormalDirectionArg(string name, string value) =>
            Sdk.SetNormalDirectionArg(name, value);
        public bool SetSAInteractionModeArg(string name, string value) =>
            Sdk.SetSAInteractionModeArg(name, value);
        public bool SetSlotTypeArg(string name, string value) =>
            Sdk.SetSlotTypeArg(name, value);
        public bool SetSphereFitComputationModeArg(string name, string value) =>
            Sdk.SetSphereFitComputationModeArg(name, value);
        public bool SetWindowStateArg(string name, string value) =>
            Sdk.SetWindowStateArg(name, value);

        public bool SetAutoFilterProximitySettingsArg(
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
            bool assertPlaneBoundaries) =>
            Sdk.SetAutoFilterProximitySettingsArg(
                name,
                surfaceInclusionProximity,
                edgeExclusionProximity,
                planarInclusionProximity,
                planarExclusionProximity,
                radialInclusionProximity,
                geometryExtractionTolerance,
                surfaceProximityMode,
                planarProximityMode,
                radialProximityMode,
                projectToPlane,
                assertPlaneBoundaries);

        public bool SetCloudThinningOptionsArg(
            string name,
            string mode,
            int pointIncrement,
            int minimumNumberOfPoints,
            int maximumNumberOfPoints) =>
            Sdk.SetCloudThinningOptionsArg(
                name,
                mode,
                pointIncrement,
                minimumNumberOfPoints,
                maximumNumberOfPoints);

        public bool SetColorizationOptionsArg(
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
            double lowTolerance) =>
            Sdk.SetColorizationOptionsArg(
                name,
                colorRangeMethod,
                baseHighColor,
                baseMidColor,
                baseLowColor,
                drawTubes,
                drawArrowheads,
                indicateValues,
                vectorMagnification,
                vectorWidth,
                drawBlotches,
                blotchSize,
                showOutOfToleranceOnly,
                showColorBarInView,
                showColorBarPercentages,
                showColorBarFractions,
                highSaturationLimit,
                lowSaturationLimit,
                highTolerance,
                lowTolerance);

        public bool SetFitConstraintScalarOptionsArg(
            string name,
            bool useHigh,
            double highTolerance,
            bool useLow,
            double lowTolerance) =>
            Sdk.SetFitConstraintScalarOptionsArg(
                name,
                useHigh,
                highTolerance,
                useLow,
                lowTolerance);

        public bool SetFitDofOptionsArg(
            string name,
            bool allowX,
            bool allowY,
            bool allowZ,
            bool allowRx,
            bool allowRy,
            bool allowRz,
            bool rotateAboutCentroid) =>
            Sdk.SetFitDofOptionsArg(
                name,
                allowX,
                allowY,
                allowZ,
                allowRx,
                allowRy,
                allowRz,
                rotateAboutCentroid);

        public bool SetReportOutputOptionsArg(
            string name,
            string outputType,
            string pathOrEmbeddedName) =>
            Sdk.SetReportOutputOptionsArg(name, outputType, pathOrEmbeddedName);

        public bool SetReportViewOptionsArg(
            string name,
            string viewType,
            string collectionName,
            string calloutName) =>
            Sdk.SetReportViewOptionsArg(
                name,
                viewType,
                collectionName,
                calloutName);

        public bool SetToleranceScalarOptionsArg(
            string name,
            bool useHigh,
            double highTolerance,
            bool useLow,
            double lowTolerance) =>
            Sdk.SetToleranceScalarOptionsArg(
                name,
                useHigh,
                highTolerance,
                useLow,
                lowTolerance);

        public bool SetProjectionOptionsArg(
            string name,
            string projectionType,
            bool ignoreEdgeProjections,
            bool overrideTargetOffsets,
            double overrideTargetOffsetsValue,
            bool addExtraMaterialThickness,
            double extraMaterialThicknessValue) =>
            Sdk.SetProjectionOptionsArg(
                name,
                projectionType,
                ignoreEdgeProjections,
                overrideTargetOffsets,
                overrideTargetOffsetsValue,
                addExtraMaterialThickness,
                extraMaterialThicknessValue);

        public bool SetPointDeltaReportOptionsArg(
            string name,
            string coordinateSystem,
            string detailsFormat,
            bool showPointA,
            bool showPointB,
            bool showDelta,
            bool showMagnitude,
            bool showComponent1,
            bool showComponent2,
            bool showComponent3,
            bool sortPointNames,
            bool showToleranceFields,
            bool colorizeInToleranceFields) =>
            Sdk.SetPointDeltaReportOptionsArg(
                name,
                coordinateSystem,
                detailsFormat,
                showPointA,
                showPointB,
                showDelta,
                showMagnitude,
                showComponent1,
                showComponent2,
                showComponent3,
                sortPointNames,
                showToleranceFields,
                colorizeInToleranceFields);

        public bool GetFitConstraintScalarOptionsArg(
            string name,
            ref bool useHigh,
            ref double highTolerance,
            ref bool useLow,
            ref double lowTolerance) =>
            Sdk.GetFitConstraintScalarOptionsArg(
                name,
                ref useHigh,
                ref highTolerance,
                ref useLow,
                ref lowTolerance);

        public bool GetToleranceScalarOptionsArg(
            string name,
            ref bool useHigh,
            ref double highTolerance,
            ref bool useLow,
            ref double lowTolerance) =>
            Sdk.GetToleranceScalarOptionsArg(
                name,
                ref useHigh,
                ref highTolerance,
                ref useLow,
                ref lowTolerance);
    }
}
