using System.Security.Cryptography;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Briosa.Generator;

internal sealed record ReleaseEvidenceGenerationResult(IReadOnlyList<string> Files);

internal static class ReleaseEvidenceGenerator
{
    internal const string GeneratedArtifactIdentity =
        "Briosa.Generator release evidence";

    private const string SupportMatrixSchemaReference =
        "../../../../release/schemas/v1/support-matrix.schema.json";
    private const string ReleaseAuditSchemaReference =
        "../../../../release/schemas/v1/release-audit.schema.json";

    private static readonly JsonSerializerOptions InputJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    private static readonly JsonSerializerOptions OutputJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true
    };

    public static ReleaseEvidenceGenerationResult Generate(
        string repositoryRoot,
        string outputRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputRoot);

        var root = Path.GetFullPath(repositoryRoot);
        var output = Path.GetFullPath(outputRoot);
        var catalogRoot = Path.Combine(root, "catalog");
        var catalogValidation = CommandCatalogValidator.ValidateDirectory(catalogRoot);
        if (!catalogValidation.IsValid)
        {
            throw new InvalidDataException(
                "Release-evidence generation requires a valid catalog: " +
                string.Join(" ", catalogValidation.Errors));
        }

        var generatedFiles = new List<string>();
        foreach (var policyPath in Directory
                     .EnumerateFiles(
                         Path.Combine(root, "release", "sa"),
                         "audit-policy.json",
                         SearchOption.AllDirectories)
                     .OrderBy(path => path, StringComparer.Ordinal))
        {
            var policy = Deserialize<ReleaseAuditPolicy>(policyPath);
            ValidatePolicy(root, policyPath, policy);

            var target = policy.SpatialAnalyzerTarget;
            var inventoryPath = Path.Combine(
                root,
                "inventory",
                "sa",
                target,
                "inventory.json");
            var dispositionRoot = Path.Combine(root, "disposition", "sa", target);
            var dispositionValidation =
                CommandDispositionLedger.Validate(inventoryPath, dispositionRoot);
            if (!dispositionValidation.IsValid)
            {
                throw new InvalidDataException(
                    $"Release-evidence generation requires a valid {target} disposition ledger: " +
                    string.Join(" ", dispositionValidation.Errors));
            }

            var dispositionManifestPath = Path.Combine(dispositionRoot, "manifest.json");
            var dispositionManifest =
                Deserialize<CommandDispositionManifest>(dispositionManifestPath);
            var dispositions = dispositionManifest.Shards
                .SelectMany(reference =>
                {
                    var shardPath = ResolveContained(
                        dispositionRoot,
                        reference.Path,
                        "disposition shard");
                    return Deserialize<CommandDispositionShard>(shardPath).Entries;
                })
                .OrderBy(entry => entry.InventoryKey, StringComparer.Ordinal)
                .ToArray();

            var catalogManifestPath = Path.Combine(
                catalogRoot,
                "sa",
                target,
                "catalog.json");
            var catalog = Deserialize<CommandCatalogManifest>(catalogManifestPath);
            var catalogTargetRoot = Path.GetDirectoryName(catalogManifestPath) ??
                throw new InvalidDataException("The catalog manifest has no parent directory.");
            var operationPaths = catalog.OperationFiles
                .Select(path => ResolveContained(catalogTargetRoot, path, "catalog operation"))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            var operations = operationPaths
                .Select(Deserialize<CommandCatalogOperation>)
                .OrderBy(operation => operation.OperationId, StringComparer.Ordinal)
                .ToArray();

            var conformancePath = Path.Combine(
                root,
                "generated",
                "conformance",
                "sa",
                target,
                "manifest.json");
            var conformanceOperationIds = ReadConformanceOperationIds(conformancePath);

            ValidateJoins(
                dispositionManifest,
                dispositions,
                catalog,
                operations,
                conformanceOperationIds);

            string[] supportEvidencePaths =
            [
                inventoryPath,
                dispositionManifestPath,
                .. dispositionManifest.Shards.Select(reference =>
                    ResolveContained(dispositionRoot, reference.Path, "disposition shard")),
                catalogManifestPath,
                .. operationPaths,
                conformancePath
            ];
            var supportEvidence = CreateEvidence(root, supportEvidencePaths);
            var matrix = CreateSupportMatrix(
                target,
                dispositions,
                operations,
                conformanceOperationIds,
                supportEvidence);
            var matrixRelativePath =
                $"generated/release/sa/{target}/support-matrix.json";
            var matrixJson = Serialize(matrix);
            WriteGeneratedFile(output, matrixRelativePath, matrixJson);
            generatedFiles.Add(matrixRelativePath);

            var fullSurfacePolicyPath = Path.Combine(root, "eng", "full-surface-policy.json");
            var ciWorkflowPath = Path.Combine(root, ".github", "workflows", "ci.yml");
            var releaseWorkflowPath =
                Path.Combine(root, ".github", "workflows", "release.yml");
            ValidateRepositoryReleaseGates(
                root,
                fullSurfacePolicyPath,
                ciWorkflowPath,
                releaseWorkflowPath);
            string[] auditEvidencePaths =
            [
                policyPath,
                fullSurfacePolicyPath,
                ciWorkflowPath,
                releaseWorkflowPath,
                .. policy.OperatorGuidePaths.Select(path =>
                    ResolveContained(root, path, "operator guide"))
            ];
            var auditEvidence = CreateEvidence(root, auditEvidencePaths)
                .Append(new ReleaseEvidenceInput(
                    matrixRelativePath,
                    Sha256(Encoding.UTF8.GetBytes(matrixJson))))
                .OrderBy(item => item.Path, StringComparer.Ordinal)
                .ToArray();
            var audit = CreateReleaseAudit(
                root,
                policy,
                matrix,
                auditEvidence,
                fullSurfacePolicyPath);
            var auditRelativePath =
                $"generated/release/sa/{target}/release-audit.json";
            WriteGeneratedFile(output, auditRelativePath, Serialize(audit));
            generatedFiles.Add(auditRelativePath);

            var supportDocumentRelativePath =
                $"docs/reference/generated/sa/{target}/support-matrix.md";
            WriteGeneratedFile(
                output,
                supportDocumentRelativePath,
                CreateSupportMatrixDocument(matrix));
            generatedFiles.Add(supportDocumentRelativePath);

            var auditDocumentRelativePath =
                $"docs/reference/generated/sa/{target}/release-audit.md";
            WriteGeneratedFile(
                output,
                auditDocumentRelativePath,
                CreateReleaseAuditDocument(audit));
            generatedFiles.Add(auditDocumentRelativePath);
        }

        return new ReleaseEvidenceGenerationResult(
            generatedFiles.OrderBy(path => path, StringComparer.Ordinal).ToArray());
    }

    private static ReleaseSupportMatrix CreateSupportMatrix(
        string target,
        IReadOnlyList<CommandDispositionEntry> dispositions,
        IReadOnlyList<CommandCatalogOperation> operations,
        HashSet<string> conformanceOperationIds,
        IReadOnlyList<ReleaseEvidenceInput> evidence)
    {
        var operationsByInventoryKey = operations.ToDictionary(
            operation => operation.InventoryKey,
            StringComparer.Ordinal);
        var commands = dispositions.Select(disposition =>
        {
            operationsByInventoryKey.TryGetValue(disposition.InventoryKey, out var operation);
            var cataloged = operation is not null;
            var classification = disposition.Disposition switch
            {
                "approved_candidate" when cataloged => "cataloged_portable_only",
                "approved_candidate" => "approved_not_cataloged",
                "blocked" => "blocked",
                "intentional_exclusion" => "intentional_exclusion",
                "sdk_unavailable" => "sdk_unavailable",
                _ => throw new InvalidDataException(
                    $"Unknown disposition '{disposition.Disposition}' for " +
                    $"'{disposition.InventoryKey}'.")
            };
            var prerequisites = operation is null
                ? []
                : CreatePrerequisites(operation);

            return new ReleaseSupportCommand(
                disposition.InventoryKey,
                disposition.MpStep,
                [.. disposition.CategoryPath],
                disposition.Disposition,
                disposition.ReviewState,
                classification,
                operation?.OperationId,
                disposition.RiskEffect,
                [.. disposition.RiskFlags],
                operation?.Risk.ReplaySafety,
                operation?.ExecutionScope,
                prerequisites,
                new ReleaseValidationTier(
                    "reviewed_metadata",
                    cataloged ? "portable_briosa_contract" : "not_applicable",
                    cataloged ? "not_performed" : "not_applicable"),
                [.. disposition.BlockerReferences]);
        }).ToArray();

        var counts = new ReleaseSupportCounts(
            commands.Length,
            commands.Count(command => command.OperationId is not null),
            commands.Count(command => command.Disposition == "approved_candidate"),
            commands.Count(command =>
                command.ReleaseClassification == "approved_not_cataloged"),
            commands.Count(command => command.Disposition == "blocked"),
            commands.Count(command => command.Disposition == "intentional_exclusion"),
            commands.Count(command => command.Disposition == "sdk_unavailable"),
            commands.Count(command =>
                command.Validation.Portable == "portable_briosa_contract"),
            commands.Count(command =>
                command.Validation.ProtectedSpatialAnalyzer != "not_performed" &&
                command.Validation.ProtectedSpatialAnalyzer != "not_applicable"));

        return new ReleaseSupportMatrix(
            SupportMatrixSchemaReference,
            1,
            GeneratedArtifactIdentity,
            target,
            evidence,
            counts,
            commands);
    }

    private static ReleaseAudit CreateReleaseAudit(
        string repositoryRoot,
        ReleaseAuditPolicy policy,
        ReleaseSupportMatrix matrix,
        IReadOnlyList<ReleaseEvidenceInput> evidence,
        string fullSurfacePolicyPath)
    {
        var protectedStatus = policy.ProtectedConformance.Status == "pending"
            ? "blocked"
            : throw new InvalidDataException(
                "Only the fail-closed 'pending' protected-conformance state is supported " +
                "until issue #69 defines its evidence manifest.");
        var identityStatus = policy.RuntimeIdentityValidation.Status == "pending"
            ? "blocked"
            : throw new InvalidDataException(
                "Only the fail-closed 'pending' runtime-identity-validation state is " +
                "supported until issue #70 records protected evidence.");
        var riskyCatalogedOperations = matrix.Commands
            .Where(command =>
                command.OperationId is not null &&
                (command.RiskEffect == "mutating" ||
                 command.RiskFlags.Contains("device_control", StringComparer.Ordinal)))
            .Select(command => command.OperationId!)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        using var fullSurfaceDocument = JsonDocument.Parse(
            File.ReadAllBytes(fullSurfacePolicyPath));
        var releasedBaselines = fullSurfaceDocument.RootElement
            .GetProperty("released_protocol_baselines")
            .GetArrayLength();

        var criteria = new[]
        {
            new ReleaseAuditCriterion(
                "epic-47-portable-conformance",
                policy.MasterEpic,
                "Portable tests cover every cataloged operation.",
                "passed",
                [
                    "generated/release/sa/" + policy.SpatialAnalyzerTarget +
                        "/support-matrix.json",
                    "generated/conformance/sa/" + policy.SpatialAnalyzerTarget +
                        "/manifest.json"
                ],
                []),
            new ReleaseAuditCriterion(
                "epic-47-protected-runner",
                policy.MasterEpic,
                "The protected runner executes the approved real-SA matrix from trusted artifacts.",
                protectedStatus,
                [],
                [.. policy.ProtectedConformance.BlockerReferences]),
            new ReleaseAuditCriterion(
                "epic-47-risk-fixtures",
                policy.MasterEpic,
                "Mutating and device-related operations have reviewed fixtures and controls.",
                riskyCatalogedOperations.Length == 0 ? "not_applicable" : protectedStatus,
                riskyCatalogedOperations,
                riskyCatalogedOperations.Length == 0
                    ? []
                    : [.. policy.ProtectedConformance.BlockerReferences]),
            new ReleaseAuditCriterion(
                "epic-47-performance-and-reproducibility",
                policy.MasterEpic,
                "Full-surface budgets and byte-reproducible package gates are configured.",
                "passed",
                [
                    "eng/full-surface-policy.json",
                    "eng/Test-RuntimePerformance.ps1",
                    "eng/Test-WindowsPackage.ps1",
                    "eng/Test-ProtocolArtifact.ps1",
                    ".github/workflows/release.yml"
                ],
                []),
            new ReleaseAuditCriterion(
                "epic-47-support-matrix",
                policy.MasterEpic,
                "Every exact-target inventory command has one fail-closed classification.",
                "passed",
                ["generated/release/sa/" + policy.SpatialAnalyzerTarget +
                    "/support-matrix.json"],
                []),
            new ReleaseAuditCriterion(
                "issue-72-runtime-identity-validation",
                "https://github.com/spatialanalyzer/briosa/issues/72",
                "Exact-target runtime identity policy has protected matching and mismatch evidence.",
                identityStatus,
                [],
                [.. policy.RuntimeIdentityValidation.BlockerReferences]),
            new ReleaseAuditCriterion(
                "issue-72-protocol-baselines",
                "https://github.com/spatialanalyzer/briosa/issues/72",
                releasedBaselines == 0
                    ? "No immutable protocol baseline is required before the first public release."
                    : "Every prior public protocol baseline is pinned to an immutable tag and commit.",
                releasedBaselines == 0 ? "not_applicable" : "passed",
                ["eng/full-surface-policy.json"],
                [])
        };
        var ready = criteria.All(criterion =>
            criterion.Status is "passed" or "not_applicable");

        return new ReleaseAudit(
            ReleaseAuditSchemaReference,
            1,
            GeneratedArtifactIdentity,
            policy.SpatialAnalyzerTarget,
            policy.MasterEpic,
            ready,
            evidence,
            new ReleaseAuditSummary(
                criteria.Count(criterion => criterion.Status == "passed"),
                criteria.Count(criterion => criterion.Status == "blocked"),
                criteria.Count(criterion => criterion.Status == "not_applicable")),
            criteria);
    }

    private static string[] CreatePrerequisites(CommandCatalogOperation operation)
    {
        var prerequisites = new SortedSet<string>(StringComparer.Ordinal)
        {
            "exact_target_identity",
            "execution_channel_readiness",
            "licensed_spatial_analyzer_installation",
            "runtime_policy_allowlist"
        };
        if (operation.ExecutionScope != "self_contained")
        {
            prerequisites.Add("single_tenant_coordination");
        }

        foreach (var flag in operation.Risk.Flags)
        {
            prerequisites.Add($"risk_policy_{flag}");
        }

        return [.. prerequisites];
    }

    private static void ValidatePolicy(
        string repositoryRoot,
        string policyPath,
        ReleaseAuditPolicy policy)
    {
        const string expectedSchema = "../../schemas/v1/audit-policy.schema.json";
        if (policy.Schema != expectedSchema ||
            policy.SchemaVersion != 1 ||
            string.IsNullOrWhiteSpace(policy.SpatialAnalyzerTarget) ||
            policy.MasterEpic != "https://github.com/spatialanalyzer/briosa/issues/47")
        {
            throw new InvalidDataException(
                $"Release audit policy identity is invalid: {policyPath}");
        }

        var expectedDirectory = Path.Combine(
            repositoryRoot,
            "release",
            "sa",
            policy.SpatialAnalyzerTarget);
        if (!string.Equals(
                Path.GetFullPath(Path.GetDirectoryName(policyPath)!),
                Path.GetFullPath(expectedDirectory),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"Release audit policy target/path identity is invalid: {policyPath}");
        }

        ValidatePendingEvidence(
            policy.ProtectedConformance,
            "protected_conformance");
        ValidatePendingEvidence(
            policy.RuntimeIdentityValidation,
            "runtime_identity_validation");
        RequireOrdinalUnique(policy.OperatorGuidePaths, "operator_guide_paths");
        foreach (var path in policy.OperatorGuidePaths)
        {
            _ = ResolveContained(repositoryRoot, path, "operator guide");
        }
    }

    private static void ValidatePendingEvidence(
        ReleasePendingEvidence policy,
        string name)
    {
        if (policy.Status != "pending" ||
            policy.BlockerReferences.Count == 0)
        {
            throw new InvalidDataException(
                $"{name} must remain pending with at least one blocker until its " +
                "versioned protected-evidence contract exists.");
        }

        RequireOrdinalUnique(policy.BlockerReferences, $"{name}.blocker_references");
    }

    private static void ValidateJoins(
        CommandDispositionManifest dispositionManifest,
        CommandDispositionEntry[] dispositions,
        CommandCatalogManifest catalog,
        IReadOnlyList<CommandCatalogOperation> operations,
        HashSet<string> conformanceOperationIds)
    {
        if (dispositionManifest.SpatialAnalyzerTarget != catalog.SpatialAnalyzerTarget)
        {
            throw new InvalidDataException(
                "Disposition and catalog exact-target identities differ.");
        }

        RequireUnique(
            dispositions.Select(entry => entry.InventoryKey),
            "disposition inventory_key");
        RequireUnique(
            operations.Select(operation => operation.InventoryKey),
            "catalog inventory_key");
        RequireUnique(
            operations.Select(operation => operation.OperationId),
            "catalog operation_id");

        var dispositionsByKey = dispositions.ToDictionary(
            entry => entry.InventoryKey,
            StringComparer.Ordinal);
        foreach (var operation in operations)
        {
            if (!dispositionsByKey.TryGetValue(operation.InventoryKey, out var disposition))
            {
                throw new InvalidDataException(
                    $"Catalog operation '{operation.OperationId}' has no disposition entry.");
            }

            if (disposition.Disposition != "approved_candidate" ||
                disposition.ReviewState != "reviewed" ||
                disposition.MpStep != operation.MpStep)
            {
                throw new InvalidDataException(
                    $"Catalog operation '{operation.OperationId}' is not backed by the exact " +
                    "reviewed approved disposition and MP identity.");
            }

            if (disposition.RiskEffect != operation.Risk.Effect ||
                !disposition.RiskFlags.SequenceEqual(operation.Risk.Flags))
            {
                throw new InvalidDataException(
                    $"Catalog operation '{operation.OperationId}' risk metadata differs from " +
                    "its reviewed disposition.");
            }

            if (!conformanceOperationIds.Contains(operation.OperationId))
            {
                throw new InvalidDataException(
                    $"Catalog operation '{operation.OperationId}' is absent from portable " +
                    "conformance evidence.");
            }
        }

        var catalogOperationIds = operations
            .Select(operation => operation.OperationId)
            .ToHashSet(StringComparer.Ordinal);
        if (!catalogOperationIds.SetEquals(conformanceOperationIds))
        {
            throw new InvalidDataException(
                "Portable conformance operation coverage differs from the supported catalog.");
        }

        if (dispositions.Length != dispositionManifest.Inventory.CommandCount)
        {
            throw new InvalidDataException(
                "Disposition entry count does not reconcile the inventory command count.");
        }
    }

    private static void ValidateRepositoryReleaseGates(
        string repositoryRoot,
        string fullSurfacePolicyPath,
        string ciWorkflowPath,
        string releaseWorkflowPath)
    {
        using var fullSurfaceDocument = JsonDocument.Parse(
            File.ReadAllBytes(fullSurfacePolicyPath));
        var surfaceIds = fullSurfaceDocument.RootElement.GetProperty("surfaces")
            .EnumerateArray()
            .Select(surface => surface.GetProperty("id").GetString())
            .ToHashSet(StringComparer.Ordinal);
        if (!surfaceIds.Contains("release-evidence"))
        {
            throw new InvalidDataException(
                "The full-surface policy does not include release evidence.");
        }

        var ciWorkflow = File.ReadAllText(ciWorkflowPath);
        if (!ciWorkflow.Contains(
                "./eng/Verify-FullSurface.ps1",
                StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "Ordinary CI does not enforce the complete full-surface gate.");
        }

        var releaseWorkflow = File.ReadAllText(releaseWorkflowPath);
        var orderedCommands = new[]
        {
            "./eng/Test-WindowsPackage.ps1",
            "./eng/Test-ProtocolArtifact.ps1",
            "./eng/Test-GeneratedClientScenarios.ps1",
            "./eng/Assert-ReleaseReady.ps1",
            "actions/upload-artifact@",
            "gh @releaseArguments @files"
        };
        var previousIndex = -1;
        foreach (var command in orderedCommands)
        {
            var index = releaseWorkflow.IndexOf(
                command,
                previousIndex + 1,
                StringComparison.Ordinal);
            if (index < 0)
            {
                throw new InvalidDataException(
                    $"Release workflow is missing or misorders required gate '{command}'.");
            }

            previousIndex = index;
        }

        foreach (var script in new[]
                 {
                     "eng/Assert-ReleaseReady.ps1",
                     "eng/Test-GeneratedClientScenarios.ps1",
                     "eng/Test-ProtocolArtifact.ps1",
                     "eng/Test-RuntimePerformance.ps1",
                     "eng/Test-WindowsPackage.ps1",
                     "eng/Verify-ReleaseEvidence.ps1"
                 })
        {
            _ = ResolveContained(repositoryRoot, script, "release gate");
            if (!File.Exists(Path.Combine(
                    repositoryRoot,
                    script.Replace('/', Path.DirectorySeparatorChar))))
            {
                throw new InvalidDataException(
                    $"Release gate path does not exist: {script}");
            }
        }
    }

    private static HashSet<string> ReadConformanceOperationIds(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(path));
        var root = document.RootElement;
        if (root.GetProperty("schema_version").GetInt32() != 1 ||
            root.GetProperty("generated_by").GetString() !=
                PortableConformanceGenerator.GeneratedArtifactIdentity)
        {
            throw new InvalidDataException(
                $"Portable conformance identity is invalid: {path}");
        }

        var operationIds = root.GetProperty("operations")
            .EnumerateArray()
            .Select(operation =>
                operation.GetProperty("operation_id").GetString() ??
                throw new InvalidDataException(
                    "Portable conformance has an empty operation identity."))
            .ToArray();
        RequireUnique(operationIds, "portable conformance operation_id");
        return operationIds.ToHashSet(StringComparer.Ordinal);
    }

    private static ReleaseEvidenceInput[] CreateEvidence(
        string repositoryRoot,
        IEnumerable<string> paths) =>
        paths
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => new ReleaseEvidenceInput(
                NormalizeRelative(repositoryRoot, path),
                Sha256(File.ReadAllBytes(path))))
            .OrderBy(item => item.Path, StringComparer.Ordinal)
            .ToArray();

    private static string CreateSupportMatrixDocument(ReleaseSupportMatrix matrix)
    {
        var builder = new StringBuilder();
        builder.AppendLine(
            CultureInfo.InvariantCulture,
            $"# SA {matrix.SpatialAnalyzerTarget} support matrix");
        builder.AppendLine();
        builder.AppendLine(
            "This generated matrix reconciles every exact-target inventory command using " +
            "Briosa-authored disposition and catalog metadata. It does not reproduce vendor " +
            "documentation. A cataloged operation is not automatically enabled at runtime, " +
            "and portable fake coverage is not licensed SpatialAnalyzer validation.");
        builder.AppendLine();
        builder.AppendLine("## Operator boundary");
        builder.AppendLine();
        builder.AppendLine(
            "Cataloged operations require a separately installed and licensed exact-target " +
            "SpatialAnalyzer, exact-match runtime identity evidence, a current execution-channel " +
            "readiness proof, and explicit runtime allowlisting. Capability discovery reports " +
            "only the catalog/policy intersection. Cancellation does not prove COM cancellation " +
            "or rollback, and ambiguous completion never authorizes automatic replay.");
        builder.AppendLine();
        builder.AppendLine("## Accounting");
        builder.AppendLine();
        builder.AppendLine("| Classification | Count |");
        builder.AppendLine("| --- | ---: |");
        builder.AppendLine(
            CultureInfo.InvariantCulture,
            $"| Inventory commands | {matrix.Counts.InventoryCommands} |");
        builder.AppendLine(
            CultureInfo.InvariantCulture,
            $"| Cataloged, portable-only operations | {matrix.Counts.CatalogedOperations} |");
        builder.AppendLine(
            CultureInfo.InvariantCulture,
            $"| Approved candidates not cataloged | {matrix.Counts.ApprovedNotCataloged} |");
        builder.AppendLine(
            CultureInfo.InvariantCulture,
            $"| Blocked | {matrix.Counts.Blocked} |");
        builder.AppendLine(
            CultureInfo.InvariantCulture,
            $"| Intentional exclusions | {matrix.Counts.IntentionalExclusions} |");
        builder.AppendLine(
            CultureInfo.InvariantCulture,
            $"| SDK unavailable | {matrix.Counts.SdkUnavailable} |");
        builder.AppendLine();
        builder.AppendLine("## Exact command matrix");
        builder.AppendLine();
        builder.AppendLine(
            "| Inventory key | MP step | Category | Classification | Operation | Risk | " +
            "Portable | Protected SA |");
        builder.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- |");
        foreach (var command in matrix.Commands)
        {
            builder.Append("| `")
                .Append(EscapeMarkdown(command.InventoryKey))
                .Append("` | ")
                .Append(EscapeMarkdown(command.MpStep))
                .Append(" | ")
                .Append(EscapeMarkdown(string.Join(" / ", command.CategoryPath)))
                .Append(" | `")
                .Append(command.ReleaseClassification)
                .Append("` | ")
                .Append(command.OperationId is null
                    ? "—"
                    : $"`{EscapeMarkdown(command.OperationId)}`")
                .Append(" | `")
                .Append(command.RiskEffect)
                .Append('`')
                .Append(command.RiskFlags.Count == 0
                    ? string.Empty
                    : $" ({EscapeMarkdown(string.Join(", ", command.RiskFlags))})")
                .Append(" | `")
                .Append(command.Validation.Portable)
                .Append("` | `")
                .Append(command.Validation.ProtectedSpatialAnalyzer)
                .AppendLine("` |");
        }

        return builder.ToString().Replace(
            "\r\n",
            "\n",
            StringComparison.Ordinal);
    }

    private static string CreateReleaseAuditDocument(ReleaseAudit audit)
    {
        var builder = new StringBuilder();
        builder.AppendLine(
            CultureInfo.InvariantCulture,
            $"# SA {audit.SpatialAnalyzerTarget} release audit");
        builder.AppendLine();
        builder.AppendLine(
            "This generated audit is fail-closed. Ordinary CI verifies that it is current; " +
            "the release workflow additionally refuses publication while `release_ready` is false.");
        builder.AppendLine();
        builder.AppendLine(
            CultureInfo.InvariantCulture,
            $"- Release ready: `{(audit.ReleaseReady ? "true" : "false")}`");
        builder.AppendLine(
            CultureInfo.InvariantCulture,
            $"- Passed criteria: {audit.Summary.Passed}");
        builder.AppendLine(
            CultureInfo.InvariantCulture,
            $"- Blocked criteria: {audit.Summary.Blocked}");
        builder.AppendLine(
            CultureInfo.InvariantCulture,
            $"- Not applicable: {audit.Summary.NotApplicable}");
        builder.AppendLine();
        builder.AppendLine("| Criterion | Status | Evidence | Blockers |");
        builder.AppendLine("| --- | --- | --- | --- |");
        foreach (var criterion in audit.Criteria)
        {
            builder.Append("| `")
                .Append(criterion.CriterionId)
                .Append("` — ")
                .Append(EscapeMarkdown(criterion.Description))
                .Append(" | `")
                .Append(criterion.Status)
                .Append("` | ")
                .Append(criterion.Evidence.Count == 0
                    ? "—"
                    : string.Join("<br>", criterion.Evidence.Select(
                        value => $"`{EscapeMarkdown(value)}`")))
                .Append(" | ")
                .Append(criterion.BlockerReferences.Count == 0
                    ? "—"
                    : string.Join("<br>", criterion.BlockerReferences.Select(
                        value => $"[{EscapeMarkdown(value)}]({value})")))
                .AppendLine(" |");
        }

        builder.AppendLine();
        builder.AppendLine(
            "A passing repository-owned portable gate does not substitute for protected, " +
            "licensed-SA evidence. The pending protected-runner and runtime-identity criteria " +
            "must be resolved through their owning issues before release.");
        return builder.ToString().Replace(
            "\r\n",
            "\n",
            StringComparison.Ordinal);
    }

    private static T Deserialize<T>(string path)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(
                    File.ReadAllBytes(path),
                    InputJsonOptions) ??
                throw new InvalidDataException($"JSON document is empty: {path}");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                $"JSON document is invalid: {path}: {exception.Message}",
                exception);
        }
    }

    private static string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, OutputJsonOptions).Replace(
            "\r\n",
            "\n",
            StringComparison.Ordinal) + "\n";

    private static void WriteGeneratedFile(
        string outputRoot,
        string relativePath,
        string content)
    {
        var path = ResolveContained(outputRoot, relativePath, "generated output");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(
            path,
            content.Replace("\r\n", "\n", StringComparison.Ordinal),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string ResolveContained(
        string root,
        string relativePath,
        string description)
    {
        var fullRoot = Path.GetFullPath(root);
        var fullPath = Path.GetFullPath(
            relativePath.Replace('/', Path.DirectorySeparatorChar),
            fullRoot);
        if (!fullPath.StartsWith(
                fullRoot + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"{description} path escapes its root: {relativePath}");
        }

        return fullPath;
    }

    private static string NormalizeRelative(string root, string path) =>
        Path.GetRelativePath(root, path).Replace('\\', '/');

    private static string Sha256(byte[] content) =>
        Convert.ToHexStringLower(SHA256.HashData(content));

    private static string EscapeMarkdown(string value) =>
        value.Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("`", "\\`", StringComparison.Ordinal);

    private static void RequireUnique(
        IEnumerable<string> values,
        string description)
    {
        var items = values.ToArray();
        if (items.Length != items.Distinct(StringComparer.Ordinal).Count())
        {
            throw new InvalidDataException($"{description} contains duplicate identities.");
        }
    }

    private static void RequireOrdinalUnique(
        IReadOnlyList<string> values,
        string description)
    {
        RequireUnique(values, description);
        if (!values.SequenceEqual(values.OrderBy(value => value, StringComparer.Ordinal)))
        {
            throw new InvalidDataException(
                $"{description} must use ordinal sort order.");
        }
    }
}

internal sealed class ReleaseAuditPolicy
{
    [JsonPropertyName("$schema")]
    [JsonRequired]
    public required string Schema { get; init; }

    [JsonRequired]
    public required int SchemaVersion { get; init; }

    [JsonRequired]
    public required string SpatialAnalyzerTarget { get; init; }

    [JsonRequired]
    public required string MasterEpic { get; init; }

    [JsonRequired]
    public required ReleasePendingEvidence ProtectedConformance { get; init; }

    [JsonRequired]
    public required ReleasePendingEvidence RuntimeIdentityValidation { get; init; }

    [JsonRequired]
    public required List<string> OperatorGuidePaths { get; init; }
}

internal sealed class ReleasePendingEvidence
{
    [JsonRequired]
    public required string Status { get; init; }

    [JsonRequired]
    public required List<string> BlockerReferences { get; init; }
}

internal sealed record ReleaseEvidenceInput(
    string Path,
    string Sha256);

internal sealed record ReleaseSupportMatrix(
    [property: JsonPropertyName("$schema")] string Schema,
    int SchemaVersion,
    string GeneratedBy,
    string SpatialAnalyzerTarget,
    IReadOnlyList<ReleaseEvidenceInput> EvidenceInputs,
    ReleaseSupportCounts Counts,
    IReadOnlyList<ReleaseSupportCommand> Commands);

internal sealed record ReleaseSupportCounts(
    int InventoryCommands,
    int CatalogedOperations,
    int ApprovedCandidates,
    int ApprovedNotCataloged,
    int Blocked,
    int IntentionalExclusions,
    int SdkUnavailable,
    int PortableValidatedCatalogedOperations,
    int ProtectedValidatedCatalogedOperations);

internal sealed record ReleaseSupportCommand(
    string InventoryKey,
    string MpStep,
    IReadOnlyList<string> CategoryPath,
    string Disposition,
    string ReviewState,
    string ReleaseClassification,
    string? OperationId,
    string RiskEffect,
    IReadOnlyList<string> RiskFlags,
    string? ReplaySafety,
    string? ExecutionScope,
    IReadOnlyList<string> Prerequisites,
    ReleaseValidationTier Validation,
    IReadOnlyList<string> BlockerReferences);

internal sealed record ReleaseValidationTier(
    string Metadata,
    string Portable,
    string ProtectedSpatialAnalyzer);

internal sealed record ReleaseAudit(
    [property: JsonPropertyName("$schema")] string Schema,
    int SchemaVersion,
    string GeneratedBy,
    string SpatialAnalyzerTarget,
    string MasterEpic,
    bool ReleaseReady,
    IReadOnlyList<ReleaseEvidenceInput> EvidenceInputs,
    ReleaseAuditSummary Summary,
    IReadOnlyList<ReleaseAuditCriterion> Criteria);

internal sealed record ReleaseAuditSummary(
    int Passed,
    int Blocked,
    int NotApplicable);

internal sealed record ReleaseAuditCriterion(
    string CriterionId,
    string Source,
    string Description,
    string Status,
    IReadOnlyList<string> Evidence,
    IReadOnlyList<string> BlockerReferences);
