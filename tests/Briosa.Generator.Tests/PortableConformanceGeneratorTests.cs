using System.Security.Cryptography;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Briosa.Generator;

namespace Briosa.Generator.Tests;

public sealed class PortableConformanceGeneratorTests
{
    [Theory]
    [InlineData("identifier", "string", false)]
    [InlineData("structured", "CollectionObjectName", true)]
    [InlineData("transform", "Transform", true)]
    [InlineData("enum", "ObjectType", false)]
    public void MalformedShapeCasesApplyOnlyToMessageValuedPublicTypes(
        string shape,
        string publicType,
        bool expected)
    {
        Assert.Equal(
            expected,
            PortableConformanceGenerator.RequiresMalformedMessageShapeCase(
                shape,
                publicType));
    }

    [Fact]
    public void CommittedManifestMatchesDeterministicEvidenceGeneration()
    {
        var root = FindRepositoryRoot();
        var output = TemporaryDirectory("briosa-conformance-generation");
        try
        {
            var result = PortableConformanceGenerator.Generate(root.FullName, output);

            var relativePath = Assert.Single(result.Files);
            Assert.Equal(
                "generated/conformance/sa/2026.1.0529.7/manifest.json",
                relativePath);
            Assert.Equal(
                File.ReadAllBytes(Path.Combine(root.FullName, relativePath)),
                File.ReadAllBytes(Path.Combine(output, relativePath)));
        }
        finally
        {
            Directory.Delete(output, recursive: true);
        }
    }

