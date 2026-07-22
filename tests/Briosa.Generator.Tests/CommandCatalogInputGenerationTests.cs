using System.Text.Json.Nodes;
using Briosa.Generator;

namespace Briosa.Generator.Tests;

public sealed class CommandCatalogInputGenerationTests
{
    [Fact]
    public void SyntheticCatalogGeneratesEveryModeledTypeAndInputBehavior()
    {
        var repositoryRoot = FindRepositoryRoot();
        var temporaryRoot = Path.Combine(
            Path.GetTempPath(),
            $"briosa-generator-inputs-{Guid.NewGuid():N}");
        var catalogRoot = Path.Combine(temporaryRoot, "catalog");
        var outputRoot = Path.Combine(temporaryRoot, "output");
        try
        {
            CopyDirectory(Path.Combine(repositoryRoot.FullName, "catalog"), catalogRoot);
            var targetRoot = Path.Combine(catalogRoot, "sa", "2026.1.0529.7");
            var manifestPath = Path.Combine(targetRoot, "catalog.json");
            var manifest = JsonNode.Parse(File.ReadAllText(manifestPath))!.AsObject();
            manifest["operation_files"]!.AsArray().Add("operations/synthetic.all_types.json");
            File.WriteAllText(manifestPath, manifest.ToJsonString(JsonOptions));
            File.WriteAllText(
                Path.Combine(targetRoot, "operations", "synthetic.all_types.json"),
                SyntheticOperation);

            _ = CommandCatalogGenerator.Generate(catalogRoot, outputRoot);

            var binding = File.ReadAllText(Path.Combine(
                outputRoot,
                "src",
                "Briosa.Server",
                "Generated",
                "Sa",
                "V2026_1_0529_7",
                "V1Alpha1",
                "Operations.g.cs"));
            Assert.Contains("SetBoolArg", binding, StringComparison.Ordinal);
            Assert.Contains("SetIntegerArg", binding, StringComparison.Ordinal);
            Assert.Contains("SetDoubleArg", binding, StringComparison.Ordinal);
            Assert.Contains("SetStringArg", binding, StringComparison.Ordinal);
            Assert.Contains("SetPointNameArg", binding, StringComparison.Ordinal);
            Assert.Contains("SetVectorArg", binding, StringComparison.Ordinal);
            Assert.Contains("SetToleranceVectorOptionsArg", binding, StringComparison.Ordinal);
            Assert.Contains("if (request.HasOptionalCount)", binding, StringComparison.Ordinal);
            Assert.Contains("DoubleValue: 1.5d", binding, StringComparison.Ordinal);
            Assert.Contains("must contain every exact-target component", binding, StringComparison.Ordinal);
            Assert.Contains("GetToleranceVectorOptionsArg", binding, StringComparison.Ordinal);
            Assert.Contains("new TargetProtocol.PointName", binding, StringComparison.Ordinal);
            Assert.Contains("new TargetProtocol.Vector3", binding, StringComparison.Ordinal);
            Assert.Contains("new TargetProtocol.ToleranceVectorOptions", binding, StringComparison.Ordinal);
            Assert.Contains("Execution = completed.Details", binding, StringComparison.Ordinal);

            var coverage = File.ReadAllText(Path.Combine(
                outputRoot,
                "generated",
                "catalog",
                "sa",
                "2026.1.0529.7",
                "coverage.json"));
            Assert.Contains("\"omission_behavior\": \"omit_sdk_setter\"", coverage, StringComparison.Ordinal);
            Assert.Contains("\"default_status\": \"reviewed\"", coverage, StringComparison.Ordinal);
            Assert.Contains("\"direction\": \"input_output\"", coverage, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(temporaryRoot))
            {
                Directory.Delete(temporaryRoot, recursive: true);
            }
        }
    }

