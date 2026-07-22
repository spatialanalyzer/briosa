using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Briosa.Generator;

internal sealed record CommandCatalogValidationResult(
    IReadOnlyList<string> Errors,
    int CatalogCount,
    int OperationCount)
{
    public bool IsValid => Errors.Count == 0;
}

internal static partial class CommandCatalogValidator
{
    private const string CatalogSchemaReference = "../../schemas/v1/catalog.schema.json";
    private const string OperationSchemaReference = "../../../schemas/v1/operation.schema.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static CommandCatalogValidationResult ValidateDirectory(string catalogRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogRoot);

        var root = Path.GetFullPath(catalogRoot);
        var errors = new List<string>();
        var saRoot = Path.Combine(root, "sa");
        if (!Directory.Exists(saRoot))
        {
            errors.Add("The catalog root must contain an 'sa' directory.");
            return new CommandCatalogValidationResult(errors, 0, 0);
        }

        var manifestPaths = Directory
            .EnumerateDirectories(saRoot)
            .Select(directory => Path.Combine(directory, "catalog.json"))
            .Where(File.Exists)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        if (manifestPaths.Length == 0)
        {
            errors.Add("The catalog root contains no exact-target catalog manifests.");
            return new CommandCatalogValidationResult(errors, 0, 0);
        }

        var operationCount = 0;
        foreach (var manifestPath in manifestPaths)
        {
            operationCount += ValidateManifest(root, manifestPath, errors);
        }

