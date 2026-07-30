using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Briosa.Generator;

namespace Briosa.Generator.Tests;

public sealed class CommandCatalogScaffolderTests
{
    private const string AngleInventoryKey =
        "documentation:AnalysisOperations/AngleBetweenLineAndPlane.htm";
    private const string ItemInventoryKey =
        "documentation:InstrumentOperations/GetLastSolvedTCPFixture.htm";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true
    };

    [Fact]
    public void CommittedEvidenceProducesDeterministicTraceableScaffolds()
    {
        using var fixture = ScaffoldFixture.Create();

        var first = fixture.Generate(fixture.FirstOutput);
        var second = fixture.Generate(fixture.SecondOutput);

        Assert.True(first.IsSuccessful, DisplayConflicts(first));
        Assert.True(second.IsSuccessful, DisplayConflicts(second));
        Assert.Equal(684, first.ApprovedCandidateCount);
        Assert.Equal(13, first.ExistingCatalogOperationCount);
        Assert.Equal(671, first.ScaffoldCount);
        Assert.Equal(ReadTree(fixture.FirstOutput), ReadTree(fixture.SecondOutput));
        Assert.False(File.Exists(CandidatePath(
            fixture.FirstOutput,
            "documentation:FileOperations/GetWorkingDirectory.htm")));
        Assert.False(File.Exists(CandidatePath(
            fixture.FirstOutput,
            "documentation:AnalysisOperations/GetNumberOfCollections.htm")));
        Assert.False(File.Exists(CandidatePath(
            fixture.FirstOutput,
            "documentation:ConstructionOperations/PointsandGroups/ConstructAPointInWorkingCoordinates.htm")));
        Assert.False(File.Exists(CandidatePath(
            fixture.FirstOutput,
            "documentation:ConstructionOperations/PointsandGroups/ConstructaPointAtCircleCenter.htm")));
        Assert.False(File.Exists(CandidatePath(
            fixture.FirstOutput,
            "documentation:ConstructionOperations/PointsandGroups/ConstructaPointAtLineMidPoint.htm")));
        Assert.False(File.Exists(CandidatePath(
            fixture.FirstOutput,
            "documentation:ConstructionOperations/PointsandGroups/ConstructPointFitToPoints.htm")));
        Assert.False(File.Exists(CandidatePath(
            fixture.FirstOutput,
            "documentation:ConstructionOperations/PointsandGroups/ConstructPointGroupfromPointNameRefList.htm")));
        Assert.False(File.Exists(CandidatePath(
            fixture.FirstOutput,
            "documentation:ConstructionOperations/RenamePoint.htm")));
        Assert.False(File.Exists(CandidatePath(
            fixture.FirstOutput,
            "documentation:ConstructionOperations/DeletePoints.htm")));

        var angle = ReadScaffold(fixture.FirstOutput, AngleInventoryKey);
        Assert.Equal("incomplete", angle.ReviewStatus);
        Assert.Null(angle.CatalogDraft.OperationId);
        Assert.Null(angle.CatalogDraft.Protocol);
        Assert.Null(angle.CatalogDraft.Risk.ReplaySafety);
        Assert.NotEmpty(angle.Blockers);
        Assert.Equal(
            [
                "collection_object_name",
                "collection_object_name",
                "floating_point",
                "floating_point",
                "floating_point"
            ],
            angle.Arguments.Select(argument => argument.SemanticType));
        var angleAssignment = Assert.Single(angle.Arguments[0].FamilyAssignments);
        Assert.Equal("SetCollectionObjectNameArg2", angleAssignment.Method);
        Assert.Equal("exact_command_assignment", angleAssignment.Source);
        Assert.Equal(0, angleAssignment.SdkOrder);
        Assert.Equal([0], angleAssignment.DocumentedOrdinals);
        Assert.All(
            angle.Arguments,
            argument =>
            {
                Assert.Null(argument.CatalogFields.ArgumentId);
                Assert.Null(argument.CatalogFields.DataClassification);
                Assert.Null(argument.CatalogFields.FieldNumbers);
                Assert.Null(argument.CatalogFields.Input);
                Assert.Null(argument.CatalogFields.Documentation);
            });
        Assert.Contains(
            "/arguments/0/catalog_fields/field_numbers",
            angle.Blockers,
            StringComparer.Ordinal);
        Assert.Matches("^[a-f0-9]{64}$", angle.SourceFingerprints.InventorySha256);
        Assert.Matches(
            "^[a-f0-9]{64}$",
            angle.SourceFingerprints.ValueFamilyCatalogSha256);

        var item = ReadScaffold(fixture.FirstOutput, ItemInventoryKey);
        var itemArgument = Assert.Single(
            item.Arguments,
            argument => argument.SdkBinding.Setter == "SetCollectionObjectNameArg2");
        Assert.Equal("collection_item_name", itemArgument.SemanticType);
        Assert.NotEqual(angle.Arguments[0].SemanticType, itemArgument.SemanticType);
        Assert.Equal(
            "exact_command_assignment",
            Assert.Single(itemArgument.FamilyAssignments).Source);

        var saveAs = ReadScaffold(
            fixture.FirstOutput,
            "documentation:FileOperations/SaveAs.htm");
        Assert.NotNull(saveAs.ReviewedDisposition.OperationContract);
        Assert.Equal(
            "constrained_candidate",
            saveAs.ReviewedDisposition.OperationContract.Decision);
        Assert.Equal(
            "performed",
            saveAs.ReviewedDisposition.OperationContract.ValidationStatus);
        Assert.DoesNotContain(
            "live_validation_not_performed",
            saveAs.ReviewedDisposition.OperationContract.EvidenceLimitations,
            StringComparer.Ordinal);
        Assert.Contains(
            "replace_mode_requires_explicit_consent",
            saveAs.ReviewedDisposition.OperationContract.Constraints,
            StringComparer.Ordinal);
        Assert.DoesNotContain(
            "/reviewed_disposition/operation_contract/validation_status",
            saveAs.Blockers,
            StringComparer.Ordinal);
    }

    [Fact]
    public void IncrementalGenerationReportsConflictsWithoutOverwritingDrafts()
    {
        using var fixture = ScaffoldFixture.Create();
        var initial = fixture.Generate(fixture.FirstOutput);
        Assert.True(initial.IsSuccessful, DisplayConflicts(initial));
        var path = CandidatePath(fixture.FirstOutput, AngleInventoryKey);
        File.AppendAllText(path, " ", Encoding.UTF8);
        var changedText = File.ReadAllText(path);
        var manifestText = File.ReadAllText(Path.Combine(fixture.FirstOutput, "manifest.json"));

        var repeated = fixture.Generate(fixture.FirstOutput);

        Assert.False(repeated.IsSuccessful);
        var conflict = Assert.Single(repeated.Conflicts);
        Assert.Equal("existing_scaffold_differs", conflict.Reason);
        Assert.Equal(changedText, File.ReadAllText(path));
        Assert.Equal(
            manifestText,
            File.ReadAllText(Path.Combine(fixture.FirstOutput, "manifest.json")));
    }

    [Fact]
    public void IncompleteScaffoldCannotValidateOrGenerateAsAPublicOperation()
    {
        using var fixture = ScaffoldFixture.Create();
        var generation = fixture.Generate(fixture.FirstOutput);
        Assert.True(generation.IsSuccessful, DisplayConflicts(generation));
        var catalogCopy = Path.Combine(fixture.Root, "catalog-copy");
        CopyDirectory(fixture.CatalogRoot, catalogCopy);
        var targetDirectory = Path.Combine(catalogCopy, "sa", "2026.1.0529.7");
        var invalidRelativePath = "operations/review.todo.json";
        File.Copy(
            CandidatePath(fixture.FirstOutput, AngleInventoryKey),
            Path.Combine(targetDirectory, "operations", "review.todo.json"));
        var manifestPath = Path.Combine(targetDirectory, "catalog.json");
        var manifest = JsonNode.Parse(File.ReadAllText(manifestPath))!.AsObject();
        manifest["operation_files"]!.AsArray().Add(invalidRelativePath);
        File.WriteAllText(
            manifestPath,
            manifest.ToJsonString(JsonOptions) + Environment.NewLine);

        var validation = CommandCatalogValidator.ValidateDirectory(catalogCopy);

        Assert.False(validation.IsValid);
        Assert.Throws<InvalidDataException>(() => CommandCatalogGenerator.Generate(
            catalogCopy,
            Path.Combine(fixture.Root, "invalid-generated-output")));
    }

    [Fact]
    public void ScaffoldOutputCannotOverlapTheSupportedCatalog()
    {
        using var fixture = ScaffoldFixture.Create();

        var exception = Assert.Throws<InvalidDataException>(() =>
            fixture.Generate(Path.Combine(fixture.CatalogRoot, "review-scaffolds")));

        Assert.Contains("must be separate", exception.Message, StringComparison.Ordinal);
    }

    private static CommandCatalogScaffoldDocument ReadScaffold(
        string outputRoot,
        string inventoryKey) =>
        JsonSerializer.Deserialize<CommandCatalogScaffoldDocument>(
            File.ReadAllText(CandidatePath(outputRoot, inventoryKey)),
            JsonOptions)!;

    private static string CandidatePath(string outputRoot, string inventoryKey)
    {
        var hash = Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(inventoryKey)));
        return Path.Combine(outputRoot, "candidates", $"{hash}.json");
    }

    private static string[] ReadTree(string root) =>
        Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Select(path =>
                $"{Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/')}\n" +
                File.ReadAllText(path))
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

    private static string DisplayConflicts(CommandCatalogScaffoldGenerationResult result) =>
        string.Join(
            Environment.NewLine,
            result.Conflicts.Select(conflict => $"{conflict.Path}: {conflict.Reason}"));

    private static void CopyDirectory(string source, string destination)
    {
        foreach (var directory in Directory.EnumerateDirectories(
                     source,
                     "*",
                     SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(
                destination,
                Path.GetRelativePath(source, directory)));
        }

        Directory.CreateDirectory(destination);
        foreach (var file in Directory.EnumerateFiles(
                     source,
                     "*",
                     SearchOption.AllDirectories))
        {
            var destinationPath = Path.Combine(
                destination,
                Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            File.Copy(file, destinationPath);
        }
    }

    private sealed class ScaffoldFixture : IDisposable
    {
        private ScaffoldFixture(string repositoryRoot, string root)
        {
            RepositoryRoot = repositoryRoot;
            Root = root;
            InventoryPath = Path.Combine(
                repositoryRoot,
                "inventory",
                "sa",
                "2026.1.0529.7",
                "inventory.json");
            DispositionDirectory = Path.Combine(
                repositoryRoot,
                "disposition",
                "sa",
                "2026.1.0529.7");
            ValueFamilyCatalogPath = Path.Combine(
                repositoryRoot,
                "values",
                "sa",
                "2026.1.0529.7",
                "catalog.json");
            CatalogRoot = Path.Combine(repositoryRoot, "catalog");
            FirstOutput = Path.Combine(root, "first");
            SecondOutput = Path.Combine(root, "second");
        }

        public string RepositoryRoot { get; }

        public string Root { get; }

        public string InventoryPath { get; }

        public string DispositionDirectory { get; }

        public string ValueFamilyCatalogPath { get; }

        public string CatalogRoot { get; }

        public string FirstOutput { get; }

        public string SecondOutput { get; }

        public static ScaffoldFixture Create()
        {
            var repositoryRoot = FindRepositoryRoot().FullName;
            return new ScaffoldFixture(
                repositoryRoot,
                Path.Combine(
                    Path.GetTempPath(),
                    $"briosa-catalog-scaffold-tests-{Guid.NewGuid():N}"));
        }

        public CommandCatalogScaffoldGenerationResult Generate(string outputDirectory) =>
            CommandCatalogScaffolder.Generate(
                InventoryPath,
                DispositionDirectory,
                ValueFamilyCatalogPath,
                CatalogRoot,
                outputDirectory);

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "Briosa.slnx")))
        {
            directory = directory.Parent;
        }

        return directory ??
            throw new InvalidOperationException("Could not find the repository root.");
    }
}
