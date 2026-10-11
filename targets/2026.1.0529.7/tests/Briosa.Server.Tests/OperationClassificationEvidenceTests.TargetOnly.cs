using static Briosa.OperationDurationClass;
using static Briosa.Server.Security.OperationRisks;

namespace Briosa.Server.Tests;

public sealed partial class OperationClassificationEvidenceTests
{
    // Documentation-corrected operations that only the 2026 target registers.
    private static readonly ClassificationEvidence[] TargetOnlyEvidence =
        [
            new("instrument_operations.set_ladar_auto_meas_point", DeviceConfig, Quick,
                "Adjusts the sample time of the instrument's point measurement mode."),
            new("instrument_operations.set_ladar_auto_meas_sphere", DeviceConfig, Quick,
                "Adjusts the instrument's sphere measurement mode settings."),
            new("instrument_operations.set_ladar_feature_meas_circle", DeviceConfig, Quick,
                "Adjusts the scan settings of the instrument's circle measurement mode."),
            new("instrument_operations.set_ladar_feature_meas_cylinder", DeviceConfig, Quick,
                "Adjusts the scan settings of the instrument's cylinder measurement mode."),
            new("instrument_operations.set_ladar_feature_meas_slot", DeviceConfig, Quick,
                "Adjusts the scan settings of the instrument's slot measurement mode."),
            new("instrument_operations.set_ladar_feature_meas_sphere", DeviceConfig, Quick,
                "Adjusts the scan line spacing of the instrument's sphere feature measurement mode."),
            new("relationship_operations.set_geom_relationship_auto_measure_nominal_feature", DeviceConfig, Quick,
                "Assigns the instrument and measurement mode in the relationship's trapping control settings."),
            new("instrument_operations.edit_scan_perimeter_profile", DeviceConfig | Destructive, Quick,
                "Clear Profile? (Briosa default true) clears the regions saved in the measurement profile.")
        ];
}
