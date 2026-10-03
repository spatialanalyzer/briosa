using System.Text.Json;
using System.Text.RegularExpressions;
using Briosa.Server.Operations;

namespace Briosa.Server.Tests;

/// <summary>
/// Keeps the curated SA 2024 binding facts (#278) parseable and aligned with the registered
/// operation set. The facts are committed public evidence; these checks never read private exports.
/// They do not compare operation mappings with the facts; that cross-check is separate (#272).
/// </summary>
public sealed partial class Sa2024BindingFactsTests
{
    private const string EvidenceProvenance = "briosa-evidence@07acae4";
    private const string DocsProvenance = "briosa-docs";

    private static readonly HashSet<string> Directions = new(StringComparer.Ordinal) { "input", "output", "input_output" };
    private static readonly HashSet<string> Statuses = new(StringComparer.Ordinal) { "available", "unavailable", "unpublished", "not_observed" };
    private static readonly HashSet<string> SourceKinds = new(StringComparer.Ordinal) { "captured_2024_signature", "reference_signature", "none" };

    [Fact]
    public void FactFileCoversEveryRegisteredOperation()
    {
        using var facts = LoadFacts();
        var root = facts.RootElement;
        Assert.Equal(1, root.GetProperty("schema_version").GetInt32());
        Assert.Equal(SpatialAnalyzerApi.TargetVersion, root.GetProperty("spatial_analyzer_target").GetString());

        var operations = root.GetProperty("operations").EnumerateArray().ToArray();
        var ids = operations.Select(operation => operation.GetProperty("operation_id").GetString()!).ToArray();
        Assert.Equal(ids.Order(StringComparer.Ordinal), ids);
        Assert.Equal(ids.Distinct(StringComparer.Ordinal), ids);
        Assert.Equal(
            SpatialAnalyzerApi.Operations.Select(operation => operation.OperationId).Order(StringComparer.Ordinal),
            ids);

        // The catalog heading is the SDK step text; registry casing is compared separately (#272).
        var registeredSteps = SpatialAnalyzerApi.Operations.ToDictionary(
            operation => operation.OperationId, operation => operation.MpStep, StringComparer.Ordinal);
        foreach (var operation in operations)
        {
            var id = operation.GetProperty("operation_id").GetString()!;
            Assert.Equal(registeredSteps[id], operation.GetProperty("mp_step").GetString(), ignoreCase: true);
        }
    }

    [Fact]
    public void EveryOperationCitesItsPublicSource()
    {
        using var facts = LoadFacts();
        foreach (var operation in facts.RootElement.GetProperty("operations").EnumerateArray())
        {
            var source = operation.GetProperty("source");
            var kind = source.GetProperty("kind").GetString()!;
            Assert.Contains(kind, SourceKinds);
            var findings = Strings(operation.GetProperty("findings"));
            if (kind == "none")
            {
                Assert.Contains("no_public_2024_signature", findings);
                Assert.Equal(JsonValueKind.Null, source.GetProperty("document").ValueKind);
                Assert.Empty(operation.GetProperty("arguments").EnumerateArray());
                continue;
            }

            Assert.Matches(CatalogDocument(), source.GetProperty("document").GetString()!);
            Assert.False(string.IsNullOrWhiteSpace(source.GetProperty("anchor").GetString()));
            Assert.DoesNotContain("no_public_2024_signature", findings);
        }
    }

    [Fact]
    public void ArgumentsRecordExactNamesDirectionsAndBindings()
    {
        using var facts = LoadFacts();
        var interopMethods = LoadInteropMethods();
        foreach (var operation in facts.RootElement.GetProperty("operations").EnumerateArray())
        {
            var kind = operation.GetProperty("source").GetProperty("kind").GetString();
            var ordinal = 0;
            foreach (var argument in operation.GetProperty("arguments").EnumerateArray())
            {
                Assert.Equal(ordinal++, argument.GetProperty("ordinal").GetInt32());
                Assert.False(string.IsNullOrEmpty(argument.GetProperty("sdk_name").GetString()));
                Assert.False(string.IsNullOrEmpty(argument.GetProperty("argument_kind").GetString()));
                var direction = argument.GetProperty("direction").GetString()!;
                Assert.Contains(direction, Directions);

                var setter = argument.GetProperty("setter");
                var getter = argument.GetProperty("getter");
                AssertBinding(setter, "Set", interopMethods, argument, "setter_method_absent_from_interop");
                AssertBinding(getter, "Get", interopMethods, argument, "getter_method_absent_from_interop");
                var setterStatus = setter.GetProperty("status").GetString();
                var getterStatus = getter.GetProperty("status").GetString();
                Assert.Equal(direction == "output", setterStatus == "not_observed");
                if (direction == "input")
                {
                    Assert.Equal("not_observed", getterStatus);
                }
                else
                {
                    Assert.NotEqual("not_observed", getterStatus);
                }

                var basis = argument.GetProperty("method_basis");
                var hasMethod = setter.GetProperty("method").ValueKind == JsonValueKind.String ||
                    getter.GetProperty("method").ValueKind == JsonValueKind.String;
                Assert.Equal(hasMethod, basis.ValueKind == JsonValueKind.String);
                if (hasMethod)
                {
                    Assert.Equal(
                        kind == "captured_2024_signature" ? "captured_argument_kind" : "collection_object_binding_statement",
                        basis.GetString());
                }
            }
        }
    }

