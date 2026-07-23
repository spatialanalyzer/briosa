using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Briosa.Generator;

internal static partial class SdkBindingRegistry
{
    private const string ReviewSchemaReference = "../../schemas/v1/review.schema.json";
    private const string RegistrySchemaReference = "../../schemas/v1/registry.schema.json";
    private const string InteropAssemblyName = "Briosa.SpatialAnalyzer.Interop.dll";
    private const string InteropApiName = "Briosa.SpatialAnalyzer.Interop.PublicApi.txt";
    private const string InteropTypeName = "Briosa.SpatialAnalyzer.Interop.ISpatialAnalyzerSDK";

    private static readonly string[] Shapes =
    [
        "array",
        "enum",
        "identifier",
        "path",
        "reference_list",
        "scalar",
        "structured",
        "transform"
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

    public static SdkBindingRegistrySyncResult Sync(
        string inventoryPath,
        string dispositionDirectory,
        string interopDirectory,
        string targetDirectory)
    {
        var build = Build(
            inventoryPath,
            dispositionDirectory,
            interopDirectory,
            targetDirectory);
        var registryPath = Path.Combine(Path.GetFullPath(targetDirectory), "registry.json");
        var reportPath = Path.Combine(Path.GetFullPath(targetDirectory), "report.md");
        WriteText(registryPath, build.RegistryText);
        WriteText(reportPath, build.ReportText);
        return new SdkBindingRegistrySyncResult(
            [registryPath, reportPath],
            build.Registry.Bindings.Count,
            build.Registry.ValueFamilies.Count);
    }

    public static SdkBindingRegistryValidationResult Validate(
        string inventoryPath,
        string dispositionDirectory,
        string interopDirectory,
        string targetDirectory)
    {
        var errors = new List<string>();
        SdkBindingRegistryBuild? build = null;
        try
        {
            build = Build(
                inventoryPath,
                dispositionDirectory,
                interopDirectory,
                targetDirectory);
        }
        catch (Exception exception) when (
            exception is InvalidDataException or IOException or JsonException or
            BadImageFormatException or FileLoadException)
        {
            errors.Add(exception.Message);
        }

        if (build is null)
        {
            return new SdkBindingRegistryValidationResult(errors, 0, 0);
        }

        CompareFile(
            Path.Combine(Path.GetFullPath(targetDirectory), "registry.json"),
            build.RegistryText,
            errors);
        CompareFile(
            Path.Combine(Path.GetFullPath(targetDirectory), "report.md"),
            build.ReportText,
            errors);
        return new SdkBindingRegistryValidationResult(
            errors,
            build.Registry.Bindings.Count,
            build.Registry.ValueFamilies.Count);
    }

    private static SdkBindingRegistryBuild Build(
        string inventoryPath,
        string dispositionDirectory,
        string interopDirectory,
        string targetDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inventoryPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(dispositionDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(interopDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetDirectory);

        var fullInventoryPath = Path.GetFullPath(inventoryPath);
        var fullDispositionDirectory = Path.GetFullPath(dispositionDirectory);
        var fullInteropDirectory = Path.GetFullPath(interopDirectory);
        var fullTargetDirectory = Path.GetFullPath(targetDirectory);
        var target = Path.GetFileName(fullTargetDirectory.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar));
        var inventory = ReadRequired<MpCommandInventory>(fullInventoryPath);
        if (!string.Equals(inventory.SpatialAnalyzerTarget, target, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Binding registry target '{target}' does not match inventory target " +
                $"'{inventory.SpatialAnalyzerTarget}'.");
        }

        var reviewPath = Path.Combine(fullTargetDirectory, "review.json");
        var review = ReadRequired<SdkBindingReview>(reviewPath);
        ValidateReview(review, target);

        var dispositions = ReadDispositions(fullDispositionDirectory, target);
        var missingInventoryKeys = inventory.Commands
            .Select(command => command.InventoryKey)
            .Where(key => !dispositions.ContainsKey(key))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();
        if (missingInventoryKeys.Length > 0)
        {
            throw new InvalidDataException(
                "The disposition ledger does not cover inventory key(s): " +
                string.Join(", ", missingInventoryKeys));
        }

        var interopAssemblyPath = Path.Combine(fullInteropDirectory, InteropAssemblyName);
        var interopApiPath = Path.Combine(fullInteropDirectory, InteropApiName);
        RequireFile(interopAssemblyPath);
        RequireFile(interopApiPath);
        var interopMethods = ReadInteropMethods(interopAssemblyPath);
        var observations = ReadObservations(inventory);
        var methodNames = interopMethods.Keys
            .Concat(observations.Keys)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(method => method, StringComparer.Ordinal)
            .ToArray();

        var bindings = new List<SdkBindingRegistryEntry>(methodNames.Length);
        foreach (var methodName in methodNames)
        {
            var hasInterop = interopMethods.TryGetValue(methodName, out var signature);
            var methodObservations = observations.TryGetValue(methodName, out var observed)
                ? observed
                : [];
            var observedCommands = methodObservations
                .Select(item => item.InventoryKey)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var excludedCommandCount = observedCommands.Count(key =>
                string.Equals(
                    dispositions[key].Disposition,
                    "intentional_exclusion",
                    StringComparison.Ordinal));
            var remainingCommandCount = observedCommands.Length - excludedCommandCount;
            var sourceStatus = hasInterop
                ? methodObservations.Count > 0
                    ? "inventory_and_interop"
                    : "interop_only"
                : "inventory_only";
            var registryStatus = sourceStatus switch
            {
                "inventory_only" => "blocked_missing_interop",
                "interop_only" => "unobserved_interop",
                _ when remainingCommandCount == 0 => "excluded_only",
                _ => "usable"
            };
            var core = SemanticCore(methodName);
            if (!review.Families.TryGetValue(core, out var familyReview))
            {
                throw new InvalidDataException(
                    $"review.json has no semantic family for binding core '{core}' " +
                    $"used by '{methodName}'.");
            }

            bindings.Add(new SdkBindingRegistryEntry
            {
                Method = methodName,
                Direction = methodName.StartsWith("Set", StringComparison.Ordinal)
                    ? "setter"
                    : "getter",
                SourceStatus = sourceStatus,
                RegistryStatus = registryStatus,
                SemanticValueFamily = familyReview.FamilyId,
                Coverage = CreateCoverage(review, methodName, registryStatus),
                InteropSignature = signature,
                InventoryUsage = CreateUsage(methodObservations, excludedCommandCount),
                Rationale = Rationale(registryStatus),
                DecisionReferences = [.. review.DecisionReferences],
                BlockerReferences = registryStatus == "blocked_missing_interop"
                    ? [review.MissingInteropBlocker]
                    : []
            });
        }

        ValidateImplementedCoverage(review, bindings);
        var families = CreateFamilies(review, bindings);
        var registry = new SdkBindingRegistryDocument
        {
            Schema = RegistrySchemaReference,
            SchemaVersion = 1,
            SpatialAnalyzerTarget = target,
            Inventory = CreateSourceReference(fullTargetDirectory, fullInventoryPath),
            Dispositions = CreateSourceReference(
                fullTargetDirectory,
                Path.Combine(fullDispositionDirectory, "manifest.json")),
            InteropAssembly = CreateSourceReference(fullTargetDirectory, interopAssemblyPath),
            InteropPublicApi = CreateSourceReference(fullTargetDirectory, interopApiPath),
            Review = CreateSourceReference(fullTargetDirectory, reviewPath),
            ValueFamilies = families,
            Bindings = bindings
        };
        var registryText = Serialize(registry);
        var reportText = CreateReport(registry);
        return new SdkBindingRegistryBuild(registry, registryText, reportText);
    }

    private static void ValidateReview(SdkBindingReview review, string target)
    {
        if (!string.Equals(review.Schema, ReviewSchemaReference, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"review.json $schema must be '{ReviewSchemaReference}'.");
        }

        if (review.SchemaVersion != 1)
        {
            throw new InvalidDataException("review.json schema_version must be 1.");
        }

        if (!string.Equals(review.SpatialAnalyzerTarget, target, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"review.json target '{review.SpatialAnalyzerTarget}' does not match '{target}'.");
        }

        if (review.DecisionReferences.Count == 0 ||
            review.DecisionReferences.Any(reference => !DecisionReference().IsMatch(reference)))
        {
            throw new InvalidDataException(
                "review.json requires canonical spatialanalyzer GitHub issue or PR decision references.");
        }

        if (!DecisionReference().IsMatch(review.MissingInteropBlocker))
        {
            throw new InvalidDataException(
                "review.json missing_interop_blocker must be a canonical spatialanalyzer " +
                "GitHub issue or PR reference.");
        }

        RequireSortedUnique(review.DecisionReferences, "decision_references");
        foreach (var coverage in CoverageLists(review))
        {
            RequireSortedUnique(
                coverage.Methods,
                $"implemented_coverage.{coverage.Component}");
        }
        var familyIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var pair in review.Families)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
            {
                throw new InvalidDataException("review.json family core names cannot be empty.");
            }

            var family = pair.Value;
            if (!Identifier().IsMatch(family.FamilyId))
            {
                throw new InvalidDataException(
                    $"Family '{pair.Key}' has invalid family_id '{family.FamilyId}'.");
            }

            if (!familyIds.Add(family.FamilyId))
            {
                throw new InvalidDataException(
                    $"Duplicate semantic family_id '{family.FamilyId}'.");
            }

            if (!Shapes.Contains(family.Shape, StringComparer.Ordinal))
            {
                throw new InvalidDataException(
                    $"Family '{pair.Key}' has unknown shape '{family.Shape}'.");
            }

            if (string.IsNullOrWhiteSpace(family.PublicTypeTarget) ||
                string.IsNullOrWhiteSpace(family.WorkerTypeTarget))
            {
                throw new InvalidDataException(
                    $"Family '{pair.Key}' requires public and worker type targets.");
            }
        }

    }

    private static Dictionary<string, CommandDispositionEntry> ReadDispositions(
        string dispositionDirectory,
        string target)
    {
        var manifestPath = Path.Combine(dispositionDirectory, "manifest.json");
        var manifest = ReadRequired<CommandDispositionManifest>(manifestPath);
        if (!string.Equals(manifest.SpatialAnalyzerTarget, target, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Disposition target '{manifest.SpatialAnalyzerTarget}' does not match '{target}'.");
        }

        var result = new Dictionary<string, CommandDispositionEntry>(StringComparer.Ordinal);
        foreach (var shardReference in manifest.Shards)
        {
            var shardPath = Path.GetFullPath(Path.Combine(
                dispositionDirectory,
                shardReference.Path.Replace('/', Path.DirectorySeparatorChar)));
            if (!IsWithin(shardPath, dispositionDirectory))
            {
                throw new InvalidDataException(
                    $"Disposition shard '{shardReference.Path}' escapes its target directory.");
            }

            var shard = ReadRequired<CommandDispositionShard>(shardPath);
            foreach (var entry in shard.Entries)
            {
                if (!result.TryAdd(entry.InventoryKey, entry))
                {
                    throw new InvalidDataException(
                        $"Duplicate disposition inventory key '{entry.InventoryKey}'.");
                }
            }
        }

        return result;
    }

    private static Dictionary<string, SdkBindingInteropSignature> ReadInteropMethods(
        string assemblyPath)
    {
        var assembly = Assembly.LoadFrom(assemblyPath);
        var type = assembly.GetType(InteropTypeName, throwOnError: true) ??
            throw new InvalidDataException($"Interop type '{InteropTypeName}' was not found.");
        var result = new Dictionary<string, SdkBindingInteropSignature>(StringComparer.Ordinal);
        foreach (var method in type.GetMethods()
                     .Where(method => BindingMethod().IsMatch(method.Name))
                     .OrderBy(method => method.Name, StringComparer.Ordinal))
        {
            var signature = new SdkBindingInteropSignature
            {
                ReturnType = NormalizeClrType(method.ReturnType),
                Parameters = method.GetParameters()
                    .OrderBy(parameter => parameter.Position)
                    .Select(parameter => new SdkBindingInteropParameter
                    {
                        Position = parameter.Position,
                        Name = parameter.Name ?? string.Empty,
                        ClrType = NormalizeClrType(parameter.ParameterType),
                        Passing = parameter.ParameterType.IsByRef ? "ref" : "value"
                    })
                    .ToList()
            };
            if (!result.TryAdd(method.Name, signature))
            {
                throw new InvalidDataException(
                    $"Interop API contains overloaded argument method '{method.Name}', which " +
                    "requires an explicit registry identity design.");
            }
        }

        return result;
    }

    private static Dictionary<string, List<SdkBindingObservation>> ReadObservations(
        MpCommandInventory inventory)
    {
        var result = new Dictionary<string, List<SdkBindingObservation>>(StringComparer.Ordinal);
        foreach (var command in inventory.Commands.OrderBy(
                     command => command.InventoryKey,
                     StringComparer.Ordinal))
        {
            foreach (var argument in command.Arguments.OrderBy(
                         argument => argument.Ordinal ?? int.MaxValue))
            {
                AddObservation(
                    result,
                    command.InventoryKey,
                    argument,
                    "setter",
                    argument.SdkBinding.Setter);
                AddObservation(
                    result,
                    command.InventoryKey,
                    argument,
                    "getter",
                    argument.SdkBinding.Getter);
            }
        }

        return result;
    }

    private static void AddObservation(
        Dictionary<string, List<SdkBindingObservation>> result,
        string inventoryKey,
        MpCommandInventoryArgument argument,
        string direction,
        MpCommandInventoryBindingEvidence evidence)
    {
        if (!string.Equals(evidence.Status, "available", StringComparison.Ordinal))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(evidence.Method) ||
            !BindingMethod().IsMatch(evidence.Method))
        {
            throw new InvalidDataException(
                $"Inventory key '{inventoryKey}' has an invalid available {direction} method.");
        }

        if (!result.TryGetValue(evidence.Method, out var observations))
        {
            observations = [];
            result.Add(evidence.Method, observations);
        }

        observations.Add(new SdkBindingObservation(
            inventoryKey,
            argument.Ordinal,
            argument.MpName,
            direction));
    }

    private static SdkBindingInventoryUsage CreateUsage(
        List<SdkBindingObservation> observations,
        int excludedCommandCount)
    {
        var lines = observations
            .OrderBy(item => item.InventoryKey, StringComparer.Ordinal)
            .ThenBy(item => item.Ordinal)
            .ThenBy(item => item.Direction, StringComparer.Ordinal)
            .ThenBy(item => item.MpName, StringComparer.Ordinal)
            .Select(item => string.Join(
                '|',
                item.InventoryKey,
                item.Ordinal?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                item.Direction,
                item.MpName))
            .ToArray();
        var commandCount = observations
            .Select(item => item.InventoryKey)
            .Distinct(StringComparer.Ordinal)
            .Count();
        return new SdkBindingInventoryUsage
        {
            ArgumentCount = observations.Count,
            CommandCount = commandCount,
            ExcludedCommandCount = excludedCommandCount,
            RemainingCommandCount = commandCount - excludedCommandCount,
            Sha256 = Sha256(Encoding.UTF8.GetBytes(string.Join('\n', lines) + "\n"))
        };
    }

    private static string NormalizeClrType(Type type)
    {
        var normalized = type.IsByRef ? type.GetElementType() ?? type : type;
        if (normalized == typeof(bool))
        {
            return "boolean";
        }

        if (normalized == typeof(byte))
        {
            return "byte";
        }

        if (normalized == typeof(int))
        {
            return "int32";
        }

        if (normalized == typeof(double))
        {
            return "double";
        }

        if (normalized == typeof(string))
        {
            return "string";
        }

        if (normalized == typeof(object))
        {
            return "object";
        }

        return normalized.FullName ?? normalized.Name;
    }

    private static SdkBindingCoverage CreateCoverage(
        SdkBindingReview review,
        string method,
        string registryStatus) =>
        new()
        {
            Protocol = CoverageStatus(
                registryStatus,
                review.ImplementedCoverage.Protocol,
                method),
            Worker = CoverageStatus(
                registryStatus,
                review.ImplementedCoverage.Worker,
                method),
            Adapter = CoverageStatus(
                registryStatus,
                review.ImplementedCoverage.Adapter,
                method),
            Fake = CoverageStatus(
                registryStatus,
                review.ImplementedCoverage.Fake,
                method),
            Generator = CoverageStatus(
                registryStatus,
                review.ImplementedCoverage.Generator,
                method)
        };

    private static string CoverageStatus(
        string registryStatus,
        IReadOnlyCollection<string> implementedMethods,
        string method) =>
        registryStatus switch
        {
            "blocked_missing_interop" => "blocked",
            "excluded_only" or "unobserved_interop" => "not_required",
            _ when implementedMethods.Contains(method, StringComparer.Ordinal) => "implemented",
            _ => "planned"
        };

    private static void ValidateImplementedCoverage(
        SdkBindingReview review,
        IReadOnlyList<SdkBindingRegistryEntry> bindings)
    {
        var bindingsByMethod = bindings.ToDictionary(
            binding => binding.Method,
            StringComparer.Ordinal);
        foreach (var coverage in CoverageLists(review))
        {
            foreach (var method in coverage.Methods)
            {
                if (!bindingsByMethod.TryGetValue(method, out var binding))
                {
                    throw new InvalidDataException(
                        $"implemented_coverage.{coverage.Component} contains unknown method " +
                        $"'{method}'.");
                }

                if (!string.Equals(binding.RegistryStatus, "usable", StringComparison.Ordinal))
                {
                    throw new InvalidDataException(
                        $"Implemented {coverage.Component} binding '{method}' must have usable " +
                        "exact-target evidence.");
                }
            }
        }
    }

    private static IEnumerable<(string Component, List<string> Methods)> CoverageLists(
        SdkBindingReview review)
    {
        yield return ("adapter", review.ImplementedCoverage.Adapter);
        yield return ("fake", review.ImplementedCoverage.Fake);
        yield return ("generator", review.ImplementedCoverage.Generator);
        yield return ("protocol", review.ImplementedCoverage.Protocol);
        yield return ("worker", review.ImplementedCoverage.Worker);
    }

    private static List<SdkBindingValueFamily> CreateFamilies(
        SdkBindingReview review,
        IReadOnlyList<SdkBindingRegistryEntry> bindings)
    {
        var bindingsByFamily = bindings
            .GroupBy(binding => binding.SemanticValueFamily, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var actualCores = bindings
            .Select(binding => SemanticCore(binding.Method))
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);
        var unusedCores = review.Families.Keys
            .Where(core => !actualCores.Contains(core))
            .OrderBy(core => core, StringComparer.Ordinal)
            .ToArray();
        if (unusedCores.Length > 0)
        {
            throw new InvalidDataException(
                "review.json defines unused interop core(s): " + string.Join(", ", unusedCores));
        }

        return review.Families.Values
            .OrderBy(family => family.FamilyId, StringComparer.Ordinal)
            .Select(family =>
            {
                if (!bindingsByFamily.TryGetValue(family.FamilyId, out var familyBindings))
                {
                    throw new InvalidDataException(
                        $"Semantic family '{family.FamilyId}' has no binding methods.");
                }

                var usable = familyBindings
                    .Where(binding => string.Equals(
                        binding.RegistryStatus,
                        "usable",
                        StringComparison.Ordinal))
                    .ToArray();
                var implementationStatus = usable.Length > 0
                    ? usable.All(IsFullyImplemented)
                        ? "implemented"
                        : "planned"
                    : familyBindings.Any(binding => string.Equals(
                        binding.RegistryStatus,
                        "blocked_missing_interop",
                        StringComparison.Ordinal))
                        ? "blocked"
                        : "not_required";
                return new SdkBindingValueFamily
                {
                    FamilyId = family.FamilyId,
                    Shape = family.Shape,
                    PublicTypeTarget = family.PublicTypeTarget,
                    WorkerTypeTarget = family.WorkerTypeTarget,
                    DirectionSupport = familyBindings
                        .Select(binding => binding.Direction)
                        .Distinct(StringComparer.Ordinal)
                        .OrderBy(direction => direction, StringComparer.Ordinal)
                        .ToList(),
                    ImplementationStatus = implementationStatus,
                    BindingMethods = familyBindings
                        .Select(binding => binding.Method)
                        .OrderBy(method => method, StringComparer.Ordinal)
                        .ToList()
                };
            })
            .ToList();
    }

    private static bool IsFullyImplemented(SdkBindingRegistryEntry binding) =>
        string.Equals(binding.Coverage.Protocol, "implemented", StringComparison.Ordinal) &&
        string.Equals(binding.Coverage.Worker, "implemented", StringComparison.Ordinal) &&
        string.Equals(binding.Coverage.Adapter, "implemented", StringComparison.Ordinal) &&
        string.Equals(binding.Coverage.Fake, "implemented", StringComparison.Ordinal) &&
        string.Equals(binding.Coverage.Generator, "implemented", StringComparison.Ordinal);

    private static string SemanticCore(string methodName)
    {
        var core = methodName[3..];
        core = Regex.Replace(core, "Arg[0-9]*$", string.Empty, RegexOptions.CultureInvariant);
        return string.Equals(core, "BSPlineFitOptions", StringComparison.Ordinal)
            ? "BSplineFitOptions"
            : core;
    }

    private static string Rationale(string registryStatus) => registryStatus switch
    {
        "usable" =>
            "The exact interop signature and semantic value family are reviewed. " +
            "Operation approval remains a separate disposition decision.",
        "excluded_only" =>
            "The binding is observed only on commands intentionally excluded from Briosa.",
        "blocked_missing_interop" =>
            "The binding is emitted by exact-target View SDK Code but is absent from the " +
            "committed exact-target interop API.",
        "unobserved_interop" =>
            "The method is exposed by the exact-target interop API but was not observed in " +
            "the extracted command evidence.",
        _ => throw new InvalidDataException($"Unknown registry status '{registryStatus}'.")
    };

    private static string CreateReport(SdkBindingRegistryDocument registry)
    {
        var builder = new StringBuilder();
        builder.AppendLine(
            CultureInfo.InvariantCulture,
            $"# SA {registry.SpatialAnalyzerTarget} SDK binding registry");
        builder.AppendLine();
        builder.AppendLine(
            "This deterministic report reconciles extracted View SDK Code evidence with the " +
            "committed exact-target interop API. A usable binding does not approve any MP " +
            "operation by itself.");
        builder.AppendLine();
        builder.AppendLine("## Coverage");
        builder.AppendLine();
        builder.AppendLine(
            CultureInfo.InvariantCulture,
            $"- Binding methods: {registry.Bindings.Count}");
        builder.AppendLine(
            CultureInfo.InvariantCulture,
            $"- Semantic value families: {registry.ValueFamilies.Count}");
        builder.AppendLine(
            CultureInfo.InvariantCulture,
            $"- Inventory-observed setters: {CountObserved(registry, "setter")}");
        builder.AppendLine(
            CultureInfo.InvariantCulture,
            $"- Inventory-observed getters: {CountObserved(registry, "getter")}");
        builder.AppendLine(
            CultureInfo.InvariantCulture,
            $"- Interop-exposed setters: {CountInterop(registry, "setter")}");
        builder.AppendLine(
            CultureInfo.InvariantCulture,
            $"- Interop-exposed getters: {CountInterop(registry, "getter")}");
        AppendCounts(builder, "Source status", registry.Bindings.Select(binding => binding.SourceStatus));
        AppendCounts(builder, "Registry status", registry.Bindings.Select(binding => binding.RegistryStatus));
        AppendCounts(
            builder,
            "Protocol coverage",
            registry.Bindings.Select(binding => binding.Coverage.Protocol));
        AppendCounts(
            builder,
            "Worker coverage",
            registry.Bindings.Select(binding => binding.Coverage.Worker));
        AppendCounts(
            builder,
            "Adapter coverage",
            registry.Bindings.Select(binding => binding.Coverage.Adapter));
        AppendCounts(
            builder,
            "Fake coverage",
            registry.Bindings.Select(binding => binding.Coverage.Fake));
        AppendCounts(
            builder,
            "Generator coverage",
            registry.Bindings.Select(binding => binding.Coverage.Generator));
        AppendCounts(
            builder,
            "Value-family implementation status",
            registry.ValueFamilies.Select(family => family.ImplementationStatus));

        builder.AppendLine();
        builder.AppendLine("## Semantic value families");
        builder.AppendLine();
        builder.AppendLine(
            "| Family | Shape | Directions | Public target | Worker target | Implementation | Bindings |");
        builder.AppendLine("| --- | --- | --- | --- | --- | --- | --- |");
        foreach (var family in registry.ValueFamilies)
        {
            builder.AppendLine(
                CultureInfo.InvariantCulture,
                $"| `{family.FamilyId}` | `{family.Shape}` | " +
                $"{EscapeMarkdown(string.Join(", ", family.DirectionSupport))} | " +
                $"`{family.PublicTypeTarget}` | `{family.WorkerTypeTarget}` | " +
                $"`{family.ImplementationStatus}` | " +
                $"{EscapeMarkdown(string.Join(", ", family.BindingMethods))} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Exact binding methods");
        builder.AppendLine();
        builder.AppendLine(
            "| Method | Direction | Sources | Status | Family | Protocol | Worker | Adapter | " +
            "Fake | Generator | Commands | Excluded | Remaining | Signature | Blocker |");
        builder.AppendLine(
            "| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | ---: | ---: | " +
            "---: | --- | --- |");
        foreach (var binding in registry.Bindings)
        {
            builder.AppendLine(
                CultureInfo.InvariantCulture,
                $"| `{binding.Method}` | `{binding.Direction}` | `{binding.SourceStatus}` | " +
                $"`{binding.RegistryStatus}` | `{binding.SemanticValueFamily}` | " +
                $"`{binding.Coverage.Protocol}` | `{binding.Coverage.Worker}` | " +
                $"`{binding.Coverage.Adapter}` | `{binding.Coverage.Fake}` | " +
                $"`{binding.Coverage.Generator}` | {binding.InventoryUsage.CommandCount} | " +
                $"{binding.InventoryUsage.ExcludedCommandCount} | " +
                $"{binding.InventoryUsage.RemainingCommandCount} | " +
                $"{EscapeMarkdown(Signature(binding.InteropSignature))} | " +
                $"{EscapeMarkdown(string.Join(", ", binding.BlockerReferences))} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Interpretation");
        builder.AppendLine();
        builder.AppendLine(
            "- `usable` means only that Briosa has a reviewed exact SDK call shape and semantic " +
            "family. The command disposition and supported catalog remain the public allowlist.");
        builder.AppendLine(
            "- `blocked_missing_interop` is not callable: generated sample code named a method " +
            "that the exact-target interop API does not expose.");
        builder.AppendLine(
            "- `unobserved_interop` methods are preserved for drift accounting but are not " +
            "implementation candidates without command evidence.");
        builder.AppendLine(
            "- `excluded_only` methods require no adapter solely for the current reviewed " +
            "product scope; mixed-use methods remain usable for their in-scope commands.");
        return builder.ToString().ReplaceLineEndings("\n");
    }

    private static int CountObserved(SdkBindingRegistryDocument registry, string direction) =>
        registry.Bindings.Count(binding =>
            string.Equals(binding.Direction, direction, StringComparison.Ordinal) &&
            !string.Equals(binding.SourceStatus, "interop_only", StringComparison.Ordinal));

    private static int CountInterop(SdkBindingRegistryDocument registry, string direction) =>
        registry.Bindings.Count(binding =>
            string.Equals(binding.Direction, direction, StringComparison.Ordinal) &&
            binding.InteropSignature is not null);

    private static void AppendCounts(
        StringBuilder builder,
        string heading,
        IEnumerable<string> values)
    {
        builder.AppendLine();
        builder.AppendLine(CultureInfo.InvariantCulture, $"## {heading}");
        builder.AppendLine();
        builder.AppendLine("| Value | Count |");
        builder.AppendLine("| --- | ---: |");
        foreach (var group in values
                     .GroupBy(value => value, StringComparer.Ordinal)
                     .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            builder.AppendLine(
                CultureInfo.InvariantCulture,
                $"| `{group.Key}` | {group.Count()} |");
        }
    }

    private static string Signature(SdkBindingInteropSignature? signature)
    {
        if (signature is null)
        {
            return "not present in interop";
        }

        var parameters = signature.Parameters.Select(parameter =>
            $"{parameter.Passing} {parameter.ClrType} {parameter.Name}");
        return $"{signature.ReturnType} ({string.Join(", ", parameters)})";
    }

    private static string EscapeMarkdown(string value) =>
        value.Replace("|", "\\|", StringComparison.Ordinal);

    private static SdkBindingSourceReference CreateSourceReference(
        string targetDirectory,
        string path) =>
        new()
        {
            Path = NormalizePath(Path.GetRelativePath(targetDirectory, path)),
            Sha256 = Sha256(File.ReadAllBytes(path))
        };

    private static T ReadRequired<T>(string path)
    {
        RequireFile(path);
        return JsonSerializer.Deserialize<T>(File.ReadAllText(path), ReadOptions) ??
            throw new InvalidDataException($"'{path}' did not contain a JSON object.");
    }

    private static void RequireFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new InvalidDataException($"Required binding registry input is missing: {path}");
        }
    }

    private static string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, WriteOptions).ReplaceLineEndings("\n") + "\n";

    private static void WriteText(string path, string content)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(
            path,
            content,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static void CompareFile(string path, string expected, List<string> errors)
    {
        if (!File.Exists(path))
        {
            errors.Add($"Generated binding registry artifact is missing: {path}");
            return;
        }

        var actual = File.ReadAllText(path);
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            errors.Add($"Generated binding registry artifact is stale: {path}");
        }
    }

    private static void RequireSortedUnique(IReadOnlyList<string> values, string field)
    {
        var expected = values.Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        if (!values.SequenceEqual(expected, StringComparer.Ordinal))
        {
            throw new InvalidDataException(
                $"review.json {field} must contain unique ordinally sorted values.");
        }
    }

    private static bool IsWithin(string path, string directory)
    {
        var fullPath = Path.GetFullPath(path);
        var fullDirectory = Path.GetFullPath(directory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
            Path.DirectorySeparatorChar;
        return fullPath.StartsWith(fullDirectory, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePath(string path) => path.Replace('\\', '/');

    private static string Sha256(byte[] bytes) =>
        Convert.ToHexStringLower(SHA256.HashData(bytes));

    [GeneratedRegex("^(?:Set|Get)[A-Za-z0-9]+Arg[0-9]*$", RegexOptions.CultureInvariant)]
    private static partial Regex BindingMethod();

    [GeneratedRegex("^[a-z][a-z0-9_]*$", RegexOptions.CultureInvariant)]
    private static partial Regex Identifier();

    [GeneratedRegex(
        "^https://github\\.com/spatialanalyzer/[A-Za-z0-9_.-]+/(?:issues|pull)/[1-9][0-9]*$",
        RegexOptions.CultureInvariant)]
    private static partial Regex DecisionReference();
}

internal sealed record SdkBindingRegistrySyncResult(
    IReadOnlyList<string> Files,
    int BindingCount,
    int ValueFamilyCount);

internal sealed record SdkBindingRegistryValidationResult(
    IReadOnlyList<string> Errors,
    int BindingCount,
    int ValueFamilyCount)
{
    public bool IsValid => Errors.Count == 0;
}

internal sealed record SdkBindingRegistryBuild(
    SdkBindingRegistryDocument Registry,
    string RegistryText,
    string ReportText);

internal sealed record SdkBindingObservation(
    string InventoryKey,
    int? Ordinal,
    string MpName,
    string Direction);

internal sealed class SdkBindingReview
{
    [JsonPropertyName("$schema")]
    [JsonRequired]
    public required string Schema { get; init; }

    [JsonRequired]
    public required int SchemaVersion { get; init; }

    [JsonRequired]
    public required string SpatialAnalyzerTarget { get; init; }

    [JsonRequired]
    public required List<string> DecisionReferences { get; init; }

    [JsonRequired]
    public required string MissingInteropBlocker { get; init; }

    [JsonRequired]
    public required SdkBindingCoverageReview ImplementedCoverage { get; init; }

    [JsonRequired]
    public required Dictionary<string, SdkBindingFamilyReview> Families { get; init; }
}

internal sealed class SdkBindingFamilyReview
{
    [JsonRequired]
    public required string FamilyId { get; init; }

    [JsonRequired]
    public required string Shape { get; init; }

    [JsonRequired]
    public required string PublicTypeTarget { get; init; }

    [JsonRequired]
    public required string WorkerTypeTarget { get; init; }
}

internal sealed class SdkBindingCoverageReview
{
    [JsonRequired]
    public required List<string> Protocol { get; init; }

    [JsonRequired]
    public required List<string> Worker { get; init; }

    [JsonRequired]
    public required List<string> Adapter { get; init; }

    [JsonRequired]
    public required List<string> Fake { get; init; }

    [JsonRequired]
    public required List<string> Generator { get; init; }
}

internal sealed class SdkBindingRegistryDocument
{
    [JsonPropertyName("$schema")]
    [JsonRequired]
    public required string Schema { get; init; }

    [JsonRequired]
    public required int SchemaVersion { get; init; }

    [JsonRequired]
    public required string SpatialAnalyzerTarget { get; init; }

    [JsonRequired]
    public required SdkBindingSourceReference Inventory { get; init; }

    [JsonRequired]
    public required SdkBindingSourceReference Dispositions { get; init; }

    [JsonRequired]
    public required SdkBindingSourceReference InteropAssembly { get; init; }

    [JsonRequired]
    public required SdkBindingSourceReference InteropPublicApi { get; init; }

    [JsonRequired]
    public required SdkBindingSourceReference Review { get; init; }

    [JsonRequired]
    public required List<SdkBindingValueFamily> ValueFamilies { get; init; }

    [JsonRequired]
    public required List<SdkBindingRegistryEntry> Bindings { get; init; }
}

internal sealed class SdkBindingSourceReference
{
    [JsonRequired]
    public required string Path { get; init; }

    [JsonRequired]
    public required string Sha256 { get; init; }
}

internal sealed class SdkBindingValueFamily
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
    public required List<string> DirectionSupport { get; init; }

    [JsonRequired]
    public required string ImplementationStatus { get; init; }

    [JsonRequired]
    public required List<string> BindingMethods { get; init; }
}

internal sealed class SdkBindingRegistryEntry
{
    [JsonRequired]
    public required string Method { get; init; }

    [JsonRequired]
    public required string Direction { get; init; }

    [JsonRequired]
    public required string SourceStatus { get; init; }

    [JsonRequired]
    public required string RegistryStatus { get; init; }

    [JsonRequired]
    public required string SemanticValueFamily { get; init; }

    [JsonRequired]
    public required SdkBindingCoverage Coverage { get; init; }

    public SdkBindingInteropSignature? InteropSignature { get; init; }

    [JsonRequired]
    public required SdkBindingInventoryUsage InventoryUsage { get; init; }

    [JsonRequired]
    public required string Rationale { get; init; }

    [JsonRequired]
    public required List<string> DecisionReferences { get; init; }

    [JsonRequired]
    public required List<string> BlockerReferences { get; init; }
}

internal sealed class SdkBindingCoverage
{
    [JsonRequired]
    public required string Protocol { get; init; }

    [JsonRequired]
    public required string Worker { get; init; }

    [JsonRequired]
    public required string Adapter { get; init; }

    [JsonRequired]
    public required string Fake { get; init; }

    [JsonRequired]
    public required string Generator { get; init; }
}

internal sealed class SdkBindingInteropSignature
{
    [JsonRequired]
    public required string ReturnType { get; init; }

    [JsonRequired]
    public required List<SdkBindingInteropParameter> Parameters { get; init; }
}

internal sealed class SdkBindingInteropParameter
{
    [JsonRequired]
    public required int Position { get; init; }

    [JsonRequired]
    public required string Name { get; init; }

    [JsonRequired]
    public required string ClrType { get; init; }

    [JsonRequired]
    public required string Passing { get; init; }
}

internal sealed class SdkBindingInventoryUsage
{
    [JsonRequired]
    public required int ArgumentCount { get; init; }

    [JsonRequired]
    public required int CommandCount { get; init; }

    [JsonRequired]
    public required int ExcludedCommandCount { get; init; }

    [JsonRequired]
    public required int RemainingCommandCount { get; init; }

    [JsonRequired]
    public required string Sha256 { get; init; }
}
