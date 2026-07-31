using System.Reflection;
using Briosa.Worker.Sdk;

namespace Briosa.Worker.Tests;

public sealed partial class SpatialAnalyzerSdkAdapterTests
{
    public static TheoryData<object, object, string, string>
        SpecializedEnumCases => new()
        {
            { SdkValueKind.AsciiImportFileFormat, new SdkSpecializedEnumValue<SdkAsciiImportFileFormatValue>(SdkAsciiImportFileFormatValue.Xyz), "SetAsciiFileFormatArg", "X Y Z" },
            { SdkValueKind.AsciiFrameSetFormat, new SdkSpecializedEnumValue<SdkAsciiFrameSetFormatValue>(SdkAsciiFrameSetFormatValue.FrameNameXyzEulerZyxTimestamp), "SetAsciiFileFormatArg", "FrameName X Y Z  Euler ZYX [Timestamp]" },
            { SdkValueKind.AxisIdentifier, new SdkSpecializedEnumValue<SdkAxisIdentifierValue>(SdkAxisIdentifierValue.PositiveX), "SetAxisNameArg", "+X Axis" },
            { SdkValueKind.WcfAxisIdentifier, new SdkSpecializedEnumValue<SdkWcfAxisIdentifierValue>(SdkWcfAxisIdentifierValue.X), "SetAxisNameArg", "X Axis" },
            { SdkValueKind.BaseColorType, new SdkSpecializedEnumValue<SdkBaseColorTypeValue>(SdkBaseColorTypeValue.Red), "SetBaseColorTypeArg", "Red" },
            { SdkValueKind.BaseMidColorType, new SdkSpecializedEnumValue<SdkBaseMidColorTypeValue>(SdkBaseMidColorTypeValue.Gray), "SetBaseMidColorTypeArg", "Gray" },
            { SdkValueKind.ChartType, new SdkSpecializedEnumValue<SdkChartTypeValue>(SdkChartTypeValue.RunChart), "SetChartTypeArg", "Run Chart" },
            { SdkValueKind.CollimationBaselineType, new SdkSpecializedEnumValue<SdkCollimationBaselineTypeValue>(SdkCollimationBaselineTypeValue.DeterminedByValue), "SetCollimationBaselineTypeArg", "Determined By Value" },
            { SdkValueKind.CollimationType, new SdkSpecializedEnumValue<SdkCollimationTypeValue>(SdkCollimationTypeValue.FullCollimation), "SetCollimationTypeArg", "Full Collimation" },
            { SdkValueKind.ColorRangeMethod, new SdkSpecializedEnumValue<SdkColorRangeMethodValue>(SdkColorRangeMethodValue.SingleColor), "SetColorRangeMethodArg", "Single Color" },
            { SdkValueKind.CoordinateSystemType, new SdkSpecializedEnumValue<SdkCoordinateSystemTypeValue>(SdkCoordinateSystemTypeValue.Cartesian), "SetCoordinateSystemTypeArg", "Cartesian" },
            { SdkValueKind.VectorComponent, new SdkSpecializedEnumValue<SdkVectorComponentValue>(SdkVectorComponentValue.X), "SetDatasetTypeArg", "X" },
            { SdkValueKind.DynamicCircleMode, new SdkSpecializedEnumValue<SdkDynamicCircleModeValue>(SdkDynamicCircleModeValue.TwoConesIntersection), "SetDynamicCircleModeArg", "Two Cones Intersection" },
            { SdkValueKind.DynamicEllipseMode, new SdkSpecializedEnumValue<SdkDynamicEllipseModeValue>(SdkDynamicEllipseModeValue.CylinderPlaneIntersection), "SetDynamicEllipseModeArg", "Cylinder and Plane Intersection" },
            { SdkValueKind.DynamicLineMode, new SdkSpecializedEnumValue<SdkDynamicLineModeValue>(SdkDynamicLineModeValue.ConeAxis), "SetDynamicLineModeArg", "Cone Axis" },
            { SdkValueKind.DynamicPlaneMode, new SdkSpecializedEnumValue<SdkDynamicPlaneModeValue>(SdkDynamicPlaneModeValue.TwoConesFirstConeAxis), "SetDynamicPlaneModeArg", "Twp Cones Intersection - Hold Normal to First Cone Axis" },
            { SdkValueKind.DynamicPointMode, new SdkSpecializedEnumValue<SdkDynamicPointModeValue>(SdkDynamicPointModeValue.IntersectionLinePlane), "SetDynamicPointModeArg", "Intersection of Line and Plane" },
            { SdkValueKind.EdgeMode, new SdkSpecializedEnumValue<SdkEdgeModeValue>(SdkEdgeModeValue.IncludeEdges), "SetEdgeModeArg", "Include Edges" },
            { SdkValueKind.ExportDataDelimiterType, new SdkSpecializedEnumValue<SdkExportDataDelimiterTypeValue>(SdkExportDataDelimiterTypeValue.Comma), "SetExportDataDelimeterTypeArg", "Comma" },
            { SdkValueKind.ExportTargetNameFormat, new SdkSpecializedEnumValue<SdkExportTargetNameFormatValue>(SdkExportTargetNameFormatValue.Target), "SetExportTargetNameFormatArg", "Target" },
            { SdkValueKind.ExportVectorNameFormat, new SdkSpecializedEnumValue<SdkExportVectorNameFormatValue>(SdkExportVectorNameFormatValue.Vector), "SetExportVectorNameFormatArg", "Vector" },
            { SdkValueKind.GeometryType, new SdkSpecializedEnumValue<SdkGeometryTypeValue>(SdkGeometryTypeValue.Line), "SetGeometryTypeArg", "Line" },
            { SdkValueKind.InstrumentType, new SdkSpecializedEnumValue<SdkInstrumentTypeValue>(SdkInstrumentTypeValue.CreaformVxElements), "SetInstTypeNameArg", "Creaform VXelements" },
            { SdkValueKind.ObjectType, new SdkSpecializedEnumValue<SdkObjectTypeValue>(SdkObjectTypeValue.Cone), "SetObjectTypeArg", "Cone" },
            { SdkValueKind.OffsetDirectionType, new SdkSpecializedEnumValue<SdkOffsetDirectionTypeValue>(SdkOffsetDirectionTypeValue.PositiveOnly), "SetOffsetDirectionTypeArg", "Positive only" },
            { SdkValueKind.PointFilterInputType, new SdkSpecializedEnumValue<SdkPointFilterInputTypeValue>(SdkPointFilterInputTypeValue.CardinalPoints), "SetPointFilterInputTypeArg", "Cardinal Points" },
            { SdkValueKind.RelationshipWeightingMode, new SdkSpecializedEnumValue<SdkRelationshipWeightingModeValue>(SdkRelationshipWeightingModeValue.ResetAllWeights), "SetRelWeightingModeArg", "Reset All weights to 1.0" },
            { SdkValueKind.RenderModeType, new SdkSpecializedEnumValue<SdkRenderModeTypeValue>(SdkRenderModeTypeValue.SolidAndEdges), "SetRenderModeTypeArg", "Solid+Edges" },
            { SdkValueKind.ReportPageOrientation, new SdkSpecializedEnumValue<SdkReportPageOrientationValue>(SdkReportPageOrientationValue.Landscape), "SetReportPageSettingsArg", "Landscape" },
            { SdkValueKind.SaturationLimitType, new SdkSpecializedEnumValue<SdkSaturationLimitTypeValue>(SdkSaturationLimitTypeValue.SigmaRule), "SetSaturationLimitTypeArg", "Sigma Rule" },
            { SdkValueKind.ShowUsmnDialogType, new SdkSpecializedEnumValue<SdkShowUsmnDialogTypeValue>(SdkShowUsmnDialogTypeValue.OnToleranceViolation), "SetShowUsmnDialogTypeArg", "On Tolerance Violation" },
            { SdkValueKind.SurfaceAnalysisMode, new SdkSpecializedEnumValue<SdkSurfaceAnalysisModeValue>(SdkSurfaceAnalysisModeValue.DeviationRms), "SetSurfaceAnalysisModeArg", "Deviation RMS" },
            { SdkValueKind.SurfaceDissectionModeType, new SdkSpecializedEnumValue<SdkSurfaceDissectionModeTypeValue>(SdkSurfaceDissectionModeTypeValue.SelectFaces), "SetSurfDissectModeTypeArg", "Select Faces" },
            { SdkValueKind.TargetComputationMethod, new SdkSpecializedEnumValue<SdkTargetComputationMethodValue>(SdkTargetComputationMethodValue.UseOnlyMostRecentShot), "SetTargetComputationMethodArg", "Use only most recent shot" },
            { SdkValueKind.TranslucencyType, new SdkSpecializedEnumValue<SdkTranslucencyTypeValue>(SdkTranslucencyTypeValue.Translucent), "SetTranslucencyTypeArg", "Translucent" }
        };

