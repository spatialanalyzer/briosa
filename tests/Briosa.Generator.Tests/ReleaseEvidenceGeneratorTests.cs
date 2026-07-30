using System.Text.Json;
using System.Text.Json.Nodes;
using Briosa.Generator;

namespace Briosa.Generator.Tests;

public sealed class ReleaseEvidenceGeneratorTests
{
    [Fact]
    public void CommittedReleaseEvidenceMatchesDeterministicGeneration()
    {
        var root = FindRepositoryRoot();
        var output = TemporaryDirectory("briosa-release-evidence");
        try
        {
            var result = ReleaseEvidenceGenerator.Generate(root.FullName, output);

            Assert.Equal(
                [
                    "docs/reference/generated/sa/2026.1.0529.7/release-audit.md",
                    "docs/reference/generated/sa/2026.1.0529.7/support-matrix.md",
                    "generated/release/sa/2026.1.0529.7/release-audit.json",
                    "generated/release/sa/2026.1.0529.7/support-matrix.json"
                ],
                result.Files);
            Assert.All(result.Files, relativePath => Assert.Equal(
                File.ReadAllBytes(Path.Combine(root.FullName, relativePath)),
                File.ReadAllBytes(Path.Combine(output, relativePath))));
        }
        finally
        {
            Directory.Delete(output, recursive: true);
        }
    }

