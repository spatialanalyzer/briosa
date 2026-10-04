using Briosa.Server.Security;
using static Briosa.Server.Security.OperationDurationClass;
using static Briosa.Server.Security.OperationRisks;

namespace Briosa.Server.Tests;

/// <summary>
/// Pins the classification rows corrected against the installed exact-target MP
/// documentation while reviewing #293 step 1, so a table edit cannot silently
/// revert them. Each entry records the documented behavior that requires its
/// risks, forbids risks the review removed, and fixes its duration and isolation;
/// <c>eng/classification/README.md</c> names the documentation pages. Operations
/// that only this target registers are listed in the target's
/// <c>OperationClassificationEvidenceTests.TargetOnly.cs</c>.
/// </summary>
public sealed partial class OperationClassificationEvidenceTests
{
    private const OperationIsolationClass Exclusive = OperationIsolationClass.ExclusiveWorkflow;

    private static readonly ClassificationEvidence[] SharedEvidence =
        [
            // Operator UI, runtime selection, or operator-guided measurement routines.
            new("relationship_operations.edit_geometry_relationship_point_list", InteractiveUi, Interactive,
                "Opens the selected point-list dialog and pauses the MP until the operator closes it."),
            new("construction_operations.construct_surface_by_dissecting_surfaces", InteractiveUi, Interactive,
                "A runtime-select step that prompts the operator to pick the surfaces or faces to dissect."),
            new("reporting_operations.set_report_options_for_object", InteractiveUi, Interactive,
                "Opens the report options dialog for the object."),
            new("instrument_operations.align_two_targets_with_axis_wcf_x", DeviceSession | InteractiveUi, Interactive,
                "Guides the operator through aligning two points with a running instrument interface."),
            new("instrument_operations.drift_check", DeviceSession | InteractiveUi, Interactive,
                "Measures through the Drift Check dialog, which the operator finishes or cancels."),
            new("instrument_operations.build_target", DeviceSession | InteractiveUi, Interactive,
                "Guides the operator through measuring a point while showing live deviations."),
            new("instrument_operations.locate_instrument_ref_tie_in", DeviceSession | InteractiveUi, Interactive,
                "Guides the operator through measuring the reference points that locate the instrument."),
            new("instrument_operations.auto_correspond_closest_point", DeviceSession | InteractiveUi, Interactive,
                "Measures inside the Auto-Correspond dialog.", Isolation: Exclusive),
            new("instrument_operations.initiate_servo_guide", PhysicalMotion | DeviceSession | InteractiveUi,
                Interactive, "Guides the operator through the points; the operator starts each measurement.",
                Isolation: Exclusive),
            new("instrument_operations.auto_measure_points", PhysicalMotion | DeviceSession | InteractiveUi,
                Interactive, "Always shows the full or abbreviated auto-measure dialog."),
            new("instrument_operations.guide_objects_in_6d_based_on_point_measurements", DeviceSession | InteractiveUi,
                Interactive, "Starts the operator-guided 6D build routine.", Isolation: Exclusive),

            // Live device sessions and device configuration.
            new("robot_operations.get_robot_machine_parameter", DeviceSession, LongRunning,
                "Reads a machine-specific parameter; the robot/machine interface must be active."),
            new("robot_operations.set_robot_machine_parameter", DeviceSession | DeviceConfig, LongRunning,
                "Sets a machine-specific parameter; the robot/machine interface must be active."),
            new("instrument_operations.set_probe_offset_frame_online", DeviceSession | DeviceConfig, LongRunning,
                "Triggers a raw-frame measurement and waits up to a timeout for it."),
            new("instrument_operations.lr_set_red_laser_intensity", DeviceSession | DeviceConfig, LongRunning,
                "Sets the intensity of the laser radar's red laser."),
            new("instrument_operations.configure_and_measure", DeviceSession | DeviceConfig, LongRunning,
                "Sets the instrument's target name and measurement mode, then measures."),
            new("instrument_operations.instrument_operational_check", PhysicalMotion | DeviceSession | DeviceConfig,
                LongRunning, "Check strings set instrument modes and include Home, Jog, Change Face and motor control."),
            new("instrument_operations.set_instrument_axes", DeviceSession | DeviceConfig, LongRunning,
                "Sets joint values; the instrument's interface must be active."),
            new("instrument_operations.set_remeasure_failed_checks_only", Destructive | DeviceSession | DeviceConfig,
                LongRunning, "Clears the points of failed checks and starts trapping."),
            new("robot_operations.compute_robot_machine_adjusted_goal_frame", DeviceSession, LongRunning,
                "The robot/machine interface must be active."),
            new("instrument_operations.construct_mirror_from_plane", DeviceSession | DeviceConfig, LongRunning,
                "Creates a mirror in the laser radar interface."),
            new("instrument_operations.construct_mirror_from_two_points", DeviceSession | DeviceConfig, LongRunning,
                "Can send the mirror plane definition to the instrument."),
            new("instrument_operations.create_new_dynamic_reference", DeviceSession | DeviceConfig, LongRunning,
                "Passes the dynamic reference frame to the MoveInspect system."),
            new("view_control.show_hide_instrument_interface", DeviceSession, LongRunning,
                "Fails when the instrument is not connected to an interface."),
            new("robot_operations.get_calibration_appliance_data", DeviceSession | DeviceConfig | ExternalIo,
                LongRunning, "Exchanges packets with the calibration appliance's host and port."),
            new("robot_operations.get_calibration_appliance_integer_value", DeviceSession | DeviceConfig | ExternalIo,
                LongRunning, "Exchanges packets with the calibration appliance's host and port."),
            new("robot_operations.get_calibration_appliance_real_value", DeviceSession | DeviceConfig | ExternalIo,
                LongRunning, "Exchanges packets with the calibration appliance's host and port."),
            new("robot_operations.set_calibration_appliance_data", DeviceSession | DeviceConfig | ExternalIo,
                LongRunning, "Exchanges packets with the calibration appliance's host and port."),
            new("robot_operations.set_calibration_appliance_integer_value", DeviceSession | DeviceConfig | ExternalIo,
                LongRunning, "Exchanges packets with the calibration appliance's host and port."),
            new("robot_operations.set_calibration_appliance_real_value", DeviceSession | DeviceConfig | ExternalIo,
                LongRunning, "Exchanges packets with the calibration appliance's host and port."),
            new("instrument_operations.lr_hardware_connect", DeviceSession | ExternalIo, LongRunning,
                "Connects to the caller-supplied laser radar host and port.", Isolation: Exclusive),

            // Hardware motion.
            new("instrument_operations.measure_existing_single_point", PhysicalMotion | DeviceSession, LongRunning,
                "Points the instrument at the existing point, locks on, then measures."),
            new("instrument_operations.measure_existing_single_point_and_compare", PhysicalMotion | DeviceSession,
                LongRunning, "Points the instrument at the existing point, locks on, then measures."),
            new("instrument_operations.measure_existing_single_point_manual_guide",
                PhysicalMotion | DeviceSession | InteractiveUi, Interactive,
                "Points the instrument at the existing point; otherwise the operator steers the head to it."),
            new("instrument_operations.point_at_target", PhysicalMotion | DeviceSession, LongRunning,
                "Points the instrument at the named point."),
            new("instrument_operations.measure_nominal_feature", PhysicalMotion | DeviceSession, LongRunning,
                "Points the laser radar at the nominal feature and measures it."),
            new("instrument_operations.track_tape_measurement", PhysicalMotion | DeviceSession, LongRunning,
                "Scans the instrument along a line of targets."),
            new("instrument_operations.edge_scan_measurement", PhysicalMotion | DeviceSession, LongRunning,
                "Scans the laser radar from a seed point toward a direction point to find an edge."),
            new("instrument_operations.lr_apdis_perform_mcm_calibration", PhysicalMotion | DeviceSession, LongRunning,
                "The laser radar measures the nominal points from both faces."),
            new("instrument_operations.lr_self_test_flip_test", PhysicalMotion | DeviceSession, LongRunning,
                "Measures front and back faces of the laser radar."),
            new("instrument_operations.set_alignment_projector", PhysicalMotion | DeviceSession, LongRunning,
                "Starts the projector's alignment routine."),

            // Filesystem.
            new("file_operations.rename_general_file", FilesystemRead | FilesystemWrite | FilesystemDelete, Quick,
                "Moves the source file's content to the destination; the source no longer exists."),
            new("file_operations.prepare_qdas_data_list", None, Quick,
                "Builds the QDAS data list in memory; it takes no file argument.", ForbiddenRisks: FilesystemWrite),
            new("file_operations.get_qdas_catalog_entries", None, Quick,
                "Reads the catalog that is already loaded; it takes no path.", ForbiddenRisks: FilesystemRead),

            // Duration.
            new("relationship_operations.auto_filter_points_groups_clouds_to_surface_faces", None, LongRunning,
                "Documented as able to take some time to process."),
            new("instrument_operations.locate_instruments_usmn", None, LongRunning,
                "Its uncertainty analysis runs up to a time limit given in minutes."),

            // Over-classified rows corrected downward.
            new("relationship_operations.relationship_watch_window_template", None, Quick,
                "Only creates a watch-window template and always succeeds.", ForbiddenRisks: DeviceSession),
            new("instrument_operations.enable_disable_point_set_scan_mode", DeviceConfig, Quick,
                "Only sets how SA stores point scans.", ForbiddenRisks: DeviceSession),
            new("instrument_operations.enable_disable_frame_set_scan_mode_all_instruments", DeviceConfig, Quick,
                "Only sets how SA stores 6D scans.", ForbiddenRisks: DeviceSession),
            new("instrument_operations.enable_disable_frame_set_scan_mode_by_instrument", DeviceConfig, Quick,
                "Only sets how SA stores 6D scans for one instrument.", ForbiddenRisks: DeviceSession),
            new("reporting_operations.close_all_reports", None, Quick,
                "Only closes report windows.", ForbiddenRisks: Destructive)
        ];

