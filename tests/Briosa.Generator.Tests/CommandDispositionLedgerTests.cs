using System.Text.Json;
using System.Text.Json.Nodes;
using Briosa.Generator;

namespace Briosa.Generator.Tests;

public sealed class CommandDispositionLedgerTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true
    };

    [Fact]
    public void CommittedLedgerCoversTheCompleteInventory()
    {
        var repositoryRoot = FindRepositoryRoot().FullName;
        var result = CommandDispositionLedger.Validate(
            Path.Combine(
                repositoryRoot,
                "inventory",
                "sa",
                "2026.1.0529.7",
                "inventory.json"),
            Path.Combine(
                repositoryRoot,
                "disposition",
                "sa",
                "2026.1.0529.7"));

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors));
        Assert.Equal(1, result.TargetCount);
        Assert.Equal(1412, result.EntryCount);
    }

    [Fact]
    public void Issue52ReviewPublishesExactIntentionalExclusions()
    {
        var entries = ReadCommittedEntries();
        var exclusions = entries
            .Where(entry => string.Equals(
                entry.Disposition,
                "intentional_exclusion",
                StringComparison.Ordinal))
            .ToArray();
        var counts = exclusions
            .SelectMany(entry => entry.ReasonCodes)
            .GroupBy(reason => reason, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        Assert.Equal(349, exclusions.Length);
        Assert.Equal(39, counts["client_owned_external_integration"]);
        Assert.Equal(14, counts["client_owned_office_integration"]);
        Assert.Equal(17, counts["client_owned_serialization"]);
        Assert.Equal(38, counts["client_owned_spreadsheet_integration"]);
        Assert.Equal(63, counts["client_owned_state_and_control_flow"]);
        Assert.Equal(54, counts["client_owned_user_experience"]);
        Assert.Equal(60, counts["client_owned_value_computation"]);
        Assert.Equal(64, counts["client_owned_value_construction"]);
        Assert.All(exclusions, entry =>
        {
            Assert.Equal("reviewed", entry.ReviewState);
            Assert.Equal(
                ["https://github.com/spatialanalyzer/briosa/issues/52"],
                entry.DecisionReferences);
            Assert.Empty(entry.BlockerReferences);
            Assert.Null(entry.DeliveryWave);
        });

        AssertDisposition(
            entries,
            "Vector Operations",
            "Vector Addition",
            "intentional_exclusion");
        AssertDisposition(
            entries,
            "FileOperations",
            "Close JSON File",
            "intentional_exclusion");
        AssertDisposition(
            entries,
            "UtilityOperations",
            "Get OPC UA Node Named Coordinate Frame",
            "intentional_exclusion");
        AssertDisposition(
            entries,
            "ConstructionOperations",
            "Construct Objects From Surface Faces - Runtime Select",
            "intentional_exclusion");

        AssertDisposition(
            entries,
            "Vector Operations",
            "Delete Vector by Name",
            "blocked");
        AssertDisposition(
            entries,
            "FileOperations",
            "Import Nominals from XML File",
            "blocked");
        AssertDisposition(
            entries,
            "ConstructionOperations",
            "Make a Point Name - Ensure Unique",
            "blocked");
        AssertDisposition(
            entries,
            "AnalysisOperations",
            "Get Point Properties",
            "blocked");
        AssertDisposition(
            entries,
            "ProcessFlowOperations",
            "Output SA Report to PDF",
            "blocked");

        var reportPath = Path.Combine(
            FindRepositoryRoot().FullName,
            "disposition",
            "sa",
            "2026.1.0529.7",
            "report.md");
        var report = File.ReadAllText(reportPath);
        Assert.Contains("## Reviewed intentional exclusions", report, StringComparison.Ordinal);
        Assert.Contains(
            "| FileOperations / JSON | Close JSON File |",
            report,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "| FileOperations / XML | Import Nominals from XML File |",
            report,
            StringComparison.Ordinal);
    }

    [Fact]
    public void SyncInitializesEveryCommandAsBlockedAndUnreviewed()
    {
        using var fixture = DispositionFixture.Create();

        var first = CommandDispositionLedger.Sync(fixture.InventoryPath, fixture.TargetDirectory);
        var second = CommandDispositionLedger.Sync(fixture.InventoryPath, fixture.TargetDirectory);
        var entry = fixture.ReadOnlyEntry();

        Assert.Equal(1, first.EntryCount);
        Assert.Equal(1, first.NewEntryCount);
        Assert.Equal(0, second.NewEntryCount);
        Assert.Equal("blocked", entry["disposition"]!.GetValue<string>());
        Assert.Equal("unreviewed", entry["review_state"]!.GetValue<string>());
        Assert.Equal("unknown", entry["risk_effect"]!.GetValue<string>());
        Assert.Equal("unknown", entry["value_families"]![0]!.GetValue<string>());
        Assert.Equal("awaiting_review", entry["reason_codes"]![0]!.GetValue<string>());
        Assert.Equal(
            "https://github.com/spatialanalyzer/briosa/issues/43",
            entry["blocker_references"]![0]!.GetValue<string>());
    }

    [Fact]
    public void ChangedEvidenceRequiresReviewAgain()
    {
        using var fixture = DispositionFixture.Create();
        CommandDispositionLedger.Sync(fixture.InventoryPath, fixture.TargetDirectory);
        fixture.MarkEntryReviewedCandidate();
        CommandDispositionLedger.Sync(fixture.InventoryPath, fixture.TargetDirectory);
        fixture.ChangeInventoryStep("Read Renamed Value");

        var result = CommandDispositionLedger.Sync(
            fixture.InventoryPath,
            fixture.TargetDirectory);
        var entry = fixture.ReadOnlyEntry();

        Assert.Equal(1, result.ReReviewCount);
        Assert.Equal("approved_candidate", entry["disposition"]!.GetValue<string>());
        Assert.Equal("needs_re_review", entry["review_state"]!.GetValue<string>());
        Assert.Contains(
            entry["reason_codes"]!.AsArray(),
            value => value!.GetValue<string>() == "evidence_changed");
        Assert.Contains(
            entry["blocker_references"]!.AsArray(),
            value => value!.GetValue<string>() ==
                "https://github.com/spatialanalyzer/briosa/issues/43");
    }

    [Fact]
    public void ValidationRejectsMissingInventoryCoverage()
    {
        using var fixture = DispositionFixture.Create();
        CommandDispositionLedger.Sync(fixture.InventoryPath, fixture.TargetDirectory);
        fixture.RemoveOnlyEntry();

        var result = CommandDispositionLedger.Validate(
            fixture.InventoryPath,
            fixture.TargetDirectory);

        Assert.Contains(
            result.Errors,
            error => error.Contains("missing inventory key", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidationRejectsUnknownDisposition()
    {
        using var fixture = DispositionFixture.Create();
        CommandDispositionLedger.Sync(fixture.InventoryPath, fixture.TargetDirectory);
        fixture.EditEntry(entry => entry["disposition"] = "maybe_supported");

        var result = CommandDispositionLedger.Validate(
            fixture.InventoryPath,
            fixture.TargetDirectory);

        Assert.Contains(
            result.Errors,
            error => error.Contains("unknown disposition", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidationRejectsContradictoryApprovedCandidate()
    {
        using var fixture = DispositionFixture.Create();
        CommandDispositionLedger.Sync(fixture.InventoryPath, fixture.TargetDirectory);
        fixture.EditEntry(entry =>
        {
            entry["disposition"] = "approved_candidate";
            entry["review_state"] = "reviewed";
            entry["reason_codes"] = new JsonArray("read_only_operation");
            entry["decision_references"] = new JsonArray(
                "https://github.com/spatialanalyzer/briosa/issues/48");
            entry["risk_effect"] = "read_only";
            entry["risk_flags"] = new JsonArray("filesystem_metadata");
            entry["value_families"] = new JsonArray("path");
        });

        var result = CommandDispositionLedger.Validate(
            fixture.InventoryPath,
            fixture.TargetDirectory);

        Assert.Contains(
            result.Errors,
            error => error.Contains(
                "approved candidates require a delivery wave and no blockers",
                StringComparison.Ordinal));
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

    private static CommandDispositionEntry[] ReadCommittedEntries()
    {
        var targetDirectory = Path.Combine(
            FindRepositoryRoot().FullName,
            "disposition",
            "sa",
            "2026.1.0529.7");
        var manifest = JsonSerializer.Deserialize<CommandDispositionManifest>(
            File.ReadAllText(Path.Combine(targetDirectory, "manifest.json")),
            JsonOptions)!;
        return manifest.Shards
            .SelectMany(shard =>
                JsonSerializer.Deserialize<CommandDispositionShard>(
                    File.ReadAllText(Path.Combine(
                        targetDirectory,
                        shard.Path.Replace('/', Path.DirectorySeparatorChar))),
                    JsonOptions)!.Entries)
            .ToArray();
    }

    private static void AssertDisposition(
        IEnumerable<CommandDispositionEntry> entries,
        string category,
        string mpStep,
        string disposition)
    {
        var entry = Assert.Single(entries, entry =>
            string.Equals(entry.CategoryPath[0], category, StringComparison.Ordinal) &&
            string.Equals(entry.MpStep, mpStep, StringComparison.Ordinal));
        Assert.Equal(disposition, entry.Disposition);
    }

    private sealed class DispositionFixture : IDisposable
    {
        private readonly string root;

        private DispositionFixture(string root)
        {
            this.root = root;
            InventoryPath = Path.Combine(
                root,
                "inventory",
                "sa",
                "2026.1.0529.7",
                "inventory.json");
            TargetDirectory = Path.Combine(
                root,
                "disposition",
                "sa",
                "2026.1.0529.7");
        }

        public string InventoryPath { get; }

        public string TargetDirectory { get; }

        public static DispositionFixture Create()
        {
            var fixture = new DispositionFixture(Path.Combine(
                Path.GetTempPath(),
                $"briosa-disposition-tests-{Guid.NewGuid():N}"));
            fixture.WriteInventory("Read Value");
            return fixture;
        }

        public JsonObject ReadOnlyEntry()
        {
            var shard = ReadShard();
            return shard["entries"]!.AsArray().Single()!.AsObject();
        }

        public void MarkEntryReviewedCandidate()
        {
            var shard = ReadShard();
            var entry = shard["entries"]!.AsArray().Single()!.AsObject();
            entry["disposition"] = "approved_candidate";
            entry["review_state"] = "reviewed";
            entry["rationale"] = "Selected for the read-only foundation wave.";
            entry["reason_codes"] = new JsonArray("read_only_operation");
            entry["decision_references"] = new JsonArray(
                "https://github.com/spatialanalyzer/briosa/issues/48");
            entry["blocker_references"] = new JsonArray();
            entry["risk_effect"] = "read_only";
            entry["risk_flags"] = new JsonArray("filesystem_metadata");
            entry["value_families"] = new JsonArray("path");
            entry["delivery_wave"] = "wave_1";
            WriteShard(shard);
        }

        public void EditEntry(Action<JsonObject> edit)
        {
            var shard = ReadShard();
            edit(shard["entries"]!.AsArray().Single()!.AsObject());
            WriteShard(shard);
        }

        public void ChangeInventoryStep(string step) => WriteInventory(step);

        public void RemoveOnlyEntry()
        {
            var shard = ReadShard();
            shard["entries"]!.AsArray().Clear();
            WriteShard(shard);
        }

        public void Dispose()
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }

        private JsonObject ReadShard() =>
            JsonNode.Parse(File.ReadAllText(ShardPath()))!.AsObject();

        private void WriteShard(JsonObject shard) =>
            File.WriteAllText(ShardPath(), shard.ToJsonString(JsonOptions) + Environment.NewLine);

        private string ShardPath() =>
            Directory.EnumerateFiles(
                Path.Combine(TargetDirectory, "categories"),
                "*.json").Single();

        private void WriteInventory(string step)
        {
            var inventory = new MpCommandInventory
            {
                Schema = "../../schemas/v1/inventory.schema.json",
                SchemaVersion = 1,
                SpatialAnalyzerTarget = "2026.1.0529.7",
                Provenance = new MpCommandInventoryProvenance
                {
                    Documentation = Source("documentation_html"),
                    SdkCode = Source("sdk_code_visual_basic")
                },
                Summary = new MpCommandInventorySummary
                {
                    CommandCount = 1,
                    MatchedCommandCount = 1,
                    DocumentationOnlyCommandCount = 0,
                    SdkOnlyCommandCount = 0,
                    AmbiguousCommandCount = 0,
                    FindingCounts = []
                },
                Commands =
                [
                    new MpCommandInventoryCommand
                    {
                        InventoryKey = "utility/read-value",
                        MpStep = step,
                        CategoryPath = ["UtilityOperations"],
                        Documentation = new MpCommandInventoryDocumentEvidence
                        {
                            Reference = "UtilityOperations/read-value.htm",
                            Sha256 = new string('a', 64),
                            HasInputArgumentsSection = true,
                            HasReturnArgumentsSection = true,
                            HasReturnedStatusSection = true
                        },
                        SdkEvidence =
                        [
                            new MpCommandInventorySdkEvidence
                            {
                                Reference = "UtilityOperations.txt",
                                Sha256 = new string('b', 64),
                                Occurrence = 1,
                                MpStep = step
                            }
                        ],
                        OverallOutcome = "matched",
                        Arguments = [],
                        Findings = []
                    }
                ]
            };
            Directory.CreateDirectory(Path.GetDirectoryName(InventoryPath)!);
            File.WriteAllText(
                InventoryPath,
                JsonSerializer.Serialize(inventory, JsonOptions) + Environment.NewLine);
        }

        private static MpCommandInventorySource Source(string kind) =>
            new()
            {
                Kind = kind,
                FileCount = 1,
                RecordCount = 1,
                AggregateSha256 = new string('c', 64),
                SourceMaterialCommitted = false
            };
    }
}
