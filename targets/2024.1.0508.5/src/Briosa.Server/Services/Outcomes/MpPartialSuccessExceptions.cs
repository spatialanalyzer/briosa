using System.Collections.Frozen;

namespace Briosa.Server.Services;

/// <summary>
/// The reviewed operations for which MP code 4 (PARTIAL SUCCESS) means the
/// requested work was not done. They keep the MP failure mapping; code 4 from
/// any other operation is a partial success (#351). None of them has outputs,
/// because the worker retrieves outputs after code 4 without knowing the
/// operation. This list is not part of the policy fingerprint.
/// </summary>
internal static class MpPartialSuccessExceptions
{
    public static FrozenSet<string> OperationIds { get; } = new[]
    {
        // The point cannot be constructed: no target point was found.
        "construction_operations.construct_point_from_survey_target_center",
        // The 2024 documentation does not list code 4 for the two open-file
        // commands. They are listed by maintainer decision on #351: a code 4
        // that reported a discarded open job as a success would be worse than
        // a false failure.
        "file_operations.open_sa_file",
        "file_operations.open_template_file",
        // Report options are not available for the object.
        "reporting_operations.set_report_options_for_object"
    }.ToFrozenSet(StringComparer.Ordinal);

    public static bool ReportsFailure(string operationId) => OperationIds.Contains(operationId);
}