    private static void CopyDirectory(string source, string destination)
    {
        foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(
                destination,
                Path.GetRelativePath(source, directory)));
        }

        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(destination, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
    }

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

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private const string SyntheticOperation = """
        {
          "$schema": "../../../schemas/v1/operation.schema.json",
          "operation_id": "synthetic.all_types",
          "mp_step": "Synthetic All Types",
          "category": "Synthetic",
          "protocol": {
            "service": "Synthetic",
            "rpc": "AllTypes",
            "request": "AllTypesRequest",
            "result": "AllTypesResult"
          },
          "stability": "experimental",
          "deprecation": { "status": "active" },
          "risk": { "effect": "read_only", "flags": [] },
          "documentation": { "summary": "Exercises generator mappings." },
          "arguments": [
            { "argument_id": "enabled", "ordinal": 0, "mp_name": "Enabled", "direction": "input", "result_only": "no", "semantic_type": "logical", "data_classification": "proprietary", "input": { "presence": "required", "omission_behavior": "reject_request", "default": { "status": "none" } }, "sdk_binding": { "status": "available", "setter": "SetBoolArg", "getter": null }, "documentation": "Required logical input." },
            { "argument_id": "optional_count", "ordinal": 1, "mp_name": "Optional Count", "direction": "input", "result_only": "no", "semantic_type": "whole_number", "data_classification": "proprietary", "input": { "presence": "optional", "omission_behavior": "omit_sdk_setter", "default": { "status": "none" } }, "sdk_binding": { "status": "available", "setter": "SetIntegerArg", "getter": null }, "documentation": "Optional integer input." },
            { "argument_id": "threshold", "ordinal": 2, "mp_name": "Threshold", "direction": "input", "result_only": "no", "semantic_type": "floating_point", "data_classification": "proprietary", "input": { "presence": "optional", "omission_behavior": "set_catalog_default", "default": { "status": "reviewed", "value": 1.5 } }, "sdk_binding": { "status": "available", "setter": "SetDoubleArg", "getter": null }, "documentation": "Reviewed-default floating input." },
            { "argument_id": "label", "ordinal": 3, "mp_name": "Label", "direction": "input_output", "result_only": "no", "semantic_type": "string", "data_classification": "proprietary", "input": { "presence": "required", "omission_behavior": "reject_request", "default": { "status": "none" } }, "sdk_binding": { "status": "available", "setter": "SetStringArg", "getter": "GetStringArg" }, "documentation": "String input and output." },
            { "argument_id": "point", "ordinal": 4, "mp_name": "Point", "direction": "input_output", "result_only": "no", "semantic_type": "point_name", "data_classification": "proprietary", "input": { "presence": "optional", "omission_behavior": "omit_sdk_setter", "default": { "status": "none" } }, "sdk_binding": { "status": "available", "setter": "SetPointNameArg", "getter": "GetPointNameArg" }, "documentation": "Point name input and output." },
            { "argument_id": "vector", "ordinal": 5, "mp_name": "Vector", "direction": "input_output", "result_only": "no", "semantic_type": "vector", "data_classification": "proprietary", "input": { "presence": "required", "omission_behavior": "reject_request", "default": { "status": "none" } }, "sdk_binding": { "status": "available", "setter": "SetVectorArg", "getter": "GetVectorArg" }, "documentation": "Vector input and output." },
            { "argument_id": "tolerances", "ordinal": 6, "mp_name": "Tolerances", "direction": "input_output", "result_only": "no", "semantic_type": "tolerance_vector_options", "data_classification": "proprietary", "input": { "presence": "required", "omission_behavior": "reject_request", "default": { "status": "none" } }, "sdk_binding": { "status": "available", "setter": "SetToleranceVectorOptionsArg", "getter": "GetToleranceVectorOptionsArg" }, "documentation": "Tolerance input and output." },
            { "argument_id": "logical_result", "ordinal": 7, "mp_name": "Logical Result", "direction": "output", "result_only": "yes", "semantic_type": "logical", "data_classification": "proprietary", "sdk_binding": { "status": "available", "setter": null, "getter": "GetBoolArg" }, "documentation": "Logical result." },
            { "argument_id": "integer_result", "ordinal": 8, "mp_name": "Integer Result", "direction": "output", "result_only": "yes", "semantic_type": "whole_number", "data_classification": "proprietary", "sdk_binding": { "status": "available", "setter": null, "getter": "GetIntegerArg" }, "documentation": "Integer result." },
            { "argument_id": "double_result", "ordinal": 9, "mp_name": "Double Result", "direction": "output", "result_only": "yes", "semantic_type": "floating_point", "data_classification": "proprietary", "sdk_binding": { "status": "available", "setter": null, "getter": "GetDoubleArg" }, "documentation": "Floating result." }
          ],
          "evidence": [{ "source_id": "maintainer_review", "reference": "Synthetic generator test" }]
        }
        """;
}
