namespace Briosa.Server.Security;

/// <summary>
/// Maps each <see cref="OperationRisks"/> member to its reviewed snake_case name
/// and to its discovery wire value, <see cref="global::Briosa.OperationRiskFlag"/>.
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
        _ => throw NotOneMember(risk)
    };

    /// <summary>Returns the snake_case names of every member in a combination, in declaration order.</summary>
    public static IReadOnlyList<string> GetNames(OperationRisks risks) =>
        MembersOf(risks).Select(GetName).ToArray();

    /// <summary>Returns the discovery wire value of exactly one vocabulary member.</summary>
    public static global::Briosa.OperationRiskFlag GetWireFlag(OperationRisks risk) => risk switch
    {
        OperationRisks.FilesystemMetadata => global::Briosa.OperationRiskFlag.FilesystemMetadata,
        OperationRisks.FilesystemRead => global::Briosa.OperationRiskFlag.FilesystemRead,
        OperationRisks.FilesystemWrite => global::Briosa.OperationRiskFlag.FilesystemWrite,
        OperationRisks.FilesystemDelete => global::Briosa.OperationRiskFlag.FilesystemDelete,
        OperationRisks.Destructive => global::Briosa.OperationRiskFlag.Destructive,
        OperationRisks.CodeExecution => global::Briosa.OperationRiskFlag.CodeExecution,
        OperationRisks.PhysicalMotion => global::Briosa.OperationRiskFlag.PhysicalMotion,
        OperationRisks.DeviceSession => global::Briosa.OperationRiskFlag.DeviceSession,
        OperationRisks.DeviceConfig => global::Briosa.OperationRiskFlag.DeviceConfig,
        OperationRisks.InteractiveUi => global::Briosa.OperationRiskFlag.InteractiveUi,
        OperationRisks.ExternalIo => global::Briosa.OperationRiskFlag.ExternalIo,
        _ => throw NotOneMember(risk)
    };

    /// <summary>Returns the discovery wire values of every member in a combination, in declaration order.</summary>
    public static IReadOnlyList<global::Briosa.OperationRiskFlag> GetWireFlags(OperationRisks risks) =>
        MembersOf(risks).Select(GetWireFlag).ToArray();

    private static IEnumerable<OperationRisks> MembersOf(OperationRisks risks)
    {
        if ((risks & ~All) != OperationRisks.None)
        {
            throw new ArgumentOutOfRangeException(nameof(risks), risks,
                "The value contains bits outside the operation risk vocabulary.");
        }

        return Members.Where(risk => risks.HasFlag(risk));
    }

    private static ArgumentOutOfRangeException NotOneMember(OperationRisks risk) =>
        new(nameof(risk), risk, "The value is not exactly one operation risk vocabulary member.");
}
