using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Briosa.Generator;

internal sealed record PortableConformanceGenerationResult(IReadOnlyList<string> Files);

internal static class PortableConformanceGenerator
{
    internal const string GeneratedArtifactIdentity =
        "Briosa.Generator portable conformance";

    private const string ManifestSchemaReference =
        "../../../../conformance/schemas/v1/manifest.schema.json";

    private static readonly JsonSerializerOptions CatalogJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    private static readonly JsonSerializerOptions OutputJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true
    };

    public static PortableConformanceGenerationResult Generate(
        string repositoryRoot,
        string outputRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputRoot);

        var root = Path.GetFullPath(repositoryRoot);
        var catalogRoot = Path.Combine(root, "catalog");
        var validation = CommandCatalogValidator.ValidateDirectory(catalogRoot);
        if (!validation.IsValid)
        {
            throw new InvalidDataException(
                "Portable conformance generation requires a valid catalog: " +
                string.Join(" ", validation.Errors));
        }

        var files = new List<string>();
        foreach (var manifestPath in Directory
                     .EnumerateFiles(
                         Path.Combine(catalogRoot, "sa"),
                         "catalog.json",
                         SearchOption.AllDirectories)
                     .OrderBy(path => path, StringComparer.Ordinal))
        {
            var catalog = Deserialize<CommandCatalogManifest>(manifestPath);
            var targetRoot = Path.GetDirectoryName(manifestPath) ??
                throw new InvalidDataException("A catalog manifest has no parent directory.");
            var operations = catalog.OperationFiles
                .Select(path => Deserialize<CommandCatalogOperation>(Path.Combine(
                    targetRoot,
                    path.Replace('/', Path.DirectorySeparatorChar))))
                .OrderBy(operation => operation.OperationId, StringComparer.Ordinal)
                .ToArray();
            var target = catalog.SpatialAnalyzerTarget;
            var registryPath = Path.Combine(root, "bindings", "sa", target, "registry.json");
            var reviewPath = Path.Combine(root, "bindings", "sa", target, "review.json");
            var valuesPath = Path.Combine(root, "values", "sa", target, "catalog.json");
            var evidence = LoadEvidence(target, registryPath, reviewPath, valuesPath);
            ValidateEvidence(catalog, operations, evidence);

            var document = CreateManifest(
                root,
                manifestPath,
                catalog,
                operations,
                registryPath,
                reviewPath,
                valuesPath,
                evidence);
            var relativePath =
                $"generated/conformance/sa/{target}/manifest.json";
            WriteGeneratedFile(
                Path.GetFullPath(outputRoot),
                relativePath,
                JsonSerializer.Serialize(document, OutputJsonOptions) + "\n");
            files.Add(relativePath);
        }

        return new PortableConformanceGenerationResult(files);
    }

    private static PortableConformanceManifest CreateManifest(
        string repositoryRoot,
        string catalogManifestPath,
        CommandCatalogManifest catalog,
        IReadOnlyList<CommandCatalogOperation> operations,
        string registryPath,
        string reviewPath,
        string valuesPath,
        ConformanceEvidence evidence)
    {
        var operationCases = operations.Select(operation =>
        {
            var inputs = operation.Arguments.Where(IsInput)
                .OrderBy(argument => argument.Ordinal)
                .Select(argument => CreateArgumentCase(argument, isInput: true))
                .ToArray();
            var outputs = operation.Arguments.Where(IsOutput)
                .OrderBy(argument => argument.Ordinal)
                .Select(argument => CreateArgumentCase(argument, isInput: false))
                .ToArray();
            return new OperationConformanceCase(
                operation.OperationId,
                operation.InventoryKey,
                operation.MpStep,
                $"/{catalog.TargetProtocolPackage}.{operation.Protocol.Service}/{operation.Protocol.Rpc}",
                operation.Protocol.Request,
                operation.Protocol.Result,
                operation.Risk.Effect,
                operation.ExecutionScope,
                operation.Risk.ReplaySafety,
                [.. operation.Risk.Flags],
                inputs,
                outputs,
                CreateOperationScenarios(operation, evidence.Families));
        }).ToArray();

        var bindingCases = evidence.Bindings
            .SelectMany(binding => binding.Families.SelectMany(family =>
            {
                var cases = new List<BindingConformanceCase>
                {
                    new(
                        $"binding:{binding.Method}:{family}:positive",
                        "binding.positive",
                        binding.Method,
                        binding.Direction,
                        family)
                };
                cases.Add(new BindingConformanceCase(
                    $"binding:{binding.Method}:{family}:negative",
                    binding.Direction == "setter"
                        ? "binding.setter_rejected"
                        : "binding.getter_failed",
                    binding.Method,
                    binding.Direction,
                    family));
                if (IsUnknownReturnedEnumApplicable(family, evidence.Families))
                {
                    cases.Add(new BindingConformanceCase(
                        $"binding:{binding.Method}:{family}:unknown-returned-enum",
                        "binding.unknown_returned_enum",
                        binding.Method,
                        binding.Direction,
                        family));
                }

                return cases;
            }))
            .OrderBy(item => item.CaseId, StringComparer.Ordinal)
            .ToArray();

        var familyCases = evidence.Families.Values
            .OrderBy(family => family.FamilyId, StringComparer.Ordinal)
            .SelectMany(family => new[]
            {
                new ValueFamilyConformanceCase(
                    $"family:{family.FamilyId}:positive",
                    "value_family.positive",
                    family.FamilyId,
                    family.Shape,
                    family.PublicType,
                    family.WorkerType),
                new ValueFamilyConformanceCase(
                    $"family:{family.FamilyId}:negative",
                    "value_family.negative",
                    family.FamilyId,
                    family.Shape,
                    family.PublicType,
                    family.WorkerType)
            })
            .ToArray();

        var enumCases = evidence.EnumTypes
            .SelectMany(enumType => enumType.Members.Select(member =>
                    new EnumConformanceCase(
                        $"enum:{enumType.PublicType}:{member.PublicNumber}:positive",
                        "enum.member_positive",
                        enumType.PublicType,
                        enumType.WorkerType,
                        member.PublicSymbol,
                        member.PublicNumber,
                        member.WorkerSymbol,
                        member.SdkLiteral))
                .Append(new EnumConformanceCase(
                    $"enum:{enumType.PublicType}:unknown:negative",
                    "enum.unknown_negative",
                    enumType.PublicType,
                    enumType.WorkerType,
                    PublicSymbol: null,
                    PublicNumber: null,
                    WorkerSymbol: null,
                    SdkLiteral: null)))
            .OrderBy(item => item.CaseId, StringComparer.Ordinal)
            .ToArray();

        var structuredCases = evidence.StructuredTypes
            .SelectMany(type => CreateStructuredCases(type))
            .OrderBy(item => item.CaseId, StringComparer.Ordinal)
            .ToArray();

        var bindingsByMethod = evidence.Bindings.ToDictionary(
            binding => binding.Method,
            StringComparer.Ordinal);
        var assignmentCases = evidence.Assignments
            .SelectMany(assignment =>
            {
                var rejectedFamily = bindingsByMethod[assignment.Method].Families
                    .FirstOrDefault(family => !string.Equals(
                        family,
                        assignment.FamilyId,
                        StringComparison.Ordinal)) ??
                    throw new InvalidDataException(
                        $"Reviewed assignment '{assignment.Method}' / " +
                        $"'{assignment.InventoryKey}' / {assignment.SdkOrder} does not have " +
                        "an alternate shared-method family for its negative case.");
                return new[]
                {
                    new AssignmentConformanceCase(
                        $"assignment:{assignment.Method}:{assignment.SdkOrder}:" +
                        ShortHash(assignment.InventoryKey) + ":positive",
                        "assignment.reviewed_positive",
                        assignment.Method,
                        assignment.InventoryKey,
                        assignment.SdkOrder,
                        assignment.FamilyId,
                        RejectedFamilyId: null),
                    new AssignmentConformanceCase(
                        $"assignment:{assignment.Method}:{assignment.SdkOrder}:" +
                        ShortHash(assignment.InventoryKey) + ":wrong-family-negative",
                        "assignment.unreviewed_family_negative",
                        assignment.Method,
                        assignment.InventoryKey,
                        assignment.SdkOrder,
                        assignment.FamilyId,
                        rejectedFamily)
                };
            })
            .OrderBy(item => item.CaseId, StringComparer.Ordinal)
            .ToArray();

        var manifest = new PortableConformanceManifest(
            ManifestSchemaReference,
            SchemaVersion: 1,
            GeneratedArtifactIdentity,
            catalog.CatalogId,
            catalog.CatalogRevision,
            catalog.SpatialAnalyzerTarget,
            catalog.TargetProtocolPackage,
            [
                CreateEvidenceInput(repositoryRoot, catalogManifestPath),
                .. catalog.OperationFiles.Select(path => CreateEvidenceInput(
                    repositoryRoot,
                    Path.Combine(
                        Path.GetDirectoryName(catalogManifestPath)!,
                        path.Replace('/', Path.DirectorySeparatorChar)))),
                CreateEvidenceInput(repositoryRoot, registryPath),
                CreateEvidenceInput(repositoryRoot, reviewPath),
                CreateEvidenceInput(repositoryRoot, valuesPath)
            ],
            new ConformanceCounts(
                operationCases.Length,
                bindingCases.Length,
                familyCases.Length,
                enumCases.Length,
                structuredCases.Length,
                assignmentCases.Length),
            operationCases,
            bindingCases,
            familyCases,
            enumCases,
            structuredCases,
            assignmentCases);
        ValidateCaseIdentities(manifest);
        return manifest;
    }

    private static ArgumentConformanceCase CreateArgumentCase(
        CommandCatalogArgument argument,
        bool isInput) =>
        new(
            argument.ArgumentId,
            argument.Ordinal,
            argument.SdkOrder,
            argument.MpName,
            argument.Direction,
            argument.SemanticType,
            CommandCatalogArtifactGenerator.ToWorkerValueKind(argument.SemanticType),
            isInput ? argument.SdkBinding.Setter! : argument.SdkBinding.Getter!,
            isInput ? argument.Input?.Presence : null,
            isInput ? argument.Input?.OmissionBehavior : null);

    private static IEnumerable<StructuredConformanceCase> CreateStructuredCases(
        StructuredTypeEvidence type)
    {
        yield return new StructuredConformanceCase(
            $"structured:{type.PublicType}:positive",
            "structured.positive",
            type.PublicType,
            type.WorkerType,
            FieldName: null,
            FieldNumber: null,
            FieldType: null,
            Cardinality: null);
        yield return new StructuredConformanceCase(
            $"structured:{type.PublicType}:malformed:negative",
            "structured.malformed_negative",
            type.PublicType,
            type.WorkerType,
            FieldName: null,
            FieldNumber: null,
            FieldType: null,
            Cardinality: null);
        foreach (var field in type.Fields)
        {
            yield return new StructuredConformanceCase(
                $"structured:{type.PublicType}:{field.Number}:{field.Cardinality}:present",
                $"structured.{field.Cardinality}_present",
                type.PublicType,
                type.WorkerType,
                field.Name,
                field.Number,
                field.Type,
                field.Cardinality);
            if (field.Cardinality is "optional" or "repeated")
            {
                yield return new StructuredConformanceCase(
                    $"structured:{type.PublicType}:{field.Number}:{field.Cardinality}:absent",
                    field.Cardinality == "optional"
                        ? "structured.optional_absent"
                        : "structured.repeated_empty",
                    type.PublicType,
                    type.WorkerType,
                    field.Name,
                    field.Number,
                    field.Type,
                    field.Cardinality);
            }
        }
    }

    private static OperationScenario[] CreateOperationScenarios(
        CommandCatalogOperation operation,
        IReadOnlyDictionary<string, ValueFamilyEvidence> families)
    {
        var scenarios = new List<OperationScenario>();
        Add("request:valid", "request.valid");
        Add("result:success", "result.success");
        Add("capability:metadata", "capability.metadata");
        Add("policy:allow", "policy.allowed");
        Add("policy:deny", "policy.denied");
        Add("readiness:unverified", "readiness.unverified");
        Add("error:disconnected", "error.disconnected");
        Add("error:execute-rejected", "error.execute_rejected");
        Add("error:mp-result-retrieval-failed", "error.mp_result_retrieval_failed");
        Add("error:mp-failed", "error.mp_failed");
        Add("error:deadline:not-started", "error.deadline_not_started");
        Add("error:deadline:started-unknown", "error.deadline_started_unknown");
        Add("error:cancellation:not-started", "error.cancellation_not_started");
        Add("error:cancellation:started-unknown", "error.cancellation_started_unknown");
        Add("error:worker-crash", "error.worker_crash");
        Add("error:worker-hang", "error.worker_hang");
        Add("error:malformed-worker-response", "error.malformed_worker_response");
        Add("metadata:execution-disposition", "metadata.execution_disposition");
        Add("metadata:replay-guidance", "metadata.replay_guidance");
        Add("metadata:execution-scope", "metadata.execution_scope");

        foreach (var input in operation.Arguments.Where(IsInput))
        {
            Add(
                $"request:{input.ArgumentId}:positive",
                "request.argument_positive",
                input.ArgumentId);
            if (SupportsPresentDefaultLike(input.SemanticType))
            {
                Add(
                    $"request:{input.ArgumentId}:present-default-like",
                    "request.present_default_like",
                    input.ArgumentId);
            }
            Add(
                $"binding:{input.ArgumentId}:setter-rejected",
                "binding.setter_rejected",
                input.ArgumentId);
            if (input.Input?.Presence == "required")
            {
                Add(
                    $"request:{input.ArgumentId}:missing",
                    "request.required_missing",
                    input.ArgumentId);
            }
            else
            {
                Add(
                    $"request:{input.ArgumentId}:omitted",
                    "request.optional_omitted",
                    input.ArgumentId);
            }

            if (RequiresMalformedShapeCase(input.SemanticType, families))
            {
                Add(
                    $"request:{input.ArgumentId}:malformed",
                    "request.malformed_shape",
                    input.ArgumentId);
            }

            if (families[input.SemanticType].Shape == "enum")
            {
                Add(
                    $"request:{input.ArgumentId}:unknown-enum",
                    "request.unknown_enum",
                    input.ArgumentId);
            }
        }

        foreach (var output in operation.Arguments.Where(IsOutput))
        {
            Add(
                $"result:{output.ArgumentId}:positive",
                "result.argument_positive",
                output.ArgumentId);
            Add(
                $"result:{output.ArgumentId}:getter-failed",
                "binding.getter_failed",
                output.ArgumentId);
            Add(
                $"result:{output.ArgumentId}:missing-typed-output",
                "result.missing_typed_output",
                output.ArgumentId);
            if (IsUnknownReturnedEnumApplicable(output.SemanticType, families))
            {
                Add(
                    $"result:{output.ArgumentId}:unknown-enum",
                    "result.unknown_returned_enum",
                    output.ArgumentId);
            }
        }

        return scenarios.OrderBy(item => item.ScenarioId, StringComparer.Ordinal).ToArray();

        void Add(string suffix, string kind, string? argumentId = null) =>
            scenarios.Add(new OperationScenario(
                $"operation:{operation.OperationId}:{suffix}",
                kind,
                argumentId));
    }

    private static bool RequiresMalformedShapeCase(
        string familyId,
        IReadOnlyDictionary<string, ValueFamilyEvidence> families)
    {
        var family = families[familyId];
        return RequiresMalformedMessageShapeCase(family.Shape, family.PublicType);
    }

    internal static bool RequiresMalformedMessageShapeCase(
        string shape,
        string publicType) =>
        shape is "structured" or "transform" or "identifier" &&
        publicType is not (
            "bool" or
            "bytes" or
            "double" or
            "float" or
            "int32" or
            "int64" or
            "string" or
            "uint32" or
            "uint64");

    private static bool SupportsPresentDefaultLike(string familyId) =>
        familyId is
            "logical" or
            "whole_number" or
            "floating_point" or
            "string" or
            "chart_name" or
            "cloud_name" or
            "collection_name" or
            "frame_name" or
            "vector_group_name" or
            "view_name";

    private static bool IsUnknownReturnedEnumApplicable(
        string familyId,
        IReadOnlyDictionary<string, ValueFamilyEvidence> families) =>
        families[familyId].Shape == "enum" ||
        familyId is
            "collection_item_name" or
            "collection_item_name_list" or
            "collection_object_name" or
            "collection_object_name_list";

    private static void ValidateEvidence(
        CommandCatalogManifest catalog,
        IReadOnlyList<CommandCatalogOperation> operations,
        ConformanceEvidence evidence)
    {
        RequireTarget("binding registry", catalog.SpatialAnalyzerTarget, evidence.RegistryTarget);
        RequireTarget("binding review", catalog.SpatialAnalyzerTarget, evidence.ReviewTarget);
        RequireTarget("value-family catalog", catalog.SpatialAnalyzerTarget, evidence.ValuesTarget);

        var usableMethods = evidence.Bindings.Select(binding => binding.Method)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        foreach (var coverage in evidence.ImplementedCoverage)
        {
            if (!usableMethods.SequenceEqual(coverage.Value, StringComparer.Ordinal))
            {
                throw new InvalidDataException(
                    $"Implemented {coverage.Key} methods do not exactly match usable binding evidence.");
            }
        }

        var registryFamilies = evidence.RegistryImplementedFamilies
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        var valueFamilies = evidence.Families.Keys
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        if (!registryFamilies.SequenceEqual(valueFamilies, StringComparer.Ordinal))
        {
            throw new InvalidDataException(
                "Implemented value families differ between the binding registry and value-family evidence.");
        }

        var bindings = evidence.Bindings.ToDictionary(
            binding => binding.Method,
            StringComparer.Ordinal);
        var assignments = evidence.Assignments.ToHashSet();
        foreach (var operation in operations)
        {
            foreach (var argument in operation.Arguments)
            {
                var method = IsInput(argument)
                    ? argument.SdkBinding.Setter
                    : argument.SdkBinding.Getter;
                if (string.IsNullOrWhiteSpace(method) ||
                    !bindings.TryGetValue(method, out var binding) ||
                    !binding.Families.Contains(argument.SemanticType, StringComparer.Ordinal))
                {
                    throw new InvalidDataException(
                        $"Supported operation '{operation.OperationId}' argument " +
                        $"'{argument.ArgumentId}' has no usable '{method}' / " +
                        $"'{argument.SemanticType}' binding evidence row.");
                }

                if (!evidence.Families.ContainsKey(argument.SemanticType))
                {
                    throw new InvalidDataException(
                        $"Supported operation '{operation.OperationId}' argument " +
                        $"'{argument.ArgumentId}' has no implemented value-family evidence.");
                }

                if (binding.Families.Count > 1 && !assignments.Contains(new AssignmentEvidence(
                        method,
                        operation.InventoryKey,
                        argument.SdkOrder,
                        argument.SemanticType)))
                {
                    throw new InvalidDataException(
                        $"Supported operation '{operation.OperationId}' argument " +
                        $"'{argument.ArgumentId}' lacks its exact reviewed multi-family assignment.");
                }
            }
        }
    }

    private static void ValidateCaseIdentities(PortableConformanceManifest manifest)
    {
        EnsureUnique(
            manifest.Operations.Select(operation => operation.OperationId),
            "operation_id");
        foreach (var operation in manifest.Operations)
        {
            EnsureUnique(
                operation.Scenarios.Select(scenario => scenario.ScenarioId),
                $"scenario_id for operation '{operation.OperationId}'");
        }

        EnsureUnique(manifest.BindingCases.Select(item => item.CaseId), "binding case_id");
        EnsureUnique(
            manifest.ValueFamilyCases.Select(item => item.CaseId),
            "value-family case_id");
        EnsureUnique(manifest.EnumCases.Select(item => item.CaseId), "enum case_id");
        EnsureUnique(
            manifest.StructuredCases.Select(item => item.CaseId),
            "structured case_id");
        EnsureUnique(
            manifest.AssignmentCases.Select(item => item.CaseId),
            "assignment case_id");
    }

    private static void EnsureUnique(IEnumerable<string> values, string subject)
    {
        var duplicate = values.GroupBy(value => value, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidDataException(
                $"Portable conformance {subject} '{duplicate.Key}' is duplicated.");
        }
    }

    private static ConformanceEvidence LoadEvidence(
        string target,
        string registryPath,
        string reviewPath,
        string valuesPath)
    {
        using var registry = JsonDocument.Parse(File.ReadAllBytes(registryPath));
        using var review = JsonDocument.Parse(File.ReadAllBytes(reviewPath));
        using var values = JsonDocument.Parse(File.ReadAllBytes(valuesPath));

        var bindings = registry.RootElement.GetProperty("bindings")
            .EnumerateArray()
            .Where(binding => RequiredString(binding, "registry_status") == "usable")
            .Select(binding => new BindingEvidence(
                RequiredString(binding, "method"),
                RequiredString(binding, "direction"),
                binding.GetProperty("semantic_value_families")
                    .EnumerateArray()
                    .Select(item => item.GetString()!)
                    .OrderBy(item => item, StringComparer.Ordinal)
                    .ToArray()))
            .OrderBy(binding => binding.Method, StringComparer.Ordinal)
            .ToArray();
        var registryImplementedFamilies = registry.RootElement.GetProperty("value_families")
            .EnumerateArray()
            .Where(family => RequiredString(family, "implementation_status") == "implemented")
            .Select(family => RequiredString(family, "family_id"))
            .ToArray();
        var families = values.RootElement.GetProperty("families")
            .EnumerateArray()
            .Where(family => RequiredString(family, "implementation_status") == "implemented")
            .Select(family => new ValueFamilyEvidence(
                RequiredString(family, "family_id"),
                RequiredString(family, "shape"),
                RequiredString(family, "public_type_target"),
                RequiredString(family, "worker_type_target")))
            .ToDictionary(family => family.FamilyId, StringComparer.Ordinal);
        var enumTypes = values.RootElement.GetProperty("enum_types")
            .EnumerateArray()
            .Select(type => new EnumTypeEvidence(
                RequiredString(type, "public_type"),
                RequiredString(type, "worker_type"),
                type.GetProperty("members").EnumerateArray()
                    .Select(member => new EnumMemberEvidence(
                        RequiredString(member, "public_symbol"),
                        member.GetProperty("public_number").GetInt32(),
                        RequiredString(member, "worker_symbol"),
                        RequiredString(member, "sdk_literal")))
                    .OrderBy(member => member.PublicNumber)
                    .ToArray()))
            .OrderBy(type => type.PublicType, StringComparer.Ordinal)
            .ToArray();
        var structuredTypes = values.RootElement.GetProperty("structured_types")
            .EnumerateArray()
            .Select(type => new StructuredTypeEvidence(
                RequiredString(type, "public_type"),
                RequiredString(type, "worker_type"),
                type.GetProperty("public_fields").EnumerateArray()
                    .Select(field => new StructuredFieldEvidence(
                        RequiredString(field, "name"),
                        field.GetProperty("number").GetInt32(),
                        RequiredString(field, "type"),
                        RequiredString(field, "cardinality")))
                    .OrderBy(field => field.Number)
                    .ToArray()))
            .OrderBy(type => type.PublicType, StringComparer.Ordinal)
            .ToArray();
        var assignments = values.RootElement.GetProperty("command_assignments")
            .EnumerateArray()
            .Select(item => new AssignmentEvidence(
                RequiredString(item, "method"),
                RequiredString(item, "inventory_key"),
                item.GetProperty("sdk_order").GetInt32(),
                RequiredString(item, "family_id")))
            .OrderBy(item => item.Method, StringComparer.Ordinal)
            .ThenBy(item => item.InventoryKey, StringComparer.Ordinal)
            .ThenBy(item => item.SdkOrder)
            .ThenBy(item => item.FamilyId, StringComparer.Ordinal)
            .ToArray();
        var coverageElement = review.RootElement.GetProperty("implemented_coverage");
        var coverage = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (var component in new[] { "protocol", "worker", "adapter", "fake", "generator" })
        {
            coverage.Add(
                component,
                coverageElement.GetProperty(component).EnumerateArray()
                    .Select(item => item.GetString()!)
                    .OrderBy(item => item, StringComparer.Ordinal)
                    .ToArray());
        }

        return new ConformanceEvidence(
            RequiredString(registry.RootElement, "spatial_analyzer_target"),
            RequiredString(review.RootElement, "spatial_analyzer_target"),
            RequiredString(values.RootElement, "spatial_analyzer_target"),
            bindings,
            registryImplementedFamilies,
            families,
            enumTypes,
            structuredTypes,
            assignments,
            coverage);
    }

    private static EvidenceInput CreateEvidenceInput(string root, string path) =>
        new(
            Path.GetRelativePath(root, path).Replace('\\', '/'),
            Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path))));

    private static string ShortHash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..16];

    private static string RequiredString(JsonElement element, string property) =>
        element.GetProperty(property).GetString() ??
        throw new InvalidDataException($"Evidence property '{property}' must not be null.");

    private static void RequireTarget(string source, string expected, string actual)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"The {source} target '{actual}' does not match catalog target '{expected}'.");
        }
    }

    private static bool IsInput(CommandCatalogArgument argument) =>
        argument.Direction is "input" or "input_output";

    private static bool IsOutput(CommandCatalogArgument argument) =>
        argument.Direction is "output" or "input_output";

    private static T Deserialize<T>(string path) =>
        JsonSerializer.Deserialize<T>(File.ReadAllText(path), CatalogJsonOptions) ??
        throw new InvalidDataException($"Catalog document '{path}' was empty.");

    private static void WriteGeneratedFile(
        string outputRoot,
        string relativePath,
        string content)
    {
        var path = Path.Combine(
            outputRoot,
            relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (File.Exists(path))
        {
            try
            {
                using var existing = JsonDocument.Parse(File.ReadAllBytes(path));
                if (RequiredString(existing.RootElement, "generated_by") !=
                    GeneratedArtifactIdentity)
                {
                    throw new InvalidDataException(
                        $"Refusing to overwrite non-conformance-generated file '{relativePath}'.");
                }
            }
            catch (JsonException exception)
            {
                throw new InvalidDataException(
                    $"Refusing to overwrite non-conformance-generated file '{relativePath}'.",
                    exception);
            }
        }

        File.WriteAllText(
            path,
            content.ReplaceLineEndings("\n"),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private sealed record PortableConformanceManifest(
        [property: JsonPropertyName("$schema")] string Schema,
        int SchemaVersion,
        string GeneratedBy,
        string CatalogId,
        string CatalogRevision,
        string SpatialAnalyzerTarget,
        string TargetProtocolPackage,
        IReadOnlyList<EvidenceInput> EvidenceInputs,
        ConformanceCounts Counts,
        IReadOnlyList<OperationConformanceCase> Operations,
        IReadOnlyList<BindingConformanceCase> BindingCases,
        IReadOnlyList<ValueFamilyConformanceCase> ValueFamilyCases,
        IReadOnlyList<EnumConformanceCase> EnumCases,
        IReadOnlyList<StructuredConformanceCase> StructuredCases,
        IReadOnlyList<AssignmentConformanceCase> AssignmentCases);

    private sealed record EvidenceInput(string Path, string Sha256);
    private sealed record ConformanceCounts(
        int OperationCount,
        int BindingCaseCount,
        int ValueFamilyCaseCount,
        int EnumCaseCount,
        int StructuredCaseCount,
        int AssignmentCaseCount);
    private sealed record OperationConformanceCase(
        string OperationId,
        string InventoryKey,
        string MpStep,
        string FullyQualifiedMethod,
        string Request,
        string Result,
        string Effect,
        string ExecutionScope,
        string ReplaySafety,
        IReadOnlyList<string> RiskFlags,
        IReadOnlyList<ArgumentConformanceCase> Inputs,
        IReadOnlyList<ArgumentConformanceCase> Outputs,
        IReadOnlyList<OperationScenario> Scenarios);
    private sealed record ArgumentConformanceCase(
        string ArgumentId,
        int Ordinal,
        int SdkOrder,
        string MpName,
        string Direction,
        string SemanticType,
        string WorkerValueKind,
        string Binding,
        string? Presence,
        string? OmissionBehavior);
    private sealed record OperationScenario(
        string ScenarioId,
        string Kind,
        string? ArgumentId);
    private sealed record BindingConformanceCase(
        string CaseId,
        string Kind,
        string Method,
        string Direction,
        string FamilyId);
    private sealed record ValueFamilyConformanceCase(
        string CaseId,
        string Kind,
        string FamilyId,
        string Shape,
        string PublicType,
        string WorkerType);
    private sealed record EnumConformanceCase(
        string CaseId,
        string Kind,
        string PublicType,
        string WorkerType,
        string? PublicSymbol,
        int? PublicNumber,
        string? WorkerSymbol,
        string? SdkLiteral);
    private sealed record StructuredConformanceCase(
        string CaseId,
        string Kind,
        string PublicType,
        string WorkerType,
        string? FieldName,
        int? FieldNumber,
        string? FieldType,
        string? Cardinality);
    private sealed record AssignmentConformanceCase(
        string CaseId,
        string Kind,
        string Method,
        string InventoryKey,
        int SdkOrder,
        string FamilyId,
        string? RejectedFamilyId);

    private sealed record ConformanceEvidence(
        string RegistryTarget,
        string ReviewTarget,
        string ValuesTarget,
        IReadOnlyList<BindingEvidence> Bindings,
        IReadOnlyList<string> RegistryImplementedFamilies,
        IReadOnlyDictionary<string, ValueFamilyEvidence> Families,
        IReadOnlyList<EnumTypeEvidence> EnumTypes,
        IReadOnlyList<StructuredTypeEvidence> StructuredTypes,
        IReadOnlyList<AssignmentEvidence> Assignments,
        IReadOnlyDictionary<string, IReadOnlyList<string>> ImplementedCoverage);
    private sealed record BindingEvidence(
        string Method,
        string Direction,
        IReadOnlyList<string> Families);
    private sealed record ValueFamilyEvidence(
        string FamilyId,
        string Shape,
        string PublicType,
        string WorkerType);
    private sealed record EnumTypeEvidence(
        string PublicType,
        string WorkerType,
        IReadOnlyList<EnumMemberEvidence> Members);
    private sealed record EnumMemberEvidence(
        string PublicSymbol,
        int PublicNumber,
        string WorkerSymbol,
        string SdkLiteral);
    private sealed record StructuredTypeEvidence(
        string PublicType,
        string WorkerType,
        IReadOnlyList<StructuredFieldEvidence> Fields);
    private sealed record StructuredFieldEvidence(
        string Name,
        int Number,
        string Type,
        string Cardinality);
    private sealed record AssignmentEvidence(
        string Method,
        string InventoryKey,
        int SdkOrder,
        string FamilyId);
}
