namespace Briosa.Server.Security;

/// <summary>
/// Maps each <see cref="OperationRisks"/> member to its reviewed snake_case name.
/// </summary>
internal static class OperationRiskVocabulary
{
    /// <summary>Every vocabulary member in declaration order.</summary>
    public static IReadOnlyList<OperationRisks> Members { get; } =
        [
            OperationRisks.FilesystemMetadata,
            OperationRisks.FilesystemRead,
            OperationRisks.FilesystemWrite,
            OperationRisks.FilesystemDelete,
            OperationRisks.Destructive,
            OperationRisks.CodeExecution,
            OperationRisks.PhysicalMotion,
            OperationRisks.DeviceSession,
            OperationRisks.DeviceConfig,
            OperationRisks.InteractiveUi,
            OperationRisks.ExternalIo
        ];

    /// <summary>The union of every vocabulary member.</summary>
    public static OperationRisks All { get; } =
        Members.Aggregate(OperationRisks.None, (all, risk) => all | risk);

    /// <summary>Returns the snake_case name of exactly one vocabulary member.</summary>
    public static string GetName(OperationRisks risk) => risk switch
    {
        OperationRisks.FilesystemMetadata => "filesystem_metadata",
        OperationRisks.FilesystemRead => "filesystem_read",
        OperationRisks.FilesystemWrite => "filesystem_write",
        OperationRisks.FilesystemDelete => "filesystem_delete",
        OperationRisks.Destructive => "destructive",
        OperationRisks.CodeExecution => "code_execution",
        OperationRisks.PhysicalMotion => "physical_motion",
        OperationRisks.DeviceSession => "device_session",
        OperationRisks.DeviceConfig => "device_config",
        OperationRisks.InteractiveUi => "interactive_ui",
        OperationRisks.ExternalIo => "external_io",
        _ => throw new ArgumentOutOfRangeException(nameof(risk), risk,
            "The value is not exactly one operation risk vocabulary member.")
    };

    /// <summary>Returns the snake_case names of every member in a combination, in declaration order.</summary>
    public static IReadOnlyList<string> GetNames(OperationRisks risks)
    {
        if ((risks & ~All) != OperationRisks.None)
        {
            throw new ArgumentOutOfRangeException(nameof(risks), risks,
                "The value contains bits outside the operation risk vocabulary.");
        }

        return Members.Where(risk => risks.HasFlag(risk)).Select(GetName).ToArray();
    }
}
