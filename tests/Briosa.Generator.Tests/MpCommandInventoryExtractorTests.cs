using System.Text.Json;
using Briosa.Generator;

namespace Briosa.Generator.Tests;

public sealed class MpCommandInventoryExtractorTests
{
    [Fact]
    public void ExtractionIsDeterministicAndKeepsRawEvidenceOutOfOutput()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var documentationRoot = Path.Combine(root, "documentation");
            var sdkCodeRoot = Path.Combine(root, "sdk-code");
            WriteFile(
                Path.Combine(documentationRoot, "AnalysisOperations", "GetPointProperties.htm"),
                """
                <html><body>
                  <h1>Get Point Properties</h1>
                  <p>Vendor prose that must not be copied.</p>
                  <h2>Input Arguments</h2>
                  <table><tr><td>0</td><td>Point Name</td><td>Point Name</td><td>A proprietary description.</td></tr></table>
                  <h2>Return Arguments</h2>
                  <table><tr><td>1</td><td>Double</td><td>Planar Offset</td><td>Another proprietary description.</td></tr></table>
                  <h2>Returned Status</h2>
                  <table><tr><td>SUCCESS</td><td>Success text.</td></tr></table>
                </body></html>
                """);
            WriteFile(
                Path.Combine(sdkCodeRoot, "AnalysisOperations.txt"),
                """
                NrkSdk.SetStep("Get Point Properties")
                    NrkSdk.SetPointNameArg("Point Name", "", "", "")
                NrkSdk.ExecuteStep( )

                Dim value As Double
                NrkSdk.GetDoubleArg("Planar Offset", value)
                """);

            var first = MpCommandInventoryExtractor.Extract(
                "2026.1.0529.7",
                documentationRoot,
                sdkCodeRoot);
            var second = MpCommandInventoryExtractor.Extract(
                "2026.1.0529.7",
                documentationRoot,
                sdkCodeRoot);

            Assert.Equal(first.InventoryJson, second.InventoryJson);
            Assert.Equal(first.ReportMarkdown, second.ReportMarkdown);
            Assert.Equal(1, first.Inventory.Summary.CommandCount);
            Assert.Equal(1, first.Inventory.Summary.MatchedCommandCount);

            var command = Assert.Single(first.Inventory.Commands);
            Assert.Equal("Get Point Properties", command.MpStep);
            Assert.Equal("documented", command.OverallOutcome);
            Assert.Empty(command.Findings);

            var input = Assert.Single(command.Arguments, argument => argument.MpName == "Point Name");
            Assert.Equal("input", input.Direction);
            Assert.Equal("SetPointNameArg", input.SdkBinding.Setter.Method);
            Assert.Equal("not_observed", input.SdkBinding.Getter.Status);

            var output = Assert.Single(command.Arguments, argument => argument.MpName == "Planar Offset");
            Assert.Equal("output", output.Direction);
            Assert.Equal("yes", output.ResultOnly);
            Assert.Equal("GetDoubleArg", output.SdkBinding.Getter.Method);