    [Fact]
    public void ManifestFingerprintsEverySupportedOperationSourceAndHasExactCaseCounts()
    {
        var root = FindRepositoryRoot();
        var path = Path.Combine(
            root.FullName,
            "generated",
            "conformance",
            "sa",
            "2026.1.0529.7",
            "manifest.json");
        using var document = JsonDocument.Parse(File.ReadAllBytes(path));
        var manifest = document.RootElement;
        var evidence = manifest.GetProperty("evidence_inputs").EnumerateArray()
            .ToDictionary(
                input => input.GetProperty("path").GetString()!,
                input => input.GetProperty("sha256").GetString()!,
                StringComparer.Ordinal);
        var catalogPath = Path.Combine(
            root.FullName,
            "catalog",
            "sa",
            "2026.1.0529.7",
            "catalog.json");
        using var catalog = JsonDocument.Parse(File.ReadAllBytes(catalogPath));
        var operationPaths = catalog.RootElement.GetProperty("operation_files")
            .EnumerateArray()
            .Select(item => "catalog/sa/2026.1.0529.7/" + item.GetString())
            .ToArray();

        Assert.All(operationPaths, relativePath => Assert.Equal(
            Sha256(Path.Combine(root.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar))),
            evidence[relativePath]));
        Assert.Equal(
            manifest.GetProperty("operations").GetArrayLength(),
            manifest.GetProperty("counts").GetProperty("operation_count").GetInt32());
        Assert.Equal(
            manifest.GetProperty("binding_cases").GetArrayLength(),
            manifest.GetProperty("counts").GetProperty("binding_case_count").GetInt32());
        Assert.Equal(
            manifest.GetProperty("value_family_cases").GetArrayLength(),
            manifest.GetProperty("counts").GetProperty("value_family_case_count").GetInt32());
        Assert.Equal(
            manifest.GetProperty("enum_cases").GetArrayLength(),
            manifest.GetProperty("counts").GetProperty("enum_case_count").GetInt32());
        Assert.Equal(
            manifest.GetProperty("structured_cases").GetArrayLength(),
            manifest.GetProperty("counts").GetProperty("structured_case_count").GetInt32());
        Assert.Equal(
            manifest.GetProperty("assignment_cases").GetArrayLength(),
            manifest.GetProperty("counts").GetProperty("assignment_case_count").GetInt32());
    }

    [Fact]
    public void ManifestCaseSetsExactlyMatchReviewedBindingAndValueEvidence()
    {
        var root = FindRepositoryRoot();
        using var manifest = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(
            root.FullName,
            "generated",
            "conformance",
            "sa",
            "2026.1.0529.7",
            "manifest.json")));
        using var registry = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(
            root.FullName,
            "bindings",
            "sa",
            "2026.1.0529.7",
            "registry.json")));
        using var values = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(
            root.FullName,
            "values",
            "sa",
            "2026.1.0529.7",
            "catalog.json")));

        var bindingRows = registry.RootElement.GetProperty("bindings").EnumerateArray()
            .Where(binding => Text(binding, "registry_status") == "usable")
            .SelectMany(binding => binding.GetProperty("semantic_value_families")
                .EnumerateArray()
                .Select(family => Key(
                    Text(binding, "method"),
                    Text(binding, "direction"),
                    family.GetString()!)))
            .ToHashSet(StringComparer.Ordinal);
        var bindingCases = manifest.RootElement.GetProperty("binding_cases").EnumerateArray()
            .ToArray();
        Assert.Equal(
            bindingRows.Order(StringComparer.Ordinal),
            bindingCases.Where(item => Text(item, "kind") == "binding.positive")
                .Select(BindingKey).Order(StringComparer.Ordinal));
        Assert.Equal(
            bindingRows.Order(StringComparer.Ordinal),
            bindingCases.Where(item => Text(item, "kind") is
                    "binding.setter_rejected" or "binding.getter_failed")
                .Select(BindingKey).Order(StringComparer.Ordinal));

        var implementedFamilies = values.RootElement.GetProperty("families").EnumerateArray()
            .Where(family => Text(family, "implementation_status") == "implemented")
            .Select(family => Key(
                Text(family, "family_id"),
                Text(family, "shape"),
                Text(family, "public_type_target"),
                Text(family, "worker_type_target")))
            .Order(StringComparer.Ordinal)
            .ToArray();
        var familyCases = manifest.RootElement.GetProperty("value_family_cases")
            .EnumerateArray().ToArray();
        foreach (var kind in new[] { "value_family.positive", "value_family.negative" })
        {
            Assert.Equal(
                implementedFamilies,
                familyCases.Where(item => Text(item, "kind") == kind)
                    .Select(item => Key(
                        Text(item, "family_id"),
                        Text(item, "shape"),
                        Text(item, "public_type"),
                        Text(item, "worker_type")))
                    .Order(StringComparer.Ordinal));
        }

        var expectedEnumMembers = values.RootElement.GetProperty("enum_types")
            .EnumerateArray()
            .SelectMany(type => type.GetProperty("members").EnumerateArray()
                .Select(member => Key(
                    Text(type, "public_type"),
                    Text(type, "worker_type"),
                    Text(member, "public_symbol"),
                    member.GetProperty("public_number").GetInt32().ToString(CultureInfo.InvariantCulture),
                    Text(member, "worker_symbol"),
                    Text(member, "sdk_literal"))))
            .Order(StringComparer.Ordinal)
            .ToArray();
        var enumCases = manifest.RootElement.GetProperty("enum_cases").EnumerateArray()
            .ToArray();
        Assert.Equal(
            expectedEnumMembers,
            enumCases.Where(item => Text(item, "kind") == "enum.member_positive")
                .Select(item => Key(
                    Text(item, "public_type"),
                    Text(item, "worker_type"),
                    Text(item, "public_symbol"),
                    item.GetProperty("public_number").GetInt32().ToString(CultureInfo.InvariantCulture),
                    Text(item, "worker_symbol"),
                    Text(item, "sdk_literal")))
                .Order(StringComparer.Ordinal));
        Assert.Equal(
            values.RootElement.GetProperty("enum_types").EnumerateArray()
                .Select(type => Key(Text(type, "public_type"), Text(type, "worker_type")))
                .Order(StringComparer.Ordinal),
            enumCases.Where(item => Text(item, "kind") == "enum.unknown_negative")
                .Select(item => Key(Text(item, "public_type"), Text(item, "worker_type")))
                .Order(StringComparer.Ordinal));

        var structuredTypes = values.RootElement.GetProperty("structured_types")
            .EnumerateArray().ToArray();
        var structuredCases = manifest.RootElement.GetProperty("structured_cases")
            .EnumerateArray().ToArray();
        var expectedStructuredTypes = structuredTypes
            .Select(type => Key(Text(type, "public_type"), Text(type, "worker_type")))
            .Order(StringComparer.Ordinal)
            .ToArray();
        foreach (var kind in new[] { "structured.positive", "structured.malformed_negative" })
        {
            Assert.Equal(
                expectedStructuredTypes,
                structuredCases.Where(item => Text(item, "kind") == kind)
                    .Select(item => Key(Text(item, "public_type"), Text(item, "worker_type")))
                    .Order(StringComparer.Ordinal));
        }

        var expectedFields = structuredTypes.SelectMany(type =>
            type.GetProperty("public_fields").EnumerateArray().Select(field => Key(
                Text(type, "public_type"),
                Text(type, "worker_type"),
                Text(field, "name"),
                field.GetProperty("number").GetInt32().ToString(CultureInfo.InvariantCulture),
                Text(field, "type"),
                Text(field, "cardinality"))))
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            expectedFields,
            structuredCases.Where(item => Text(item, "kind").EndsWith(
                    "_present",
                    StringComparison.Ordinal))
                .Select(StructuredFieldKey)
                .Order(StringComparer.Ordinal));
        var expectedAbsentFields = structuredTypes.SelectMany(type =>
            type.GetProperty("public_fields").EnumerateArray()
                .Where(field => Text(field, "cardinality") is "optional" or "repeated")
                .Select(field => Key(
                    Text(type, "public_type"),
                    Text(type, "worker_type"),
                    Text(field, "name"),
                    field.GetProperty("number").GetInt32().ToString(CultureInfo.InvariantCulture),
                    Text(field, "type"),
                    Text(field, "cardinality"))))
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            expectedAbsentFields,
            structuredCases.Where(item => Text(item, "kind") is
                    "structured.optional_absent" or "structured.repeated_empty")
                .Select(StructuredFieldKey)
                .Order(StringComparer.Ordinal));

        var expectedAssignments = values.RootElement.GetProperty("command_assignments")
            .EnumerateArray()
            .Select(AssignmentKey)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var assignmentCases = manifest.RootElement.GetProperty("assignment_cases")
            .EnumerateArray().ToArray();
        foreach (var kind in new[]
                 {
                     "assignment.reviewed_positive",
                     "assignment.unreviewed_family_negative"
                 })
        {
            Assert.Equal(
                expectedAssignments,
                assignmentCases.Where(item => Text(item, "kind") == kind)
                    .Select(AssignmentKey)
                    .Order(StringComparer.Ordinal));
        }
        var familiesByMethod = registry.RootElement.GetProperty("bindings")
            .EnumerateArray()
            .Where(binding => Text(binding, "registry_status") == "usable")
            .ToDictionary(
                binding => Text(binding, "method"),
                binding => binding.GetProperty("semantic_value_families")
                    .EnumerateArray().Select(item => item.GetString()!)
                    .ToHashSet(StringComparer.Ordinal),
                StringComparer.Ordinal);
        Assert.All(
            assignmentCases.Where(item => Text(item, "kind") ==
                "assignment.reviewed_positive"),
            item => Assert.Equal(JsonValueKind.Null, item.GetProperty(
                "rejected_family_id").ValueKind));
        Assert.All(
            assignmentCases.Where(item => Text(item, "kind") ==
                "assignment.unreviewed_family_negative"),
            item =>
            {
                var rejected = Text(item, "rejected_family_id");
                Assert.NotEqual(Text(item, "family_id"), rejected);
                Assert.Contains(rejected, familiesByMethod[Text(item, "method")]);
            });
    }

    [Fact]
    public void GenerationFailsWhenSupportedCatalogBindingLacksReviewedEvidence()
    {
        var root = FindRepositoryRoot();
        var temporaryRoot = CopyInputs(root, "briosa-conformance-binding-drift");
        try
        {
            var operationPath = Path.Combine(
                temporaryRoot,
                "catalog",
                "sa",
                "2026.1.0529.7",
                "operations",
                "file_operations.get_working_directory.json");
            var operation = JsonNode.Parse(File.ReadAllText(operationPath))!.AsObject();
            operation["arguments"]![0]!["semantic_type"] = "logical";
            File.WriteAllText(operationPath, operation.ToJsonString(new() { WriteIndented = true }));

            var exception = Assert.Throws<InvalidDataException>(() =>
                PortableConformanceGenerator.Generate(
                    temporaryRoot,
                    Path.Combine(temporaryRoot, "output")));

            Assert.Contains(
                "no usable 'GetStringArg' / 'logical' binding evidence",
                exception.Message,
                StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    [Fact]
    public void GenerationFailsOnDuplicateCaseIdentity()
    {
        var root = FindRepositoryRoot();
        var temporaryRoot = CopyInputs(root, "briosa-conformance-duplicate");
        try
        {
            var valuesPath = Path.Combine(
                temporaryRoot,
                "values",
                "sa",
                "2026.1.0529.7",
                "catalog.json");
            var values = JsonNode.Parse(File.ReadAllText(valuesPath))!.AsObject();
            var assignments = values["command_assignments"]!.AsArray();
            assignments.Add(assignments[0]!.DeepClone());
            File.WriteAllText(valuesPath, values.ToJsonString(new() { WriteIndented = true }));

            var exception = Assert.Throws<InvalidDataException>(() =>
                PortableConformanceGenerator.Generate(
                    temporaryRoot,
                    Path.Combine(temporaryRoot, "output")));

            Assert.Contains("assignment case_id", exception.Message, StringComparison.Ordinal);
            Assert.Contains("is duplicated", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    [Fact]
    public void GenerationFailsClosedForAnUnreviewedSharedMethodFamilyAssignment()
    {
        var root = FindRepositoryRoot();
        var temporaryRoot = CopyInputs(root, "briosa-conformance-shared-family");
        try
        {
            var operationPath = Path.Combine(
                temporaryRoot,
                "catalog",
                "sa",
                "2026.1.0529.7",
                "operations",
                "file_operations.get_working_directory.json");
            var operation = JsonNode.Parse(File.ReadAllText(operationPath))!.AsObject();
            var output = operation["arguments"]![0]!;
            output["semantic_type"] = "collection_item_name";
            output["sdk_binding"]!["getter"] = "GetCollectionObjectNameArg";
            File.WriteAllText(operationPath, operation.ToJsonString(new() { WriteIndented = true }));

            var exception = Assert.Throws<InvalidDataException>(() =>
                PortableConformanceGenerator.Generate(
                    temporaryRoot,
                    Path.Combine(temporaryRoot, "output")));

            Assert.Contains(
                "lacks its exact reviewed multi-family assignment",
                exception.Message,
                StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    private static string CopyInputs(DirectoryInfo sourceRoot, string prefix)
    {
        var destinationRoot = TemporaryDirectory(prefix);
        foreach (var directory in new[] { "catalog", "proto", "bindings", "values" })
        {
            CopyDirectory(
                Path.Combine(sourceRoot.FullName, directory),
                Path.Combine(destinationRoot, directory));
        }

        return destinationRoot;
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var path in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, path)));
        }

        foreach (var path in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(destination, Path.GetRelativePath(source, path));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(path, target);
        }
    }

    private static string TemporaryDirectory(string prefix)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{prefix}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static string Sha256(string path) =>
        Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));

    private static string BindingKey(JsonElement item) =>
        Key(Text(item, "method"), Text(item, "direction"), Text(item, "family_id"));

    private static string StructuredFieldKey(JsonElement item) =>
        Key(
            Text(item, "public_type"),
            Text(item, "worker_type"),
            Text(item, "field_name"),
            item.GetProperty("field_number").GetInt32().ToString(CultureInfo.InvariantCulture),
            Text(item, "field_type"),
            Text(item, "cardinality"));

    private static string AssignmentKey(JsonElement item) =>
        Key(
            Text(item, "method"),
            Text(item, "inventory_key"),
            item.GetProperty("sdk_order").GetInt32().ToString(CultureInfo.InvariantCulture),
            Text(item, "family_id"));

    private static string Text(JsonElement item, string property) =>
        item.GetProperty(property).GetString()!;

    private static string Key(params string[] parts) => string.Join('\u001f', parts);

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Briosa.slnx")))
        {
            directory = directory.Parent;
        }

        return directory ??
            throw new DirectoryNotFoundException("Could not locate the Briosa repository root.");
    }
}
