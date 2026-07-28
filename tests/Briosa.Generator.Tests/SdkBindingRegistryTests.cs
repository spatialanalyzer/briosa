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
        Assert.Equal(115, result.ValueFamilyCount);
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
                    ["https://github.com/spatialanalyzer/briosa/issues/79"],
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

        Assert.Equal(
            ["ascii_frame_set_format", "ascii_import_file_format"],
            bindings["SetAsciiFileFormatArg"].SemanticValueFamilies);
        Assert.Equal(
            ["axis_identifier", "wcf_axis_identifier"],
            bindings["SetAxisNameArg"].SemanticValueFamilies);
        Assert.Equal(
            ["collection_item_name", "collection_object_name"],
            bindings["GetCollectionObjectNameArg"].SemanticValueFamilies);
        Assert.Equal(
            ["collection_item_name", "collection_object_name"],
            bindings["SetCollectionObjectNameArg2"].SemanticValueFamilies);
        Assert.Equal(
            ["collection_item_name_list", "collection_object_name_list"],
            bindings["GetCollectionObjectNameRefListArg"].SemanticValueFamilies);
        Assert.Equal(
            ["collection_item_name_list", "collection_object_name_list"],
            bindings["SetCollectionObjectNameRefListArg"].SemanticValueFamilies);
        Assert.Equal("angular_unit", Family(bindings["SetAngularUnitsArg"]));
        Assert.Equal("string", Family(bindings["SetStringArg"]));
        Assert.Equal("double_array", Family(bindings["SetDoubleArrayArg"]));
        Assert.Equal("edit_text", Family(bindings["SetEditTextArg"]));
        Assert.Equal("string_list", Family(bindings["SetStringRefListArg"]));
        Assert.Equal("transform", Family(bindings["SetTransformArg"]));
        Assert.Equal("world_transform", Family(bindings["SetWorldTransformArg"]));
        Assert.Equal("file_reference", Family(bindings["SetFilePathArg"]));
        Assert.Equal(
            "b_spline_fit_options",
            Family(bindings["GetBSPlineFitOptionsArg"]));
        Assert.Equal(
            Family(bindings["GetBSPlineFitOptionsArg"]),
            Family(bindings["SetBSplineFitOptionsArg"]));
        Assert.All(
            new[] { bindings["GetBSPlineFitOptionsArg"], bindings["SetBSplineFitOptionsArg"] },
            binding =>
            {
                Assert.Equal("blocked_semantics", binding.RegistryStatus);
                Assert.Equal("blocked", binding.Coverage.Protocol);
                Assert.Equal(
                    ["https://github.com/spatialanalyzer/briosa/issues/79"],
                    binding.BlockerReferences);
            });
        Assert.Equal(97, registry.Bindings.Count(binding =>
            binding.Coverage.Protocol == "implemented"));
        Assert.Equal(97, registry.Bindings.Count(binding =>
            binding.Coverage.Worker == "implemented"));
        Assert.Equal(97, registry.Bindings.Count(binding =>
            binding.Coverage.Adapter == "implemented"));
        Assert.Equal(97, registry.Bindings.Count(binding =>
            binding.Coverage.Fake == "implemented"));
        Assert.Equal(97, registry.Bindings.Count(binding =>
            binding.Coverage.Generator == "implemented"));
        Assert.Equal(79, registry.ValueFamilies.Count(family =>
            family.ImplementationStatus == "implemented"));
        Assert.All(
            registry.Bindings.Where(binding => binding.RegistryStatus == "usable"),
            binding => Assert.DoesNotContain("unknown", binding.SemanticValueFamilies));

        var families = registry.ValueFamilies.ToDictionary(
            family => family.FamilyId,
            StringComparer.Ordinal);
        Assert.All(
            registry.Bindings.Where(binding =>
                binding.InteropSignature?.Parameters.Any(parameter =>
                    parameter.ClrType == "object") == true),
            binding => Assert.All(
                binding.SemanticValueFamilies,
                family => Assert.NotEqual("scalar", families[family].Shape)));
        Assert.All(
            registry.Bindings.Where(binding =>
                binding.Method is not "GetStringArg" and not "SetStringArg" &&
                binding.InteropSignature?.Parameters.Skip(1).All(parameter =>
                    parameter.ClrType == "string") == true),
            binding => Assert.DoesNotContain("string", binding.SemanticValueFamilies));
    }

    [Fact]
    public void ReviewAssignsEverySharedMethodObservationByExactSdkOrder()
    {
        var paths = RegistryPaths();
        using var review = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(paths.Registry, "review.json")));
        var assignments = review.RootElement
            .GetProperty("argument_family_assignments")
            .EnumerateArray()
            .ToArray();

        Assert.Equal(995, assignments.Length);
        Assert.All(
            assignments,
            assignment =>
            {
                Assert.True(assignment.GetProperty("sdk_order").GetInt32() >= 0);
                Assert.NotEqual(
                    JsonValueKind.Undefined,
                    assignment.GetProperty("documented_ordinals").ValueKind);
            });

        Assert.Equal(
            [
                "GetCollectionObjectNameArg",
                "GetCollectionObjectNameRefListArg",
                "SetAsciiFileFormatArg",
                "SetAxisNameArg",
                "SetCollectionObjectNameArg2",
                "SetCollectionObjectNameRefListArg"
            ],
            assignments
                .Select(assignment => assignment.GetProperty("method").GetString()!)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(method => method, StringComparer.Ordinal)
                .ToArray());
    }

    private static string Family(SdkBindingRegistryEntry binding) =>
        Assert.Single(binding.SemanticValueFamilies);
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