            Assert.DoesNotContain(root, first.InventoryJson, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Vendor prose", first.InventoryJson, StringComparison.Ordinal);
            Assert.DoesNotContain("proprietary description", first.InventoryJson, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    [Fact]
    public void ExtractionRecursesAndPreservesUnsupportedAndUnknownMetadata()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var documentationRoot = Path.Combine(root, "documentation");
            var sdkCodeRoot = Path.Combine(root, "sdk-code");
            WriteFile(
                Path.Combine(documentationRoot, "FileOperations", "Nested", "Unsupported.htm"),
                """
                <html><body>
                  <h1>Unsupported Example</h1>
                  <h2>Input Arguments</h2>
                  <table><tr><td>0</td><td>File Path</td><td>Path (Optional)</td><td>This input is optional.</td></tr></table>
                  <h2>Return Arguments</h2>
                </body></html>
                """);
            WriteFile(
                Path.Combine(sdkCodeRoot, "FileOperations_Nested.txt"),
                """
                NrkSdk.SetStep("Unsupported Example")
                    NrkSdk.NOT_SUPPORTED("Path (Optional)")
                NrkSdk.ExecuteStep( )
                """);

            var extraction = MpCommandInventoryExtractor.Extract(
                "2026.1.0529.7",
                documentationRoot,
                sdkCodeRoot);

            var command = Assert.Single(extraction.Inventory.Commands);
            Assert.Equal(["FileOperations", "Nested"], command.CategoryPath);
            Assert.Equal("missing", command.OverallOutcome);
            Assert.Contains("missing_returned_status_section", command.Findings);

            var argument = Assert.Single(command.Arguments);
            Assert.Equal("optional", argument.Presence);
            Assert.Equal("unavailable", argument.SdkBinding.Setter.Status);
            Assert.Contains("sdk_setter_unavailable", argument.Findings);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    [Fact]
    public void DuplicateStepNamesAreMatchedByCategoryEvidence()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var documentationRoot = Path.Combine(root, "documentation");
            var sdkCodeRoot = Path.Combine(root, "sdk-code");
            WriteFile(
                Path.Combine(documentationRoot, "FileOperations", "Save.htm"),
                CommandDocument("Save", string.Empty));
            WriteFile(
                Path.Combine(documentationRoot, "ExcelDirectConnect", "Save.htm"),
                CommandDocument(
                    "Save",
                    "<tr><td>0</td><td>Integer</td><td>Workbook Handle</td><td>A handle.</td></tr>"));
            WriteFile(
                Path.Combine(sdkCodeRoot, "FileOperations.txt"),
                "NrkSdk.SetStep(\"Save\")\nNrkSdk.ExecuteStep( )\n");
            WriteFile(
                Path.Combine(sdkCodeRoot, "ExcelDirectConnect.txt"),
                "NrkSdk.SetStep(\"Save\")\nNrkSdk.SetIntegerArg(\"Workbook Handle\", 0)\nNrkSdk.ExecuteStep( )\n");

            var extraction = MpCommandInventoryExtractor.Extract(
                "2026.1.0529.7",
                documentationRoot,
                sdkCodeRoot);

            Assert.Equal(2, extraction.Inventory.Summary.MatchedCommandCount);
            Assert.Equal(0, extraction.Inventory.Summary.AmbiguousCommandCount);
            var excelSave = Assert.Single(
                extraction.Inventory.Commands,
                command => command.CategoryPath.SequenceEqual(["ExcelDirectConnect"]));
            var workbookHandle = Assert.Single(excelSave.Arguments);
            Assert.Equal("SetIntegerArg", workbookHandle.SdkBinding.Setter.Method);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    [Fact]
    public void UniqueTypographyDifferencesAreMatchedWithoutOverwritingExactEvidence()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var documentationRoot = Path.Combine(root, "documentation");
            var sdkCodeRoot = Path.Combine(root, "sdk-code");
            WriteFile(
                Path.Combine(documentationRoot, "UtilityOperations", "Example.htm"),
                CommandDocument(
                    "Example’s Command",
                    "<tr><td>0</td><td>Boolean</td><td>Enabled?</td><td>A value.</td></tr>"));
            WriteFile(
                Path.Combine(sdkCodeRoot, "UtilityOperations.txt"),
                "NrkSdk.SetStep(\"Examples Command\")\nNrkSdk.SetBoolArg(\"Enabled\", false)\nNrkSdk.ExecuteStep( )\n");

            var extraction = MpCommandInventoryExtractor.Extract(
                "2026.1.0529.7",
                documentationRoot,
                sdkCodeRoot);

            Assert.Equal(1, extraction.Inventory.Summary.MatchedCommandCount);
            var command = Assert.Single(extraction.Inventory.Commands);
            Assert.Equal("Example’s Command", command.MpStep);
            Assert.Contains("mp_step_text_difference", command.Findings);
            var sdkEvidence = Assert.Single(command.SdkEvidence);
            Assert.Equal("Examples Command", sdkEvidence.MpStep);

            var argument = Assert.Single(command.Arguments);
            Assert.Contains("argument_name_text_difference", argument.Findings);
            Assert.Equal("Enabled?", argument.MpName);
            Assert.Equal("Enabled", argument.SdkBinding.Setter.ArgumentName);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    [Fact]
    public void CommittedInventoryIsPortableAndSelfConsistent()
    {
        var repositoryRoot = FindRepositoryRoot();
        var schemaPath = Path.Combine(
            repositoryRoot.FullName,
            "inventory",
            "schemas",
            "v1",
            "inventory.schema.json");
        using (var schemaDocument = JsonDocument.Parse(File.ReadAllText(schemaPath)))
        {
            Assert.Equal(
                "http://json-schema.org/draft-07/schema#",
                schemaDocument.RootElement.GetProperty("$schema").GetString());
        }

        var inventoryPath = Path.Combine(
            repositoryRoot.FullName,
            "inventory",
            "sa",
            "2026.1.0529.7",
            "inventory.json");
        var json = File.ReadAllText(inventoryPath);
        Assert.DoesNotContain(@"C:\Users\", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(@"C:\Program Files", json, StringComparison.OrdinalIgnoreCase);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.Equal("../../schemas/v1/inventory.schema.json", root.GetProperty("$schema").GetString());
        Assert.Equal("2026.1.0529.7", root.GetProperty("spatial_analyzer_target").GetString());

        var commands = root.GetProperty("commands").EnumerateArray().ToList();
        Assert.Equal(root.GetProperty("summary").GetProperty("command_count").GetInt32(), commands.Count);
        var keys = commands
            .Select(command => command.GetProperty("inventory_key").GetString()!)
            .ToList();
        Assert.Equal(keys.Order(StringComparer.Ordinal), keys);
        Assert.Equal(keys.Count, keys.Distinct(StringComparer.Ordinal).Count());

        foreach (var command in commands)
        {
            var documentation = command.GetProperty("documentation");
            if (documentation.ValueKind != JsonValueKind.Null)
            {
                AssertPortableReference(documentation.GetProperty("reference").GetString()!);
            }

            foreach (var evidence in command.GetProperty("sdk_evidence").EnumerateArray())
            {
                AssertPortableReference(evidence.GetProperty("reference").GetString()!);
                Assert.False(string.IsNullOrWhiteSpace(evidence.GetProperty("mp_step").GetString()));
            }
        }

        var pointProperties = Assert.Single(
            commands,
            command => command.GetProperty("mp_step").GetString() == "Get Point Properties");
        var arguments = pointProperties.GetProperty("arguments").EnumerateArray().ToList();
        Assert.Equal(9, arguments.Count);
        Assert.Single(
            arguments,
            argument =>
                argument.GetProperty("direction").GetString() == "input" &&
                argument.GetProperty("sdk_binding").GetProperty("setter").GetProperty("method").GetString() ==
                "SetPointNameArg");
        Assert.Equal(
            8,
            arguments.Count(argument =>
                argument.GetProperty("direction").GetString() == "output" &&
                argument.GetProperty("result_only").GetString() == "yes" &&
                argument.GetProperty("sdk_binding").GetProperty("getter").GetProperty("status").GetString() ==
                "available"));
    }

    private static void AssertPortableReference(string reference)
    {
        Assert.False(Path.IsPathRooted(reference));
        Assert.DoesNotContain("\\", reference, StringComparison.Ordinal);
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Briosa.slnx")))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new InvalidOperationException("Could not find the repository root.");
    }

    private static string CommandDocument(string step, string inputRow) =>
        $"""
        <html><body>
          <h1>{step}</h1>
          <h2>Input Arguments</h2>
          <table>{inputRow}</table>
          <h2>Return Arguments</h2>
          <h2>Returned Status</h2>
        </body></html>
        """;

    private static string CreateTemporaryRoot()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"briosa-inventory-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        return root;
    }

    private static void WriteFile(string path, string contents)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents);
    }

    private static void DeleteTemporaryRoot(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
