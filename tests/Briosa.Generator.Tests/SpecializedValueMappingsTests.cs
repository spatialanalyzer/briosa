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
    public void BlockedBSplineFamilyHasNoGeneratorMapping()
    {
        Assert.False(SpecializedValueMappings.IsSupported("b_spline_fit_options"));
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
