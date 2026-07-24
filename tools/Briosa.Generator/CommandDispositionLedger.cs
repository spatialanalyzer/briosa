using System.Security.Cryptography;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Briosa.Generator;

internal static partial class CommandDispositionLedger
{
    private const string ManifestSchemaReference = "../../schemas/v1/manifest.schema.json";
    private const string ShardSchemaReference = "../../../schemas/v1/shard.schema.json";
    private const string InitialBlocker =
        "https://github.com/spatialanalyzer/briosa/issues/43";

    private static readonly string[] Dispositions =
        ["approved_candidate", "blocked", "intentional_exclusion", "sdk_unavailable"];

    private static readonly string[] ReviewStates =
        ["needs_re_review", "reviewed", "unreviewed"];

    private static readonly string[] DeliveryWaves =
        ["final", "wave_1", "wave_2", "wave_3", "wave_4"];

    private static readonly string[] RiskEffects =
        ["mutating", "read_only", "unknown"];

    private static readonly string[] RiskFlags =
    [
        "credential_or_license_data",
        "device_control",
        "external_process",
        "filesystem_metadata",
        "filesystem_read",
        "filesystem_write",
        "interactive_ui",
        "long_running",
        "network_access",
        "unknown"
    ];

    private static readonly string[] DataClassifications =
    [
        "credential",
        "geometry",
        "license_data",
        "measurement",
        "non_sensitive",
        "object_identifier",
        "path",
        "proprietary",
        "unknown"
    ];

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    private static readonly JsonSerializerOptions WriteOptions = new(ReadOptions)
    {
        WriteIndented = true
    };

    private static readonly JsonSerializerOptions CompactOptions = new(ReadOptions);

    public static CommandDispositionSyncResult Sync(
        string inventoryPath,
        string targetDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inventoryPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetDirectory);

