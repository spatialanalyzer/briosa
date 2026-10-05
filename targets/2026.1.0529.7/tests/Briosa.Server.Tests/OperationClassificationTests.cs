using Briosa.Server.Operations;
using Briosa.Server.Security;

namespace Briosa.Server.Tests;

/// <summary>
/// Pins the reviewed D1 classification table (#242, #293). These tests check its
/// completeness, vocabulary, and decided mappings, and that
/// <see cref="OperationPolicy"/> admits the counts the reviewed table implies under
/// each profile. The counts are also recomputed from the table with an
/// independent copy of the proposal's profile definitions.
/// </summary>
public sealed class OperationClassificationTests
{
    private static readonly Dictionary<string, int> ExpectedProfileAdmission =
        new(StringComparer.Ordinal)
        {
            ["read-only"] = 185,
            ["standard"] = 811,
            ["device"] = 912,
            ["full"] = 929
        };

    private const int ExpectedInteractiveOperations = 59;

    private const int ExpectedExclusiveOperations = 42;

    private static readonly string[] SessionLifecycles =
        [
            "gdt_operations.start_stop_feature_check_trapping",
            "instrument_operations.auto_correspond_closest_point",
            "instrument_operations.auto_correspond_with_proximity_trigger",
            "instrument_operations.close_auto_correspond_closest_point_dialog",
            "instrument_operations.guide_objects_in_6d_based_on_point_measurements",
            "instrument_operations.initiate_servo_guide",
            "instrument_operations.jump_instrument_to_new_location",
            "instrument_operations.lr_hardware_connect",
            "instrument_operations.lr_hardware_disconnect",
            "instrument_operations.move_objects_in_6d_using_instrument_updates",
            "instrument_operations.multi_measurement_initiate",
            "instrument_operations.multi_measurement_stop",
            "instrument_operations.set_alignment_projector",
            "instrument_operations.set_remeasure_failed_checks_only",
            "instrument_operations.start_gdt_inspection",
            "instrument_operations.start_gdt_inspection_design",
            "instrument_operations.start_gdt_inspection_rehearse",
            "instrument_operations.start_instrument_interface",
            "instrument_operations.start_theodolite_interface",
            "instrument_operations.stop_active_measurement_mode",
            "instrument_operations.stop_instrument_interface",
            "instrument_operations.wait_for_trapping_to_complete",
            "instrument_operations.watch_closest_point",
            "instrument_operations.watch_instrument",
            "instrument_operations.watch_point_to_edge",
            "instrument_operations.watch_point_to_objects",
            "instrument_operations.watch_point_to_point",
            "instrument_operations.watch_point_to_point_with_view_zooming",
            "relationship_operations.start_stop_relationship_trapping",
            "robot_calibration_appliance_node_operations.clear_calibration_appliance_node_trap_manager_requests",
            "robot_calibration_appliance_node_operations.connect_disconnect_calibration_appliance_node",
            "robot_calibration_appliance_node_operations.enable_disable_calibration_appliance_node_instrument_auto_point",
            "robot_calibration_appliance_node_operations.enable_disable_calibration_appliance_node_trap_manager",
            "robot_calibration_appliance_node_operations.skip_calibration_appliance_node_measurement",
            "robot_operations.perform_robot_calibration",
            "robot_operations.perform_robot_calibration_alternate",
            "robot_operations.start_robot_machine_interface",
            "robot_operations.start_stop_robot_calibration_trapping",
            "robot_operations.stop_robot_machine_interface",
            "utility_operations.close_all_watch_windows",
            "utility_operations.lock_unlock_trapping_control",
            "view_control.set_point_of_view_from_instrument_updates"
        ];