    [Fact]
    public void DocumentedOperationsCarryTheirRequiredRisksDurationAndIsolation()
    {
        var evidence = SharedEvidence.Concat(TargetOnlyEvidence).ToArray();
        Assert.Equal(
            evidence.Length,
            evidence.Select(item => item.OperationId).Distinct(StringComparer.Ordinal).Count());

        foreach (var item in evidence)
        {
            Assert.Equal(None, item.RequiredRisks & item.ForbiddenRisks);
            var lookup = OperationClassification.Find(item.OperationId);
            Assert.True(lookup.IsReviewed, $"'{item.OperationId}' has no reviewed row.");
            var row = lookup.Row!;
            var missing = item.RequiredRisks & ~row.Risks;
            if (missing != None)
            {
                Assert.Fail($"'{item.OperationId}' must carry {missing}: {item.Evidence}");
            }

            var forbidden = item.ForbiddenRisks & row.Risks;
            if (forbidden != None)
            {
                Assert.Fail($"'{item.OperationId}' must not carry {forbidden}: {item.Evidence}");
            }

            if (row.Duration != item.Duration)
            {
                Assert.Fail($"'{item.OperationId}' must be {item.Duration}, not {row.Duration}: {item.Evidence}");
            }

            if (row.Isolation != item.Isolation)
            {
                Assert.Fail($"'{item.OperationId}' must be {item.Isolation}, not {row.Isolation}: {item.Evidence}");
            }
        }
    }

    private sealed record ClassificationEvidence(
        string OperationId,
        OperationRisks RequiredRisks,
        OperationDurationClass Duration,
        string Evidence,
        OperationRisks ForbiddenRisks = None,
        OperationIsolationClass Isolation = OperationIsolationClass.Admissible);
}
