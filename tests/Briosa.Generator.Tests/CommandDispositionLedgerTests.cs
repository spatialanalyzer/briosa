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
    public void Issue82LeavesNoPendingDefaultCandidates()
    {
        var repositoryRoot = FindRepositoryRoot().FullName;
        var queue = JsonNode.Parse(File.ReadAllText(Path.Combine(
            repositoryRoot,
            "generated",
            "values",
            "sa",
            "2026.1.0529.7",
            "default-review-queue.json")))!.AsObject();
        var summary = queue["summary"]!.AsObject();

        Assert.Equal(421, summary["corroborated_default_count"]!.GetValue<int>());
        Assert.Equal(314, summary["reviewed_no_default_count"]!.GetValue<int>());
        Assert.Equal(0, summary["needs_review_count"]!.GetValue<int>());
        Assert.Equal(1319, summary["no_candidate_count"]!.GetValue<int>());
        Assert.Empty(queue["entries"]!.AsArray());
    }

    [Fact]
    public void Issue53ResolvesEveryCandidateShapeAndScopesRemainingDependencies()
    {
        const string issue53 = "https://github.com/spatialanalyzer/briosa/issues/53";
        const string issue79 = "https://github.com/spatialanalyzer/briosa/issues/79";
        const string issue80 = "https://github.com/spatialanalyzer/briosa/issues/80";
        const string issue82 = "https://github.com/spatialanalyzer/briosa/issues/82";
        var entries = ReadCommittedEntries();
        var inventory = ReadCommittedInventory();
        var directionFindings = inventory.Commands
            .SelectMany(command => command.Arguments
                .SelectMany(argument => argument.Findings
                    .Where(finding => finding.StartsWith(
                        "direction_disagreement_",
                        StringComparison.Ordinal))
                    .Select(finding => (command.InventoryKey, Finding: finding))))
            .ToArray();
        var directionKeys = directionFindings
            .Select(finding => finding.InventoryKey)
            .ToHashSet(StringComparer.Ordinal);
        var directionDispositions = entries
            .Where(entry => directionKeys.Contains(entry.InventoryKey))
            .GroupBy(entry => entry.Disposition, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var candidates = entries
            .Where(entry => entry.Disposition == "approved_candidate")
            .ToArray();
        var blocked = entries.Where(entry => entry.Disposition == "blocked").ToArray();
        var inputs = candidates
            .SelectMany(entry => entry.CommandShape!.Arguments)
            .Select(argument => argument.Input)
            .Where(input => input is not null)
            .Cast<CommandInputResolution>()
            .ToArray();

        Assert.Equal(30, directionFindings.Length);
        Assert.Equal(16, directionKeys.Count);
        Assert.Equal(7, directionDispositions["approved_candidate"]);
        Assert.Equal(8, directionDispositions["intentional_exclusion"]);
        Assert.Equal(1, directionDispositions["sdk_unavailable"]);
        Assert.Equal(684, candidates.Length);
        Assert.Equal(45, blocked.Length);
        Assert.Equal(207, entries.Count(entry => entry.Disposition == "sdk_unavailable"));
        Assert.Equal(
            111,
            entries.Count(entry =>
                entry.Disposition == "sdk_unavailable" &&
                entry.DecisionReferences.Contains(issue53, StringComparer.Ordinal)));
        Assert.All(candidates, entry =>
        {
            Assert.Equal("resolved", entry.CommandShape!.Status);
            Assert.NotNull(entry.CommandShape.MpStep);
            Assert.Empty(entry.CommandShape.Discrepancies);
            Assert.Contains(issue53, entry.DecisionReferences);
        });
        Assert.Equal(1569, inputs.Count(input => input.Presence == "required"));
        Assert.Equal(485, inputs.Count(input => input.Presence == "optional"));
        Assert.Equal(64, inputs.Count(input => input.OmissionBehavior == "omit_sdk_setter"));
        Assert.Equal(421, inputs.Count(input => input.Default.Status == "reviewed"));
        Assert.Equal(314, inputs.Count(input => input.Default.Status == "reviewed_no_default"));
        Assert.DoesNotContain(inputs, input => input.Default.ReviewStatus == "needs_review");
        Assert.All(
            inputs.Where(input => input.Default.Status == "reviewed"),
            input =>
            {
                Assert.Equal("optional", input.Presence);
                Assert.Equal("set_catalog_default", input.OmissionBehavior);
                Assert.NotNull(input.Default.Value);
                Assert.Equal(
                    ["objectivesa_prior_release", "sa_2026_generated_vb"],
                    input.Default.Evidence);
                Assert.Null(input.Default.ReviewStatus);
                Assert.Null(input.Default.Candidates);
            });
        var reviewedNoDefault = inputs
            .Where(input => input.Default.Status == "reviewed_no_default")
            .ToArray();
        Assert.All(
            reviewedNoDefault,
            input =>
            {
                Assert.Equal("required", input.Presence);
                Assert.Equal("reject_request", input.OmissionBehavior);
                Assert.Null(input.Default.Value);
                Assert.Null(input.Default.Evidence);
                Assert.Null(input.Default.ReviewStatus);
                Assert.NotEmpty(input.Default.Candidates!);
                Assert.Equal(issue82, input.Default.DecisionReference);
                Assert.True(input.Default.EvidenceState is
                    "exact_target_sample_only" or "conflict" or "objectivesa_only");
                Assert.NotEmpty(input.Default.ReasonCodes!);
            });
        Assert.All(
            candidates.Where(entry => entry.CommandShape!.Arguments.Any(argument =>
                argument.Input?.Default.Status == "reviewed_no_default")),
            entry => Assert.Contains(issue82, entry.DecisionReferences));

        Assert.Equal(45, blocked.Count(entry => entry.BlockerReferences.SequenceEqual([issue79])));
        Assert.Equal(0, blocked.Count(entry => entry.BlockerReferences.SequenceEqual([issue80])));
        Assert.All(blocked, entry =>
        {
            Assert.Equal("blocked", entry.CommandShape!.Status);
            Assert.NotEmpty(entry.CommandShape.Discrepancies);
            Assert.All(entry.CommandShape.Discrepancies, discrepancy =>
                Assert.Contains(discrepancy.BlockerReference, entry.BlockerReferences));
        });

        var getPointProperties = Assert.Single(
            entries,
            entry => entry.MpStep == "Get Point Properties");
        Assert.Equal("resolved", getPointProperties.CommandShape!.Status);
        Assert.Equal("Get Point Properties", getPointProperties.CommandShape.MpStep);
        Assert.Equal(9, getPointProperties.CommandShape.Arguments.Count);
        Assert.Equal(
            [
                "Point Name",
                "Planar Offset",
                "Radial Offset",
                "Ux",
                "Uy",
                "Uz",
                "Umag",
                "Position Tolerance",
                "Component Weights"
            ],
            getPointProperties.CommandShape.Arguments.Select(argument => argument.MpName));
        Assert.Equal("Point Name", getPointProperties.CommandShape.Arguments[0].MpName);
        Assert.Equal("required", getPointProperties.CommandShape.Arguments[0].Input!.Presence);
        Assert.Equal("SetPointNameArg", getPointProperties.CommandShape.Arguments[0].SdkBinding.Setter);
        Assert.All(
            getPointProperties.CommandShape.Arguments.Skip(1),
            argument =>
            {
                Assert.Equal("output", argument.Direction);
                Assert.Equal("yes", argument.ResultOnly);
                Assert.Null(argument.Input);
                Assert.NotNull(argument.SdkBinding.Getter);
            });

        var orientation = Assert.Single(
            entries,
            entry => entry.MpStep == "Compute Group to Group Orientation (Rx, Ry, Rz)");
        Assert.Equal("read_only", orientation.RiskEffect);
        Assert.Equal("wave_1", orientation.DeliveryWave);
        Assert.Equal(
            ["input", "input", "output", "output", "output"],
            orientation.CommandShape!.Arguments.Select(argument => argument.Direction));

        var apdisCalibration = Assert.Single(
            entries,
            entry => entry.MpStep == "LR APDIS Activate MCM Calibration");
        Assert.Equal("wave_4", apdisCalibration.DeliveryWave);
        Assert.Contains("device_control", apdisCalibration.RiskFlags);
        Assert.Contains("long_running", apdisCalibration.RiskFlags);
        var missingInterop = Assert.Single(
            entries,
            entry => entry.MpStep == "Get Relationship Sigmoidal Gap Fit Constraints");
        Assert.Equal("blocked", missingInterop.Disposition);
        Assert.Equal([issue79], missingInterop.BlockerReferences);
        Assert.Contains(
            missingInterop.CommandShape!.Discrepancies,
            discrepancy => discrepancy.Code == "exact_interop_binding_missing");
    }

    [Fact]
    public void Issue52ReviewPublishesExactIntentionalExclusions()
    {
        var entries = ReadCommittedEntries();
        var exclusions = entries
            .Where(entry => string.Equals(
                entry.Disposition,
                "intentional_exclusion",
                StringComparison.Ordinal) &&
                entry.DecisionReferences.SequenceEqual(
                    ["https://github.com/spatialanalyzer/briosa/issues/52"]))
            .ToArray();
        var counts = exclusions
            .SelectMany(entry => entry.ReasonCodes)
            .GroupBy(reason => reason, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        Assert.Equal(348, exclusions.Length);
        Assert.Equal(39, counts["client_owned_external_integration"]);
        Assert.Equal(14, counts["client_owned_office_integration"]);
        Assert.Equal(17, counts["client_owned_serialization"]);
        Assert.Equal(38, counts["client_owned_spreadsheet_integration"]);
        Assert.Equal(63, counts["client_owned_state_and_control_flow"]);
        Assert.Equal(53, counts["client_owned_user_experience"]);
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
            "approved_candidate");
        AssertDisposition(
            entries,
            "FileOperations",
            "Import Nominals from XML File",
            "approved_candidate");
        AssertDisposition(
            entries,
            "ConstructionOperations",
            "Make a Point Name - Ensure Unique",
            "approved_candidate");
        AssertDisposition(
            entries,
            "AnalysisOperations",
            "Get Point Properties",
            "approved_candidate");
        AssertDisposition(
            entries,
            "ProcessFlowOperations",
            "Output SA Report to PDF",
            "approved_candidate");

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
        Assert.Contains("## Command-specific shape discrepancies", report, StringComparison.Ordinal);
    }

    [Fact]
    public void Issue49ReviewCuratesEveryGeometryDomainCommand()
    {
        var entries = ReadCommittedEntries();
        var reviewed = entries
            .Where(entry => entry.DecisionReferences.Contains(
                "https://github.com/spatialanalyzer/briosa/issues/49",
                StringComparer.Ordinal))
            .ToArray();

        Assert.Equal(450, reviewed.Length);
        Assert.Equal(279, reviewed.Count(entry => entry.Disposition == "approved_candidate"));
        Assert.Equal(44, reviewed.Count(entry => entry.Disposition == "blocked"));
        Assert.Equal(36, reviewed.Count(entry => entry.Disposition == "intentional_exclusion"));
        Assert.Equal(91, reviewed.Count(entry => entry.Disposition == "sdk_unavailable"));
        Assert.Equal(65, reviewed.Count(entry => entry.DeliveryWave == "wave_1"));
        Assert.Equal(132, reviewed.Count(entry => entry.DeliveryWave == "wave_2"));
        Assert.Equal(44, reviewed.Count(entry => entry.DeliveryWave == "wave_3"));
        Assert.Equal(38, reviewed.Count(entry => entry.DeliveryWave == "wave_4"));

        Assert.All(reviewed, entry => Assert.Equal("reviewed", entry.ReviewState));
        Assert.All(
            reviewed.Where(entry => entry.Disposition == "approved_candidate"),
            entry =>
            {
                Assert.Empty(entry.BlockerReferences);
                Assert.NotNull(entry.DeliveryWave);
                Assert.NotEqual("unknown", entry.RiskEffect);
                Assert.DoesNotContain("unknown", entry.RiskFlags);
                Assert.DoesNotContain("unknown", entry.DataClassifications);
                Assert.DoesNotContain("unknown", entry.ValueFamilies);
            });
        Assert.All(
            reviewed.Where(entry => entry.Disposition == "blocked"),
            entry => Assert.Equal(
                ["https://github.com/spatialanalyzer/briosa/issues/79"],
                entry.BlockerReferences));

        AssertDisposition(
            entries,
            "AnalysisOperations",
            "Get Point Properties",
            "approved_candidate");
        AssertDisposition(
            entries,
            "ConstructionOperations",
            "Construct Sphere",
            "approved_candidate");
        AssertDisposition(
            entries,
            "ConstructionOperations",
            "Construct Frame with Wizard",
            "intentional_exclusion");
        AssertDisposition(
            entries,
            "CloudMeshOps",
            "Delete Cloud Points by X Y Z Range",
            "sdk_unavailable");
        AssertDisposition(
            entries,
            "CloudMeshOps",
            "Get Cloud Point Count",
            "sdk_unavailable");

        var bestFit = Assert.Single(entries, entry =>
            entry.MpStep == "Best Fit Transformation - Group to Group");
        Assert.Equal("wave_4", bestFit.DeliveryWave);
        Assert.Equal(["filesystem_write", "long_running"], bestFit.RiskFlags);
    }

    [Fact]
    public void Issue50ReviewRemainsTraceableAfterShapeReconciliation()
    {
        const string issue50 = "https://github.com/spatialanalyzer/briosa/issues/50";
        var entries = ReadCommittedEntries();
        var reviewed = entries
            .Where(entry => entry.DecisionReferences.Contains(issue50, StringComparer.Ordinal))
            .ToArray();

        Assert.Equal(243, reviewed.Length);
        Assert.Equal(180, reviewed.Count(entry => entry.Disposition == "approved_candidate"));
        Assert.Equal(0, reviewed.Count(entry => entry.Disposition == "blocked"));
        Assert.Equal(16, reviewed.Count(entry => entry.Disposition == "intentional_exclusion"));
        Assert.Equal(47, reviewed.Count(entry => entry.Disposition == "sdk_unavailable"));
        Assert.Equal(179, reviewed.Count(entry => entry.DeliveryWave == "wave_4"));
        Assert.Equal(
            86,
            reviewed.Count(entry =>
                entry.Disposition == "approved_candidate" &&
                entry.RiskFlags.Contains("device_control", StringComparer.Ordinal)));

        Assert.All(reviewed, entry =>
        {
            Assert.Equal("reviewed", entry.ReviewState);
            Assert.NotEqual("unknown", entry.RiskEffect);
            Assert.DoesNotContain("unknown", entry.RiskFlags);
            Assert.DoesNotContain("unknown", entry.DataClassifications);
            Assert.DoesNotContain("unknown", entry.ValueFamilies);
        });
        Assert.All(
            reviewed.Where(entry => entry.Disposition == "approved_candidate"),
            entry =>
            {
                Assert.Empty(entry.BlockerReferences);
                Assert.NotNull(entry.DeliveryWave);
            });
        Assert.All(
            reviewed.Where(entry =>
                entry.Disposition == "approved_candidate" &&
                entry.RiskFlags.Contains("device_control", StringComparer.Ordinal)),
            entry => Assert.Equal("wave_4", entry.DeliveryWave));

        AssertDisposition(entries, "InstrumentOperations", "Measure", "approved_candidate");
        AssertDisposition(
            entries,
            "RobotOperations",
            "Move Robot/Machine to Frame",
            "approved_candidate");
        AssertDisposition(
            entries,
            "RobotCalibrationApplianceNodeOperations",
            "Set Calibration Appliance Node Measurement Profile",
            "approved_candidate");
        AssertDisposition(
            entries,
            "InstrumentOperations",
            "Watch Window Template 3D",
            "intentional_exclusion");
        AssertDisposition(
            entries,
            "RobotOperations",
            "Perform Robot Calibration",
            "sdk_unavailable");
        AssertDisposition(
            entries,
            "RobotOperations",
            "Get Robot/Machine Model Link Parameters",
            "sdk_unavailable");

        var deviceCandidateSteps = reviewed
            .Where(entry =>
                entry.Disposition == "approved_candidate" &&
                entry.RiskFlags.Contains("device_control", StringComparer.Ordinal))
            .Select(entry => entry.MpStep)
            .ToHashSet(StringComparer.Ordinal);
        var catalogDirectory = Path.Combine(
            FindRepositoryRoot().FullName,
            "catalog",
            "sa",
            "2026.1.0529.7");
        var catalog = JsonNode.Parse(File.ReadAllText(
            Path.Combine(catalogDirectory, "catalog.json")))!.AsObject();
        var supportedSteps = catalog["operation_files"]!.AsArray()
            .Select(file => JsonNode.Parse(File.ReadAllText(Path.Combine(
                catalogDirectory,
                file!.GetValue<string>().Replace('/', Path.DirectorySeparatorChar))))!
                ["mp_step"]!.GetValue<string>())
            .ToArray();

        Assert.DoesNotContain(supportedSteps, deviceCandidateSteps.Contains);
    }

    [Fact]
    public void Issue51ReviewCuratesEveryRemainingAdministrativeDomainCommand()
    {
        const string issue51 = "https://github.com/spatialanalyzer/briosa/issues/51";
        var entries = ReadCommittedEntries();
        var reviewed = entries
            .Where(entry => entry.DecisionReferences.Contains(issue51, StringComparer.Ordinal))
            .ToArray();

        Assert.Equal(371, reviewed.Length);
        Assert.Equal(225, reviewed.Count(entry => entry.Disposition == "approved_candidate"));
        Assert.Equal(1, reviewed.Count(entry => entry.Disposition == "blocked"));
        Assert.Equal(76, reviewed.Count(entry => entry.Disposition == "intentional_exclusion"));
        Assert.Equal(69, reviewed.Count(entry => entry.Disposition == "sdk_unavailable"));
        Assert.Equal(35, reviewed.Count(entry => entry.DeliveryWave == "wave_1"));
        Assert.Equal(97, reviewed.Count(entry => entry.DeliveryWave == "wave_2"));
        Assert.Equal(19, reviewed.Count(entry => entry.DeliveryWave == "wave_3"));
        Assert.Equal(74, reviewed.Count(entry => entry.DeliveryWave == "wave_4"));

        Assert.Equal(1412, entries.Length);
        Assert.All(entries, entry => Assert.Equal("reviewed", entry.ReviewState));
        Assert.All(reviewed, entry =>
        {
            Assert.NotEqual("unknown", entry.RiskEffect);
            Assert.DoesNotContain("unknown", entry.RiskFlags);
            Assert.DoesNotContain("unknown", entry.DataClassifications);
            Assert.DoesNotContain("unknown", entry.ValueFamilies);
        });
        Assert.All(
            reviewed.Where(entry => entry.Disposition == "blocked"),
            entry => Assert.Equal(
                ["https://github.com/spatialanalyzer/briosa/issues/79"],
                entry.BlockerReferences));
        Assert.All(
            reviewed.Where(entry =>
                entry.Disposition == "approved_candidate" &&
                entry.RiskFlags.Any(flag => flag.StartsWith("filesystem_", StringComparison.Ordinal))),
            entry => Assert.True(entry.DeliveryWave is "wave_3" or "wave_4"));
        Assert.DoesNotContain(
            reviewed.Where(entry => entry.Disposition == "approved_candidate"),
            entry => entry.RiskFlags.Contains("external_process", StringComparer.Ordinal) ||
                entry.RiskFlags.Contains("network_access", StringComparer.Ordinal));
        Assert.Equal(
            0,
            reviewed.Count(entry =>
                entry.Disposition == "blocked" &&
                entry.ReasonCodes.Contains("file_semantics_unresolved", StringComparer.Ordinal)));

        AssertDisposition(entries, "FileOperations", "Get Working Directory", "approved_candidate");
        AssertDisposition(entries, "FileOperations", "Export IGES File - Entire Model", "approved_candidate");
        AssertDisposition(entries, "FileOperations", "Save As", "approved_candidate");
        AssertDisposition(entries, "FileOperations", "Run Powershell Script", "intentional_exclusion");
        AssertDisposition(entries, "EventOperations", "Export Event Ref List", "approved_candidate");
        AssertDisposition(entries, "ProcessFlowOperations", "Output SA Report to PDF", "approved_candidate");
        AssertDisposition(
            entries,
            "RelationshipOperations",
            "Relationship Watch Window Template",
            "intentional_exclusion");
        AssertDisposition(
            entries,
            "RelationshipOperations",
            "Get General Relationship Statistics",
            "approved_candidate");
        AssertDisposition(
            entries,
            "ReportingOperations",
            "Generate Standard HTML Report",
            "approved_candidate");
        AssertDisposition(entries, "ReportingOperations", "Notify User HTML", "intentional_exclusion");
        AssertDisposition(entries, "UtilityOperations", "Set Logging State", "approved_candidate");
        AssertDisposition(
            entries,
            "Vector Operations",
            "Get Vector From Vector Group By Name",
            "approved_candidate");
        AssertDisposition(entries, "ViewControl", "Auto-Scale", "approved_candidate");

        var catalogDirectory = Path.Combine(
            FindRepositoryRoot().FullName,
            "catalog",
            "sa",
            "2026.1.0529.7");
        var catalog = JsonNode.Parse(File.ReadAllText(
            Path.Combine(catalogDirectory, "catalog.json")))!.AsObject();
        var supportedSteps = catalog["operation_files"]!.AsArray()
            .Select(file => JsonNode.Parse(File.ReadAllText(Path.Combine(
                catalogDirectory,
                file!.GetValue<string>().Replace('/', Path.DirectorySeparatorChar))))!
                ["mp_step"]!.GetValue<string>())
            .ToArray();

        Assert.Equal(
            [
                "Construct a Point at Circle Center",
                "Construct a Point at line MidPoint",
                "Construct Point (Fit to Points)",
                "Construct Point Group from Point Name Ref List",
                "Construct a Point in Working Coordinates",
                "Delete Points",
                "Get Number of Collections",
                "Get i-th Collection Name",
                "Get Number of Points in Group",
                "Make a Collection Object Name Ref List from all Groups in a Collection",
                "Make a Point Name Ref List From a Group",
                "Rename Point",
                "Get Working Directory"
            ],
            supportedSteps);
    }

    [Fact]
    public void Issue80RecordsAtRiskCandidatesWithTruthfulFixtureEvidence()
    {
        const string issue80 = "https://github.com/spatialanalyzer/briosa/issues/80";
        var entries = ReadCommittedEntries();
        var reviewed = entries
            .Where(entry => entry.DecisionReferences.Contains(issue80, StringComparer.Ordinal))
            .ToArray();

        Assert.Equal(11, reviewed.Length);
        Assert.All(reviewed, entry => Assert.Equal("approved_candidate", entry.Disposition));
        Assert.All(reviewed, entry =>
        {
            Assert.NotNull(entry.OperationContract);
            Assert.NotEmpty(entry.OperationContract.Constraints);
            Assert.Empty(entry.BlockerReferences);
            Assert.Equal("wave_3", entry.DeliveryWave);
            Assert.Equal("constrained_candidate", entry.OperationContract.Decision);
            Assert.Equal("resolved", entry.CommandShape!.Status);
            Assert.Contains("at_risk_candidate", entry.ReasonCodes, StringComparer.Ordinal);
            Assert.Contains("objectivesa_parity_reviewed", entry.ReasonCodes, StringComparer.Ordinal);
            Assert.All(
                entry.CommandShape.Arguments.Where(argument => argument.Input is not null),
                argument =>
                {
                    Assert.Equal("required", argument.Input!.Presence);
                    Assert.Equal("reject_request", argument.Input.OmissionBehavior);
                    Assert.Equal("none", argument.Input.Default.Status);
                });
            if (entry.OperationContract.ValidationStatus == "performed")
            {
                Assert.DoesNotContain(
                    "live_validation_not_performed",
                    entry.OperationContract.EvidenceLimitations,
                    StringComparer.Ordinal);
            }
            else
            {
                Assert.Equal("not_performed", entry.OperationContract.ValidationStatus);
                Assert.Contains(
                    "live_validation_not_performed",
                    entry.OperationContract.EvidenceLimitations,
                    StringComparer.Ordinal);
            }
        });

        var approvedKeys = reviewed
            .Select(entry => entry.InventoryKey)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            [
                "documentation:FileOperations/Save.htm",
                "documentation:FileOperations/SaveAs.htm",
                "documentation:FileOperations/SaveAsReadOnlyTemplate.htm",
                "documentation:FileOperations/XML/ImportNominalsFromXMLFile.htm",
                "documentation:FileOperations/XML/MergeMeasurementsintoXML.htm",
                "documentation:ProcessFlowOperations/OutputSAReportToPDF.htm",
                "sdk:EventOperations.txt#4",
                "sdk:FileOperations_FileExport.txt#1",
                "sdk:FileOperations_FileExport.txt#2",
                "sdk:FileOperations_FileImport.txt#16",
                "sdk:FileOperations_FileImport.txt#19"
            ],
            approvedKeys);

        var performedKeys = reviewed
            .Where(entry => entry.OperationContract!.ValidationStatus == "performed")
            .Select(entry => entry.InventoryKey)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            [
                "documentation:FileOperations/Save.htm",
                "documentation:FileOperations/SaveAs.htm",
                "documentation:FileOperations/SaveAsReadOnlyTemplate.htm",
                "documentation:ProcessFlowOperations/OutputSAReportToPDF.htm",
                "sdk:FileOperations_FileExport.txt#1",
                "sdk:FileOperations_FileExport.txt#2"
            ],
            performedKeys);

        var save = Assert.Single(
            reviewed,
            entry => entry.InventoryKey == "documentation:FileOperations/Save.htm");
        Assert.Contains("interactive_ui", save.RiskFlags, StringComparer.Ordinal);
        Assert.Contains(
            "named_job_required_before_enqueue",
            save.OperationContract!.Constraints,
            StringComparer.Ordinal);
        Assert.Contains(
            "unnamed_job_rejected_to_prevent_modal_save_as",
            save.OperationContract.Constraints,
            StringComparer.Ordinal);

        var pointSet = Assert.Single(
            reviewed,
            entry => entry.InventoryKey == "sdk:FileOperations_FileExport.txt#2");
        Assert.Contains(
            "typed_point_set_container_required",
            pointSet.OperationContract!.Constraints,
            StringComparer.Ordinal);
        Assert.Contains(
            "wrong_collection_object_type_rejected",
            pointSet.OperationContract.EvidenceLimitations,
            StringComparer.Ordinal);

        var eventExport = Assert.Single(
            reviewed,
            entry => entry.InventoryKey == "sdk:EventOperations.txt#4");
        Assert.Contains(
            "wildcard_event_discovery_completion_unknown_after_watchdog_termination",
            eventExport.OperationContract!.EvidenceLimitations,
            StringComparer.Ordinal);

        var pdf = Assert.Single(
            reviewed,
            entry => entry.InventoryKey ==
                "documentation:ProcessFlowOperations/OutputSAReportToPDF.htm");
        Assert.Equal("approved_candidate", pdf.Disposition);
        Assert.Contains("viewer_launch_prohibited", pdf.OperationContract!.Constraints);
        Assert.Equal("collection_item_name", pdf.ValueFamilies[0]);

        var asciiPoints = Assert.Single(
            reviewed,
            entry => entry.InventoryKey == "sdk:FileOperations_FileExport.txt#1");
        Assert.Contains(
            "existing_writable_parent_required_before_enqueue",
            asciiPoints.OperationContract!.Constraints,
            StringComparer.Ordinal);
        Assert.Contains(
            "missing_parent_completion_unknown_after_watchdog_termination",
            asciiPoints.OperationContract.EvidenceLimitations,
            StringComparer.Ordinal);
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
        Assert.Equal("unknown", entry["data_classifications"]![0]!.GetValue<string>());
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
            entry["data_classifications"] = new JsonArray("path");
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

    [Fact]
    public void ApprovedCandidateMayHaveNoSpecialRiskFlags()
    {
        using var fixture = DispositionFixture.Create();
        CommandDispositionLedger.Sync(fixture.InventoryPath, fixture.TargetDirectory);
        fixture.MarkEntryReviewedCandidate();
        fixture.EditEntry(entry => entry["risk_flags"] = new JsonArray());
        CommandDispositionLedger.Sync(fixture.InventoryPath, fixture.TargetDirectory);

        var result = CommandDispositionLedger.Validate(
            fixture.InventoryPath,
            fixture.TargetDirectory);

        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidationRejectsUnknownApprovedDataClassification()
    {
        using var fixture = DispositionFixture.Create();
        CommandDispositionLedger.Sync(fixture.InventoryPath, fixture.TargetDirectory);
        fixture.MarkEntryReviewedCandidate();
        fixture.EditEntry(entry =>
            entry["data_classifications"] = new JsonArray("unknown"));

        var result = CommandDispositionLedger.Validate(
            fixture.InventoryPath,
            fixture.TargetDirectory);

        Assert.Contains(
            result.Errors,
            error => error.Contains(
                "approved candidates require assessed risk, data classification",
                StringComparison.Ordinal));
    }

    [Fact]
    public void OperationContractValidationStatusMustMatchItsEvidenceLimitations()
    {
        using var fixture = DispositionFixture.Create();
        CommandDispositionLedger.Sync(fixture.InventoryPath, fixture.TargetDirectory);
        fixture.MarkEntryReviewedCandidate();
        fixture.EditEntry(entry => entry["operation_contract"] = new JsonObject
        {
            ["decision"] = "constrained_candidate",
            ["validation_status"] = "not_performed",
            ["constraints"] = new JsonArray("absolute_external_path_required"),
            ["evidence_limitations"] = new JsonArray()
        });
        fixture.EditEntry(entry => entry["reason_codes"] = new JsonArray(
            "operation_contract_reviewed",
            "read_only_operation"));

        var result = CommandDispositionLedger.Validate(
            fixture.InventoryPath,
            fixture.TargetDirectory);

        Assert.Contains(
            result.Errors,
            error => error.Contains(
                "must record live_validation_not_performed",
                StringComparison.Ordinal));

        fixture.EditEntry(entry =>
        {
            var contract = entry["operation_contract"]!.AsObject();
            contract["validation_status"] = "performed";
            contract["evidence_limitations"] = new JsonArray(
                "live_validation_not_performed");
        });
        result = CommandDispositionLedger.Validate(
            fixture.InventoryPath,
            fixture.TargetDirectory);
        Assert.Contains(
            result.Errors,
            error => error.Contains(
                "cannot retain live_validation_not_performed",
                StringComparison.Ordinal));
    }

    [Fact]
    public void PerformedOperationContractMayRemoveThePriorLimitation()
    {
        using var fixture = DispositionFixture.Create();
        CommandDispositionLedger.Sync(fixture.InventoryPath, fixture.TargetDirectory);
        fixture.MarkEntryReviewedCandidate();
        fixture.EditEntry(entry => entry["operation_contract"] = new JsonObject
        {
            ["decision"] = "constrained_candidate",
            ["validation_status"] = "performed",
            ["constraints"] = new JsonArray("absolute_external_path_required"),
            ["evidence_limitations"] = new JsonArray()
        });
        fixture.EditEntry(entry => entry["reason_codes"] = new JsonArray(
            "operation_contract_reviewed",
            "read_only_operation"));
        CommandDispositionLedger.Sync(fixture.InventoryPath, fixture.TargetDirectory);

        var result = CommandDispositionLedger.Validate(
            fixture.InventoryPath,
            fixture.TargetDirectory);

        Assert.Empty(result.Errors);
    }

    [Fact]
    public void OperationContractAndItsReviewMarkerCannotDriftApart()
    {
        using var fixture = DispositionFixture.Create();
        CommandDispositionLedger.Sync(fixture.InventoryPath, fixture.TargetDirectory);
        fixture.MarkEntryReviewedCandidate();
        fixture.EditEntry(entry => entry["reason_codes"] = new JsonArray(
            "operation_contract_reviewed",
            "read_only_operation"));

        var result = CommandDispositionLedger.Validate(
            fixture.InventoryPath,
            fixture.TargetDirectory);
        Assert.Contains(
            result.Errors,
            error => error.Contains(
                "operation_contract_reviewed requires operation_contract",
                StringComparison.Ordinal));

        fixture.EditEntry(entry =>
        {
            entry["reason_codes"] = new JsonArray("read_only_operation");
            entry["operation_contract"] = new JsonObject
            {
                ["decision"] = "intentional_exclusion",
                ["validation_status"] = "not_performed",
                ["constraints"] = new JsonArray("absolute_external_path_required"),
                ["evidence_limitations"] = new JsonArray(
                    "live_validation_not_performed")
            };
        });
        result = CommandDispositionLedger.Validate(
            fixture.InventoryPath,
            fixture.TargetDirectory);
        Assert.Contains(
            result.Errors,
            error => error.Contains(
                "operation_contract requires operation_contract_reviewed",
                StringComparison.Ordinal));
        Assert.Contains(
            result.Errors,
            error => error.Contains(
                "does not match disposition 'approved_candidate'",
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

    private static MpCommandInventory ReadCommittedInventory()
    {
        var inventoryPath = Path.Combine(
            FindRepositoryRoot().FullName,
            "inventory",
            "sa",
            "2026.1.0529.7",
            "inventory.json");
        return JsonSerializer.Deserialize<MpCommandInventory>(
            File.ReadAllText(inventoryPath),
            JsonOptions)!;
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
            entry["data_classifications"] = new JsonArray("path");
            entry["value_families"] = new JsonArray("path");
            entry["delivery_wave"] = "wave_1";
            entry["command_shape"] = new JsonObject
            {
                ["status"] = "resolved",
                ["mp_step"] = "Read Value",
                ["arguments"] = new JsonArray(),
                ["discrepancies"] = new JsonArray()
            };
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
