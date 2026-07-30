using System.Text.Json.Nodes;
using Briosa.SpatialAnalyzer.IntegrationTests;

namespace Briosa.Generator.Tests;

public sealed class LicensedFileOperationMatrixTests
{
    private const string Issue80 =
        "https://github.com/spatialanalyzer/briosa/issues/80";

    [Fact]
    public void FixtureDescriptorAcceptsDocumentedSnakeCaseShape()
    {
        const string json = """
            {
              "job_path": "C:\\fixtures\\objects.xit64",
              "input_path": "C:\\fixtures\\measurements.xml",
              "object": {
                "collection_name": "Collection",
                "name": "FixturePointGroup",
                "type": "Point_Group"
              },
              "items": [
                {
                  "collection_name": "Collection",
                  "name": "FixtureEvent",
                  "type": "Event"
                }
              ]
            }
            """;

        var descriptor = FixtureDescriptorReader.Parse(json);

        Assert.Equal("C:\\fixtures\\objects.xit64", descriptor.JobPath);
        Assert.Equal("C:\\fixtures\\measurements.xml", descriptor.InputPath);
        Assert.Equal("Collection", descriptor.Object!.CollectionName);
        Assert.Equal("FixturePointGroup", descriptor.Object.Name);
        Assert.Equal("Point_Group", descriptor.Object.Type);
        var item = Assert.Single(descriptor.Items!);
        Assert.Equal("FixtureEvent", item.Name);
        Assert.Equal("Event", item.Type);
    }

