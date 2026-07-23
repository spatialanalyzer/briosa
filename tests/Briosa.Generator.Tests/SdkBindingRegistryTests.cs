using System.Text.Json;
using Briosa.Generator;

namespace Briosa.Generator.Tests;

public sealed class SdkBindingRegistryTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    [Fact]
    public void CommittedRegistryReconcilesExactTargetEvidence()
    {
        var paths = RegistryPaths();

        var result = SdkBindingRegistry.Validate(
            paths.Inventory,
            paths.Dispositions,
            paths.Interop,
            paths.Registry);

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors));
        Assert.Equal(151, result.BindingCount);
        Assert.Equal(111, result.ValueFamilyCount);
    }

    [Fact]
    public void RegistryRecordsCoverageAndFailsMissingInteropBindingsClosed()
    {
        var paths = RegistryPaths();
        var registry = JsonSerializer.Deserialize<SdkBindingRegistryDocument>(
            File.ReadAllText(Path.Combine(paths.Registry, "registry.json")),
            JsonOptions)!;

        Assert.Equal(
            105,
            registry.Bindings.Count(binding =>
                binding.Direction == "setter" && binding.SourceStatus != "interop_only"));
        Assert.Equal(
            29,
            registry.Bindings.Count(binding =>
                binding.Direction == "getter" && binding.SourceStatus != "interop_only"));
        Assert.Equal(
            106,
            registry.Bindings.Count(binding =>
                binding.Direction == "setter" && binding.InteropSignature is not null));
        Assert.Equal(
            39,
            registry.Bindings.Count(binding =>
                binding.Direction == "getter" && binding.InteropSignature is not null));

        var missingInterop = registry.Bindings
            .Where(binding => binding.RegistryStatus == "blocked_missing_interop")
            .Select(binding => binding.Method)
            .OrderBy(method => method, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            [
                "GetSigmoidalGapConstraintOptionsArg",
                "SetCloudThinningModeTypeArg",
                "SetItemTypeArg",
                "SetMPGDTOptionsCheckValidatorTypeArg",
                "SetMPGDTOptionsDistanceBetweenModeArg",
                "SetMeshOrientationTypeArg"
            ],
            missingInterop);
        Assert.All(
            registry.Bindings.Where(binding =>
                binding.RegistryStatus == "blocked_missing_interop"),
            binding =>
            {
                Assert.Null(binding.InteropSignature);
                Assert.Equal("blocked", binding.Coverage.Protocol);
                Assert.Equal("blocked", binding.Coverage.Worker);
                Assert.Equal("blocked", binding.Coverage.Adapter);
                Assert.Equal("blocked", binding.Coverage.Fake);
                Assert.Equal("blocked", binding.Coverage.Generator);
                Assert.Equal(
                    ["https://github.com/spatialanalyzer/briosa/issues/53"],
                    binding.BlockerReferences);
            });
    }

    [Fact]
    public void RegistryKeepsSemanticFamiliesDistinctFromClrShapes()
    {
        var paths = RegistryPaths();
        var registry = JsonSerializer.Deserialize<SdkBindingRegistryDocument>(
            File.ReadAllText(Path.Combine(paths.Registry, "registry.json")),
            JsonOptions)!;
        var bindings = registry.Bindings.ToDictionary(
            binding => binding.Method,
            StringComparer.Ordinal);

        Assert.Equal("angular_unit", bindings["SetAngularUnitsArg"].SemanticValueFamily);
        Assert.Equal("string", bindings["SetStringArg"].SemanticValueFamily);
        Assert.Equal(
            "b_spline_fit_options",
            bindings["GetBSPlineFitOptionsArg"].SemanticValueFamily);
        Assert.Equal(
            bindings["GetBSPlineFitOptionsArg"].SemanticValueFamily,
            bindings["SetBSplineFitOptionsArg"].SemanticValueFamily);
        Assert.Equal(
            bindings["GetCollectionObjectNameArg"].SemanticValueFamily,
            bindings["SetCollectionObjectNameArg2"].SemanticValueFamily);

        Assert.Equal(14, registry.Bindings.Count(binding =>
            binding.Coverage.Protocol == "implemented"));
        Assert.Equal(14, registry.Bindings.Count(binding =>
            binding.Coverage.Worker == "implemented"));
        Assert.Equal(14, registry.Bindings.Count(binding =>
            binding.Coverage.Adapter == "implemented"));
        Assert.Equal(14, registry.Bindings.Count(binding =>
            binding.Coverage.Fake == "implemented"));
        Assert.Equal(14, registry.Bindings.Count(binding =>
            binding.Coverage.Generator == "implemented"));
        Assert.Equal(7, registry.ValueFamilies.Count(family =>
            family.ImplementationStatus == "implemented"));
        Assert.All(
            registry.Bindings.Where(binding => binding.RegistryStatus == "usable"),
            binding => Assert.NotEqual("unknown", binding.SemanticValueFamily));

        var families = registry.ValueFamilies.ToDictionary(
            family => family.FamilyId,
            StringComparer.Ordinal);
        Assert.All(
            registry.Bindings.Where(binding =>
                binding.InteropSignature?.Parameters.Any(parameter =>
                    parameter.ClrType == "object") == true),
            binding => Assert.NotEqual(
                "scalar",
                families[binding.SemanticValueFamily].Shape));
        Assert.All(
            registry.Bindings.Where(binding =>
                binding.Method is not "GetStringArg" and not "SetStringArg" &&
                binding.InteropSignature?.Parameters.Skip(1).All(parameter =>
                    parameter.ClrType == "string") == true),
            binding => Assert.NotEqual("string", binding.SemanticValueFamily));
    }

    private static (
        string Inventory,
        string Dispositions,
        string Interop,
        string Registry) RegistryPaths()
    {
        var root = FindRepositoryRoot().FullName;
        return (
            Path.Combine(root, "inventory", "sa", "2026.1.0529.7", "inventory.json"),
            Path.Combine(root, "disposition", "sa", "2026.1.0529.7"),
            Path.Combine(root, "interop", "SpatialAnalyzer", "2026.1.0529.7"),
            Path.Combine(root, "bindings", "sa", "2026.1.0529.7"));
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "Briosa.slnx")))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new InvalidOperationException(
            "Could not find the repository root.");
    }
}