    [Fact]
    public void PrivateEvidenceSuppliesOnlyEmbeddedQuoteCorrections()
    {
        using var facts = LoadFacts();
        var root = facts.RootElement;
        var corrections = root.GetProperty("provenance").GetProperty("private_quote_corrections");
        Assert.False(corrections.GetProperty("source_material_committed").GetBoolean());
        Assert.Equal("embedded_double_quotes_in_argument_labels_only", corrections.GetProperty("use").GetString());
        Assert.Equal("07acae44ad6cbed11391b38391317c4974910af8", corrections.GetProperty("revision").GetString());

        var quoteCorrected = 0;
        foreach (var argument in root.GetProperty("operations").EnumerateArray()
            .SelectMany(operation => operation.GetProperty("arguments").EnumerateArray()))
        {
            var provenance = argument.GetProperty("name_provenance").GetString();
            var name = argument.GetProperty("sdk_name").GetString()!;
            if (provenance == EvidenceProvenance)
            {
                Assert.Contains("\"", name, StringComparison.Ordinal);
                quoteCorrected++;
            }
            else
            {
                Assert.Equal(DocsProvenance, provenance);
            }
        }

        Assert.Equal(root.GetProperty("summary").GetProperty("quote_corrected_argument_count").GetInt32(), quoteCorrected);
    }

    [Fact]
    public void SummaryMatchesTheRecordedFacts()
    {
        using var facts = LoadFacts();
        var root = facts.RootElement;
        var summary = root.GetProperty("summary");
        var operations = root.GetProperty("operations").EnumerateArray().ToArray();
        var arguments = operations.SelectMany(operation => operation.GetProperty("arguments").EnumerateArray()).ToArray();

        Assert.Equal(summary.GetProperty("operation_count").GetInt32(), operations.Length);
        Assert.Equal(summary.GetProperty("captured_2024_signature_operation_count").GetInt32(), CountKind(operations, "captured_2024_signature"));
        Assert.Equal(summary.GetProperty("reference_signature_operation_count").GetInt32(), CountKind(operations, "reference_signature"));
        Assert.Equal(summary.GetProperty("unsourced_operation_count").GetInt32(), CountKind(operations, "none"));
        Assert.Equal(summary.GetProperty("argument_count").GetInt32(), arguments.Length);
        Assert.Equal(summary.GetProperty("unavailable_binding_count").GetInt32(), arguments.Count(argument => HasStatus(argument, "unavailable")));
        Assert.Equal(summary.GetProperty("unpublished_method_argument_count").GetInt32(), arguments.Count(argument => HasStatus(argument, "unpublished")));
        Assert.Equal(
            summary.GetProperty("collection_object_statement_method_count").GetInt32(),
            arguments.Count(argument => argument.GetProperty("method_basis").GetString() == "collection_object_binding_statement"));
    }

    [Fact]
    public void RuntimeSourceDoesNotConsumeTheFactFile()
    {
        var sourceRoot = Path.Combine(FindTargetRoot(), "src");
        foreach (var file in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
                !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
                (path.EndsWith(".cs", StringComparison.Ordinal) || path.EndsWith(".csproj", StringComparison.Ordinal))))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("bindings.json", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Update-Sa2024BindingFacts", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static void AssertBinding(
        JsonElement binding,
        string prefix,
        HashSet<string> interopMethods,
        JsonElement argument,
        string absentFinding)
    {
        var status = binding.GetProperty("status").GetString()!;
        Assert.Contains(status, Statuses);
        var method = binding.GetProperty("method");
        if (status != "available")
        {
            Assert.Equal(JsonValueKind.Null, method.ValueKind);
            return;
        }

        var name = method.GetString()!;
        Assert.StartsWith(prefix, name, StringComparison.Ordinal);
        Assert.Matches(SdkMethod(), name);
        var findings = Strings(argument.GetProperty("findings"));
        Assert.Equal(!interopMethods.Contains(name), findings.Contains(absentFinding));
    }

    private static bool HasStatus(JsonElement argument, string status) =>
        argument.GetProperty("setter").GetProperty("status").GetString() == status ||
        argument.GetProperty("getter").GetProperty("status").GetString() == status;

    private static int CountKind(JsonElement[] operations, string kind) =>
        operations.Count(operation => operation.GetProperty("source").GetProperty("kind").GetString() == kind);

    private static HashSet<string> Strings(JsonElement array) =>
        array.EnumerateArray().Select(item => item.GetString()!).ToHashSet(StringComparer.Ordinal);

    private static JsonDocument LoadFacts() =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(
            FindTargetRoot(), "evidence", "sa", SpatialAnalyzerApi.TargetVersion, "bindings.json")));

    private static HashSet<string> LoadInteropMethods()
    {
        var api = File.ReadAllText(Path.Combine(
            FindTargetRoot(), "interop", "SpatialAnalyzer", SpatialAnalyzerApi.TargetVersion,
            "Briosa.SpatialAnalyzer.Interop.PublicApi.txt"));
        var methods = InteropMethod().Matches(api).Select(match => match.Groups[1].Value).ToHashSet(StringComparer.Ordinal);
        Assert.NotEmpty(methods);
        return methods;
    }

    private static string FindTargetRoot()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Briosa.slnx")))
        {
            root = root.Parent;
        }

        Assert.NotNull(root);
        return root.FullName;
    }

    [GeneratedRegex(@"^mp-command-catalog/commands/[a-z0-9-]+\.md$")]
    private static partial Regex CatalogDocument();

    [GeneratedRegex(@"^(?:Set|Get)[A-Za-z0-9]+Arg[0-9]*$")]
    private static partial Regex SdkMethod();

    [GeneratedRegex(@"(?m)^\s+method ([A-Za-z0-9_]+) ")]
    private static partial Regex InteropMethod();
}