    [Fact]
    public void MatrixAccountsForEveryAtRiskIssue80Candidate()
    {
        var root = FindRepositoryRoot();
        var expected = new[]
        {
            new ExpectedScenario("documentation:FileOperations/Save.htm", "Save", "save", "ObjectiveSAClient.Save", "generated_by_test", "performed_successfully", "performed"),
            new ExpectedScenario("documentation:FileOperations/SaveAs.htm", "Save As...", "save_as", "ObjectiveSAClient.SaveAs", "generated_by_test", "performed_successfully", "performed"),
            new ExpectedScenario("documentation:FileOperations/SaveAsReadOnlyTemplate.htm", "Save As Read-Only Template", "save_as_read_only_template", "ObjectiveSAClient.SaveAsReadOnlyTemplate", "generated_by_test", "performed_successfully", "performed"),
            new ExpectedScenario("sdk:FileOperations_FileExport.txt#1", "Export ASCII Points", "export_ascii_points", "ObjectiveSAClient.ExportASCIIPoints", "generated_by_test", "performed_successfully", "performed"),
            new ExpectedScenario("sdk:FileOperations_FileExport.txt#2", "Export ASCII Point Set", "export_ascii_point_set_wrong_type", "ObjectiveSAClient.ExportASCIIPointSet", "generated_wrong_type_by_test", "wrong_type_rejected", "performed"),
            new ExpectedScenario("sdk:EventOperations.txt#4", "Export Event Ref List", "export_event_ref_list", "ObjectiveSAClient.ExportEventRefList", "local_event_reference_list_required", "no_valid_fixture", "not_performed"),
            new ExpectedScenario("documentation:FileOperations/XML/ImportNominalsFromXMLFile.htm", "Import Nominals from XML File", "import_nominals_xml", "ObjectiveSAClient.ImportNominalsFromXMLFile", "exact_target_xml_fixture_required", "not_performed", "not_performed"),
            new ExpectedScenario("documentation:FileOperations/XML/MergeMeasurementsintoXML.htm", "Merge Measurements into XML File", "merge_measurements_xml", "ObjectiveSAClient.MergeMeasurementsIntoXMLFile", "exact_target_xml_and_point_group_fixture_required", "not_performed", "not_performed"),
            new ExpectedScenario("documentation:ProcessFlowOperations/OutputSAReportToPDF.htm", "Output SA Report to PDF", "output_report_pdf", "ObjectiveSAClient.OutputSAReportToPDF", "generated_by_test", "performed_successfully", "performed"),
            new ExpectedScenario("sdk:FileOperations_FileImport.txt#16", "Import VSTARS Cameras", "import_vstars_cameras", "ObjectiveSAClient.ImportVSTARSCameras", "licensed_third_party_fixture_required", "not_performed", "not_performed"),
            new ExpectedScenario("sdk:FileOperations_FileImport.txt#19", "Import Polyworks File", "import_polyworks", "ObjectiveSAClient.ImportPolyworksFile", "licensed_third_party_fixture_required", "not_performed", "not_performed")
        }.ToDictionary(value => value.InventoryKey, StringComparer.Ordinal);
        var matrix = JsonNode.Parse(File.ReadAllText(Path.Combine(
            root.FullName,
            "tests",
            "Briosa.SpatialAnalyzer.IntegrationTests",
            "file-operation-matrix.json")))!.AsObject();
        var scenarios = matrix["scenarios"]!.AsArray();

        Assert.Equal(11, scenarios.Count);
        Assert.Equal(
            11,
            scenarios.Select(node => node!["inventory_key"]!.GetValue<string>())
                .Distinct(StringComparer.Ordinal)
                .Count());
        Assert.Equal(
            11,
            scenarios.Select(node => node!["scenario"]!.GetValue<string>())
                .Distinct(StringComparer.Ordinal)
                .Count());
        Assert.Equal(
            expected.Keys.Order(StringComparer.Ordinal),
            scenarios.Select(node => node!["inventory_key"]!.GetValue<string>())
                .Order(StringComparer.Ordinal));
        Assert.All(scenarios, scenario =>
        {
            var row = expected[scenario!["inventory_key"]!.GetValue<string>()];
            Assert.Equal(row.MpStep, scenario["mp_step"]!.GetValue<string>());
            Assert.Equal(row.Scenario, scenario["scenario"]!.GetValue<string>());
            Assert.Equal(row.ObjectiveSaMethod, scenario["objectivesa_method"]!.GetValue<string>());
            Assert.Equal(row.FixtureStatus, scenario["fixture_status"]!.GetValue<string>());
            Assert.Equal(row.ValidationStatus, scenario["validation_status"]!.GetValue<string>());
        });

        var matrixSchema = JsonNode.Parse(File.ReadAllText(Path.Combine(
            root.FullName,
            "tests",
            "Briosa.SpatialAnalyzer.IntegrationTests",
            "file-operation-matrix.schema.json")))!.AsObject();
        var scenarioSchema = matrixSchema["properties"]!["scenarios"]!["items"]!;
        Assert.Equal(
            expected.Values.Select(value => value.FixtureStatus).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal),
            scenarioSchema["properties"]!["fixture_status"]!["enum"]!.AsArray()
                .Select(node => node!.GetValue<string>()).Order(StringComparer.Ordinal));
        Assert.Equal(
            expected.Values.Select(value => value.ValidationStatus).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal),
            scenarioSchema["properties"]!["validation_status"]!["enum"]!.AsArray()
                .Select(node => node!.GetValue<string>()).Order(StringComparer.Ordinal));

        var dispositionEntries = Directory
            .EnumerateFiles(
                Path.Combine(
                    root.FullName,
                    "disposition",
                    "sa",
                    "2026.1.0529.7",
                    "categories"),
                "*.json")
            .SelectMany(path =>
                JsonNode.Parse(File.ReadAllText(path))!["entries"]!.AsArray())
            .Where(node => node!["decision_references"]!.AsArray()
                .Any(reference => reference!.GetValue<string>() == Issue80))
            .ToArray();

        Assert.Equal(11, dispositionEntries.Length);
        Assert.All(
            dispositionEntries,
            entry =>
            {
                Assert.Equal("approved_candidate", entry!["disposition"]!.GetValue<string>());
                Assert.Equal("wave_3", entry["delivery_wave"]!.GetValue<string>());
                Assert.Empty(entry["blocker_references"]!.AsArray());
                Assert.Equal("resolved", entry["command_shape"]!["status"]!.GetValue<string>());
                Assert.Equal(
                    "constrained_candidate",
                    entry["operation_contract"]!["decision"]!.GetValue<string>());
                Assert.All(
                    entry["command_shape"]!["arguments"]!.AsArray(),
                    argument =>
                    {
                        Assert.Equal(
                            "required",
                            argument!["input"]!["presence"]!.GetValue<string>());
                        Assert.Equal(
                            "reject_request",
                            argument["input"]!["omission_behavior"]!.GetValue<string>());
                    });
            });

        foreach (var entry in dispositionEntries)
        {
            var key = entry!["inventory_key"]!.GetValue<string>();
            var operationContract = entry["operation_contract"]!;
            var expectedValidation = expected[key].DispositionValidationStatus;
            Assert.Equal(
                expectedValidation,
                operationContract["validation_status"]!.GetValue<string>());
            var limitations = operationContract["evidence_limitations"]!.AsArray()
                .Select(node => node!.GetValue<string>())
                .ToArray();
            if (expectedValidation == "performed")
            {
                Assert.DoesNotContain("live_validation_not_performed", limitations);
            }
            else
            {
                Assert.Contains("live_validation_not_performed", limitations);
            }
        }

        Assert.Equal(
            dispositionEntries.Select(entry => entry!["inventory_key"]!.GetValue<string>())
                .Order(StringComparer.Ordinal),
            scenarios.Select(node => node!["inventory_key"]!.GetValue<string>())
                .Order(StringComparer.Ordinal));
    }

    private sealed record ExpectedScenario(
        string InventoryKey,
        string MpStep,
        string Scenario,
        string ObjectiveSaMethod,
        string FixtureStatus,
        string ValidationStatus,
        string DispositionValidationStatus);

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "Briosa.slnx")))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new DirectoryNotFoundException(
            "Could not find the repository root.");
    }
}
