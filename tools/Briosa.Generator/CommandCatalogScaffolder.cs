using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Briosa.Generator;

internal static class CommandCatalogScaffolder
{
    private const string ManifestSchema =
        "https://spatialanalyzer.github.io/briosa/catalog/schemas/v1/scaffold-manifest.schema.json";
    private const string ScaffoldSchema =
        "https://spatialanalyzer.github.io/briosa/catalog/schemas/v1/scaffold.schema.json";

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private static readonly JsonSerializerOptions WriteOptions = new(ReadOptions)
    {
        WriteIndented = true
    };

    public static CommandCatalogScaffoldGenerationResult Generate(
        string inventoryPath,
        string dispositionDirectory,
        string valueFamilyCatalogPath,
        string catalogRoot,
        string outputDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inventoryPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(dispositionDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(valueFamilyCatalogPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        var fullInventoryPath = Path.GetFullPath(inventoryPath);
        var fullDispositionDirectory = Path.GetFullPath(dispositionDirectory);
        var fullValueFamilyCatalogPath = Path.GetFullPath(valueFamilyCatalogPath);
        var fullCatalogRoot = Path.GetFullPath(catalogRoot);
        var fullOutputDirectory = Path.GetFullPath(outputDirectory);

        if (PathsOverlap(fullCatalogRoot, fullOutputDirectory))
        {
            throw new InvalidDataException(
                "Catalog scaffold output must be separate from the supported catalog root.");
        }

        var dispositionValidation = CommandDispositionLedger.Validate(
            fullInventoryPath,
            fullDispositionDirectory);
        if (!dispositionValidation.IsValid)
        {
            throw new InvalidDataException(
                "Catalog scaffolding requires a valid disposition ledger:" +
                Environment.NewLine +
                string.Join(Environment.NewLine, dispositionValidation.Errors));
        }

        var catalogValidation = CommandCatalogValidator.ValidateDirectory(fullCatalogRoot);
        if (!catalogValidation.IsValid)
        {
            throw new InvalidDataException(
                "Catalog scaffolding requires a valid supported catalog:" +
                Environment.NewLine +
                string.Join(Environment.NewLine, catalogValidation.Errors));
        }

        var inventory = ReadRequired<MpCommandInventory>(fullInventoryPath);
        var dispositionManifestPath = Path.Combine(fullDispositionDirectory, "manifest.json");
        var dispositionManifest = ReadRequired<CommandDispositionManifest>(
            dispositionManifestPath);
        var valueCatalog = ReadRequired<CatalogScaffoldValueFamilyCatalog>(
            fullValueFamilyCatalogPath);

        RequireEqual(
            inventory.SpatialAnalyzerTarget,
            dispositionManifest.SpatialAnalyzerTarget,
            "inventory and disposition targets");
        RequireEqual(
            inventory.SpatialAnalyzerTarget,
            valueCatalog.SpatialAnalyzerTarget,
            "inventory and value-family targets");

        var inventorySha256 = Sha256(File.ReadAllBytes(fullInventoryPath));
        if (!valueCatalog.TrackedInputs.Any(input => string.Equals(
                input.Sha256,
                inventorySha256,
                StringComparison.Ordinal)))
        {
            throw new InvalidDataException(
                "The value-family catalog does not track the supplied inventory fingerprint.");
        }

        var inventoryByKey = inventory.Commands.ToDictionary(
            command => command.InventoryKey,
            StringComparer.Ordinal);
        var dispositionEntries = ReadDispositionEntries(
            fullDispositionDirectory,
            dispositionManifest);
        var approvedEntries = dispositionEntries.Values
            .Where(source =>
                string.Equals(
                    source.Entry.Disposition,
                    "approved_candidate",
                    StringComparison.Ordinal) &&
                string.Equals(source.Entry.ReviewState, "reviewed", StringComparison.Ordinal))
            .OrderBy(source => source.Entry.InventoryKey, StringComparer.Ordinal)
            .ToArray();

        var targetCatalogDirectory = Path.Combine(
            fullCatalogRoot,
            "sa",
            inventory.SpatialAnalyzerTarget);
        var catalogManifestPath = Path.Combine(targetCatalogDirectory, "catalog.json");
        var catalogManifest = ReadRequired<CommandCatalogManifest>(catalogManifestPath);
        RequireEqual(
            inventory.SpatialAnalyzerTarget,
            catalogManifest.SpatialAnalyzerTarget,
            "inventory and supported catalog targets");

        var existingCatalogInventoryKeys = ResolveExistingCatalogInventoryKeys(
            targetCatalogDirectory,
            catalogManifest,
            inventory,
            dispositionEntries);
        var familyResolver = new CatalogScaffoldFamilyResolver(valueCatalog, inventoryByKey);
        var sourceFingerprints = new CatalogScaffoldGlobalFingerprints
        {
            InventorySha256 = inventorySha256,
            DispositionManifestSha256 = Sha256(File.ReadAllBytes(dispositionManifestPath)),
            ValueFamilyCatalogSha256 = Sha256(File.ReadAllBytes(fullValueFamilyCatalogPath)),
            SupportedCatalogManifestSha256 = Sha256(File.ReadAllBytes(catalogManifestPath))
        };

        var desiredScaffolds = new SortedDictionary<string, CatalogScaffoldDesiredFile>(
            StringComparer.Ordinal);
        foreach (var source in approvedEntries.Where(source =>
                     !existingCatalogInventoryKeys.Contains(source.Entry.InventoryKey)))
        {
            if (!inventoryByKey.TryGetValue(source.Entry.InventoryKey, out var command))
            {
                throw new InvalidDataException(
                    $"Approved disposition '{source.Entry.InventoryKey}' is absent from inventory.");
            }

            var document = CreateScaffold(
                command,
                source,
                familyResolver,
                sourceFingerprints);
            var text = Serialize(document);
            var relativePath = $"candidates/{Sha256(source.Entry.InventoryKey)}.json";
            if (!desiredScaffolds.TryAdd(
                    relativePath,
                    new CatalogScaffoldDesiredFile(
                        relativePath,
                        source.Entry.InventoryKey,
                        text,
                        Sha256(Encoding.UTF8.GetBytes(text)))))
            {
                throw new InvalidDataException(
                    $"Catalog scaffold path collision for '{source.Entry.InventoryKey}'.");
            }
        }

        var desiredManifest = new CommandCatalogScaffoldManifest
        {
            Schema = ManifestSchema,
            SchemaVersion = 1,
            SpatialAnalyzerTarget = inventory.SpatialAnalyzerTarget,
            SourceFingerprints = sourceFingerprints,
            ApprovedCandidateCount = approvedEntries.Length,
            ExistingCatalogOperationCount = existingCatalogInventoryKeys.Count,
            ScaffoldCount = desiredScaffolds.Count,
            ScaffoldFiles = desiredScaffolds.Values
                .Select(file => new CommandCatalogScaffoldManifestEntry
                {
                    Path = file.RelativePath,
                    InventoryKey = file.InventoryKey,
                    Sha256 = file.Sha256
                })
                .ToList()
        };
        var manifestText = Serialize(desiredManifest);
        var conflicts = FindConflicts(
            fullOutputDirectory,
            desiredScaffolds,
            desiredManifest);
        if (conflicts.Count > 0)
        {
            return new CommandCatalogScaffoldGenerationResult(
                [],
                conflicts,
                approvedEntries.Length,
                existingCatalogInventoryKeys.Count,
                desiredScaffolds.Count);
        }

        foreach (var desired in desiredScaffolds.Values)
        {
            var path = CombineRelativePath(fullOutputDirectory, desired.RelativePath);
            if (!File.Exists(path))
            {
                WriteText(path, desired.Text);
            }
        }

        var outputManifestPath = Path.Combine(fullOutputDirectory, "manifest.json");
        WriteText(outputManifestPath, manifestText);
        var files = desiredScaffolds.Keys
            .Select(path => CombineRelativePath(fullOutputDirectory, path))
            .Append(outputManifestPath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        return new CommandCatalogScaffoldGenerationResult(
            files,
            [],
            approvedEntries.Length,
            existingCatalogInventoryKeys.Count,
            desiredScaffolds.Count);
    }

    private static Dictionary<string, CatalogScaffoldDispositionSource> ReadDispositionEntries(
        string dispositionDirectory,
        CommandDispositionManifest manifest)
    {
        var entries = new Dictionary<string, CatalogScaffoldDispositionSource>(
            StringComparer.Ordinal);
        foreach (var shardReference in manifest.Shards)
        {
            var shardPath = CombineRelativePath(dispositionDirectory, shardReference.Path);
            var shard = ReadRequired<CommandDispositionShard>(shardPath);
            foreach (var entry in shard.Entries)
            {
                if (!entries.TryAdd(
                        entry.InventoryKey,
                        new CatalogScaffoldDispositionSource(entry, shardReference)))
                {
                    throw new InvalidDataException(
                        $"Duplicate disposition entry '{entry.InventoryKey}'.");
                }
            }
        }

        return entries;
    }

    private static HashSet<string> ResolveExistingCatalogInventoryKeys(
        string targetCatalogDirectory,
        CommandCatalogManifest manifest,
        MpCommandInventory inventory,
        Dictionary<string, CatalogScaffoldDispositionSource> dispositionEntries)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var operationFile in manifest.OperationFiles)
        {
            var operation = ReadRequired<CommandCatalogOperation>(
                CombineRelativePath(targetCatalogDirectory, operationFile));
            var candidates = ResolveCatalogOperationCandidates(
                operation,
                inventory.Commands);
            if (candidates.Count != 1)
            {
                throw new InvalidDataException(
                    $"Supported catalog operation '{operation.OperationId}' must trace to exactly " +
                    $"one inventory key; found {candidates.Count}.");
            }

            var inventoryKey = candidates[0].InventoryKey;
            if (!dispositionEntries.TryGetValue(inventoryKey, out var disposition) ||
                !string.Equals(
                    disposition.Entry.Disposition,
                    "approved_candidate",
                    StringComparison.Ordinal) ||
                !string.Equals(
                    disposition.Entry.ReviewState,
                    "reviewed",
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Supported catalog operation '{operation.OperationId}' traces to inventory " +
                    $"key '{inventoryKey}', which is not a reviewed approved candidate.");
            }

            if (!result.Add(inventoryKey))
            {
                throw new InvalidDataException(
                    $"More than one supported catalog operation traces to '{inventoryKey}'.");
            }
        }

        return result;
    }

    private static List<MpCommandInventoryCommand> ResolveCatalogOperationCandidates(
        CommandCatalogOperation operation,
        IReadOnlyList<MpCommandInventoryCommand> commands)
        => commands
            .Where(command =>
                string.Equals(command.InventoryKey, operation.InventoryKey, StringComparison.Ordinal) &&
                string.Equals(command.MpStep, operation.MpStep, StringComparison.Ordinal))
            .ToList();

    private static CommandCatalogScaffoldDocument CreateScaffold(
        MpCommandInventoryCommand command,
        CatalogScaffoldDispositionSource source,
        CatalogScaffoldFamilyResolver familyResolver,
        CatalogScaffoldGlobalFingerprints globalFingerprints)
    {
        var entry = source.Entry;
        var shape = entry.CommandShape ?? throw new InvalidDataException(
            $"Approved disposition '{entry.InventoryKey}' has no command shape.");
        if (!string.Equals(shape.Status, "resolved", StringComparison.Ordinal) ||
            shape.Discrepancies.Count > 0 ||
            entry.BlockerReferences.Count > 0)
        {
            throw new InvalidDataException(
                $"Approved disposition '{entry.InventoryKey}' is not promotion-ready.");
        }

        var arguments = shape.Arguments
            .OrderBy(argument => argument.Ordinal)
            .Select(argument => CreateScaffoldArgument(
                command,
                entry,
                argument,
                familyResolver))
            .ToList();
        var blockers = new List<string>
        {
            "/catalog_draft/operation_id",
            "/catalog_draft/category",
            "/catalog_draft/protocol",
            "/catalog_draft/stability",
            "/catalog_draft/execution_scope",
            "/catalog_draft/risk/effect",
            "/catalog_draft/risk/replay_safety",
            "/catalog_draft/risk/flags",
            "/catalog_draft/documentation/summary",
            "/catalog_draft/documentation/isolation",
            "/catalog_draft/evidence"
        };
        for (var index = 0; index < arguments.Count; index++)
        {
            blockers.Add($"/arguments/{index}/catalog_fields/argument_id");
            blockers.Add($"/arguments/{index}/catalog_fields/data_classification");
            blockers.Add($"/arguments/{index}/catalog_fields/field_numbers");
            blockers.Add($"/arguments/{index}/catalog_fields/documentation");
            if (arguments[index].ReviewedInput is not null)
            {
                blockers.Add($"/arguments/{index}/catalog_fields/input");
            }
        }

        if (shape.MpStep is null)
        {
            throw new InvalidDataException(
                $"Resolved MP step is missing for '{entry.InventoryKey}'.");
        }

        return new CommandCatalogScaffoldDocument
        {
            Schema = ScaffoldSchema,
            SchemaVersion = 1,
            SpatialAnalyzerTarget = familyResolver.SpatialAnalyzerTarget,
            InventoryKey = entry.InventoryKey,
            InventoryMpStep = entry.MpStep,
            ResolvedMpStep = shape.MpStep,
            CategoryPath = [.. entry.CategoryPath],
            DeliveryWave = entry.DeliveryWave!,
            SourceFingerprints = new CatalogScaffoldSourceFingerprints
            {
                InventorySha256 = globalFingerprints.InventorySha256,
                InventoryEntrySha256 = entry.InventoryEntrySha256,
                DispositionManifestSha256 = globalFingerprints.DispositionManifestSha256,
                DispositionShardPath = source.Shard.Path,
                DispositionShardSha256 = source.Shard.Sha256,
                ValueFamilyCatalogSha256 = globalFingerprints.ValueFamilyCatalogSha256,
                SupportedCatalogManifestSha256 =
                    globalFingerprints.SupportedCatalogManifestSha256
            },
            EvidenceReferences = [.. entry.EvidenceReferences],
            DecisionReferences = [.. entry.DecisionReferences],
            ReviewedDisposition = new CatalogScaffoldReviewedDisposition
            {
                RiskEffect = entry.RiskEffect,
                RiskFlags = [.. entry.RiskFlags],
                DataClassifications = [.. entry.DataClassifications],
                ValueFamilies = [.. entry.ValueFamilies]
            },
            CatalogDraft = new CatalogScaffoldCatalogDraft
            {
                MpStep = shape.MpStep,
                OperationId = null,
                Category = null,
                Protocol = null,
                Stability = null,
                ExecutionScope = null,
                Risk = new CatalogScaffoldDraftRisk
                {
                    Effect = null,
                    ReplaySafety = null,
                    Flags = null
                },
                Documentation = new CatalogScaffoldDraftDocumentation
                {
                    Summary = null,
                    Isolation = null
                },
                Evidence = null
            },
            Arguments = arguments,
            ReviewStatus = "incomplete",
            Blockers = blockers
        };
    }

    private static CatalogScaffoldArgument CreateScaffoldArgument(
        MpCommandInventoryCommand command,
        CommandDispositionEntry entry,
        CommandArgumentResolution argument,
        CatalogScaffoldFamilyResolver familyResolver)
    {
        if (argument.InventoryIndex < 0 || argument.InventoryIndex >= command.Arguments.Count)
        {
            throw new InvalidDataException(
                $"Disposition argument index {argument.InventoryIndex} is invalid for " +
                $"'{entry.InventoryKey}'.");
        }

        var inventoryArgument = command.Arguments[argument.InventoryIndex];
        var familyResolution = familyResolver.Resolve(
            entry,
            argument,
            inventoryArgument.SdkOrder);
        var family = familyResolution.Family;

        return new CatalogScaffoldArgument
        {
            InventoryIndex = argument.InventoryIndex,
            Ordinal = argument.Ordinal,
            SdkOrder = inventoryArgument.SdkOrder,
            MpName = argument.MpName,
            Direction = argument.Direction,
            ResultOnly = argument.ResultOnly,
            SemanticType = family.FamilyId,
            PublicTypeTarget = family.PublicTypeTarget,
            WorkerTypeTarget = family.WorkerTypeTarget,
            FamilyAssignments = familyResolution.Assignments,
            ReviewedInput = argument.Input,
            SdkBinding = argument.SdkBinding,
            CatalogFields = new CatalogScaffoldArgumentFields
            {
                ArgumentId = null,
                DataClassification = null,
                FieldNumbers = null,
                Input = null,
                Documentation = null
            }
        };
    }

    private static List<CommandCatalogScaffoldConflict> FindConflicts(
        string outputDirectory,
        IReadOnlyDictionary<string, CatalogScaffoldDesiredFile> desiredScaffolds,
        CommandCatalogScaffoldManifest desiredManifest)
    {
        var conflicts = new List<CommandCatalogScaffoldConflict>();
        foreach (var desired in desiredScaffolds.Values)
        {
            var path = CombineRelativePath(outputDirectory, desired.RelativePath);
            if (File.Exists(path) &&
                !string.Equals(File.ReadAllText(path), desired.Text, StringComparison.Ordinal))
            {
                conflicts.Add(new CommandCatalogScaffoldConflict(
                    desired.RelativePath,
                    "existing_scaffold_differs"));
            }
        }

        var manifestPath = Path.Combine(outputDirectory, "manifest.json");
        if (!File.Exists(manifestPath))
        {
            return conflicts;
        }

        CommandCatalogScaffoldManifest? existingManifest;
        try
        {
            existingManifest = ReadRequired<CommandCatalogScaffoldManifest>(manifestPath);
        }
        catch (Exception exception) when (
            exception is JsonException or InvalidDataException)
        {
            conflicts.Add(new CommandCatalogScaffoldConflict(
                "manifest.json",
                "existing_manifest_is_not_a_catalog_scaffold_manifest"));
            return conflicts;
        }

        if (!string.Equals(existingManifest.Schema, ManifestSchema, StringComparison.Ordinal))
        {
            conflicts.Add(new CommandCatalogScaffoldConflict(
                "manifest.json",
                "existing_manifest_has_unknown_schema"));
            return conflicts;
        }

        var desiredPaths = desiredManifest.ScaffoldFiles
            .Select(file => file.Path)
            .ToHashSet(StringComparer.Ordinal);
        foreach (var stalePath in existingManifest.ScaffoldFiles
                     .Select(file => file.Path)
                     .Except(desiredPaths, StringComparer.Ordinal)
                     .OrderBy(path => path, StringComparer.Ordinal))
        {
            conflicts.Add(new CommandCatalogScaffoldConflict(
                stalePath,
                "stale_scaffold_requires_manual_removal"));
        }

        return conflicts;
    }

    private static T ReadRequired<T>(string path)
    {
        if (!File.Exists(path))
        {
            throw new InvalidDataException($"Required input '{path}' does not exist.");
        }

        return JsonSerializer.Deserialize<T>(File.ReadAllText(path), ReadOptions) ??
            throw new InvalidDataException($"Required input '{path}' was empty.");
    }

    private static string Serialize<T>(T value) =>
        (JsonSerializer.Serialize(value, WriteOptions) + "\n").ReplaceLineEndings("\n");

    private static void WriteText(string path, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(
            path,
            text,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string CombineRelativePath(string root, string relativePath) =>
        Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));

    private static void RequireEqual(string left, string right, string description)
    {
        if (!string.Equals(left, right, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Catalog scaffold {description} do not match: '{left}' and '{right}'.");
        }
    }

    private static bool PathsOverlap(string first, string second) =>
        IsWithin(first, second) || IsWithin(second, first);

    private static bool IsWithin(string path, string root)
    {
        var relative = Path.GetRelativePath(root, path);
        return string.Equals(relative, ".", StringComparison.Ordinal) ||
            (!relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
             !string.Equals(relative, "..", StringComparison.Ordinal) &&
             !Path.IsPathRooted(relative));
    }

    private static string Sha256(string value) =>
        Sha256(Encoding.UTF8.GetBytes(value));

    private static string Sha256(byte[] value) =>
        Convert.ToHexStringLower(SHA256.HashData(value));
}

internal sealed class CatalogScaffoldFamilyResolver
{
    private readonly Dictionary<string, CatalogScaffoldValueFamily> familiesById;
    private readonly Dictionary<string, string[]> familiesByMethod;
    private readonly Dictionary<string, CatalogScaffoldValueFamilyAssignment> assignments;

    public CatalogScaffoldFamilyResolver(
        CatalogScaffoldValueFamilyCatalog catalog,
        IReadOnlyDictionary<string, MpCommandInventoryCommand> inventoryByKey)
    {
        SpatialAnalyzerTarget = catalog.SpatialAnalyzerTarget;
        familiesById = catalog.Families.ToDictionary(
            family => family.FamilyId,
            StringComparer.Ordinal);
        familiesByMethod = catalog.Families
            .SelectMany(family => family.BindingMethods.Select(method => (method, family.FamilyId)))
            .GroupBy(pair => pair.method, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(pair => pair.FamilyId)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(family => family, StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.Ordinal);
        assignments = catalog.CommandAssignments.ToDictionary(
            assignment => AssignmentKey(
                assignment.Method,
                assignment.InventoryKey,
                assignment.SdkOrder),
            StringComparer.Ordinal);

        foreach (var sharedMethod in catalog.SharedMethods)
        {
            if (!familiesByMethod.TryGetValue(sharedMethod.Method, out var mapped) ||
                !mapped.SequenceEqual(
                    sharedMethod.Families.OrderBy(family => family, StringComparer.Ordinal),
                    StringComparer.Ordinal))
            {
                throw new InvalidDataException(
                    $"Shared value-family method '{sharedMethod.Method}' has inconsistent families.");
            }
        }

        foreach (var assignment in catalog.CommandAssignments)
        {
            if (!inventoryByKey.TryGetValue(assignment.InventoryKey, out var command) ||
                !command.Arguments.Any(argument =>
                    argument.SdkOrder == assignment.SdkOrder &&
                    (string.Equals(
                         argument.SdkBinding.Setter.Method,
                         assignment.Method,
                         StringComparison.Ordinal) ||
                     string.Equals(
                         argument.SdkBinding.Getter.Method,
                         assignment.Method,
                         StringComparison.Ordinal))))
            {
                throw new InvalidDataException(
                    $"Value-family assignment '{assignment.InventoryKey}', method " +
                    $"'{assignment.Method}', SDK order {assignment.SdkOrder} is stale.");
            }
        }
    }

    public string SpatialAnalyzerTarget { get; }

    public CatalogScaffoldResolvedFamily Resolve(
        CommandDispositionEntry entry,
        CommandArgumentResolution argument,
        int? sdkOrder)
    {
        var methods = new[]
            {
                argument.SdkBinding.Setter,
                argument.SdkBinding.Getter
            }
            .Where(method => method is not null)
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (methods.Length == 0)
        {
            throw new InvalidDataException(
                $"Argument {argument.Ordinal} for '{entry.InventoryKey}' has no SDK binding.");
        }

        var resolutions = methods
            .Select(method => ResolveFamily(
                method,
                entry.InventoryKey,
                sdkOrder))
            .ToArray();
        var familyIds = resolutions
            .Select(resolution => resolution.FamilyId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (familyIds.Length != 1)
        {
            throw new InvalidDataException(
                $"Argument {argument.Ordinal} for '{entry.InventoryKey}' resolves to conflicting " +
                $"semantic families: {string.Join(", ", familyIds)}.");
        }

        return new CatalogScaffoldResolvedFamily(
            familiesById[familyIds[0]],
            resolutions
                .Select(resolution => new CatalogScaffoldFamilyAssignmentReference
                {
                    Method = resolution.Method,
                    Source = resolution.Source,
                    SdkOrder = sdkOrder,
                    DocumentedOrdinals = [.. resolution.DocumentedOrdinals]
                })
                .ToList());
    }

    private CatalogScaffoldFamilyResolution ResolveFamily(
        string method,
        string inventoryKey,
        int? sdkOrder)
    {
        if (!familiesByMethod.TryGetValue(method, out var families) || families.Length == 0)
        {
            throw new InvalidDataException(
                $"SDK method '{method}' has no reviewed semantic value family.");
        }

        if (families.Length == 1)
        {
            return new CatalogScaffoldFamilyResolution(
                method,
                families[0],
                "binding_method",
                []);
        }

        if (sdkOrder is null)
        {
            throw new InvalidDataException(
                $"Shared SDK method '{method}' for '{inventoryKey}' has no exact SDK order.");
        }

        var key = AssignmentKey(method, inventoryKey, sdkOrder.Value);
        if (!assignments.TryGetValue(key, out var assignment) ||
            !families.Contains(assignment.FamilyId, StringComparer.Ordinal))
        {
            throw new InvalidDataException(
                $"Shared SDK method '{method}' for '{inventoryKey}', SDK order {sdkOrder} " +
                "has no exact reviewed family assignment.");
        }

        return new CatalogScaffoldFamilyResolution(
            method,
            assignment.FamilyId,
            "exact_command_assignment",
            assignment.DocumentedOrdinals);
    }

    private static string AssignmentKey(string method, string inventoryKey, int sdkOrder) =>
        $"{method}\n{inventoryKey}\n{sdkOrder}";
}

internal sealed record CommandCatalogScaffoldGenerationResult(
    IReadOnlyList<string> Files,
    IReadOnlyList<CommandCatalogScaffoldConflict> Conflicts,
    int ApprovedCandidateCount,
    int ExistingCatalogOperationCount,
    int ScaffoldCount)
{
    public bool IsSuccessful => Conflicts.Count == 0;
}

internal sealed record CommandCatalogScaffoldConflict(string Path, string Reason);

internal sealed record CatalogScaffoldDesiredFile(
    string RelativePath,
    string InventoryKey,
    string Text,
    string Sha256);

internal sealed record CatalogScaffoldDispositionSource(
    CommandDispositionEntry Entry,
    CommandDispositionShardReference Shard);

internal sealed record CatalogScaffoldResolvedFamily(
    CatalogScaffoldValueFamily Family,
    List<CatalogScaffoldFamilyAssignmentReference> Assignments);

internal sealed record CatalogScaffoldFamilyResolution(
    string Method,
    string FamilyId,
    string Source,
    IReadOnlyList<int> DocumentedOrdinals);

internal sealed class CommandCatalogScaffoldManifest
{
    [JsonPropertyName("$schema")]
    [JsonRequired]
    public required string Schema { get; init; }

    [JsonRequired]
    public required int SchemaVersion { get; init; }

    [JsonRequired]
    public required string SpatialAnalyzerTarget { get; init; }

    [JsonRequired]
    public required CatalogScaffoldGlobalFingerprints SourceFingerprints { get; init; }

    [JsonRequired]
    public required int ApprovedCandidateCount { get; init; }

    [JsonRequired]
    public required int ExistingCatalogOperationCount { get; init; }

    [JsonRequired]
    public required int ScaffoldCount { get; init; }

    [JsonRequired]
    public required List<CommandCatalogScaffoldManifestEntry> ScaffoldFiles { get; init; }
}

internal sealed class CommandCatalogScaffoldManifestEntry
{
    [JsonRequired]
    public required string Path { get; init; }

    [JsonRequired]
    public required string InventoryKey { get; init; }

    [JsonRequired]
    public required string Sha256 { get; init; }
}

internal sealed class CatalogScaffoldGlobalFingerprints
{
    [JsonRequired]
    public required string InventorySha256 { get; init; }

    [JsonRequired]
    public required string DispositionManifestSha256 { get; init; }

    [JsonRequired]
    public required string ValueFamilyCatalogSha256 { get; init; }

    [JsonRequired]
    public required string SupportedCatalogManifestSha256 { get; init; }
}

internal sealed class CommandCatalogScaffoldDocument
{
    [JsonPropertyName("$schema")]
    [JsonRequired]
    public required string Schema { get; init; }

    [JsonRequired]
    public required int SchemaVersion { get; init; }

    [JsonRequired]
    public required string SpatialAnalyzerTarget { get; init; }

    [JsonRequired]
    public required string InventoryKey { get; init; }

    [JsonRequired]
    public required string InventoryMpStep { get; init; }

    [JsonRequired]
    public required string ResolvedMpStep { get; init; }

    [JsonRequired]
    public required List<string> CategoryPath { get; init; }

    [JsonRequired]
    public required string DeliveryWave { get; init; }

    [JsonRequired]
    public required CatalogScaffoldSourceFingerprints SourceFingerprints { get; init; }

    [JsonRequired]
    public required List<string> EvidenceReferences { get; init; }

    [JsonRequired]
    public required List<string> DecisionReferences { get; init; }

    [JsonRequired]
    public required CatalogScaffoldReviewedDisposition ReviewedDisposition { get; init; }

    [JsonRequired]
    public required CatalogScaffoldCatalogDraft CatalogDraft { get; init; }

    [JsonRequired]
    public required List<CatalogScaffoldArgument> Arguments { get; init; }

    [JsonRequired]
    public required string ReviewStatus { get; init; }

    [JsonRequired]
    public required List<string> Blockers { get; init; }
}

internal sealed class CatalogScaffoldSourceFingerprints
{
    [JsonRequired]
    public required string InventorySha256 { get; init; }

    [JsonRequired]
    public required string InventoryEntrySha256 { get; init; }

    [JsonRequired]
    public required string DispositionManifestSha256 { get; init; }

    [JsonRequired]
    public required string DispositionShardPath { get; init; }

    [JsonRequired]
    public required string DispositionShardSha256 { get; init; }

    [JsonRequired]
    public required string ValueFamilyCatalogSha256 { get; init; }

    [JsonRequired]
    public required string SupportedCatalogManifestSha256 { get; init; }
}

internal sealed class CatalogScaffoldReviewedDisposition
{
    [JsonRequired]
    public required string RiskEffect { get; init; }

    [JsonRequired]
    public required List<string> RiskFlags { get; init; }

    [JsonRequired]
    public required List<string> DataClassifications { get; init; }

    [JsonRequired]
    public required List<string> ValueFamilies { get; init; }
}

internal sealed class CatalogScaffoldCatalogDraft
{
    [JsonRequired]
    public required string MpStep { get; init; }

    [JsonRequired]
    public required string? OperationId { get; init; }

    [JsonRequired]
    public required string? Category { get; init; }

    [JsonRequired]
    public required CommandCatalogProtocolNames? Protocol { get; init; }

    [JsonRequired]
    public required string? Stability { get; init; }

    [JsonRequired]
    public required string? ExecutionScope { get; init; }

    [JsonRequired]
    public required CatalogScaffoldDraftRisk Risk { get; init; }

    [JsonRequired]
    public required CatalogScaffoldDraftDocumentation Documentation { get; init; }

    [JsonRequired]
    public required List<CommandCatalogEvidence>? Evidence { get; init; }
}

internal sealed class CatalogScaffoldDraftRisk
{
    [JsonRequired]
    public required string? Effect { get; init; }

    [JsonRequired]
    public required string? ReplaySafety { get; init; }

    [JsonRequired]
    public required List<string>? Flags { get; init; }
}

internal sealed class CatalogScaffoldDraftDocumentation
{
    [JsonRequired]
    public required string? Summary { get; init; }

    [JsonRequired]
    public required string? Isolation { get; init; }
}

internal sealed class CatalogScaffoldArgument
{
    [JsonRequired]
    public required int InventoryIndex { get; init; }

    [JsonRequired]
    public required int Ordinal { get; init; }

    [JsonRequired]
    public required int? SdkOrder { get; init; }

    [JsonRequired]
    public required string MpName { get; init; }

    [JsonRequired]
    public required string Direction { get; init; }

    [JsonRequired]
    public required string ResultOnly { get; init; }

    [JsonRequired]
    public required string SemanticType { get; init; }

    [JsonRequired]
    public required string PublicTypeTarget { get; init; }

    [JsonRequired]
    public required string WorkerTypeTarget { get; init; }

    [JsonRequired]
    public required List<CatalogScaffoldFamilyAssignmentReference> FamilyAssignments { get; init; }

    [JsonRequired]
    public required CommandInputResolution? ReviewedInput { get; init; }

    [JsonRequired]
    public required CommandSdkBindingResolution SdkBinding { get; init; }

    [JsonRequired]
    public required CatalogScaffoldArgumentFields CatalogFields { get; init; }
}

internal sealed class CatalogScaffoldFamilyAssignmentReference
{
    [JsonRequired]
    public required string Method { get; init; }

    [JsonRequired]
    public required string Source { get; init; }

    [JsonRequired]
    public required int? SdkOrder { get; init; }

    [JsonRequired]
    public required List<int> DocumentedOrdinals { get; init; }
}

internal sealed class CatalogScaffoldArgumentFields
{
    [JsonRequired]
    public required string? ArgumentId { get; init; }

    [JsonRequired]
    public required string? DataClassification { get; init; }

    [JsonRequired]
    public required CommandCatalogFieldNumbers? FieldNumbers { get; init; }

    [JsonRequired]
    public required CommandCatalogInputMetadata? Input { get; init; }

    [JsonRequired]
    public required string? Documentation { get; init; }
}

internal sealed class CatalogScaffoldValueFamilyCatalog
{
    [JsonRequired]
    public required int SchemaVersion { get; init; }

    [JsonRequired]
    public required string SpatialAnalyzerTarget { get; init; }

    [JsonRequired]
    public required List<CatalogScaffoldTrackedInput> TrackedInputs { get; init; }

    [JsonRequired]
    public required List<CatalogScaffoldSharedMethod> SharedMethods { get; init; }

    [JsonRequired]
    public required List<CatalogScaffoldValueFamily> Families { get; init; }

    [JsonRequired]
    public required List<CatalogScaffoldValueFamilyAssignment> CommandAssignments { get; init; }
}

internal sealed class CatalogScaffoldTrackedInput
{
    [JsonRequired]
    public required string Path { get; init; }

    [JsonRequired]
    public required string Sha256 { get; init; }
}

internal sealed class CatalogScaffoldSharedMethod
{
    [JsonRequired]
    public required string Method { get; init; }

    [JsonRequired]
    public required List<string> Families { get; init; }
}

internal sealed class CatalogScaffoldValueFamily
{
    [JsonRequired]
    public required string FamilyId { get; init; }

    [JsonRequired]
    public required string Shape { get; init; }

    [JsonRequired]
    public required string PublicTypeTarget { get; init; }

    [JsonRequired]
    public required string WorkerTypeTarget { get; init; }

    [JsonRequired]
    public required string ImplementationStatus { get; init; }

    [JsonRequired]
    public required List<string> BindingMethods { get; init; }
}

internal sealed class CatalogScaffoldValueFamilyAssignment
{
    [JsonRequired]
    public required string Method { get; init; }

    [JsonRequired]
    public required string InventoryKey { get; init; }

    [JsonRequired]
    public required int SdkOrder { get; init; }

    [JsonRequired]
    public required List<int> DocumentedOrdinals { get; init; }

    [JsonRequired]
    public required string FamilyId { get; init; }
}
