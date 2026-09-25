using Briosa.Worker.Control;
using System.Reflection;
using Briosa.Worker.Sdk;

namespace Briosa.Worker.Tests;

public sealed partial class SpatialAnalyzerSdkAdapterTests
{
    public static TheoryData<object, object, string, string>
        SpecializedEnumCases => new()
        {
            { WorkerMpValueKind.AsciiImportFileFormat, new WorkerChoiceValue<WorkerAsciiImportFileFormatValue>(WorkerAsciiImportFileFormatValue.Xyz), "SetAsciiFileFormatArg", "X Y Z" },
            { WorkerMpValueKind.AsciiFrameSetFormat, new WorkerChoiceValue<WorkerAsciiFrameSetFormatValue>(WorkerAsciiFrameSetFormatValue.FrameNameXyzEulerZyxTimestamp), "SetAsciiFileFormatArg", "FrameName X Y Z  Euler ZYX [Timestamp]" },
            { WorkerMpValueKind.AxisIdentifier, new WorkerChoiceValue<WorkerAxisIdentifierValue>(WorkerAxisIdentifierValue.PositiveX), "SetAxisNameArg", "+X Axis" },
            { WorkerMpValueKind.WcfAxisIdentifier, new WorkerChoiceValue<WorkerWcfAxisIdentifierValue>(WorkerWcfAxisIdentifierValue.X), "SetAxisNameArg", "X Axis" },
            { WorkerMpValueKind.BaseColorType, new WorkerChoiceValue<WorkerBaseColorTypeValue>(WorkerBaseColorTypeValue.Red), "SetBaseColorTypeArg", "Red" },
            { WorkerMpValueKind.BaseMidColorType, new WorkerChoiceValue<WorkerBaseMidColorTypeValue>(WorkerBaseMidColorTypeValue.Gray), "SetBaseMidColorTypeArg", "Gray" },
            { WorkerMpValueKind.ChartType, new WorkerChoiceValue<WorkerChartTypeValue>(WorkerChartTypeValue.RunChart), "SetChartTypeArg", "Run Chart" },
            { WorkerMpValueKind.CollimationBaselineType, new WorkerChoiceValue<WorkerCollimationBaselineTypeValue>(WorkerCollimationBaselineTypeValue.DeterminedByValue), "SetCollimationBaselineTypeArg", "Determined By Value" },
            { WorkerMpValueKind.CollimationType, new WorkerChoiceValue<WorkerCollimationTypeValue>(WorkerCollimationTypeValue.FullCollimation), "SetCollimationTypeArg", "Full Collimation" },
            { WorkerMpValueKind.ColorRangeMethod, new WorkerChoiceValue<WorkerColorRangeMethodValue>(WorkerColorRangeMethodValue.SingleColor), "SetColorRangeMethodArg", "Single Color" },
            { WorkerMpValueKind.CoordinateSystemType, new WorkerChoiceValue<WorkerCoordinateSystemTypeValue>(WorkerCoordinateSystemTypeValue.Cartesian), "SetCoordinateSystemTypeArg", "Cartesian" },
            { WorkerMpValueKind.VectorComponent, new WorkerChoiceValue<WorkerVectorComponentValue>(WorkerVectorComponentValue.X), "SetDatasetTypeArg", "X" },
            { WorkerMpValueKind.DynamicCircleMode, new WorkerChoiceValue<WorkerDynamicCircleModeValue>(WorkerDynamicCircleModeValue.TwoConesIntersection), "SetDynamicCircleModeArg", "Two Cones Intersection" },
            { WorkerMpValueKind.DynamicEllipseMode, new WorkerChoiceValue<WorkerDynamicEllipseModeValue>(WorkerDynamicEllipseModeValue.CylinderPlaneIntersection), "SetDynamicEllipseModeArg", "Cylinder and Plane Intersection" },
            { WorkerMpValueKind.DynamicLineMode, new WorkerChoiceValue<WorkerDynamicLineModeValue>(WorkerDynamicLineModeValue.ConeAxis), "SetDynamicLineModeArg", "Cone Axis" },
            { WorkerMpValueKind.DynamicPlaneMode, new WorkerChoiceValue<WorkerDynamicPlaneModeValue>(WorkerDynamicPlaneModeValue.TwoConesFirstConeAxis), "SetDynamicPlaneModeArg", "Twp Cones Intersection - Hold Normal to First Cone Axis" },
            { WorkerMpValueKind.DynamicPointMode, new WorkerChoiceValue<WorkerDynamicPointModeValue>(WorkerDynamicPointModeValue.IntersectionLinePlane), "SetDynamicPointModeArg", "Intersection of Line and Plane" },
            { WorkerMpValueKind.EdgeMode, new WorkerChoiceValue<WorkerEdgeModeValue>(WorkerEdgeModeValue.IncludeEdges), "SetEdgeModeArg", "Include Edges" },
            { WorkerMpValueKind.ExportDataDelimiterType, new WorkerChoiceValue<WorkerExportDataDelimiterTypeValue>(WorkerExportDataDelimiterTypeValue.Comma), "SetExportDataDelimeterTypeArg", "Comma" },
            { WorkerMpValueKind.ExportTargetNameFormat, new WorkerChoiceValue<WorkerExportTargetNameFormatValue>(WorkerExportTargetNameFormatValue.Target), "SetExportTargetNameFormatArg", "Target" },
            { WorkerMpValueKind.ExportVectorNameFormat, new WorkerChoiceValue<WorkerExportVectorNameFormatValue>(WorkerExportVectorNameFormatValue.Vector), "SetExportVectorNameFormatArg", "Vector" },
            { WorkerMpValueKind.GeometryType, new WorkerChoiceValue<WorkerGeometryTypeValue>(WorkerGeometryTypeValue.Line), "SetGeometryTypeArg", "Line" },
            { WorkerMpValueKind.GdtDistanceBetweenMode, new WorkerChoiceValue<WorkerGdtDistanceBetweenModeValue>(WorkerGdtDistanceBetweenModeValue.MinMax), "SetMPGDTOptionsDistanceBetweenModeArg", "Min/Max" },
            { WorkerMpValueKind.GdtEvaluationMethod, new WorkerChoiceValue<WorkerGdtEvaluationMethodValue>(WorkerGdtEvaluationMethodValue.Iso2017), "SetMPGDTOptionsCheckValidatorTypeArg", "ISO 2017" },
            { WorkerMpValueKind.InstrumentType, new WorkerChoiceValue<WorkerInstrumentTypeValue>(WorkerInstrumentTypeValue.CreaformVxElements), "SetInstTypeNameArg", "Creaform VXelements" },
            { WorkerMpValueKind.ObjectType, new WorkerChoiceValue<WorkerObjectTypeValue>(WorkerObjectTypeValue.Cone), "SetObjectTypeArg", "Cone" },
            { WorkerMpValueKind.OffsetDirectionType, new WorkerChoiceValue<WorkerOffsetDirectionTypeValue>(WorkerOffsetDirectionTypeValue.PositiveOnly), "SetOffsetDirectionTypeArg", "Positive only" },
            { WorkerMpValueKind.PointFilterInputType, new WorkerChoiceValue<WorkerPointFilterInputTypeValue>(WorkerPointFilterInputTypeValue.CardinalPoints), "SetPointFilterInputTypeArg", "Cardinal Points" },
            { WorkerMpValueKind.RelationshipWeightingMode, new WorkerChoiceValue<WorkerRelationshipWeightingModeValue>(WorkerRelationshipWeightingModeValue.ResetAllWeights), "SetRelWeightingModeArg", "Reset All weights to 1.0" },
            { WorkerMpValueKind.RenderModeType, new WorkerChoiceValue<WorkerRenderModeTypeValue>(WorkerRenderModeTypeValue.SolidAndEdges), "SetRenderModeTypeArg", "Solid+Edges" },
            { WorkerMpValueKind.ReportPageOrientation, new WorkerChoiceValue<WorkerReportPageOrientationValue>(WorkerReportPageOrientationValue.Landscape), "SetReportPageSettingsArg", "Landscape" },
            { WorkerMpValueKind.SaturationLimitType, new WorkerChoiceValue<WorkerSaturationLimitTypeValue>(WorkerSaturationLimitTypeValue.SigmaRule), "SetSaturationLimitTypeArg", "Sigma Rule" },
            { WorkerMpValueKind.ShowUsmnDialogType, new WorkerChoiceValue<WorkerShowUsmnDialogTypeValue>(WorkerShowUsmnDialogTypeValue.OnToleranceViolation), "SetShowUsmnDialogTypeArg", "On Tolerance Violation" },
            { WorkerMpValueKind.SurfaceAnalysisMode, new WorkerChoiceValue<WorkerSurfaceAnalysisModeValue>(WorkerSurfaceAnalysisModeValue.DeviationRms), "SetSurfaceAnalysisModeArg", "Deviation RMS" },
            { WorkerMpValueKind.SurfaceDissectionModeType, new WorkerChoiceValue<WorkerSurfaceDissectionModeTypeValue>(WorkerSurfaceDissectionModeTypeValue.SelectFaces), "SetSurfDissectModeTypeArg", "Select Faces" },
            { WorkerMpValueKind.TargetComputationMethod, new WorkerChoiceValue<WorkerTargetComputationMethodValue>(WorkerTargetComputationMethodValue.UseOnlyMostRecentShot), "SetTargetComputationMethodArg", "Use only most recent shot" },
            { WorkerMpValueKind.TranslucencyType, new WorkerChoiceValue<WorkerTranslucencyTypeValue>(WorkerTranslucencyTypeValue.Translucent), "SetTranslucencyTypeArg", "Translucent" },
            { WorkerMpValueKind.CompTechnique, new WorkerChoiceValue<WorkerCompTechniqueValue>(WorkerCompTechniqueValue.MaxInscribed), "SetCompTechniqueArg", "Max Inscribed" },
            { WorkerMpValueKind.DegreeOfFreedom, new WorkerChoiceValue<WorkerDegreeOfFreedomValue>(WorkerDegreeOfFreedomValue.LockFocusLocation), "SetDegreeOfFreedomArg", "Lock Focus Location" },
            { WorkerMpValueKind.FitMethod, new WorkerChoiceValue<WorkerFitMethodValue>(WorkerFitMethodValue.MinimumRms), "SetFitMethodArg", "Minimum RMS" },
            { WorkerMpValueKind.MeasuredSideForPlanarOffset, new WorkerChoiceValue<WorkerMeasuredSideForPlanarOffsetValue>(WorkerMeasuredSideForPlanarOffsetValue.AbovePlane), "SetMeasuredSideForPlanarOffsetArg", "Above Plane" },
            { WorkerMpValueKind.MeasuredSideForRadialOffset, new WorkerChoiceValue<WorkerMeasuredSideForRadialOffsetValue>(WorkerMeasuredSideForRadialOffsetValue.Outside), "SetMeasuredSideForRadialOffsetArg", "Outside" },
            { WorkerMpValueKind.MpDialogInteractionMode, new WorkerChoiceValue<WorkerMpDialogInteractionModeValue>(WorkerMpDialogInteractionModeValue.AllowApplicationInteraction), "SetMPDialogInteractionModeArg", "Allow Application Interaction" },
            { WorkerMpValueKind.MpInteractionMode, new WorkerChoiceValue<WorkerMpInteractionModeValue>(WorkerMpInteractionModeValue.NeverHalt), "SetMPInteractionModeArg", "Never Halt" },
            { WorkerMpValueKind.NormalDirection, new WorkerChoiceValue<WorkerNormalDirectionValue>(WorkerNormalDirectionValue.ProbingDirection), "SetNormalDirectionArg", "Probing Direction" },
            { WorkerMpValueKind.SaInteractionMode, new WorkerChoiceValue<WorkerSaInteractionModeValue>(WorkerSaInteractionModeValue.Silent), "SetSAInteractionModeArg", "Silent" },
            { WorkerMpValueKind.SlotType, new WorkerChoiceValue<WorkerSlotTypeValue>(WorkerSlotTypeValue.Round), "SetSlotTypeArg", "Round" },
            { WorkerMpValueKind.SphereFitComputationMode, new WorkerChoiceValue<WorkerSphereFitComputationModeValue>(WorkerSphereFitComputationModeValue.MinCircumscribed), "SetSphereFitComputationModeArg", "Min Circumscribed" },
            { WorkerMpValueKind.WindowState, new WorkerChoiceValue<WorkerWindowStateValue>(WorkerWindowStateValue.Hide), "SetWindowStateArg", "Hide" }
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
        var value = Assert.IsAssignableFrom<WorkerMpValue>(specializedValue);
        using var calls = new RecordingSdkCalls();
        using var adapter = new SpatialAnalyzerSdkAdapter(calls);
        var command = new WorkerMpCommand(
            "specialized-enum",
            "Specialized Enum",
            [new WorkerMpInputArgument("Value", kind, value, sdkBinding: setter)],
            []);

        using var stream = new MemoryStream();
        using var channel = new WorkerControlChannel(stream, leaveOpen: true);
        channel.Send(WorkerControlMessage.Execute(Guid.NewGuid(), command));
        stream.Position = 0;
        var received = channel.Receive().Command!;
        Assert.Equal(value, Assert.Single(received.InputArguments).Value);
        var result = adapter.Execute(received);

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

        Assert.Equal(502, mappedValueCount);
    }