        return new CommandCatalogValidationResult(errors, manifestPaths.Length, operationCount);
    }

    private static int ValidateManifest(
        string catalogRoot,
        string manifestPath,
        List<string> errors)
    {
        var displayPath = DisplayPath(catalogRoot, manifestPath);
        var manifest = Deserialize<CommandCatalogManifest>(manifestPath, displayPath, errors);
        if (manifest is null)
        {
            return 0;
        }

        var targetDirectory = Path.GetDirectoryName(manifestPath) ??
            throw new InvalidOperationException("A catalog manifest must have a parent directory.");
        var target = Path.GetFileName(targetDirectory);

        RequireEqual(manifest.Schema, CatalogSchemaReference, displayPath, "$schema", errors);
        if (manifest.SchemaVersion != 1)
        {
            errors.Add($"{displayPath}: schema_version must be 1.");
        }

        RequireEqual(
            manifest.SpatialAnalyzerTarget,
            target,
            displayPath,
            "spatial_analyzer_target",
            errors);
        RequireEqual(
            manifest.CatalogId,
            $"briosa.sa.{target}",
            displayPath,
            "catalog_id",
            errors);
        RequireEqual(
            manifest.TargetProtocolPackage,
            $"briosa.sa.v{target.Replace('.', '_')}.v1alpha1",
            displayPath,
            "target_protocol_package",
            errors);

        if (!int.TryParse(
                manifest.CatalogRevision,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var revision) || revision < 1)
        {
            errors.Add($"{displayPath}: catalog_revision must be a positive integer string.");
        }

        var sourceIds = new HashSet<string>(StringComparer.Ordinal);
        var sourceKinds = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var source in manifest.Sources)
        {
            if (!sourceIds.Add(source.SourceId))
            {
                errors.Add($"{displayPath}: duplicate source_id '{source.SourceId}'.");
            }
            else
            {
                sourceKinds.Add(source.SourceId, source.Kind);
            }
        }

        RequireSorted(manifest.OperationFiles, displayPath, "operation_files", errors);
        var operationsDirectory = Path.Combine(targetDirectory, "operations");
        var actualOperationFiles = Directory.Exists(operationsDirectory)
            ? Directory
                .EnumerateFiles(operationsDirectory, "*.json")
                .Select(path => NormalizePath(Path.GetRelativePath(targetDirectory, path)))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray()
            : [];
        var listedOperationFiles = manifest.OperationFiles
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        if (!actualOperationFiles.SequenceEqual(listedOperationFiles, StringComparer.Ordinal))
        {
            errors.Add(
                $"{displayPath}: operation_files must list every operations/*.json file exactly once.");
        }

        var operationIds = new HashSet<string>(StringComparer.Ordinal);
        var operationCount = 0;
        foreach (var relativeOperationPath in manifest.OperationFiles)
        {
            var operationPath = Path.GetFullPath(
                Path.Combine(targetDirectory, relativeOperationPath.Replace('/', Path.DirectorySeparatorChar)));
            if (!IsWithin(operationPath, targetDirectory))
            {
                errors.Add($"{displayPath}: operation path '{relativeOperationPath}' escapes its target directory.");
                continue;
            }

            var operationDisplayPath = DisplayPath(catalogRoot, operationPath);
            if (!File.Exists(operationPath))
            {
                errors.Add($"{displayPath}: operation file '{relativeOperationPath}' does not exist.");
                continue;
            }

            var operation = Deserialize<CommandCatalogOperation>(
                operationPath,
                operationDisplayPath,
                errors);
            if (operation is null)
            {
                continue;
            }

            operationCount++;
            if (!operationIds.Add(operation.OperationId))
            {
                errors.Add($"{operationDisplayPath}: duplicate operation_id '{operation.OperationId}'.");
            }

            ValidateOperation(
                operation,
                relativeOperationPath,
                operationDisplayPath,
                sourceKinds,
                errors);
        }

        return operationCount;
    }

    private static void ValidateOperation(
        CommandCatalogOperation operation,
        string relativeOperationPath,
        string displayPath,
        Dictionary<string, string> sourceKinds,
        List<string> errors)
    {
        RequireEqual(operation.Schema, OperationSchemaReference, displayPath, "$schema", errors);

        var expectedPath = $"operations/{operation.OperationId}.json";
        RequireEqual(relativeOperationPath, expectedPath, displayPath, "operation file path", errors);

        var identitySegments = operation.OperationId.Split('.');
        if (identitySegments.Length != 2 || !SnakeCaseIdentifier().IsMatch(identitySegments[0]) ||
            !SnakeCaseIdentifier().IsMatch(identitySegments[1]))
        {
            errors.Add(
                $"{displayPath}: operation_id must contain exactly two lower_snake_case segments.");
        }
        else
        {
            var expectedService = ToPascalCase(identitySegments[0]);
            var expectedRpc = ToPascalCase(identitySegments[1]);
            RequireEqual(operation.Protocol.Service, expectedService, displayPath, "protocol.service", errors);
            RequireEqual(operation.Protocol.Rpc, expectedRpc, displayPath, "protocol.rpc", errors);
            RequireEqual(
                operation.Protocol.Request,
                $"{expectedRpc}Request",
                displayPath,
                "protocol.request",
                errors);
            RequireEqual(
                operation.Protocol.Result,
                $"{expectedRpc}Result",
                displayPath,
                "protocol.result",
                errors);
        }

        var isDeprecated = string.Equals(operation.Deprecation.Status, "deprecated", StringComparison.Ordinal);
        if (isDeprecated != string.Equals(operation.Stability, "deprecated", StringComparison.Ordinal))
        {
            errors.Add(
                $"{displayPath}: stability and deprecation.status must agree on deprecation.");
        }

        if (isDeprecated && string.IsNullOrWhiteSpace(operation.Deprecation.Reason))
        {
            errors.Add($"{displayPath}: deprecated operations require a deprecation reason.");
        }

        if (string.Equals(operation.Risk.Effect, "unknown", StringComparison.Ordinal) ||
            operation.Risk.Flags.Contains("unknown", StringComparer.Ordinal))
        {
            errors.Add($"{displayPath}: supported operations cannot retain unknown risk metadata.");
        }

        var argumentIds = new HashSet<string>(StringComparer.Ordinal);
        var ordinals = new HashSet<int>();
        var expectedOrdinalOrder = operation.Arguments
            .Select(argument => argument.Ordinal)
            .OrderBy(ordinal => ordinal)
            .ToArray();
        if (!operation.Arguments.Select(argument => argument.Ordinal).SequenceEqual(expectedOrdinalOrder))
        {
            errors.Add($"{displayPath}: arguments must be ordered by ordinal.");
        }

        foreach (var argument in operation.Arguments)
        {
            if (!argumentIds.Add(argument.ArgumentId))
            {
                errors.Add($"{displayPath}: duplicate argument_id '{argument.ArgumentId}'.");
            }

            if (!ordinals.Add(argument.Ordinal))
            {
                errors.Add($"{displayPath}: duplicate argument ordinal {argument.Ordinal}.");
            }

            ValidateArgument(argument, displayPath, errors);
        }

        var hasMaintainerReview = false;
        foreach (var evidence in operation.Evidence)
        {
            if (!sourceKinds.TryGetValue(evidence.SourceId, out var sourceKind))
            {
                errors.Add(
                    $"{displayPath}: evidence references unknown source_id '{evidence.SourceId}'.");
            }
            else if (string.Equals(sourceKind, "maintainer_review", StringComparison.Ordinal))
            {
                hasMaintainerReview = true;
            }
        }

        if (!hasMaintainerReview)
        {
            errors.Add($"{displayPath}: supported operations require maintainer-review evidence.");
        }
    }

    private static void ValidateArgument(
        CommandCatalogArgument argument,
        string displayPath,
        List<string> errors)
    {
        var argumentPath = $"{displayPath}: argument '{argument.ArgumentId}'";
        if (string.Equals(argument.Direction, "unknown", StringComparison.Ordinal))
        {
            errors.Add($"{argumentPath} has unknown direction.");
            return;
        }

        if (string.Equals(argument.ResultOnly, "unknown", StringComparison.Ordinal))
        {
            errors.Add($"{argumentPath} has unknown result_only status.");
        }

        if (string.Equals(argument.SemanticType, "unknown", StringComparison.Ordinal))
        {
            errors.Add($"{argumentPath} has unknown semantic_type.");
        }

        var isInput = string.Equals(argument.Direction, "input", StringComparison.Ordinal) ||
            string.Equals(argument.Direction, "input_output", StringComparison.Ordinal);
        var isOutput = string.Equals(argument.Direction, "output", StringComparison.Ordinal) ||
            string.Equals(argument.Direction, "input_output", StringComparison.Ordinal);
        var shouldBeResultOnly = string.Equals(argument.Direction, "output", StringComparison.Ordinal);
        if (shouldBeResultOnly != string.Equals(argument.ResultOnly, "yes", StringComparison.Ordinal))
        {
            errors.Add($"{argumentPath} has result_only inconsistent with direction.");
        }

        if (!string.Equals(argument.SdkBinding.Status, "available", StringComparison.Ordinal))
        {
            errors.Add($"{argumentPath} must have available SDK binding metadata.");
        }

        ValidateBinding(argument.SdkBinding.Setter, isInput, "setter", argumentPath, errors);
        ValidateBinding(argument.SdkBinding.Getter, isOutput, "getter", argumentPath, errors);

        if (isInput)
        {
            if (argument.Input is null)
            {
                errors.Add($"{argumentPath} requires input metadata.");
            }
            else
            {
                ValidateInput(argument.Input, argumentPath, errors);
            }
        }
        else if (argument.Input is not null)
        {
            errors.Add($"{argumentPath} cannot define input metadata for an output-only argument.");
        }
    }

    private static void ValidateBinding(
        string? binding,
        bool required,
        string bindingKind,
        string argumentPath,
        List<string> errors)
    {
        if (required)
        {
            if (string.IsNullOrWhiteSpace(binding))
            {
                errors.Add($"{argumentPath} requires an SDK {bindingKind}.");
            }
            else if (!SdkBindingName().IsMatch(binding))
            {
                errors.Add($"{argumentPath} has invalid SDK {bindingKind} '{binding}'.");
            }
        }
        else if (binding is not null)
        {
            errors.Add($"{argumentPath} must set unused SDK {bindingKind} metadata to null.");
        }
    }

    private static void ValidateInput(
        CommandCatalogInputMetadata input,
        string argumentPath,
        List<string> errors)
    {
        if (string.Equals(input.Presence, "unknown", StringComparison.Ordinal))
        {
            errors.Add($"{argumentPath} has unknown input presence.");
        }

        if (string.Equals(input.OmissionBehavior, "unknown", StringComparison.Ordinal))
        {
            errors.Add($"{argumentPath} has unknown omission_behavior.");
        }

        if (string.Equals(input.Default.Status, "unknown", StringComparison.Ordinal))
        {
            errors.Add($"{argumentPath} has unknown default status.");
        }

        var requiresValue = string.Equals(input.Default.Status, "reviewed", StringComparison.Ordinal) ||
            string.Equals(input.Default.Status, "generated_sample", StringComparison.Ordinal);
        if (requiresValue != input.Default.Value.HasValue)
        {
            errors.Add(
                $"{argumentPath} default value presence is inconsistent with default status.");
        }

        if (string.Equals(input.Presence, "required", StringComparison.Ordinal) &&
            !string.Equals(input.OmissionBehavior, "reject_request", StringComparison.Ordinal))
        {
            errors.Add($"{argumentPath} required inputs must reject omission.");
        }

        if (string.Equals(input.Presence, "optional", StringComparison.Ordinal) &&
            string.Equals(input.OmissionBehavior, "reject_request", StringComparison.Ordinal))
        {
            errors.Add($"{argumentPath} optional inputs cannot reject omission.");
        }

        if (string.Equals(input.OmissionBehavior, "set_catalog_default", StringComparison.Ordinal) &&
            !string.Equals(input.Default.Status, "reviewed", StringComparison.Ordinal))
        {
            errors.Add(
                $"{argumentPath} can use a catalog default only when that default is reviewed.");
        }
    }

    private static T? Deserialize<T>(
        string path,
        string displayPath,
        List<string> errors)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions);
        }
        catch (JsonException exception)
        {
            errors.Add($"{displayPath}: invalid JSON: {exception.Message}");
            return default;
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
            errors.Add($"{displayPath}: {property} must be '{expected}', not '{actual}'.");
        }
    }

    private static void RequireSorted(
        IReadOnlyList<string> values,
        string displayPath,
        string property,
        List<string> errors)
    {
        if (!values.SequenceEqual(values.OrderBy(value => value, StringComparer.Ordinal)))
        {
            errors.Add($"{displayPath}: {property} must use ordinal sort order.");
        }
    }

    private static bool IsWithin(string path, string directory)
    {
        var directoryPrefix = Path.GetFullPath(directory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
            Path.DirectorySeparatorChar;
        return path.StartsWith(directoryPrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static string DisplayPath(string catalogRoot, string path) =>
        NormalizePath(Path.GetRelativePath(catalogRoot, path));

    private static string NormalizePath(string path) => path.Replace('\\', '/');

    private static string ToPascalCase(string value) => string.Concat(
        value.Split('_', StringSplitOptions.RemoveEmptyEntries)
            .Select(segment => $"{char.ToUpperInvariant(segment[0])}{segment[1..]}"));

    [GeneratedRegex("^[a-z][a-z0-9_]*$")]
    private static partial Regex SnakeCaseIdentifier();

    [GeneratedRegex("^(?:Set|Get)[A-Za-z0-9]+Arg[0-9]*$")]
    private static partial Regex SdkBindingName();
}
