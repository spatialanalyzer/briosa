using Briosa.Worker.Control;
using System.Reflection;
using Briosa.Worker.Sdk;

namespace Briosa.Worker.Tests;

public sealed partial class SpatialAnalyzerSdkAdapterTests
{
    public static TheoryData<object, object, string, string>
        SpecializedEnumCases => new()
        {
            { WorkerMpValueKind.AsciiImportFileFormat, new SdkSpecializedEnumValue<SdkAsciiImportFileFormatValue>(SdkAsciiImportFileFormatValue.Xyz), "SetAsciiFileFormatArg", "X Y Z" },
            { WorkerMpValueKind.AsciiFrameSetFormat, new SdkSpecializedEnumValue<SdkAsciiFrameSetFormatValue>(SdkAsciiFrameSetFormatValue.FrameNameXyzEulerZyxTimestamp), "SetAsciiFileFormatArg", "FrameName X Y Z  Euler ZYX [Timestamp]" },
            { WorkerMpValueKind.AxisIdentifier, new SdkSpecializedEnumValue<SdkAxisIdentifierValue>(SdkAxisIdentifierValue.PositiveX), "SetAxisNameArg", "+X Axis" },
            { WorkerMpValueKind.WcfAxisIdentifier, new SdkSpecializedEnumValue<SdkWcfAxisIdentifierValue>(SdkWcfAxisIdentifierValue.X), "SetAxisNameArg", "X Axis" },
            { WorkerMpValueKind.BaseColorType, new SdkSpecializedEnumValue<SdkBaseColorTypeValue>(SdkBaseColorTypeValue.Red), "SetBaseColorTypeArg", "Red" },
            { WorkerMpValueKind.BaseMidColorType, new SdkSpecializedEnumValue<SdkBaseMidColorTypeValue>(SdkBaseMidColorTypeValue.Gray), "SetBaseMidColorTypeArg", "Gray" },
            { WorkerMpValueKind.ChartType, new SdkSpecializedEnumValue<SdkChartTypeValue>(SdkChartTypeValue.RunChart), "SetChartTypeArg", "Run Chart" },
            { WorkerMpValueKind.CollimationBaselineType, new SdkSpecializedEnumValue<SdkCollimationBaselineTypeValue>(SdkCollimationBaselineTypeValue.DeterminedByValue), "SetCollimationBaselineTypeArg", "Determined By Value" },
            { WorkerMpValueKind.CollimationType, new SdkSpecializedEnumValue<SdkCollimationTypeValue>(SdkCollimationTypeValue.FullCollimation), "SetCollimationTypeArg", "Full Collimation" },
            { WorkerMpValueKind.ColorRangeMethod, new SdkSpecializedEnumValue<SdkColorRangeMethodValue>(SdkColorRangeMethodValue.SingleColor), "SetColorRangeMethodArg", "Single Color" },
            { WorkerMpValueKind.CoordinateSystemType, new SdkSpecializedEnumValue<SdkCoordinateSystemTypeValue>(SdkCoordinateSystemTypeValue.Cartesian), "SetCoordinateSystemTypeArg", "Cartesian" },
            { WorkerMpValueKind.VectorComponent, new SdkSpecializedEnumValue<SdkVectorComponentValue>(SdkVectorComponentValue.X), "SetDatasetTypeArg", "X" },
            { WorkerMpValueKind.DynamicCircleMode, new SdkSpecializedEnumValue<SdkDynamicCircleModeValue>(SdkDynamicCircleModeValue.TwoConesIntersection), "SetDynamicCircleModeArg", "Two Cones Intersection" },
            { WorkerMpValueKind.DynamicEllipseMode, new SdkSpecializedEnumValue<SdkDynamicEllipseModeValue>(SdkDynamicEllipseModeValue.CylinderPlaneIntersection), "SetDynamicEllipseModeArg", "Cylinder and Plane Intersection" },
            { WorkerMpValueKind.DynamicLineMode, new SdkSpecializedEnumValue<SdkDynamicLineModeValue>(SdkDynamicLineModeValue.ConeAxis), "SetDynamicLineModeArg", "Cone Axis" },
            { WorkerMpValueKind.DynamicPlaneMode, new SdkSpecializedEnumValue<SdkDynamicPlaneModeValue>(SdkDynamicPlaneModeValue.TwoConesFirstConeAxis), "SetDynamicPlaneModeArg", "Twp Cones Intersection - Hold Normal to First Cone Axis" },
            { WorkerMpValueKind.DynamicPointMode, new SdkSpecializedEnumValue<SdkDynamicPointModeValue>(SdkDynamicPointModeValue.IntersectionLinePlane), "SetDynamicPointModeArg", "Intersection of Line and Plane" },
            { WorkerMpValueKind.EdgeMode, new SdkSpecializedEnumValue<SdkEdgeModeValue>(SdkEdgeModeValue.IncludeEdges), "SetEdgeModeArg", "Include Edges" },
            { WorkerMpValueKind.ExportDataDelimiterType, new SdkSpecializedEnumValue<SdkExportDataDelimiterTypeValue>(SdkExportDataDelimiterTypeValue.Comma), "SetExportDataDelimeterTypeArg", "Comma" },
            { WorkerMpValueKind.ExportTargetNameFormat, new SdkSpecializedEnumValue<SdkExportTargetNameFormatValue>(SdkExportTargetNameFormatValue.Target), "SetExportTargetNameFormatArg", "Target" },
            { WorkerMpValueKind.ExportVectorNameFormat, new SdkSpecializedEnumValue<SdkExportVectorNameFormatValue>(SdkExportVectorNameFormatValue.Vector), "SetExportVectorNameFormatArg", "Vector" },
            { WorkerMpValueKind.GeometryType, new SdkSpecializedEnumValue<SdkGeometryTypeValue>(SdkGeometryTypeValue.Line), "SetGeometryTypeArg", "Line" },
            { WorkerMpValueKind.GdtDistanceBetweenMode, new SdkSpecializedEnumValue<SdkGdtDistanceBetweenModeValue>(SdkGdtDistanceBetweenModeValue.MinMax), "SetMPGDTOptionsDistanceBetweenModeArg", "Min/Max" },
            { WorkerMpValueKind.GdtEvaluationMethod, new SdkSpecializedEnumValue<SdkGdtEvaluationMethodValue>(SdkGdtEvaluationMethodValue.Iso2017), "SetMPGDTOptionsCheckValidatorTypeArg", "ISO 2017" },
            { WorkerMpValueKind.InstrumentType, new SdkSpecializedEnumValue<SdkInstrumentTypeValue>(SdkInstrumentTypeValue.CreaformVxElements), "SetInstTypeNameArg", "Creaform VXelements" },
            { WorkerMpValueKind.ObjectType, new SdkSpecializedEnumValue<WorkerObjectTypeValue>(WorkerObjectTypeValue.Cone), "SetObjectTypeArg", "Cone" },
            { WorkerMpValueKind.OffsetDirectionType, new SdkSpecializedEnumValue<SdkOffsetDirectionTypeValue>(SdkOffsetDirectionTypeValue.PositiveOnly), "SetOffsetDirectionTypeArg", "Positive only" },
            { WorkerMpValueKind.PointFilterInputType, new SdkSpecializedEnumValue<SdkPointFilterInputTypeValue>(SdkPointFilterInputTypeValue.CardinalPoints), "SetPointFilterInputTypeArg", "Cardinal Points" },
            { WorkerMpValueKind.RelationshipWeightingMode, new SdkSpecializedEnumValue<SdkRelationshipWeightingModeValue>(SdkRelationshipWeightingModeValue.ResetAllWeights), "SetRelWeightingModeArg", "Reset All weights to 1.0" },
            { WorkerMpValueKind.RenderModeType, new SdkSpecializedEnumValue<SdkRenderModeTypeValue>(SdkRenderModeTypeValue.SolidAndEdges), "SetRenderModeTypeArg", "Solid+Edges" },
            { WorkerMpValueKind.ReportPageOrientation, new SdkSpecializedEnumValue<SdkReportPageOrientationValue>(SdkReportPageOrientationValue.Landscape), "SetReportPageSettingsArg", "Landscape" },
            { WorkerMpValueKind.SaturationLimitType, new SdkSpecializedEnumValue<SdkSaturationLimitTypeValue>(SdkSaturationLimitTypeValue.SigmaRule), "SetSaturationLimitTypeArg", "Sigma Rule" },
            { WorkerMpValueKind.ShowUsmnDialogType, new SdkSpecializedEnumValue<SdkShowUsmnDialogTypeValue>(SdkShowUsmnDialogTypeValue.OnToleranceViolation), "SetShowUsmnDialogTypeArg", "On Tolerance Violation" },
            { WorkerMpValueKind.SurfaceAnalysisMode, new SdkSpecializedEnumValue<SdkSurfaceAnalysisModeValue>(SdkSurfaceAnalysisModeValue.DeviationRms), "SetSurfaceAnalysisModeArg", "Deviation RMS" },
            { WorkerMpValueKind.SurfaceDissectionModeType, new SdkSpecializedEnumValue<SdkSurfaceDissectionModeTypeValue>(SdkSurfaceDissectionModeTypeValue.SelectFaces), "SetSurfDissectModeTypeArg", "Select Faces" },
            { WorkerMpValueKind.TargetComputationMethod, new SdkSpecializedEnumValue<SdkTargetComputationMethodValue>(SdkTargetComputationMethodValue.UseOnlyMostRecentShot), "SetTargetComputationMethodArg", "Use only most recent shot" },
            { WorkerMpValueKind.TranslucencyType, new SdkSpecializedEnumValue<SdkTranslucencyTypeValue>(SdkTranslucencyTypeValue.Translucent), "SetTranslucencyTypeArg", "Translucent" },
            { WorkerMpValueKind.CompTechnique, new SdkSpecializedEnumValue<SdkCompTechniqueValue>(SdkCompTechniqueValue.MaxInscribed), "SetCompTechniqueArg", "Max Inscribed" },
            { WorkerMpValueKind.DegreeOfFreedom, new SdkSpecializedEnumValue<SdkDegreeOfFreedomValue>(SdkDegreeOfFreedomValue.LockFocusLocation), "SetDegreeOfFreedomArg", "Lock Focus Location" },
            { WorkerMpValueKind.FitMethod, new SdkSpecializedEnumValue<SdkFitMethodValue>(SdkFitMethodValue.MinimumRms), "SetFitMethodArg", "Minimum RMS" },
            { WorkerMpValueKind.MeasuredSideForPlanarOffset, new SdkSpecializedEnumValue<SdkMeasuredSideForPlanarOffsetValue>(SdkMeasuredSideForPlanarOffsetValue.AbovePlane), "SetMeasuredSideForPlanarOffsetArg", "Above Plane" },
            { WorkerMpValueKind.MeasuredSideForRadialOffset, new SdkSpecializedEnumValue<SdkMeasuredSideForRadialOffsetValue>(SdkMeasuredSideForRadialOffsetValue.Outside), "SetMeasuredSideForRadialOffsetArg", "Outside" },
            { WorkerMpValueKind.MpDialogInteractionMode, new SdkSpecializedEnumValue<SdkMpDialogInteractionModeValue>(SdkMpDialogInteractionModeValue.AllowApplicationInteraction), "SetMPDialogInteractionModeArg", "Allow Application Interaction" },
            { WorkerMpValueKind.MpInteractionMode, new SdkSpecializedEnumValue<SdkMpInteractionModeValue>(SdkMpInteractionModeValue.NeverHalt), "SetMPInteractionModeArg", "Never Halt" },
            { WorkerMpValueKind.NormalDirection, new SdkSpecializedEnumValue<SdkNormalDirectionValue>(SdkNormalDirectionValue.ProbingDirection), "SetNormalDirectionArg", "Probing Direction" },
            { WorkerMpValueKind.SaInteractionMode, new SdkSpecializedEnumValue<SdkSaInteractionModeValue>(SdkSaInteractionModeValue.Silent), "SetSAInteractionModeArg", "Silent" },
            { WorkerMpValueKind.SlotType, new SdkSpecializedEnumValue<SdkSlotTypeValue>(SdkSlotTypeValue.Round), "SetSlotTypeArg", "Round" },
            { WorkerMpValueKind.SphereFitComputationMode, new SdkSpecializedEnumValue<SdkSphereFitComputationModeValue>(SdkSphereFitComputationModeValue.MinCircumscribed), "SetSphereFitComputationModeArg", "Min Circumscribed" },
            { WorkerMpValueKind.WindowState, new SdkSpecializedEnumValue<SdkWindowStateValue>(SdkWindowStateValue.Hide), "SetWindowStateArg", "Hide" }
        };