    [Theory]
    [MemberData(nameof(SpecializedEnumCases))]
    public void SpecializedEnumUsesExactSetterAndReviewedSdkText(
        object kindValue,
        object specializedValue,
        string setter,
        string sdkText)
    {
        var kind = Assert.IsType<SdkValueKind>(kindValue);
        var value = Assert.IsAssignableFrom<ISdkSpecializedEnumValue>(specializedValue);
        using var calls = new RecordingSdkCalls();
        using var adapter = new SpatialAnalyzerSdkAdapter(calls);
        var command = new SdkCommand(
            "specialized-enum",
            "Specialized Enum",
            [new("Value", kind, SpecializedEnumValue: value, SdkBinding: setter)],
            []);

        var result = adapter.Execute(command);

        Assert.True(result.MpResult.Succeeded);
        Assert.Contains($"{setter}:Value", calls.Events);
        Assert.Equal(sdkText, calls.StringArguments["Value"]);
    }

    [Fact]
    public void EveryReviewedSdkEnumValueHasAnExactTextMapping()
    {
        var methods = typeof(SdkSpecializedValueCodec)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method =>
                method.Name == nameof(SdkSpecializedValueCodec.ToSdkString) &&
                method.GetParameters() is [{ ParameterType.IsEnum: true }]);
        var mappedValueCount = 0;