        var fullInventoryPath = Path.GetFullPath(inventoryPath);
        var fullTargetDirectory = Path.GetFullPath(targetDirectory);
        var inventory = ReadRequired<MpCommandInventory>(fullInventoryPath);
        var target = Path.GetFileName(
            fullTargetDirectory.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar));
        if (!string.Equals(inventory.SpatialAnalyzerTarget, target, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Disposition target directory '{target}' does not match inventory target " +
                $"'{inventory.SpatialAnalyzerTarget}'.");
        }

        var existingEntries = ReadExistingEntries(fullTargetDirectory);
        var newEntryCount = 0;
        var reReviewCount = 0;
        var entries = new List<CommandDispositionEntry>(inventory.Commands.Count);
        foreach (var command in inventory.Commands.OrderBy(
                     command => command.InventoryKey,
                     StringComparer.Ordinal))
        {
            var fingerprint = InventoryEntryFingerprint(command);
            var evidenceReferences = EvidenceReferences(command);
            if (!existingEntries.TryGetValue(command.InventoryKey, out var existing))
            {
                newEntryCount++;
                entries.Add(CreateUnreviewedEntry(command, fingerprint, evidenceReferences));
                continue;
            }

            var evidenceChanged = !string.Equals(
                existing.InventoryEntrySha256,
                fingerprint,
                StringComparison.Ordinal);
            if (evidenceChanged &&
                !string.Equals(existing.ReviewState, "unreviewed", StringComparison.Ordinal))
            {
                reReviewCount++;
            }

            entries.Add(UpdateEntry(
                existing,
                command,
                fingerprint,
                evidenceReferences,
                evidenceChanged));
        }

        var categoriesDirectory = Path.Combine(fullTargetDirectory, "categories");
        Directory.CreateDirectory(categoriesDirectory);
        var categories = entries
            .Select(entry => Category(entry.CategoryPath))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(category => category, StringComparer.Ordinal)
            .ToArray();
        var categoryPaths = CreateCategoryPaths(categories);
        var shardReferences = new List<CommandDispositionShardReference>(categories.Length);
        var writtenFiles = new List<string>(categories.Length + 2);
        foreach (var category in categories)
        {
            var shard = new CommandDispositionShard
            {
                Schema = ShardSchemaReference,
                SchemaVersion = 1,
                SpatialAnalyzerTarget = inventory.SpatialAnalyzerTarget,
                Category = category,
                Entries = entries
                    .Where(entry => string.Equals(
                        Category(entry.CategoryPath),
                        category,
                        StringComparison.Ordinal))
                    .OrderBy(entry => entry.InventoryKey, StringComparer.Ordinal)
                    .ToList()
            };
            var relativePath = categoryPaths[category];
            var shardPath = Path.Combine(
                fullTargetDirectory,
                relativePath.Replace('/', Path.DirectorySeparatorChar));
            var shardText = Serialize(shard);
            WriteText(shardPath, shardText);
            writtenFiles.Add(shardPath);
            shardReferences.Add(new CommandDispositionShardReference
            {
                Category = category,
                Path = relativePath,
                EntryCount = shard.Entries.Count,
                Sha256 = Sha256(Encoding.UTF8.GetBytes(shardText))
            });
        }

        DeleteStaleShards(fullTargetDirectory, categoryPaths.Values);

        var manifest = new CommandDispositionManifest
        {
            Schema = ManifestSchemaReference,
            SchemaVersion = 1,
            SpatialAnalyzerTarget = inventory.SpatialAnalyzerTarget,
            Inventory = new CommandDispositionInventoryReference
            {
                Path = NormalizePath(Path.GetRelativePath(
                    fullTargetDirectory,
                    fullInventoryPath)),
                Sha256 = Sha256(File.ReadAllBytes(fullInventoryPath)),
                CommandCount = inventory.Commands.Count
            },
            Shards = shardReferences
                .OrderBy(shard => shard.Path, StringComparer.Ordinal)
                .ToList()
        };
        var manifestPath = Path.Combine(fullTargetDirectory, "manifest.json");
        WriteText(manifestPath, Serialize(manifest));
        writtenFiles.Add(manifestPath);

        var reportPath = Path.Combine(fullTargetDirectory, "report.md");
        WriteText(reportPath, CreateReport(manifest, entries));
        writtenFiles.Add(reportPath);

        var validation = Validate(fullInventoryPath, fullTargetDirectory);
        if (!validation.IsValid)
        {
            throw new InvalidDataException(
                "Synchronized disposition ledger is invalid:" +
                Environment.NewLine +
                string.Join(Environment.NewLine, validation.Errors));
        }

        return new CommandDispositionSyncResult(
            writtenFiles
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray(),
            entries.Count,
            newEntryCount,
            reReviewCount);
    }

    public static CommandDispositionValidationResult Validate(
        string inventoryPath,
        string targetDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inventoryPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetDirectory);

        var fullInventoryPath = Path.GetFullPath(inventoryPath);
        var fullTargetDirectory = Path.GetFullPath(targetDirectory);
        var errors = new List<string>();
        var inventory = Deserialize<MpCommandInventory>(
            fullInventoryPath,
            "inventory",
            errors);
        if (inventory is null)
        {
            return new CommandDispositionValidationResult(errors, 0, 0);
        }

        var manifestPath = Path.Combine(fullTargetDirectory, "manifest.json");
        var manifest = Deserialize<CommandDispositionManifest>(
            manifestPath,
            "manifest.json",
            errors);
        if (manifest is null)
        {
            return new CommandDispositionValidationResult(errors, 0, 0);
        }

        ValidateManifestIdentity(
            manifest,
            fullInventoryPath,
            fullTargetDirectory,
            inventory,
            errors);

        RequireSorted(
            manifest.Shards.Select(shard => shard.Path),
            "manifest.json",
            "shards by path",
            errors);
        RequireUnique(
            manifest.Shards.Select(shard => shard.Category),
            "manifest.json",
            "shard categories",
            errors);
        RequireUnique(
            manifest.Shards.Select(shard => shard.Path),
            "manifest.json",
            "shard paths",
            errors);

        var actualShardPaths = Directory.Exists(Path.Combine(fullTargetDirectory, "categories"))
            ? Directory
                .EnumerateFiles(
                    Path.Combine(fullTargetDirectory, "categories"),
                    "*.json",
                    SearchOption.AllDirectories)
                .Select(path => NormalizePath(Path.GetRelativePath(fullTargetDirectory, path)))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray()
            : [];
        var listedShardPaths = manifest.Shards
            .Select(shard => shard.Path)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        if (!actualShardPaths.SequenceEqual(listedShardPaths, StringComparer.Ordinal))
        {
            errors.Add(
                "manifest.json: shards must list every categories/*.json file exactly once.");
        }

        var inventoryByKey = inventory.Commands.ToDictionary(
            command => command.InventoryKey,
            StringComparer.Ordinal);
        var entriesByKey = new Dictionary<string, CommandDispositionEntry>(StringComparer.Ordinal);
        foreach (var shardReference in manifest.Shards)
        {
            ValidateShard(
                shardReference,
                manifest,
                fullTargetDirectory,
                inventoryByKey,
                entriesByKey,
                errors);
        }

        foreach (var inventoryKey in inventoryByKey.Keys
                     .Except(entriesByKey.Keys, StringComparer.Ordinal)
                     .OrderBy(key => key, StringComparer.Ordinal))
        {
            errors.Add($"Disposition ledger is missing inventory key '{inventoryKey}'.");
        }

        foreach (var inventoryKey in entriesByKey.Keys
                     .Except(inventoryByKey.Keys, StringComparer.Ordinal)
                     .OrderBy(key => key, StringComparer.Ordinal))
        {
            errors.Add($"Disposition ledger contains unknown inventory key '{inventoryKey}'.");
        }

        var reportPath = Path.Combine(fullTargetDirectory, "report.md");
        if (!File.Exists(reportPath))
        {
            errors.Add("Disposition ledger must contain report.md.");
        }
        else
        {
            var expectedReport = CreateReport(manifest, entriesByKey.Values);
            var actualReport = File.ReadAllText(reportPath);
            if (!string.Equals(actualReport, expectedReport, StringComparison.Ordinal))
            {
                errors.Add("report.md is stale; run disposition-sync.");
            }
        }

        return new CommandDispositionValidationResult(
            errors,
            1,
            entriesByKey.Count);
    }

    private static void ValidateManifestIdentity(
        CommandDispositionManifest manifest,
        string inventoryPath,
        string targetDirectory,
        MpCommandInventory inventory,
        List<string> errors)
    {
        RequireEqual(
            manifest.Schema,
            ManifestSchemaReference,
            "manifest.json",
            "$schema",
            errors);
        if (manifest.SchemaVersion != 1)
        {
            errors.Add("manifest.json: schema_version must be 1.");
        }

        var target = Path.GetFileName(
            targetDirectory.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar));
        RequireEqual(
            manifest.SpatialAnalyzerTarget,
            target,
            "manifest.json",
            "spatial_analyzer_target",
            errors);
        RequireEqual(
            manifest.SpatialAnalyzerTarget,
            inventory.SpatialAnalyzerTarget,
            "manifest.json",
            "inventory target",
            errors);
        RequireEqual(
            manifest.Inventory.Path,
            NormalizePath(Path.GetRelativePath(targetDirectory, inventoryPath)),
            "manifest.json",
            "inventory.path",
            errors);
        RequireEqual(
            manifest.Inventory.Sha256,
            Sha256(File.ReadAllBytes(inventoryPath)),
            "manifest.json",
            "inventory.sha256",
            errors);
        if (manifest.Inventory.CommandCount != inventory.Commands.Count)
        {
            errors.Add(
                $"manifest.json: inventory.command_count must be {inventory.Commands.Count}, " +
                $"not {manifest.Inventory.CommandCount}.");
        }
    }

    private static void ValidateShard(
        CommandDispositionShardReference shardReference,
        CommandDispositionManifest manifest,
        string targetDirectory,
        IReadOnlyDictionary<string, MpCommandInventoryCommand> inventoryByKey,
        IDictionary<string, CommandDispositionEntry> entriesByKey,
        List<string> errors)
    {
        var shardPath = Path.GetFullPath(
            Path.Combine(
                targetDirectory,
                shardReference.Path.Replace('/', Path.DirectorySeparatorChar)));
        if (!IsWithin(shardPath, targetDirectory))
        {
            errors.Add(
                $"manifest.json: shard path '{shardReference.Path}' escapes its target directory.");
            return;
        }

        if (!File.Exists(shardPath))
        {
            errors.Add($"manifest.json: shard '{shardReference.Path}' does not exist.");
            return;
        }

        RequireEqual(
            shardReference.Sha256,
            Sha256(File.ReadAllBytes(shardPath)),
            "manifest.json",
            $"sha256 for {shardReference.Path}",
            errors);
        var shard = Deserialize<CommandDispositionShard>(
            shardPath,
            shardReference.Path,
            errors);
        if (shard is null)
        {
            return;
        }

        RequireEqual(
            shard.Schema,
            ShardSchemaReference,
            shardReference.Path,
            "$schema",
            errors);
        if (shard.SchemaVersion != 1)
        {
            errors.Add($"{shardReference.Path}: schema_version must be 1.");
        }

        RequireEqual(
            shard.SpatialAnalyzerTarget,
            manifest.SpatialAnalyzerTarget,
            shardReference.Path,
            "spatial_analyzer_target",
            errors);
        RequireEqual(
            shard.Category,
            shardReference.Category,
            shardReference.Path,
            "category",
            errors);
        if (shard.Entries.Count != shardReference.EntryCount)
        {
            errors.Add(
                $"manifest.json: entry_count for {shardReference.Path} must be " +
                $"{shard.Entries.Count}, not {shardReference.EntryCount}.");
        }

        RequireSorted(
            shard.Entries.Select(entry => entry.InventoryKey),
            shardReference.Path,
            "entries by inventory_key",
            errors);
        foreach (var entry in shard.Entries)
        {
            if (!entriesByKey.TryAdd(entry.InventoryKey, entry))
            {
                errors.Add($"Duplicate disposition inventory key '{entry.InventoryKey}'.");
                continue;
            }

            RequireEqual(
                Category(entry.CategoryPath),
                shard.Category,
                shardReference.Path,
                $"category for '{entry.InventoryKey}'",
                errors);
            ValidateEntry(
                entry,
                shardReference.Path,
                inventoryByKey,
                errors);
        }
    }

    private static void ValidateEntry(
        CommandDispositionEntry entry,
        string shardPath,
        IReadOnlyDictionary<string, MpCommandInventoryCommand> inventoryByKey,
        List<string> errors)
    {
        var displayPath = $"{shardPath}: '{entry.InventoryKey}'";
        if (!inventoryByKey.TryGetValue(entry.InventoryKey, out var command))
        {
            return;
        }

        RequireEqual(entry.MpStep, command.MpStep, displayPath, "mp_step", errors);
        if (!entry.CategoryPath.SequenceEqual(command.CategoryPath, StringComparer.Ordinal))
        {
            errors.Add($"{displayPath}: category_path does not match inventory evidence.");
        }

        RequireEqual(
            entry.InventoryEntrySha256,
            InventoryEntryFingerprint(command),
            displayPath,
            "inventory_entry_sha256",
            errors);
        RequireExactList(
            entry.EvidenceReferences,
            EvidenceReferences(command),
            displayPath,
            "evidence_references",
            errors);
        RequireSortedUnique(entry.ReasonCodes, displayPath, "reason_codes", errors);
        RequireSortedUnique(
            entry.EvidenceReferences,
            displayPath,
            "evidence_references",
            errors);
        RequireSortedUnique(
            entry.DecisionReferences,
            displayPath,
            "decision_references",
            errors);
        RequireSortedUnique(
            entry.BlockerReferences,
            displayPath,
            "blocker_references",
            errors);
        RequireSortedUnique(entry.RiskFlags, displayPath, "risk_flags", errors);
        RequireSortedUnique(
            entry.DataClassifications,
            displayPath,
            "data_classifications",
            errors);
        RequireSortedUnique(entry.ValueFamilies, displayPath, "value_families", errors);

        if (!Dispositions.Contains(entry.Disposition, StringComparer.Ordinal))
        {
            errors.Add($"{displayPath}: unknown disposition '{entry.Disposition}'.");
        }

        if (!ReviewStates.Contains(entry.ReviewState, StringComparer.Ordinal))
        {
            errors.Add($"{displayPath}: unknown review_state '{entry.ReviewState}'.");
        }

        if (string.IsNullOrWhiteSpace(entry.Rationale))
        {
            errors.Add($"{displayPath}: rationale must not be empty.");
        }

        if (entry.ReasonCodes.Count == 0)
        {
            errors.Add($"{displayPath}: reason_codes require at least one value.");
        }

        foreach (var reasonCode in entry.ReasonCodes.Where(
                     reasonCode => !ReasonCode().IsMatch(reasonCode)))
        {
            errors.Add($"{displayPath}: invalid reason code '{reasonCode}'.");
        }

        foreach (var reference in entry.DecisionReferences)
        {
            ValidateGitHubReference(reference, displayPath, "decision reference", errors);
        }

        foreach (var reference in entry.BlockerReferences)
        {
            ValidateGitHubReference(reference, displayPath, "blocker reference", errors);
        }

        if (entry.DeliveryWave is not null &&
            !DeliveryWaves.Contains(entry.DeliveryWave, StringComparer.Ordinal))
        {
            errors.Add($"{displayPath}: unknown delivery_wave '{entry.DeliveryWave}'.");
        }

        if (!RiskEffects.Contains(entry.RiskEffect, StringComparer.Ordinal))
        {
            errors.Add($"{displayPath}: unknown risk_effect '{entry.RiskEffect}'.");
        }

        foreach (var riskFlag in entry.RiskFlags.Where(
                     riskFlag => !RiskFlags.Contains(riskFlag, StringComparer.Ordinal)))
        {
            errors.Add($"{displayPath}: unknown risk flag '{riskFlag}'.");
        }

        if (entry.RiskFlags.Count > 1 &&
            entry.RiskFlags.Contains("unknown", StringComparer.Ordinal))
        {
            errors.Add(
                $"{displayPath}: risk_flags cannot combine unknown with assessed flags.");
        }

        foreach (var classification in entry.DataClassifications.Where(
                     classification => !DataClassifications.Contains(
                         classification,
                         StringComparer.Ordinal)))
        {
            errors.Add(
                $"{displayPath}: unknown data classification '{classification}'.");
        }

        if (entry.DataClassifications.Count == 0 ||
            (entry.DataClassifications.Count > 1 &&
             entry.DataClassifications.Contains("unknown", StringComparer.Ordinal)))
        {
            errors.Add(
                $"{displayPath}: data_classifications require at least one value and cannot " +
                "combine unknown with assessed classifications.");
        }

        foreach (var valueFamily in entry.ValueFamilies.Where(
                     valueFamily => !ReasonCode().IsMatch(valueFamily)))
        {
            errors.Add($"{displayPath}: invalid value family '{valueFamily}'.");
        }

        if (entry.ValueFamilies.Count == 0 ||
            (entry.ValueFamilies.Count > 1 &&
             entry.ValueFamilies.Contains("unknown", StringComparer.Ordinal)))
        {
            errors.Add(
                $"{displayPath}: value_families require at least one value and cannot combine " +
                "unknown with assessed families.");
        }

        ValidateReviewState(entry, displayPath, errors);
    }

    private static void ValidateReviewState(
        CommandDispositionEntry entry,
        string displayPath,
        List<string> errors)
    {
        if (string.Equals(entry.ReviewState, "unreviewed", StringComparison.Ordinal))
        {
            if (!string.Equals(entry.Disposition, "blocked", StringComparison.Ordinal) ||
                entry.DeliveryWave is not null ||
                entry.DecisionReferences.Count != 0 ||
                entry.BlockerReferences.Count == 0 ||
                !entry.ReasonCodes.Contains("awaiting_review", StringComparer.Ordinal))
            {
                errors.Add(
                    $"{displayPath}: unreviewed entries must be blocked with awaiting_review, " +
                    "at least one blocker, no decision references, and no delivery wave.");
            }

            return;
        }

        if (string.Equals(entry.ReviewState, "needs_re_review", StringComparison.Ordinal))
        {
            if (entry.BlockerReferences.Count == 0 ||
                !entry.ReasonCodes.Contains("evidence_changed", StringComparer.Ordinal))
            {
                errors.Add(
                    $"{displayPath}: needs_re_review entries require evidence_changed and " +
                    "at least one blocker.");
            }

            return;
        }

        if (!string.Equals(entry.ReviewState, "reviewed", StringComparison.Ordinal))
        {
            return;
        }

        if (entry.DecisionReferences.Count == 0)
        {
            errors.Add($"{displayPath}: reviewed entries require a decision reference.");
        }

        if (entry.ReasonCodes.Contains("awaiting_review", StringComparer.Ordinal) ||
            entry.ReasonCodes.Contains("evidence_changed", StringComparer.Ordinal))
        {
            errors.Add(
                $"{displayPath}: reviewed entries cannot retain unresolved review reason codes.");
        }

        switch (entry.Disposition)
        {
            case "approved_candidate":
                if (entry.DeliveryWave is null || entry.BlockerReferences.Count != 0)
                {
                    errors.Add(
                        $"{displayPath}: approved candidates require a delivery wave and no blockers.");
                }

                if (string.Equals(entry.RiskEffect, "unknown", StringComparison.Ordinal) ||
                    entry.RiskFlags.Contains("unknown", StringComparer.Ordinal) ||
                    entry.DataClassifications.Contains("unknown", StringComparer.Ordinal) ||
                    entry.ValueFamilies.Contains("unknown", StringComparer.Ordinal))
                {
                    errors.Add(
                        $"{displayPath}: approved candidates require assessed risk, data " +
                        "classification, and value family metadata.");
                }

                break;
            case "blocked":
                if (entry.DeliveryWave is not null || entry.BlockerReferences.Count == 0)
                {
                    errors.Add(
                        $"{displayPath}: reviewed blocked entries require blockers and no delivery wave.");
                }

                break;
            case "intentional_exclusion":
            case "sdk_unavailable":
                if (entry.DeliveryWave is not null || entry.BlockerReferences.Count != 0)
                {
                    errors.Add(
                        $"{displayPath}: final non-supported dispositions require no delivery " +
                        "wave or blocker.");
                }

                break;
        }
    }

    private static CommandDispositionEntry CreateUnreviewedEntry(
        MpCommandInventoryCommand command,
        string fingerprint,
        List<string> evidenceReferences) =>
        new()
        {
            InventoryKey = command.InventoryKey,
            MpStep = command.MpStep,
            CategoryPath = command.CategoryPath,
            InventoryEntrySha256 = fingerprint,
            Disposition = "blocked",
            ReviewState = "unreviewed",
            Rationale = "Awaiting exact-target command disposition review.",
            ReasonCodes = ["awaiting_review"],
            EvidenceReferences = evidenceReferences,
            DecisionReferences = [],
            BlockerReferences = [InitialBlocker],
            RiskEffect = "unknown",
            RiskFlags = ["unknown"],
            DataClassifications = ["unknown"],
            ValueFamilies = ["unknown"],
            DeliveryWave = null
        };

    private static CommandDispositionEntry UpdateEntry(
        CommandDispositionEntry existing,
        MpCommandInventoryCommand command,
        string fingerprint,
        List<string> evidenceReferences,
        bool evidenceChanged)
    {
        if (!evidenceChanged ||
            string.Equals(existing.ReviewState, "unreviewed", StringComparison.Ordinal))
        {
            return new CommandDispositionEntry
            {
                InventoryKey = command.InventoryKey,
                MpStep = command.MpStep,
                CategoryPath = command.CategoryPath,
                InventoryEntrySha256 = fingerprint,
                Disposition = existing.Disposition,
                ReviewState = existing.ReviewState,
                Rationale = existing.Rationale,
                ReasonCodes = SortedDistinct(existing.ReasonCodes),
                EvidenceReferences = evidenceReferences,
                DecisionReferences = SortedDistinct(existing.DecisionReferences),
                BlockerReferences = SortedDistinct(existing.BlockerReferences),
                RiskEffect = existing.RiskEffect,
                RiskFlags = SortedDistinct(existing.RiskFlags),
                DataClassifications = SortedDistinct(existing.DataClassifications),
                ValueFamilies = SortedDistinct(existing.ValueFamilies),
                DeliveryWave = existing.DeliveryWave
            };
        }

        return new CommandDispositionEntry
        {
            InventoryKey = command.InventoryKey,
            MpStep = command.MpStep,
            CategoryPath = command.CategoryPath,
            InventoryEntrySha256 = fingerprint,
            Disposition = existing.Disposition,
            ReviewState = "needs_re_review",
            Rationale = existing.Rationale,
            ReasonCodes = SortedDistinct([.. existing.ReasonCodes, "evidence_changed"]),
            EvidenceReferences = evidenceReferences,
            DecisionReferences = SortedDistinct(existing.DecisionReferences),
            BlockerReferences = SortedDistinct(
                [.. existing.BlockerReferences, InitialBlocker]),
            RiskEffect = existing.RiskEffect,
            RiskFlags = SortedDistinct(existing.RiskFlags),
            DataClassifications = SortedDistinct(existing.DataClassifications),
            ValueFamilies = SortedDistinct(existing.ValueFamilies),
            DeliveryWave = existing.DeliveryWave
        };
    }

    private static Dictionary<string, CommandDispositionEntry> ReadExistingEntries(
        string targetDirectory)
    {
        var result = new Dictionary<string, CommandDispositionEntry>(StringComparer.Ordinal);
        var categoriesDirectory = Path.Combine(targetDirectory, "categories");
        if (!Directory.Exists(categoriesDirectory))
        {
            return result;
        }

        foreach (var shardPath in Directory
                     .EnumerateFiles(categoriesDirectory, "*.json", SearchOption.AllDirectories)
                     .OrderBy(path => path, StringComparer.Ordinal))
        {
            var shard = ReadRequired<CommandDispositionShard>(shardPath);
            foreach (var entry in shard.Entries)
            {
                if (!result.TryAdd(entry.InventoryKey, entry))
                {
                    throw new InvalidDataException(
                        $"Existing disposition ledger contains duplicate key '{entry.InventoryKey}'.");
                }
            }
        }

        return result;
    }

    private static Dictionary<string, string> CreateCategoryPaths(
        IEnumerable<string> categories)
    {
        var categoryArray = categories.OrderBy(category => category, StringComparer.Ordinal).ToArray();
        var slugGroups = categoryArray
            .GroupBy(CategorySlug, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.ToArray(),
                StringComparer.Ordinal);
        return categoryArray.ToDictionary(
            category => category,
            category =>
            {
                var slug = CategorySlug(category);
                var suffix = slugGroups[slug].Length == 1
                    ? string.Empty
                    : $"_{Sha256(Encoding.UTF8.GetBytes(category))[..8]}";
                return $"categories/{slug}{suffix}.json";
            },
            StringComparer.Ordinal);
    }

    [SuppressMessage(
        "Globalization",
        "CA1308:Normalize strings to uppercase",
        Justification = "Disposition shard paths are specified as lowercase identifiers.")]
    private static string CategorySlug(string category)
    {
        var withWordBoundaries = WordBoundary().Replace(category, "$1_$2");
        var normalized = NonIdentifierCharacter().Replace(
            withWordBoundaries,
            "_");
        normalized = RepeatedUnderscore().Replace(normalized, "_");
        return normalized.Trim('_').ToLowerInvariant();
    }

    private static string Category(List<string> categoryPath) =>
        categoryPath.Count == 0 ? "_uncategorized" : categoryPath[0];

    private static string InventoryEntryFingerprint(MpCommandInventoryCommand command) =>
        Sha256(JsonSerializer.SerializeToUtf8Bytes(command, CompactOptions));

    private static List<string> EvidenceReferences(MpCommandInventoryCommand command)
    {
        var references = new List<string>();
        if (command.Documentation is not null)
        {
            references.Add($"documentation:{command.Documentation.Reference}");
        }

        references.AddRange(command.SdkEvidence.Select(
            evidence => $"sdk:{evidence.Reference}#{evidence.Occurrence}"));
        return SortedDistinct(references);
    }

    private static void DeleteStaleShards(
        string targetDirectory,
        IEnumerable<string> expectedRelativePaths)
    {
        var expected = expectedRelativePaths.ToHashSet(StringComparer.Ordinal);
        var categoriesDirectory = Path.Combine(targetDirectory, "categories");
        foreach (var path in Directory.EnumerateFiles(
                     categoriesDirectory,
                     "*.json",
                     SearchOption.AllDirectories))
        {
            var relativePath = NormalizePath(Path.GetRelativePath(targetDirectory, path));
            if (!expected.Contains(relativePath))
            {
                File.Delete(path);
            }
        }
    }

    private static string CreateReport(
        CommandDispositionManifest manifest,
        IEnumerable<CommandDispositionEntry> sourceEntries)
    {
        var entries = sourceEntries
            .OrderBy(entry => entry.InventoryKey, StringComparer.Ordinal)
            .ToArray();
        var unresolvedEntries = entries.Where(IsUnresolved).ToArray();
        var builder = new StringBuilder();
        builder.AppendLine(
            CultureInfo.InvariantCulture,
            $"# SA {manifest.SpatialAnalyzerTarget} command disposition report");
        builder.AppendLine();
        builder.AppendLine(
            "This deterministic report summarizes Briosa-authored disposition metadata. " +
            "It does not republish installed vendor documentation or generated SDK source.");
        builder.AppendLine();
        builder.AppendLine("## Inventory");
        builder.AppendLine();
        builder.AppendLine(CultureInfo.InvariantCulture, $"- Path: `{manifest.Inventory.Path}`");
        builder.AppendLine(CultureInfo.InvariantCulture, $"- SHA-256: `{manifest.Inventory.Sha256}`");
        builder.AppendLine(CultureInfo.InvariantCulture, $"- Commands: {manifest.Inventory.CommandCount}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"- Disposition shards: {manifest.Shards.Count}");
        builder.AppendLine();
        builder.AppendLine("## Dispositions");
        builder.AppendLine();
        builder.AppendLine("| Disposition | Count |");
        builder.AppendLine("| --- | ---: |");
        foreach (var disposition in Dispositions)
        {
            builder.AppendLine(
                CultureInfo.InvariantCulture,
                $"| `{disposition}` | {entries.Count(entry => string.Equals(
                    entry.Disposition,
                    disposition,
                    StringComparison.Ordinal))} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Review states");
        builder.AppendLine();
        builder.AppendLine("| Review state | Count |");
        builder.AppendLine("| --- | ---: |");
        foreach (var reviewState in ReviewStates)
        {
            builder.AppendLine(
                CultureInfo.InvariantCulture,
                $"| `{reviewState}` | {entries.Count(entry => string.Equals(
                    entry.ReviewState,
                    reviewState,
                    StringComparison.Ordinal))} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Categories");
        builder.AppendLine();
        builder.AppendLine(
            "| Category | Entries | Approved | Excluded | SDK unavailable | Blocked | " +
            "Unresolved | Unreviewed | Needs re-review |");
        builder.AppendLine(
            "| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |");
        foreach (var group in entries
                     .GroupBy(entry => Category(entry.CategoryPath), StringComparer.Ordinal)
                     .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            builder.AppendLine(
                CultureInfo.InvariantCulture,
                $"| {EscapeMarkdown(group.Key)} | {group.Count()} | " +
                $"{Count(group, "approved_candidate", null)} | " +
                $"{Count(group, "intentional_exclusion", null)} | " +
                $"{Count(group, "sdk_unavailable", null)} | " +
                $"{Count(group, "blocked", null)} | " +
                $"{group.Count(IsUnresolved)} | " +
                $"{Count(group, null, "unreviewed")} | " +
                $"{Count(group, null, "needs_re_review")} |");
        }

        AppendCountSection(
            builder,
            "Unresolved work by risk effect",
            unresolvedEntries.Select(entry => entry.RiskEffect));
        AppendCountSection(
            builder,
            "Unresolved work by risk flag",
            unresolvedEntries.SelectMany(entry => entry.RiskFlags));
        AppendCountSection(
            builder,
            "Unresolved work by data classification",
            unresolvedEntries.SelectMany(entry => entry.DataClassifications));
        AppendCountSection(
            builder,
            "Unresolved work by value family",
            unresolvedEntries.SelectMany(entry => entry.ValueFamilies));
        AppendCountSection(
            builder,
            "Reason codes",
            entries.SelectMany(entry => entry.ReasonCodes));
        AppendCountSection(
            builder,
            "Blockers",
            entries.SelectMany(entry => entry.BlockerReferences));
        AppendCountSection(
            builder,
            "Delivery waves",
            entries
                .Select(entry => entry.DeliveryWave)
                .Where(wave => wave is not null)
                .Cast<string>());
        AppendIntentionalExclusions(builder, entries);

        builder.AppendLine();
        builder.AppendLine("## Promotion policy");
        builder.AppendLine();
        builder.AppendLine(
            "- Only `approved_candidate` entries with `reviewed` state can be promoted " +
            "into the supported command catalog.");
        builder.AppendLine(
            "- `unreviewed` and `needs_re_review` entries fail closed and remain absent " +
            "from runtime capabilities.");
        builder.AppendLine(
            "- `intentional_exclusion` and `sdk_unavailable` are final non-supported " +
            "dispositions with Briosa-authored reasons.");
        builder.AppendLine(
            "- `blocked` identifies a named dependency and cannot silently become " +
            "supported.");
        builder.AppendLine(
            "- A changed per-command inventory fingerprint requires re-review before " +
            "promotion.");
        return builder.ToString().ReplaceLineEndings("\n");
    }

    private static void AppendIntentionalExclusions(
        StringBuilder builder,
        IEnumerable<CommandDispositionEntry> entries)
    {
        var exclusions = entries
            .Where(entry => string.Equals(
                entry.Disposition,
                "intentional_exclusion",
                StringComparison.Ordinal))
            .OrderBy(
                entry => string.Join('/', entry.CategoryPath),
                StringComparer.Ordinal)
            .ThenBy(entry => entry.MpStep, StringComparer.Ordinal)
            .ThenBy(entry => entry.InventoryKey, StringComparer.Ordinal)
            .ToArray();
        builder.AppendLine();
        builder.AppendLine("## Reviewed intentional exclusions");
        builder.AppendLine();
        if (exclusions.Length == 0)
        {
            builder.AppendLine("None.");
            return;
        }

        builder.AppendLine(
            "| Category path | MP step | Inventory key | Reason codes | Rationale | Decision |");
        builder.AppendLine("| --- | --- | --- | --- | --- | --- |");
        foreach (var entry in exclusions)
        {
            builder.AppendLine(
                CultureInfo.InvariantCulture,
                $"| {EscapeMarkdown(string.Join(" / ", entry.CategoryPath))} | " +
                $"{EscapeMarkdown(entry.MpStep)} | " +
                $"{EscapeMarkdown(entry.InventoryKey)} | " +
                $"{EscapeMarkdown(string.Join(", ", entry.ReasonCodes))} | " +
                $"{EscapeMarkdown(entry.Rationale)} | " +
                $"{EscapeMarkdown(string.Join(", ", entry.DecisionReferences))} |");
        }
    }

    private static bool IsUnresolved(CommandDispositionEntry entry) =>
        !string.Equals(entry.ReviewState, "reviewed", StringComparison.Ordinal) ||
        string.Equals(entry.Disposition, "blocked", StringComparison.Ordinal);

    private static int Count(
        IEnumerable<CommandDispositionEntry> entries,
        string? disposition,
        string? reviewState) =>
        entries.Count(entry =>
            (disposition is null ||
             string.Equals(entry.Disposition, disposition, StringComparison.Ordinal)) &&
            (reviewState is null ||
             string.Equals(entry.ReviewState, reviewState, StringComparison.Ordinal)));

    private static void AppendCountSection(
        StringBuilder builder,
        string heading,
        IEnumerable<string> values)
    {
        var counts = values
            .GroupBy(value => value, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToArray();
        builder.AppendLine();
        builder.AppendLine(CultureInfo.InvariantCulture, $"## {heading}");
        builder.AppendLine();
        if (counts.Length == 0)
        {
            builder.AppendLine("None.");
            return;
        }

        builder.AppendLine("| Value | Count |");
        builder.AppendLine("| --- | ---: |");
        foreach (var group in counts)
        {
            builder.AppendLine(
                CultureInfo.InvariantCulture,
                $"| `{EscapeMarkdown(group.Key)}` | {group.Count()} |");
        }
    }

    private static string EscapeMarkdown(string value) =>
        value.Replace("|", "\\|", StringComparison.Ordinal);

    private static void ValidateGitHubReference(
        string reference,
        string displayPath,
        string kind,
        List<string> errors)
    {
        if (!GitHubReference().IsMatch(reference))
        {
            errors.Add($"{displayPath}: invalid {kind} '{reference}'.");
        }
    }

    private static void RequireExactList(
        IReadOnlyList<string> actual,
        IReadOnlyList<string> expected,
        string displayPath,
        string property,
        List<string> errors)
    {
        if (!actual.SequenceEqual(expected, StringComparer.Ordinal))
        {
            errors.Add($"{displayPath}: {property} does not match inventory evidence.");
        }
    }

    private static void RequireSortedUnique(
        IReadOnlyList<string> values,
        string displayPath,
        string property,
        List<string> errors)
    {
        RequireSorted(values, displayPath, property, errors);
        RequireUnique(values, displayPath, property, errors);
    }

    private static void RequireSorted(
        IEnumerable<string> values,
        string displayPath,
        string property,
        List<string> errors)
    {
        var array = values.ToArray();
        if (!array.SequenceEqual(array.OrderBy(value => value, StringComparer.Ordinal)))
        {
            errors.Add($"{displayPath}: {property} must use ordinal sort order.");
        }
    }

    private static void RequireUnique(
        IEnumerable<string> values,
        string displayPath,
        string property,
        List<string> errors)
    {
        var array = values.ToArray();
        if (array.Distinct(StringComparer.Ordinal).Count() != array.Length)
        {
            errors.Add($"{displayPath}: {property} must not contain duplicates.");
        }
    }

    private static void RequireEqual(
        string actual,
        string expected,
        string displayPath,
        string property,
        List<string> errors)
    {
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            errors.Add(
                $"{displayPath}: {property} must be '{expected}', not '{actual}'.");
        }
    }

    private static List<string> SortedDistinct(IEnumerable<string> values) =>
        values
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToList();

    private static T ReadRequired<T>(string path) =>
        JsonSerializer.Deserialize<T>(File.ReadAllText(path), ReadOptions) ??
        throw new InvalidDataException($"JSON document '{path}' is empty.");

    private static T? Deserialize<T>(
        string path,
        string displayPath,
        List<string> errors)
    {
        if (!File.Exists(path))
        {
            errors.Add($"{displayPath}: file does not exist.");
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), ReadOptions);
        }
        catch (JsonException exception)
        {
            errors.Add($"{displayPath}: invalid JSON: {exception.Message}");
            return default;
        }
    }

    private static string Serialize<T>(T value) =>
        (JsonSerializer.Serialize(value, WriteOptions) + "\n").ReplaceLineEndings("\n");

    private static void WriteText(string path, string content)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(
            path,
            content.ReplaceLineEndings("\n"),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string Sha256(ReadOnlySpan<byte> bytes) =>
        Convert.ToHexStringLower(SHA256.HashData(bytes));

    private static bool IsWithin(string path, string directory)
    {
        var directoryPrefix = Path.GetFullPath(directory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
            Path.DirectorySeparatorChar;
        return path.StartsWith(directoryPrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePath(string path) => path.Replace('\\', '/');

    [GeneratedRegex("([a-z0-9])([A-Z])", RegexOptions.CultureInvariant)]
    private static partial Regex WordBoundary();

    [GeneratedRegex("[^A-Za-z0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex NonIdentifierCharacter();

    [GeneratedRegex("_+", RegexOptions.CultureInvariant)]
    private static partial Regex RepeatedUnderscore();

    [GeneratedRegex("^[a-z][a-z0-9_]*$", RegexOptions.CultureInvariant)]
    private static partial Regex ReasonCode();

    [GeneratedRegex(
        "^https://github\\.com/spatialanalyzer/[a-z0-9_.-]+/(?:issues|pull)/[1-9][0-9]*$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex GitHubReference();
}