    [Fact]
    public void MatrixReconcilesEveryInventoryCommandWithoutImplyingProtectedSupport()
    {
        var root = FindRepositoryRoot();
        using var matrix = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(
            root.FullName,
            "generated",
            "release",
            "sa",
            "2026.1.0529.7",
            "support-matrix.json")));
        var document = matrix.RootElement;
        var counts = document.GetProperty("counts");
        var commands = document.GetProperty("commands").EnumerateArray().ToArray();
        var expectedCatalogedOperations = CatalogOperationCount(root.FullName);

        Assert.Equal(1412, counts.GetProperty("inventory_commands").GetInt32());
        Assert.Equal(
            expectedCatalogedOperations,
            counts.GetProperty("cataloged_operations").GetInt32());
        Assert.Equal(684, counts.GetProperty("approved_candidates").GetInt32());
        Assert.Equal(
            684 - expectedCatalogedOperations,
            counts.GetProperty("approved_not_cataloged").GetInt32());
        Assert.Equal(40, counts.GetProperty("blocked").GetInt32());
        Assert.Equal(477, counts.GetProperty("intentional_exclusions").GetInt32());
        Assert.Equal(211, counts.GetProperty("sdk_unavailable").GetInt32());
        Assert.Equal(1412, commands.Length);
        Assert.Equal(
            1412,
            commands.Select(command => Text(command, "inventory_key"))
                .Distinct(StringComparer.Ordinal)
                .Count());

        var cataloged = commands.Where(command =>
            command.GetProperty("operation_id").ValueKind != JsonValueKind.Null).ToArray();
        Assert.Equal(expectedCatalogedOperations, cataloged.Length);
        Assert.All(cataloged, command =>
        {
            Assert.Equal(
                "cataloged_portable_only",
                Text(command, "release_classification"));
            Assert.Equal(
                "portable_briosa_contract",
                Text(command.GetProperty("validation"), "portable"));
            Assert.Equal(
                "not_performed",
                Text(command.GetProperty("validation"), "protected_spatial_analyzer"));
            Assert.Contains(
                command.GetProperty("prerequisites").EnumerateArray(),
                prerequisite => prerequisite.GetString() == "runtime_policy_allowlist");
        });
    }

    [Fact]
    public void FutureCatalogedMutationIsAutomaticallyAddedAndBlocksRiskFixtureAudit()
    {
        var root = FindRepositoryRoot();
        var temporaryRoot = CopyInputs(root, "briosa-release-future-catalog");
        try
        {
            var baselineCatalogedOperations = CatalogOperationCount(temporaryRoot);
            var disposition = ReadDispositionEntries(temporaryRoot)
                .First(entry =>
                    entry["disposition"]!.GetValue<string>() == "approved_candidate" &&
                    entry["risk_effect"]!.GetValue<string>() == "mutating");
            const string operationId = "file_operations.future_mutating_operation";
            AddCatalogOperation(
                temporaryRoot,
                disposition,
                operationId,
                "FutureMutatingOperation");
            AddConformanceOperation(temporaryRoot, operationId);

            var output = Path.Combine(temporaryRoot, "output");
            ReleaseEvidenceGenerator.Generate(temporaryRoot, output);
            using var matrix = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(
                output,
                "generated",
                "release",
                "sa",
                "2026.1.0529.7",
                "support-matrix.json")));
            using var audit = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(
                output,
                "generated",
                "release",
                "sa",
                "2026.1.0529.7",
                "release-audit.json")));

            Assert.Equal(
                baselineCatalogedOperations + 1,
                matrix.RootElement.GetProperty("counts")
                    .GetProperty("cataloged_operations")
                    .GetInt32());
            Assert.Contains(
                matrix.RootElement.GetProperty("commands").EnumerateArray(),
                command => command.GetProperty("operation_id").ValueKind !=
                    JsonValueKind.Null &&
                    Text(command, "operation_id") == operationId);
            var fixtureCriterion = Assert.Single(
                audit.RootElement.GetProperty("criteria").EnumerateArray(),
                criterion => Text(criterion, "criterion_id") ==
                    "epic-47-risk-fixtures");
            Assert.Equal("blocked", Text(fixtureCriterion, "status"));
            Assert.Contains(
                fixtureCriterion.GetProperty("evidence").EnumerateArray(),
                item => item.GetString() == operationId);
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    [Fact]
    public void CatalogOperationWithoutApprovedDispositionFailsClosed()
    {
        var root = FindRepositoryRoot();
        var temporaryRoot = CopyInputs(root, "briosa-release-disposition-drift");
        try
        {
            var excluded = ReadDispositionEntries(temporaryRoot)
                .First(entry =>
                    entry["disposition"]!.GetValue<string>() == "intentional_exclusion" &&
                    entry["risk_effect"]!.GetValue<string>() != "unknown" &&
                    !entry["risk_flags"]!.AsArray().Any(flag =>
                        flag!.GetValue<string>() == "unknown"));
            var operationPath = Path.Combine(
                temporaryRoot,
                "catalog",
                "sa",
                "2026.1.0529.7",
                "operations",
                "file_operations.get_working_directory.json");
            var operation = JsonNode.Parse(File.ReadAllText(operationPath))!.AsObject();
            operation["inventory_key"] = excluded["inventory_key"]!.GetValue<string>();
            operation["mp_step"] = excluded["mp_step"]!.GetValue<string>();
            operation["risk"]!["effect"] = excluded["risk_effect"]!.GetValue<string>();
            operation["risk"]!["flags"] = excluded["risk_flags"]!.DeepClone();
            WriteJson(operationPath, operation);

            var exception = Assert.Throws<InvalidDataException>(() =>
                ReleaseEvidenceGenerator.Generate(
                    temporaryRoot,
                    Path.Combine(temporaryRoot, "output")));

            Assert.Contains(
                "not backed by the exact reviewed approved disposition",
                exception.Message,
                StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    [Fact]
    public void ProtectedEvidenceCannotBeClaimedBeforeItsContractExists()
    {
        var root = FindRepositoryRoot();
        var temporaryRoot = CopyInputs(root, "briosa-release-premature-evidence");
        try
        {
            var policyPath = Path.Combine(
                temporaryRoot,
                "release",
                "sa",
                "2026.1.0529.7",
                "audit-policy.json");
            var policy = JsonNode.Parse(File.ReadAllText(policyPath))!.AsObject();
            policy["protected_conformance"]!["status"] = "verified";
            WriteJson(policyPath, policy);

            var exception = Assert.Throws<InvalidDataException>(() =>
                ReleaseEvidenceGenerator.Generate(
                    temporaryRoot,
                    Path.Combine(temporaryRoot, "output")));

            Assert.Contains(
                "must remain pending",
                exception.Message,
                StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    [Fact]
    public void ReleaseWorkflowCannotBypassTheGeneratedReadinessAssertion()
    {
        var root = FindRepositoryRoot();
        var temporaryRoot = CopyInputs(root, "briosa-release-workflow-bypass");
        try
        {
            var workflowPath = Path.Combine(
                temporaryRoot,
                ".github",
                "workflows",
                "release.yml");
            var workflow = File.ReadAllText(workflowPath).Replace(
                "./eng/Assert-ReleaseReady.ps1",
                "./eng/Verify-ReleaseEvidence.ps1",
                StringComparison.Ordinal);
            File.WriteAllText(workflowPath, workflow);

            var exception = Assert.Throws<InvalidDataException>(() =>
                ReleaseEvidenceGenerator.Generate(
                    temporaryRoot,
                    Path.Combine(temporaryRoot, "output")));

            Assert.Contains(
                "Assert-ReleaseReady.ps1",
                exception.Message,
                StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    private static void AddCatalogOperation(
        string root,
        JsonObject disposition,
        string operationId,
        string rpc)
    {
        var targetRoot = Path.Combine(root, "catalog", "sa", "2026.1.0529.7");
        var existingPath = Path.Combine(
            targetRoot,
            "operations",
            "file_operations.get_working_directory.json");
        var operation = JsonNode.Parse(File.ReadAllText(existingPath))!.AsObject();
        operation["operation_id"] = operationId;
        operation["inventory_key"] = disposition["inventory_key"]!.GetValue<string>();
        operation["mp_step"] = disposition["mp_step"]!.GetValue<string>();
        operation["protocol"] = new JsonObject
        {
            ["service"] = "FileOperations",
            ["rpc"] = rpc,
            ["request"] = $"{rpc}Request",
            ["result"] = $"{rpc}Result"
        };
        operation["execution_scope"] = "global_state_mutation";
        operation["risk"] = new JsonObject
        {
            ["effect"] = disposition["risk_effect"]!.GetValue<string>(),
            ["replay_safety"] = "unsafe",
            ["flags"] = disposition["risk_flags"]!.DeepClone()
        };
        var relativePath = $"operations/{operationId}.json";
        WriteJson(Path.Combine(targetRoot, relativePath.Replace(
            '/',
            Path.DirectorySeparatorChar)), operation);

        var manifestPath = Path.Combine(targetRoot, "catalog.json");
        var manifest = JsonNode.Parse(File.ReadAllText(manifestPath))!.AsObject();
        var files = manifest["operation_files"]!.AsArray()
            .Select(item => item!.GetValue<string>())
            .Append(relativePath)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        manifest["operation_files"] = new JsonArray(
            files.Select(value => (JsonNode?)JsonValue.Create(value)).ToArray());
        WriteJson(manifestPath, manifest);
    }

    private static void AddConformanceOperation(string root, string operationId)
    {
        var path = Path.Combine(
            root,
            "generated",
            "conformance",
            "sa",
            "2026.1.0529.7",
            "manifest.json");
        var document = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        document["operations"]!.AsArray().Add(new JsonObject
        {
            ["operation_id"] = operationId
        });
        document["counts"]!["operation_count"] =
            document["operations"]!.AsArray().Count;
        WriteJson(path, document);
    }

    private static JsonObject[] ReadDispositionEntries(string root)
    {
        var dispositionRoot = Path.Combine(
            root,
            "disposition",
            "sa",
            "2026.1.0529.7");
        var manifest = JsonNode.Parse(File.ReadAllText(Path.Combine(
            dispositionRoot,
            "manifest.json")))!.AsObject();
        return manifest["shards"]!.AsArray()
            .SelectMany(reference =>
            {
                var path = Path.Combine(
                    dispositionRoot,
                    reference!["path"]!.GetValue<string>().Replace(
                        '/',
                        Path.DirectorySeparatorChar));
                return JsonNode.Parse(File.ReadAllText(path))!["entries"]!.AsArray()
                    .Select(entry => entry!.AsObject());
            })
            .ToArray();
    }

    private static string CopyInputs(DirectoryInfo sourceRoot, string prefix)
    {
        var destinationRoot = TemporaryDirectory(prefix);
        foreach (var directory in new[]
                 {
                     ".github",
                     "catalog",
                     "disposition",
                     "docs",
                     "eng",
                     "generated",
                     "inventory",
                     "proto",
                     "release"
                 })
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
        foreach (var path in Directory.EnumerateDirectories(
                     source,
                     "*",
                     SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(
                destination,
                Path.GetRelativePath(source, path)));
        }

        foreach (var path in Directory.EnumerateFiles(
                     source,
                     "*",
                     SearchOption.AllDirectories))
        {
            var target = Path.Combine(destination, Path.GetRelativePath(source, path));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(path, target);
        }
    }

    private static void WriteJson(string path, JsonObject document)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(
            path,
            document.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) +
                Environment.NewLine);
    }

    private static string TemporaryDirectory(string prefix)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{prefix}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static int CatalogOperationCount(string repositoryRoot)
    {
        using var catalog = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(
            repositoryRoot,
            "catalog",
            "sa",
            "2026.1.0529.7",
            "catalog.json")));
        return catalog.RootElement.GetProperty("operation_files").GetArrayLength();
    }

    private static string Text(JsonElement value, string property) =>
        value.GetProperty(property).GetString()!;

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "Briosa.slnx")))
        {
            directory = directory.Parent;
        }

        return directory ??
            throw new DirectoryNotFoundException(
                "Could not locate the Briosa repository root.");
    }
}
