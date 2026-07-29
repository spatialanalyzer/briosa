using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Briosa.Generator;

internal sealed record CommandCatalogGenerationResult(IReadOnlyList<string> Files);

internal static class CommandCatalogGenerator
{
    internal const string GeneratedArtifactIdentity = "Briosa.Generator command catalog";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static CommandCatalogGenerationResult Generate(
        string catalogRoot,
        string outputRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputRoot);

        var validation = CommandCatalogValidator.ValidateDirectory(catalogRoot);
        if (!validation.IsValid)
        {
            throw new InvalidDataException(
                $"Catalog generation requires a valid catalog: {string.Join(" ", validation.Errors)}");
        }

        var fullCatalogRoot = Path.GetFullPath(catalogRoot);
        var fullOutputRoot = Path.GetFullPath(outputRoot);
        var generatedFiles = new List<string>();
        var generatedServices = new List<(string GeneratedNamespace, string Service)>();
        foreach (var manifestPath in Directory
            .EnumerateFiles(
                Path.Combine(fullCatalogRoot, "sa"),
                "catalog.json",
                SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal))
        {
            var manifest = Deserialize<CommandCatalogManifest>(manifestPath);
            var targetDirectory = Path.GetDirectoryName(manifestPath) ??
                throw new InvalidDataException("A catalog manifest has no parent directory.");
            var operations = manifest.OperationFiles
                .Select(path => Deserialize<CommandCatalogOperation>(
                    Path.Combine(
                        targetDirectory,
                        path.Replace('/', Path.DirectorySeparatorChar))))
                .OrderBy(operation => operation.OperationId, StringComparer.Ordinal)
                .ToArray();
            var releaseMemberships = manifest.ReleaseMembershipFiles
                .Select(path => Deserialize<CommandCatalogReleaseMembership>(
                    Path.Combine(
                        targetDirectory,
                        path.Replace('/', Path.DirectorySeparatorChar))))
                .OrderBy(membership => membership.MembershipId, StringComparer.Ordinal)
                .ToArray();

            var packagePath = manifest.TargetProtocolPackage.Replace('.', '/');
            foreach (var partition in manifest.ProtocolPartitions
                         .OrderBy(partition => partition.Alias, StringComparer.Ordinal))
            {
                var partitionOperations = operations
                    .Where(operation => string.Equals(
                        operation.OperationId.Split('.')[0],
                        partition.Alias,
                        StringComparison.Ordinal))
                    .OrderBy(operation => operation.Protocol.Rpc, StringComparer.Ordinal)
                    .ToArray();
                WriteGeneratedFile(
                    fullOutputRoot,
                    $"proto/{packagePath}/{partition.ProtoFile}",
                    GenerateProto(manifest, partition, partitionOperations),
                    generatedFiles);
            }

            var targetNamespace = ToCSharpNamespace(manifest.TargetProtocolPackage);
            var generatedNamespace = $"Briosa.Server.Generated.{targetNamespace["Briosa.".Length..]}";
            generatedServices.AddRange(manifest.ProtocolPartitions.Select(partition =>
                (generatedNamespace, partition.Service)));
            WriteGeneratedFile(
                fullOutputRoot,
                $"src/Briosa.Server/Generated/{targetNamespace["Briosa.".Length..].Replace('.', '/')}/Operations.g.cs",
                CommandCatalogArtifactGenerator.GenerateServerOperations(
                    generatedNamespace,
                    targetNamespace,
                    manifest,
                    operations),
                generatedFiles);
            WriteGeneratedFile(
                fullOutputRoot,
                $"docs/reference/generated/sa/{manifest.SpatialAnalyzerTarget}/operations.md",
                CommandCatalogArtifactGenerator.GenerateDocumentation(
                    manifest,
                    operations,
                    releaseMemberships),
                generatedFiles);
            WriteGeneratedFile(
                fullOutputRoot,
                $"generated/catalog/sa/{manifest.SpatialAnalyzerTarget}/coverage.json",
                CommandCatalogArtifactGenerator.GenerateCoverageManifest(
                    manifest,
                    operations,
                    releaseMemberships),
                generatedFiles);
        }

        WriteGeneratedFile(
            fullOutputRoot,
            "src/Briosa.Server/Generated/CatalogServiceRegistration.g.cs",
            CommandCatalogArtifactGenerator.GenerateServiceRegistration(generatedServices),
            generatedFiles);

