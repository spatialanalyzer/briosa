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
            var syntheticOperation = JsonNode.Parse(SyntheticOperation)!.AsObject();
            var syntheticArguments = syntheticOperation["arguments"]!.AsArray();
            foreach (var argument in JsonNode.Parse(IdentityReferenceArguments)!.AsArray())
            {
                syntheticArguments.Add(argument?.DeepClone());
            }

            File.WriteAllText(
                Path.Combine(targetRoot, "operations", "synthetic.all_types.json"),
                syntheticOperation.ToJsonString(JsonOptions));

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
            Assert.Contains("SetChartNameArg", binding, StringComparison.Ordinal);
            Assert.Contains("SetCloudNameArg", binding, StringComparison.Ordinal);
            Assert.Contains("SetColInstIdArg", binding, StringComparison.Ordinal);
            Assert.Contains("SetColInstIdRefListArg", binding, StringComparison.Ordinal);
            Assert.Contains("SetColMachineIdArg", binding, StringComparison.Ordinal);
            Assert.Contains("SetCollectionGroupNameRefListArg", binding, StringComparison.Ordinal);
            Assert.Contains("SetCollectionNameArg", binding, StringComparison.Ordinal);
            Assert.Contains("SetCollectionObjectNameArg2", binding, StringComparison.Ordinal);
            Assert.Contains("SetCollectionObjectNameRefListArg", binding, StringComparison.Ordinal);
            Assert.Contains("SetColVectorGroupNameArg", binding, StringComparison.Ordinal);
            Assert.Contains("SetCollectionVectorGroupNameRefListArg", binding, StringComparison.Ordinal);
            Assert.Contains("SetFrameNameArg", binding, StringComparison.Ordinal);
            Assert.Contains("SetPointNameRefListArg", binding, StringComparison.Ordinal);
            Assert.Contains("SetStringRefListArg", binding, StringComparison.Ordinal);
            Assert.Contains("SetVectorGroupNameArg", binding, StringComparison.Ordinal);
            Assert.Contains("SetVectorNameRefListArg", binding, StringComparison.Ordinal);
            Assert.Contains("SetViewNameArg", binding, StringComparison.Ordinal);
            Assert.Contains("GetCollectionObjectNameArg", binding, StringComparison.Ordinal);
            Assert.Contains("GetVectorNameRefListArg", binding, StringComparison.Ordinal);
            Assert.Contains("WorkerMpValueKind.CollectionMachineId", binding, StringComparison.Ordinal);
            Assert.Contains("new TargetProtocol.CollectionInstrumentIdList", binding, StringComparison.Ordinal);
            Assert.Contains("new TargetProtocol.CollectionObjectNameList", binding, StringComparison.Ordinal);
            Assert.Contains("new TargetProtocol.PointNameList", binding, StringComparison.Ordinal);
            Assert.Contains("new TargetProtocol.StringList", binding, StringComparison.Ordinal);
            Assert.Contains("new TargetProtocol.VectorNameList", binding, StringComparison.Ordinal);
            Assert.Contains("!item.HasName", binding, StringComparison.Ordinal);
            Assert.Contains("Name = value.VectorName", binding, StringComparison.Ordinal);

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

    private const string IdentityReferenceArguments = """
        [
          { "argument_id": "chart", "ordinal": 10, "mp_name": "Chart", "direction": "input", "result_only": "no", "semantic_type": "chart_name", "data_classification": "object_identifier", "input": { "presence": "required", "omission_behavior": "reject_request", "default": { "status": "none" } }, "sdk_binding": { "status": "available", "setter": "SetChartNameArg", "getter": null }, "documentation": "Chart name." },
          { "argument_id": "cloud", "ordinal": 11, "mp_name": "Cloud", "direction": "input", "result_only": "no", "semantic_type": "cloud_name", "data_classification": "object_identifier", "input": { "presence": "required", "omission_behavior": "reject_request", "default": { "status": "none" } }, "sdk_binding": { "status": "available", "setter": "SetCloudNameArg", "getter": null }, "documentation": "Cloud name." },
          { "argument_id": "instrument", "ordinal": 12, "mp_name": "Instrument", "direction": "input_output", "result_only": "no", "semantic_type": "collection_instrument_id", "data_classification": "object_identifier", "input": { "presence": "required", "omission_behavior": "reject_request", "default": { "status": "none" } }, "sdk_binding": { "status": "available", "setter": "SetColInstIdArg", "getter": "GetColInstIdArg" }, "documentation": "Instrument identifier." },
          { "argument_id": "instruments", "ordinal": 13, "mp_name": "Instruments", "direction": "input_output", "result_only": "no", "semantic_type": "collection_instrument_id_list", "data_classification": "object_identifier", "input": { "presence": "required", "omission_behavior": "reject_request", "default": { "status": "none" } }, "sdk_binding": { "status": "available", "setter": "SetColInstIdRefListArg", "getter": "GetColInstIdRefListArg" }, "documentation": "Instrument identifiers." },
          { "argument_id": "machine", "ordinal": 14, "mp_name": "Machine", "direction": "input", "result_only": "no", "semantic_type": "collection_machine_id", "data_classification": "object_identifier", "input": { "presence": "required", "omission_behavior": "reject_request", "default": { "status": "none" } }, "sdk_binding": { "status": "available", "setter": "SetColMachineIdArg", "getter": null }, "documentation": "Machine identifier." },
          { "argument_id": "groups", "ordinal": 15, "mp_name": "Groups", "direction": "input", "result_only": "no", "semantic_type": "collection_group_name_list", "data_classification": "object_identifier", "input": { "presence": "required", "omission_behavior": "reject_request", "default": { "status": "none" } }, "sdk_binding": { "status": "available", "setter": "SetCollectionGroupNameRefListArg", "getter": null }, "documentation": "Group names." },
          { "argument_id": "collection", "ordinal": 16, "mp_name": "Collection", "direction": "input_output", "result_only": "no", "semantic_type": "collection_name", "data_classification": "object_identifier", "input": { "presence": "required", "omission_behavior": "reject_request", "default": { "status": "none" } }, "sdk_binding": { "status": "available", "setter": "SetCollectionNameArg", "getter": "GetCollectionNameArg" }, "documentation": "Collection name." },
          { "argument_id": "object", "ordinal": 17, "mp_name": "Object", "direction": "input_output", "result_only": "no", "semantic_type": "collection_object_name", "data_classification": "object_identifier", "input": { "presence": "required", "omission_behavior": "reject_request", "default": { "status": "none" } }, "sdk_binding": { "status": "available", "setter": "SetCollectionObjectNameArg2", "getter": "GetCollectionObjectNameArg" }, "documentation": "Object name." },
          { "argument_id": "objects", "ordinal": 18, "mp_name": "Objects", "direction": "input_output", "result_only": "no", "semantic_type": "collection_object_name_list", "data_classification": "object_identifier", "input": { "presence": "required", "omission_behavior": "reject_request", "default": { "status": "none" } }, "sdk_binding": { "status": "available", "setter": "SetCollectionObjectNameRefListArg", "getter": "GetCollectionObjectNameRefListArg" }, "documentation": "Object names." },
          { "argument_id": "collection_vector_group", "ordinal": 19, "mp_name": "Vector Group", "direction": "input", "result_only": "no", "semantic_type": "collection_vector_group_name", "data_classification": "object_identifier", "input": { "presence": "required", "omission_behavior": "reject_request", "default": { "status": "none" } }, "sdk_binding": { "status": "available", "setter": "SetColVectorGroupNameArg", "getter": null }, "documentation": "Collection vector group." },
          { "argument_id": "collection_vector_groups", "ordinal": 20, "mp_name": "Vector Groups", "direction": "input", "result_only": "no", "semantic_type": "collection_vector_group_name_list", "data_classification": "object_identifier", "input": { "presence": "required", "omission_behavior": "reject_request", "default": { "status": "none" } }, "sdk_binding": { "status": "available", "setter": "SetCollectionVectorGroupNameRefListArg", "getter": null }, "documentation": "Collection vector groups." },
          { "argument_id": "frame", "ordinal": 21, "mp_name": "Frame", "direction": "input", "result_only": "no", "semantic_type": "frame_name", "data_classification": "object_identifier", "input": { "presence": "required", "omission_behavior": "reject_request", "default": { "status": "none" } }, "sdk_binding": { "status": "available", "setter": "SetFrameNameArg", "getter": null }, "documentation": "Frame name." },
          { "argument_id": "points", "ordinal": 22, "mp_name": "Points", "direction": "input_output", "result_only": "no", "semantic_type": "point_name_list", "data_classification": "object_identifier", "input": { "presence": "required", "omission_behavior": "reject_request", "default": { "status": "none" } }, "sdk_binding": { "status": "available", "setter": "SetPointNameRefListArg", "getter": "GetPointNameRefListArg" }, "documentation": "Point names." },
          { "argument_id": "strings", "ordinal": 23, "mp_name": "Strings", "direction": "input_output", "result_only": "no", "semantic_type": "string_list", "data_classification": "proprietary", "input": { "presence": "required", "omission_behavior": "reject_request", "default": { "status": "none" } }, "sdk_binding": { "status": "available", "setter": "SetStringRefListArg", "getter": "GetStringRefListArg" }, "documentation": "Strings." },
          { "argument_id": "vector_group", "ordinal": 24, "mp_name": "Vector Group Name", "direction": "input", "result_only": "no", "semantic_type": "vector_group_name", "data_classification": "object_identifier", "input": { "presence": "required", "omission_behavior": "reject_request", "default": { "status": "none" } }, "sdk_binding": { "status": "available", "setter": "SetVectorGroupNameArg", "getter": null }, "documentation": "Vector group name." },
          { "argument_id": "vectors", "ordinal": 25, "mp_name": "Vectors", "direction": "input_output", "result_only": "no", "semantic_type": "vector_name_list", "data_classification": "object_identifier", "input": { "presence": "required", "omission_behavior": "reject_request", "default": { "status": "none" } }, "sdk_binding": { "status": "available", "setter": "SetVectorNameRefListArg", "getter": "GetVectorNameRefListArg" }, "documentation": "Vector names." },
          { "argument_id": "view", "ordinal": 26, "mp_name": "View", "direction": "input", "result_only": "no", "semantic_type": "view_name", "data_classification": "object_identifier", "input": { "presence": "required", "omission_behavior": "reject_request", "default": { "status": "none" } }, "sdk_binding": { "status": "available", "setter": "SetViewNameArg", "getter": null }, "documentation": "View name." }
        ]
        """;
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
