using System.Text.Json;
using Briosa.Generator;

namespace Briosa.Generator.Tests;

public sealed class SpecializedValueMappingsTests
{
    [Fact]
    public void ExactTargetEnumMappingRejectsUnspecifiedAndRemovesWireSentinel()
    {
        var validation = SpecializedValueMappings.ValidationCondition(
            "render_mode_type",
            "request.Mode");
        var input = SpecializedValueMappings.CreateInputExpression(
            "render_mode_type",
            "new WorkerMpInputArgument(",
            "request.Mode");

        Assert.Equal(
            "request.Mode == 0 || !Enum.IsDefined(request.Mode)",
            validation);
        Assert.Contains("SpecializedEnumValue: new((int)request.Mode - 1)", input, StringComparison.Ordinal);
        Assert.Equal("RenderModeType", SpecializedValueMappings.ToTypeName("render_mode_type"));
    }

    [Fact]
    public void StructuredMappingRequiresEveryComponentAndNestedEnum()
    {
        var validation = SpecializedValueMappings.ValidationCondition(
            "auto_filter_proximity_settings",
            "request.Filter");
        var input = SpecializedValueMappings.CreateInputExpression(
            "auto_filter_proximity_settings",
            "new WorkerMpInputArgument(",
            "request.Filter");

        Assert.Contains("!request.Filter.HasSurfaceInclusionProximity", validation, StringComparison.Ordinal);
        Assert.Contains("!request.Filter.HasAssertPlaneBoundaries", validation, StringComparison.Ordinal);
        Assert.Contains("request.Filter.SurfaceProximityMode == 0", validation, StringComparison.Ordinal);
        Assert.Contains("AutoFilterProximitySettingsValue", input, StringComparison.Ordinal);
        Assert.Contains("(int)request.Filter.RadialProximityMode - 1", input, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("ascii_frame_set_format", "AsciiFrameSetFormat")]
    [InlineData("ascii_import_file_format", "AsciiImportFileFormat")]
    [InlineData("axis_identifier", "AxisIdentifier")]
    [InlineData("wcf_axis_identifier", "WcfAxisIdentifier")]
    [InlineData("vector_component", "VectorComponent")]
    public void SplitAndRenamedEnumFamiliesHaveGeneratorMappings(
        string semanticType,
        string protocolType)
    {
        Assert.True(SpecializedValueMappings.IsEnum(semanticType));
        Assert.Equal(protocolType, SpecializedValueMappings.ToTypeName(semanticType));
    }

    [Fact]
    public void ReportOutputMappingPreservesDestinationStructure()
    {
        var validation = SpecializedValueMappings.ValidationCondition(
            "report_output_options",
            "request.Output");
        var input = SpecializedValueMappings.CreateInputExpression(
            "report_output_options",
            "new WorkerMpInputArgument(",
            "request.Output");

        Assert.Contains("DestinationOneofCase.None", validation, StringComparison.Ordinal);
        Assert.Contains("EmbeddedFile.HasCollectionName", validation, StringComparison.Ordinal);
        Assert.Contains("DestinationOneofCase.ExternalPath", input, StringComparison.Ordinal);
        Assert.Contains("EmbeddedFile.FileName", input, StringComparison.Ordinal);
    }
    [Fact]
    public void ScalarOptionGetterBuildsAnExactTargetResult()
    {
        var expression = SpecializedValueMappings.ResultValueExpression(
            "tolerance_scalar_options",
            "output");

        Assert.Contains("TargetProtocol.ToleranceScalarOptions", expression, StringComparison.Ordinal);
        Assert.Contains("output.ToleranceScalarOptionsValue!.High.Enabled", expression, StringComparison.Ordinal);
        Assert.Contains("output.ToleranceScalarOptionsValue.Low.Value", expression, StringComparison.Ordinal);
    }

    [Fact]
    public void BlockedSemanticFamiliesHaveNoGeneratorMapping()
    {
        Assert.False(SpecializedValueMappings.IsSupported("b_spline_fit_options"));
        Assert.False(SpecializedValueMappings.IsSupported("point_delta_report_options"));
        Assert.False(SpecializedValueMappings.IsSupported("projection_options"));
        Assert.Throws<NotSupportedException>(() =>
            SpecializedValueMappings.ToTypeName("b_spline_fit_options"));
    }

    [Fact]
    public void EnumDefaultsUseExactTargetProtobufNumbers()
    {
        using var document = JsonDocument.Parse("2");

        Assert.Equal(
            "(TargetProtocol.RenderModeType)2",
            SpecializedValueMappings.DefaultExpression(
                "render_mode_type",
                document.RootElement));
    }
}
