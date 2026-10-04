using Briosa.Server.Security;
using static Briosa.Server.Security.OperationDurationClass;
using static Briosa.Server.Security.OperationRisks;

namespace Briosa.Server.Tests;

/// <summary>
/// Pins the classification rows corrected against the installed exact-target MP
/// documentation while reviewing #293 step 1, so a table edit cannot silently
/// drop them. Each entry records the documented behavior that requires its risks
/// and duration; <c>eng/classification/README.md</c> names the documentation
/// pages. Operations that only this target registers are listed in the target's
/// <c>OperationClassificationEvidenceTests.TargetOnly.cs</c>.
/// </summary>
public sealed partial class OperationClassificationEvidenceTests
{
    private static readonly ClassificationEvidence[] SharedEvidence =
        [
            new("relationship_operations.edit_geometry_relationship_point_list", InteractiveUi, Interactive,
                "Opens the selected point-list dialog and pauses the MP until the operator closes it."),
            new("construction_operations.construct_surface_by_dissecting_surfaces", InteractiveUi, Interactive,
                "A runtime-select step that prompts the operator to pick the surfaces or faces to dissect."),
            new("reporting_operations.set_report_options_for_object", InteractiveUi, Interactive,
                "Opens the report options dialog for the object."),
            new("robot_operations.get_robot_machine_parameter", DeviceSession, LongRunning,
                "Reads a machine-specific parameter; the robot/machine interface must be active."),
            new("robot_operations.set_robot_machine_parameter", DeviceSession | DeviceConfig, LongRunning,
                "Sets a machine-specific parameter; the robot/machine interface must be active."),
            new("instrument_operations.align_two_targets_with_axis_wcf_x", DeviceSession, LongRunning,
                "Measures two points with the instrument, whose interface must be running."),
            new("instrument_operations.measure_existing_single_point", PhysicalMotion | DeviceSession, LongRunning,
                "Points the instrument at the existing point, locks on, then measures."),
            new("instrument_operations.measure_existing_single_point_and_compare", PhysicalMotion | DeviceSession,
                LongRunning, "Points the instrument at the existing point, locks on, then measures."),
            new("instrument_operations.measure_existing_single_point_manual_guide",
                PhysicalMotion | DeviceSession | InteractiveUi, Interactive,
                "Points the instrument at the existing point; otherwise the operator steers the head to it."),
            new("instrument_operations.point_at_target", PhysicalMotion | DeviceSession, LongRunning,
                "Points the instrument at the named point.")
        ];

    [Fact]
    public void DocumentedOperationsCarryTheirRequiredRisksAndDuration()
    {
        var evidence = SharedEvidence.Concat(TargetOnlyEvidence).ToArray();
        Assert.Equal(
            evidence.Length,
            evidence.Select(item => item.OperationId).Distinct(StringComparer.Ordinal).Count());

        foreach (var item in evidence)
        {
            var lookup = OperationClassification.Find(item.OperationId);
            Assert.True(lookup.IsReviewed, $"'{item.OperationId}' has no reviewed row.");
            var row = lookup.Row!;
            var missing = item.RequiredRisks & ~row.Risks;
            if (missing != None)
            {
                Assert.Fail($"'{item.OperationId}' must carry {missing}: {item.Evidence}");
            }

            if (row.Duration != item.Duration)
            {
                Assert.Fail($"'{item.OperationId}' must be {item.Duration}, not {row.Duration}: {item.Evidence}");
            }

            Assert.Equal(OperationIsolationClass.Admissible, row.Isolation);
        }
    }

    private sealed record ClassificationEvidence(
        string OperationId,
        OperationRisks RequiredRisks,
        OperationDurationClass Duration,
        string Evidence);
}