        return new CommandCatalogGenerationResult(generatedFiles);
    }

    private static string GenerateProto(
        CommandCatalogManifest manifest,
        CommandCatalogProtocolPartition partition,
        IReadOnlyList<CommandCatalogOperation> operations)
    {
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("// Generated from the reviewed Briosa command catalog. Do not edit by hand.");
        builder.AppendLine("syntax = \"proto3\";");
        builder.AppendLine();
        builder.Append("package ").Append(manifest.TargetProtocolPackage).AppendLine(";");
        builder.AppendLine();
        builder.AppendLine("import \"briosa/core/v1alpha1/operation_outcomes.proto\";");
        var semanticTypes = operations
            .SelectMany(operation => operation.Arguments)
            .Select(argument => argument.SemanticType)
            .ToArray();
        var targetPath = manifest.TargetProtocolPackage.Replace('.', '/');
        if (semanticTypes.Any(semanticType =>
                IsMessageType(semanticType) &&
                !SpecializedValueMappings.IsStructured(semanticType)))
        {
            builder.Append("import \"")
                .Append(targetPath)
                .AppendLine("/values.proto\";");
        }

        if (semanticTypes.Any(SpecializedValueMappings.IsSupported))
        {
            builder.Append("import \"")
                .Append(targetPath)
                .AppendLine("/specialized_values.proto\";");
        }

        builder.AppendLine();
        builder.Append("option csharp_namespace = \"")
            .Append(ToCSharpNamespace(manifest.TargetProtocolPackage))
            .AppendLine("\";");

        builder.AppendLine();
        builder.Append("service ").Append(partition.Service).AppendLine(" {");
        foreach (var operation in operations)
        {
            AppendComment(builder, operation.Documentation.Summary, indentation: "  ");
            builder.Append("  rpc ")
                .Append(operation.Protocol.Rpc)
                .Append('(')
                .Append(operation.Protocol.Request)
                .Append(") returns (")
                .Append(operation.Protocol.Result)
                .AppendLine(");");
        }

        builder.AppendLine("}");

        foreach (var operation in operations)
        {
            builder.AppendLine();
            AppendMessage(
                builder,
                operation.Protocol.Request,
                [.. operation.Arguments.Where(IsInput)]);
            builder.AppendLine();
            AppendResultMessage(
                builder,
                operation.Protocol.Result,
                [.. operation.Arguments.Where(IsOutput)]);
        }

        return builder.ToString();
    }

    private static void AppendMessage(
        StringBuilder builder,
        string name,
        IReadOnlyList<CommandCatalogArgument> arguments)
    {
        if (arguments.Count == 0)
        {
            builder.Append("message ").Append(name).AppendLine(" {}");
            return;
        }

        builder.Append("message ").Append(name).AppendLine(" {");
        foreach (var argument in arguments.OrderBy(argument => argument.Ordinal))
        {
            AppendComment(builder, argument.Documentation, indentation: "  ");
            builder.Append("  ");
            if (!IsMessageType(argument.SemanticType))
            {
                builder.Append("optional ");
            }

            builder.Append(ToProtoType(argument.SemanticType))
                .Append(' ')
                .Append(argument.ArgumentId)
                .Append(" = ")
                .Append(argument.FieldNumbers.Request!.Value.ToString(CultureInfo.InvariantCulture))
                .AppendLine(";");
        }

        builder.AppendLine("}");
    }

    private static void AppendResultMessage(
        StringBuilder builder,
        string name,
        IReadOnlyList<CommandCatalogArgument> arguments)
    {
        builder.Append("message ").Append(name).AppendLine(" {");
        foreach (var argument in arguments.OrderBy(argument => argument.Ordinal))
        {
            AppendComment(builder, argument.Documentation, indentation: "  ");
            builder.Append("  ");
            if (!IsMessageType(argument.SemanticType))
            {
                builder.Append("optional ");
            }

            builder.Append(ToProtoType(argument.SemanticType))
                .Append(' ')
                .Append(argument.ArgumentId)
                .Append(" = ")
                .Append(argument.FieldNumbers.Result!.Value.ToString(CultureInfo.InvariantCulture))
                .AppendLine(";");
        }

        builder.AppendLine("  // Explicit MP and result-only argument retrieval outcome.");
        builder.AppendLine("  briosa.core.v1alpha1.MpExecutionDetails execution = 1000;");
        builder.AppendLine("}");
    }

    private static void AppendComment(StringBuilder builder, string value, string indentation)
    {
        foreach (var line in value.ReplaceLineEndings("\n").Split('\n'))
        {
            builder.Append(indentation).Append("// ").AppendLine(line.Trim());
        }
    }

    private static bool IsInput(CommandCatalogArgument argument) =>
        argument.Direction is "input" or "input_output";

    private static bool IsOutput(CommandCatalogArgument argument) =>
        argument.Direction is "output" or "input_output";

    private static bool IsMessageType(string semanticType) =>
        semanticType is
            "double_array" or
            "edit_text" or
            "transform" or
            "world_transform" or
            "rgb_color" or
            "file_reference" or
            "font" or
            "point_name" or
            "vector" or
            "tolerance_vector_options" or
            "collection_group_name_list" or
            "collection_instrument_id" or
            "collection_instrument_id_list" or
            "collection_machine_id" or
            "collection_item_name" or
            "collection_item_name_list" or
            "collection_object_name" or
            "collection_object_name_list" or
            "collection_vector_group_name" or
            "collection_vector_group_name_list" or
            "point_name_list" or
            "string_list" or
            "vector_name_list" ||
        SpecializedValueMappings.IsStructured(semanticType);

    private static string ToProtoType(string semanticType) =>
        semanticType switch
        {
            "logical" => "bool",
            "whole_number" => "int32",
            "floating_point" => "double",
            "string" or
            "chart_name" or
            "cloud_name" or
            "collection_name" or
            "frame_name" or
            "vector_group_name" or
            "view_name" => "string",
            "angular_unit" => "AngularUnit",
            "distance_unit" => "DistanceUnit",
            "temperature_unit" => "TemperatureUnit",
            "double_array" => "DoubleArray",
            "edit_text" => "StringList",
            "transform" => "Transform",
            "world_transform" => "WorldTransform",
            "rgb_color" => "RgbColor",
            "file_reference" => "FileReference",
            "font" => "Font",
            "point_name" => "PointName",
            "vector" => "Vector3",
            "tolerance_vector_options" => "ToleranceVectorOptions",
            "collection_group_name_list" => "CollectionGroupNameList",
            "collection_instrument_id" => "CollectionInstrumentId",
            "collection_instrument_id_list" => "CollectionInstrumentIdList",
            "collection_machine_id" => "CollectionMachineId",
            "collection_item_name" => "CollectionItemName",
            "collection_item_name_list" => "CollectionItemNameList",
            "collection_object_name" => "CollectionObjectName",
            "collection_object_name_list" => "CollectionObjectNameList",
            "collection_vector_group_name" => "CollectionVectorGroupName",
            "collection_vector_group_name_list" => "CollectionVectorGroupNameList",
            "point_name_list" => "PointNameList",
            "string_list" => "StringList",
            "vector_name_list" => "VectorNameList",
            _ => SpecializedValueMappings.ToTypeName(semanticType)
        };

    private static string ToCSharpNamespace(string package)
    {
        var segments = package.Split('.');
        return string.Join('.', segments.Select(segment => segment switch
        {
            "briosa" => "Briosa",
            "sa" => "Sa",
            var version when version.StartsWith('v') => ToVersionNamespaceSegment(version),
            _ => ToPascalCase(segment)
        }));
    }

    private static string ToVersionNamespaceSegment(string value)
    {
        var result = char.ToUpperInvariant(value[0]) + value[1..];
        result = result.Replace("alpha", "Alpha", StringComparison.Ordinal);
        return result.Replace("beta", "Beta", StringComparison.Ordinal);
    }

    private static string ToPascalCase(string value) =>
        string.Concat(value
            .Split('_', StringSplitOptions.RemoveEmptyEntries)
            .Select(segment => char.ToUpperInvariant(segment[0]) + segment[1..]));

    private static string EscapeCSharp(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);

    private static T Deserialize<T>(string path) =>
        JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions) ??
            throw new InvalidDataException($"Catalog document '{path}' was empty.");

    private static void WriteGeneratedFile(
        string outputRoot,
        string relativePath,
        string content,
        List<string> generatedFiles)
    {
        var normalizedPath = relativePath.Replace('\\', '/');
        var outputPath = Path.Combine(
            outputRoot,
            normalizedPath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        if (File.Exists(outputPath) &&
            !IsCatalogGeneratedArtifact(File.ReadAllText(outputPath)))
        {
            throw new InvalidDataException(
                $"Refusing to overwrite non-catalog-generated file '{normalizedPath}'.");
        }

        File.WriteAllText(
            outputPath,
            content.ReplaceLineEndings("\n"),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        generatedFiles.Add(normalizedPath);
    }

    private static bool IsCatalogGeneratedArtifact(string content)
    {
        var normalizedContent = content.ReplaceLineEndings("\n");
        if (normalizedContent.StartsWith(
                "// <auto-generated />\n// Generated from the reviewed Briosa command catalog.",
                StringComparison.Ordinal) ||
            normalizedContent.StartsWith(
                "<!-- <auto-generated /> -->\n<!-- Generated from the reviewed Briosa command catalog.",
                StringComparison.Ordinal))
        {
            return true;
        }

        try
        {
            using var document = JsonDocument.Parse(normalizedContent);
            return document.RootElement.TryGetProperty("generated_by", out var generatedBy) &&
                string.Equals(
                    generatedBy.GetString(),
                    GeneratedArtifactIdentity,
                    StringComparison.Ordinal);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