    [Fact]
    public void InstrumentTypeMappingCoversRetainedExactTargetEvidence()
    {
        Assert.Equal(185, Enum.GetValues<WorkerInstrumentTypeValue>().Length);
        Assert.Equal("Faro Vantage", SdkSpecializedValueCodec.ToSdkString(WorkerInstrumentTypeValue.FaroVantage));
        Assert.Equal(
            "CimCore Arm 6DOF: 3012i, 5012, 1.2m",
            SdkSpecializedValueCodec.ToSdkString(WorkerInstrumentTypeValue.CimCoreArm6Dof3012i501212m));
    }

    [Fact]
    public void SpecializedStructuresUseExactSettersAndScalarGetters()
    {
        using var calls = new RecordingSdkCalls();
        using var adapter = new SpatialAnalyzerSdkAdapter(calls);
        var high = new WorkerToleranceLimit(true, 1.25);
        var low = new WorkerToleranceLimit(false, -2.5);
        var command = new WorkerMpCommand(
            "specialized-structures",
            "Specialized Structures",
            [
                new WorkerMpInputArgument("Auto", WorkerMpValueKind.AutoFilterProximitySettings, new WorkerAutoFilterProximitySettingsValue(1, 2, 3, 4, 5, 6, (int)(WorkerOffsetDirectionTypeValue.Both), (int)(WorkerOffsetDirectionTypeValue.PositiveOnly), (int)(WorkerOffsetDirectionTypeValue.NegativeOnly), true, false), sdkBinding: "SetAutoFilterProximitySettingsArg"),
                new WorkerMpInputArgument("Thin", WorkerMpValueKind.CloudThinningOptions, new WorkerCloudThinningOptionsValue((int)(WorkerCloudThinningModeValue.NthPoint), 2, 3, 4), sdkBinding: "SetCloudThinningOptionsArg"),
                new WorkerMpInputArgument("Color", WorkerMpValueKind.ColorizationOptions, new WorkerColorizationOptionsValue((int)(WorkerColorRangeMethodValue.DiscreteColors), (int)(WorkerBaseColorTypeValue.Red), (int)(WorkerBaseMidColorTypeValue.Gray), (int)(WorkerBaseColorTypeValue.Blue), true, false, true, 2, 3, false, 4, true, false, true, false, 5, -5, 1, -1), sdkBinding: "SetColorizationOptionsArg"),
                new WorkerMpInputArgument("Constraint", WorkerMpValueKind.FitConstraintScalarOptions, new WorkerFitConstraintScalarOptionsValue(high, low), sdkBinding: "SetFitConstraintScalarOptionsArg"),
                new WorkerMpInputArgument("Dof", WorkerMpValueKind.FitDegreeOfFreedomOptions, new WorkerFitDegreeOfFreedomOptionsValue(true, false, true, false, true, false, true), sdkBinding: "SetFitDofOptionsArg"),
                new WorkerMpInputArgument("Output", WorkerMpValueKind.ReportOutputOptions, new WorkerReportOutputOptionsValue((int)(WorkerReportOutputTypeValue.Pdf), "report.pdf", null), sdkBinding: "SetReportOutputOptionsArg"),
                new WorkerMpInputArgument("Embedded Output", WorkerMpValueKind.ReportOutputOptions, new WorkerReportOutputOptionsValue((int)(WorkerReportOutputTypeValue.SaReport), null, new WorkerEmbeddedReportFileValue("Collection", "Report")), sdkBinding: "SetReportOutputOptionsArg"),
                new WorkerMpInputArgument("View", WorkerMpValueKind.ReportViewOptions, new WorkerReportViewOptionsValue((int)(WorkerReportViewTypeValue.CalloutView), "Collection", "Callout"), sdkBinding: "SetReportViewOptionsArg"),
                new WorkerMpInputArgument("Tolerance", WorkerMpValueKind.ToleranceScalarOptions, new WorkerToleranceScalarOptionsValue(high, low), sdkBinding: "SetToleranceScalarOptionsArg"),
                new WorkerMpInputArgument("Projection", WorkerMpValueKind.ProjectionOptions, new WorkerProjectionOptionsValue("Object To Probe Vectors", true, true, 1.5, false, 0), sdkBinding: "SetProjectionOptionsArg"),
                new WorkerMpInputArgument("Point Delta", WorkerMpValueKind.PointDeltaReportOptions, new WorkerPointDeltaReportOptionsValue((int)(WorkerCoordinateSystemTypeValue.Cartesian), "Single", true, true, true, true, true, true, true, false, true, true), sdkBinding: "SetPointDeltaReportOptionsArg"),
                new WorkerMpInputArgument("UDP", WorkerMpValueKind.UdpTransmitSettings, new WorkerUdpTransmitSettingsValue(true, false, "127.0.0.1", 12000), sdkBinding: "SetUdpTransmitSettingsArg")
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
        var command = new WorkerMpCommand(
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
    public void MismatchedSpecializedEnumTypeIsRejectedBeforeCommandConstruction()
    {
        Assert.Throws<ArgumentException>(() => new WorkerMpInputArgument(
            "Mode", WorkerMpValueKind.RenderModeType,
            new WorkerChoiceValue<WorkerEdgeModeValue>(WorkerEdgeModeValue.EdgesOnly),
            sdkBinding: "SetRenderModeTypeArg"));
    }
}