        foreach (var method in methods)
        {
            var parameterType = method.GetParameters()[0].ParameterType;
            foreach (var value in Enum.GetValues(parameterType))
            {
                var result = Assert.IsType<string>(method.Invoke(null, [value]));
                Assert.False(string.IsNullOrWhiteSpace(result));
                mappedValueCount++;
            }
        }

        Assert.Equal(454, mappedValueCount);
    }

    [Fact]
    public void InstrumentTypeMappingCoversRetainedExactTargetEvidence()
    {
        Assert.Equal(190, Enum.GetValues<SdkInstrumentTypeValue>().Length);
        Assert.Equal("Faro Vantage", SdkSpecializedValueCodec.ToSdkString(SdkInstrumentTypeValue.FaroVantage));
        Assert.Equal(
            "CimCore Arm 6DOF: 3012i, 5012, 1.2m",
            SdkSpecializedValueCodec.ToSdkString(SdkInstrumentTypeValue.CimCoreArm6Dof3012i501212m));
    }

    [Fact]
    public void SpecializedStructuresUseExactSettersAndScalarGetters()
    {
        using var calls = new RecordingSdkCalls();
        using var adapter = new SpatialAnalyzerSdkAdapter(calls);
        var high = new SdkToleranceLimit(true, 1.25);
        var low = new SdkToleranceLimit(false, -2.5);
        var command = new SdkCommand(
            "specialized-structures",
            "Specialized Structures",
            [
                new("Auto", SdkValueKind.AutoFilterProximitySettings, AutoFilterProximitySettingsValue: new(1, 2, 3, 4, 5, 6, SdkOffsetDirectionTypeValue.Both, SdkOffsetDirectionTypeValue.PositiveOnly, SdkOffsetDirectionTypeValue.NegativeOnly, true, false), SdkBinding: "SetAutoFilterProximitySettingsArg"),
                new("Thin", SdkValueKind.CloudThinningOptions, CloudThinningOptionsValue: new(SdkCloudThinningModeValue.NthPoint, 2, 3, 4), SdkBinding: "SetCloudThinningOptionsArg"),
                new("Color", SdkValueKind.ColorizationOptions, ColorizationOptionsValue: new(SdkColorRangeMethodValue.DiscreteColors, SdkBaseColorTypeValue.Red, SdkBaseMidColorTypeValue.Gray, SdkBaseColorTypeValue.Blue, true, false, true, 2, 3, false, 4, true, false, true, false, 5, -5, 1, -1), SdkBinding: "SetColorizationOptionsArg"),
                new("Constraint", SdkValueKind.FitConstraintScalarOptions, FitConstraintScalarOptionsValue: new(high, low), SdkBinding: "SetFitConstraintScalarOptionsArg"),
                new("Dof", SdkValueKind.FitDegreeOfFreedomOptions, FitDegreeOfFreedomOptionsValue: new(true, false, true, false, true, false, true), SdkBinding: "SetFitDofOptionsArg"),
                new("Output", SdkValueKind.ReportOutputOptions, ReportOutputOptionsValue: new(SdkReportOutputTypeValue.Pdf, "report.pdf", null), SdkBinding: "SetReportOutputOptionsArg"),
                new("Embedded Output", SdkValueKind.ReportOutputOptions, ReportOutputOptionsValue: new(SdkReportOutputTypeValue.SaReport, null, new("Collection", "Report")), SdkBinding: "SetReportOutputOptionsArg"),
                new("View", SdkValueKind.ReportViewOptions, ReportViewOptionsValue: new(SdkReportViewTypeValue.CalloutView, "Collection", "Callout"), SdkBinding: "SetReportViewOptionsArg"),
                new("Tolerance", SdkValueKind.ToleranceScalarOptions, ToleranceScalarOptionsValue: new(high, low), SdkBinding: "SetToleranceScalarOptionsArg")
            ],
            [
                new("Constraint Result", SdkValueKind.FitConstraintScalarOptions, "GetFitConstraintScalarOptionsArg"),
                new("Tolerance Result", SdkValueKind.ToleranceScalarOptions, "GetToleranceScalarOptionsArg")
            ]);

        var result = adapter.Execute(command);

        Assert.True(result.MpResult.Succeeded);
        Assert.Equal(9, calls.SpecializedArguments.Count);
        Assert.Equal([0, 1, 2], calls.SpecializedArguments["Auto"][6..9]);
        Assert.Equal("Nth Point", calls.SpecializedArguments["Thin"][0]);
        Assert.Equal("PDF", calls.SpecializedArguments["Output"][0]);
        Assert.Equal("report.pdf", calls.SpecializedArguments["Output"][1]);
        Assert.Equal("SAReport", calls.SpecializedArguments["Embedded Output"][0]);
        Assert.Equal("Collection::Report", calls.SpecializedArguments["Embedded Output"][1]);
        Assert.All(result.OutputValues, output => Assert.True(output.Retrieved));
        Assert.Equal(1.25, result.OutputValues[0].FitConstraintScalarOptionsValue!.High.Value);
        Assert.Equal(-2.5, result.OutputValues[1].ToleranceScalarOptionsValue!.Low.Value);
    }

    [Fact]
    public void SetterOnlySpecializedKindCannotBeRetrieved()
    {
        using var calls = new RecordingSdkCalls();
        using var adapter = new SpatialAnalyzerSdkAdapter(calls);
        var command = new SdkCommand(
            "setter-only-output",
            "Setter Only Output",
            [],
            [new("Mode", SdkValueKind.RenderModeType)]);

        var result = adapter.Execute(command);

        var output = Assert.Single(result.OutputValues);
        Assert.False(output.Retrieved);
        Assert.Equal("sdk-output-retrieval-failed", result.DiagnosticCode);
    }
    [Fact]
    public void MismatchedSpecializedEnumTypeIsRejectedBeforeExecution()
    {
        using var calls = new RecordingSdkCalls();
        using var adapter = new SpatialAnalyzerSdkAdapter(calls);
        var command = new SdkCommand(
            "wrong-specialized-enum",
            "Wrong Specialized Enum",
            [new(
                "Mode",
                SdkValueKind.RenderModeType,
                SpecializedEnumValue: new SdkSpecializedEnumValue<SdkEdgeModeValue>(SdkEdgeModeValue.EdgesOnly),
                SdkBinding: "SetRenderModeTypeArg")],
            []);

        var result = adapter.Execute(command);

        Assert.False(result.ExecuteStepReturned);
        Assert.Equal("sdk-argument-rejected", result.DiagnosticCode);
        Assert.DoesNotContain("ExecuteStep", calls.Events);
    }
}