    [Theory]
    [MemberData(nameof(SpecializedEnumCases))]
    public void SpecializedEnumUsesExactSetterAndReviewedSdkText(
        object kindValue,
        object specializedValue,
        string setter,
        string sdkText)
    {
        var kind = Assert.IsType<WorkerMpValueKind>(kindValue);
        var value = Assert.IsAssignableFrom<ISdkSpecializedEnumValue>(specializedValue);
        using var calls = new RecordingSdkCalls();
        using var adapter = new SpatialAnalyzerSdkAdapter(calls);
        var command = new SdkCommand(
            "specialized-enum",
            "Specialized Enum",
            [new("Value", kind, SpecializedEnumValue: value, SdkBinding: setter)],
            []);

        var result = adapter.Execute(command);

        Assert.True(result.MpSucceeded);
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
                if (value is WorkerObjectTypeValue.Unspecified or WorkerItemTypeValue.Unspecified)
                {
                    Assert.IsType<ArgumentOutOfRangeException>(
                        Assert.Throws<TargetInvocationException>(() => method.Invoke(null, [value])).InnerException);
                    continue;
                }
                var result = Assert.IsType<string>(method.Invoke(null, [value]));
                Assert.False(string.IsNullOrWhiteSpace(result));
                mappedValueCount++;
            }
        }

        Assert.Equal(512, mappedValueCount);
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
        var high = new WorkerToleranceLimit(true, 1.25);
        var low = new WorkerToleranceLimit(false, -2.5);
        var command = new SdkCommand(
            "specialized-structures",
            "Specialized Structures",
            [
                new("Auto", WorkerMpValueKind.AutoFilterProximitySettings, AutoFilterProximitySettingsValue: new(1, 2, 3, 4, 5, 6, SdkOffsetDirectionTypeValue.Both, SdkOffsetDirectionTypeValue.PositiveOnly, SdkOffsetDirectionTypeValue.NegativeOnly, true, false), SdkBinding: "SetAutoFilterProximitySettingsArg"),
                new("Thin", WorkerMpValueKind.CloudThinningOptions, CloudThinningOptionsValue: new(SdkCloudThinningModeValue.NthPoint, 2, 3, 4), SdkBinding: "SetCloudThinningOptionsArg"),
                new("Color", WorkerMpValueKind.ColorizationOptions, ColorizationOptionsValue: new(SdkColorRangeMethodValue.DiscreteColors, SdkBaseColorTypeValue.Red, SdkBaseMidColorTypeValue.Gray, SdkBaseColorTypeValue.Blue, true, false, true, 2, 3, false, 4, true, false, true, false, 5, -5, 1, -1), SdkBinding: "SetColorizationOptionsArg"),
                new("Constraint", WorkerMpValueKind.FitConstraintScalarOptions, FitConstraintScalarOptionsValue: new(high, low), SdkBinding: "SetFitConstraintScalarOptionsArg"),
                new("Dof", WorkerMpValueKind.FitDegreeOfFreedomOptions, FitDegreeOfFreedomOptionsValue: new(true, false, true, false, true, false, true), SdkBinding: "SetFitDofOptionsArg"),
                new("Output", WorkerMpValueKind.ReportOutputOptions, ReportOutputOptionsValue: new(SdkReportOutputTypeValue.Pdf, "report.pdf", null), SdkBinding: "SetReportOutputOptionsArg"),
                new("Embedded Output", WorkerMpValueKind.ReportOutputOptions, ReportOutputOptionsValue: new(SdkReportOutputTypeValue.SaReport, null, new("Collection", "Report")), SdkBinding: "SetReportOutputOptionsArg"),
                new("View", WorkerMpValueKind.ReportViewOptions, ReportViewOptionsValue: new(SdkReportViewTypeValue.CalloutView, "Collection", "Callout"), SdkBinding: "SetReportViewOptionsArg"),
                new("Tolerance", WorkerMpValueKind.ToleranceScalarOptions, ToleranceScalarOptionsValue: new(high, low), SdkBinding: "SetToleranceScalarOptionsArg"),
                new("Projection", WorkerMpValueKind.ProjectionOptions, ProjectionOptionsValue: new("Object To Probe Vectors", true, true, 1.5, false, 0), SdkBinding: "SetProjectionOptionsArg"),
                new("Point Delta", WorkerMpValueKind.PointDeltaReportOptions, PointDeltaReportOptionsValue: new(SdkCoordinateSystemTypeValue.Cartesian, "Single", true, true, true, true, true, true, true, false, true, true), SdkBinding: "SetPointDeltaReportOptionsArg"),
                new("UDP", WorkerMpValueKind.UdpTransmitSettings, UdpTransmitSettingsValue: new(true, false, "127.0.0.1", 12000), SdkBinding: "SetUdpTransmitSettingsArg")
            ],
            [
                new("Constraint Result", WorkerMpValueKind.FitConstraintScalarOptions, "GetFitConstraintScalarOptionsArg"),
                new("Tolerance Result", WorkerMpValueKind.ToleranceScalarOptions, "GetToleranceScalarOptionsArg")
            ]);

        var result = adapter.Execute(command);

        Assert.True(result.MpSucceeded);
        Assert.Equal(12, calls.SpecializedArguments.Count);
        Assert.Equal([0, 1, 2], calls.SpecializedArguments["Auto"][6..9]);
        Assert.Equal("Nth Point", calls.SpecializedArguments["Thin"][0]);
        Assert.Equal("PDF", calls.SpecializedArguments["Output"][0]);
        Assert.Equal("report.pdf", calls.SpecializedArguments["Output"][1]);
        Assert.Equal("SAReport", calls.SpecializedArguments["Embedded Output"][0]);
        Assert.Equal("Collection::Report", calls.SpecializedArguments["Embedded Output"][1]);
        Assert.Equal("Object To Probe Vectors", calls.SpecializedArguments["Projection"][0]);
        Assert.Equal("Cartesian", calls.SpecializedArguments["Point Delta"][0]);
        Assert.Equal([true, false, "127.0.0.1", 12000], calls.SpecializedArguments["UDP"]);
        Assert.All(result.OutputValues, output => Assert.True(output.Retrieved));
        Assert.Equal(1.25, (result.OutputValues[0].ReadValue() as WorkerFitConstraintScalarOptionsValue)!.High.Value);
        Assert.Equal(-2.5, (result.OutputValues[1].ReadValue() as WorkerToleranceScalarOptionsValue)!.Low.Value);
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
            [new("Mode", WorkerMpValueKind.RenderModeType)]);

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
                WorkerMpValueKind.RenderModeType,
                SpecializedEnumValue: new SdkSpecializedEnumValue<SdkEdgeModeValue>(SdkEdgeModeValue.EdgesOnly),
                SdkBinding: "SetRenderModeTypeArg")],
            []);

        var result = adapter.Execute(command);

        Assert.False(result.ExecuteStepReturned);
        Assert.Equal("sdk-argument-rejected", result.DiagnosticCode);
        Assert.DoesNotContain("ExecuteStep", calls.Events);
    }
}