    // get_working_directory, the eight Briosa.SmokeClient reads, the five
    // briosa-examples tutorial operations, and get_working_frame_properties.
    private static readonly string[] DefaultProfileContract =
        [
            "file_operations.get_working_directory",
            "utility_operations.get_active_units",
            "utility_operations.get_working_frame_properties",
            "construction_operations.get_active_collection_name",
            "analysis_operations.get_number_of_collections",
            "analysis_operations.get_ith_collection_name",
            "analysis_operations.get_object_reporting_frame",
            "relationship_operations.get_relationship_reporting_frame",
            "construction_operations.construct_collection",
            "construction_operations.construct_point_in_working_coordinates",
            "analysis_operations.get_point_coordinate",
            "analysis_operations.get_point_to_point_distance"
        ];

    [Fact]
    public void EveryRegisteredOperationHasExactlyOneRowAndNoRowIsUnregistered()
    {
        var rowIds = OperationClassification.Rows.Select(row => row.OperationId).ToArray();
        var duplicates = rowIds
            .GroupBy(id => id, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);
        Assert.Empty(duplicates);

        var registeredIds = SpatialAnalyzerApi.Operations.Select(operation => operation.OperationId);
        Assert.Equal(registeredIds.Order(StringComparer.Ordinal), rowIds.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void EveryRowIsCompleteAndUsesOnlyTheClosedRiskVocabulary()
    {
        Assert.Equal(
            [
                "filesystem_metadata", "filesystem_read", "filesystem_write", "filesystem_delete",
                "destructive", "code_execution", "physical_motion", "device_session", "device_config",
                "interactive_ui", "external_io"
            ],
            OperationRiskVocabulary.Members.Select(OperationRiskVocabulary.GetName));
        Assert.Equal(
            Enum.GetValues<OperationRisks>().Where(risk => risk != OperationRisks.None),
            OperationRiskVocabulary.Members);
        Assert.Throws<ArgumentOutOfRangeException>(() => OperationRiskVocabulary.GetNames((OperationRisks)(1 << 11)));

        foreach (var row in OperationClassification.Rows)
        {
            Assert.True(row.IsComplete, row.OperationId);
            Assert.Equal(OperationRisks.None, row.Risks & ~OperationRiskVocabulary.All);
            Assert.All(OperationRiskVocabulary.GetNames(row.Risks),
                name => Assert.Matches("^[a-z]+(_[a-z]+)*$", name));
        }
    }

    [Fact]
    public void LookupFailsClosedWithoutAReviewedRow()
    {
        Assert.Equal(OperationClassificationStatus.Unreviewed, default(OperationClassificationLookup).Status);
        Assert.False(default(OperationClassificationLookup).IsReviewed);

        var missing = OperationClassification.Find("file_operations.not_a_registered_operation");
        Assert.Equal(OperationClassificationStatus.Unreviewed, missing.Status);
        Assert.Null(missing.Row);
        Assert.False(missing.IsReviewed);

        Assert.False(new OperationClassificationRow(
            "file_operations.get_working_directory",
            OperationRisks.FilesystemMetadata,
            OperationDurationClass.Unspecified,
            OperationValidationStatus.NoRecordedGap,
            OperationIsolationClass.Admissible).IsComplete);
        Assert.False(new OperationClassificationRow(
            "file_operations.get_working_directory",
            (OperationRisks)(1 << 11),
            OperationDurationClass.Quick,
            OperationValidationStatus.NoRecordedGap,
            OperationIsolationClass.Admissible).IsComplete);

        foreach (var operation in SpatialAnalyzerApi.Operations)
        {
            var lookup = OperationClassification.Find(operation.OperationId);
            Assert.True(lookup.IsReviewed, operation.OperationId);
            Assert.Equal(operation.OperationId, lookup.Row!.OperationId);
        }
    }

    [Fact]
    public void ProfileAdmissionCountsMatchTheDecision()
    {
        foreach (var profile in OperationProfileAdmission.Profiles)
        {
            Assert.Equal(
                ExpectedProfileAdmission[profile.Name],
                SpatialAnalyzerApi.Operations.Count(operation => OperationProfileAdmission.Admits(profile, operation)));
            var policy = OperationPolicyTests.CreatePolicy(profile: profile.Name);
            Assert.Equal(ExpectedProfileAdmission[profile.Name], policy.AllowedOperations.Count);
            Assert.Equal(
                SpatialAnalyzerApi.Operations
                    .Where(operation => OperationProfileAdmission.Admits(profile, operation))
                    .Select(operation => operation.OperationId)
                    .Order(StringComparer.Ordinal),
                policy.AllowedOperations.Select(operation => operation.OperationId));
        }

        Assert.Equal(
            ExpectedProfileAdmission.Keys.Order(StringComparer.Ordinal),
            OperationAdmissionProfile.All.Select(profile => profile.Name).Order(StringComparer.Ordinal));

        var interactive = OperationClassification.Rows
            .Where(row => row.Risks.HasFlag(OperationRisks.InteractiveUi))
            .Select(row => row.OperationId)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Equal(ExpectedInteractiveOperations, interactive.Count);
        Assert.All(
            SpatialAnalyzerApi.Operations.Where(operation => interactive.Contains(operation.OperationId)),
            operation => Assert.DoesNotContain(
                OperationProfileAdmission.Profiles,
                profile => OperationProfileAdmission.Admits(profile, operation)));
        Assert.All(
            OperationAdmissionProfile.All,
            profile => Assert.DoesNotContain(
                OperationPolicyTests.CreatePolicy(profile: profile.Name).AllowedOperations,
                operation => interactive.Contains(operation.OperationId)));
    }

    [Fact]
    public void ExclusiveWorkflowsAreDeniedUnderEveryProfileAndOptIn()
    {
        var exclusive = OperationClassification.Rows
            .Where(row => row.Isolation == OperationIsolationClass.ExclusiveWorkflow)
            .Select(row => row.OperationId)
            .ToArray();
        Assert.Equal(ExpectedExclusiveOperations, exclusive.Length);

        var widest = OperationPolicyTests.CreatePolicy(
            profile: "full",
            settings: new() { ["Flags:interactive_ui"] = "allow" });
        Assert.Equal(SpatialAnalyzerApi.Operations.Count - ExpectedExclusiveOperations, widest.AllowedOperations.Count);
        foreach (var operationId in exclusive)
        {
            var decision = widest.Evaluate(operationId);
            Assert.Equal(OperationPolicyDecisionKind.Denied, decision.Kind);
            Assert.Equal("operation-isolation-unsupported", decision.DiagnosticCode);
            Assert.Equal(global::Briosa.OperationExecutionScope.ExclusiveWorkflow, decision.Operation!.ExecutionScope);
        }
    }

    [Fact]
    public void StandardProfileAdmitsTheDefaultProfileContract()
    {
        var standard = Assert.Single(OperationProfileAdmission.Profiles, profile => profile.Name == "standard");
        var operations = SpatialAnalyzerApi.Operations.ToDictionary(
            operation => operation.OperationId, StringComparer.Ordinal);

        Assert.Equal(12, DefaultProfileContract.Distinct(StringComparer.Ordinal).Count());
        foreach (var operationId in DefaultProfileContract)
        {
            Assert.True(OperationProfileAdmission.Admits(standard, operations[operationId]), operationId);
        }
    }

    [Fact]
    public void OnlySessionLifecyclesAreExclusiveWorkflowsAndOtherInteractiveOperationsStayAdmissible()
    {
        Assert.Equal(
            SessionLifecycles.Order(StringComparer.Ordinal),
            OperationClassification.Rows
                .Where(row => row.Isolation == OperationIsolationClass.ExclusiveWorkflow)
                .Select(row => row.OperationId)
                .Order(StringComparer.Ordinal));

        foreach (var row in OperationClassification.Rows)
        {
            var interactive = row.Risks.HasFlag(OperationRisks.InteractiveUi);
            Assert.Equal(interactive, row.Duration == OperationDurationClass.Interactive);
            if (interactive && !SessionLifecycles.Contains(row.OperationId, StringComparer.Ordinal))
            {
                Assert.Equal(OperationIsolationClass.Admissible, row.Isolation);
            }
        }
    }

    [Fact]
    public void LegacyDescriptorFlagsMapAsDecided()
    {
        foreach (var operation in SpatialAnalyzerApi.Operations)
        {
            var row = OperationClassification.Find(operation.OperationId).Row!;
            foreach (var flag in operation.RiskFlags)
            {
                switch (flag)
                {
                    case "destructive":
                        Assert.True(row.Risks.HasFlag(OperationRisks.Destructive), operation.OperationId);
                        break;
                    case "filesystem_metadata":
                        Assert.True(row.Risks.HasFlag(OperationRisks.FilesystemMetadata), operation.OperationId);
                        break;
                    case "instrument-control":
                        Assert.True(row.Risks.HasFlag(OperationRisks.DeviceSession), operation.OperationId);
                        break;
                    case "long-running":
                        Assert.Equal(OperationDurationClass.LongRunning, row.Duration);
                        break;
                    case "fixture_validation_pending":
                    case "at-risk-no-runtime-validation":
                        break;
                    default:
                        Assert.Fail($"'{operation.OperationId}' carries unreviewed descriptor flag '{flag}'.");
                        break;
                }
            }

            var expectedStatus = operation.RiskFlags.Contains("at-risk-no-runtime-validation", StringComparer.Ordinal)
                ? OperationValidationStatus.AtRiskUnvalidated
                : operation.RiskFlags.Contains("fixture_validation_pending", StringComparer.Ordinal)
                    ? OperationValidationStatus.FixturePending
                    : OperationValidationStatus.NoRecordedGap;
            Assert.Equal(expectedStatus, row.ValidationStatus);
        }
    }

    /// <summary>
    /// The D1 proposal's profile definitions (section 3.3) as amended by the #242
    /// decision: a profile admits an operation when every risk is in its admitted
    /// set and its effect filter passes. <c>interactive_ui</c> is in no set, and an
    /// exclusive workflow is never admitted.
    /// </summary>
    private static class OperationProfileAdmission
    {
        private const OperationRisks ReadOnlyRisks = OperationRisks.FilesystemMetadata;
        private const OperationRisks StandardRisks = ReadOnlyRisks |
            OperationRisks.FilesystemRead | OperationRisks.FilesystemWrite | OperationRisks.Destructive;
        private const OperationRisks DeviceRisks = StandardRisks |
            OperationRisks.DeviceSession | OperationRisks.DeviceConfig | OperationRisks.PhysicalMotion;
        private const OperationRisks FullRisks = DeviceRisks |
            OperationRisks.ExternalIo | OperationRisks.CodeExecution | OperationRisks.FilesystemDelete;

        public static IReadOnlyList<ProfileDefinition> Profiles { get; } =
            [
                new("read-only", ReadOnlyRisks, ReadOnlyEffectOnly: true),
                new("standard", StandardRisks, ReadOnlyEffectOnly: false),
                new("device", DeviceRisks, ReadOnlyEffectOnly: false),
                new("full", FullRisks, ReadOnlyEffectOnly: false)
            ];

        public static bool Admits(ProfileDefinition profile, OperationDescriptor operation)
        {
            var lookup = OperationClassification.Find(operation.OperationId);
            return lookup.Row is { } row &&
                lookup.IsReviewed &&
                row.Isolation == OperationIsolationClass.Admissible &&
                (row.Risks & ~profile.AdmittedRisks) == OperationRisks.None &&
                (!profile.ReadOnlyEffectOnly || operation.Effect == global::Briosa.OperationEffect.ReadOnly);
        }
    }

    private sealed record ProfileDefinition(string Name, OperationRisks AdmittedRisks, bool ReadOnlyEffectOnly);
}
