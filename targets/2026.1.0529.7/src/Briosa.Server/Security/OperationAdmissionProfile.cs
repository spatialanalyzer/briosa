namespace Briosa.Server.Security;

/// <summary>
/// A named admission profile decided on #242: a set of admitted risks plus an
/// optional read-only effect filter. A profile admits an operation when every
/// risk on its reviewed classification row is admitted and the effect filter
/// passes. <see cref="OperationRisks.InteractiveUi"/> is in no profile; an
/// operator opts in with <c>Flags:interactive_ui=allow</c> or a per-operation
/// override. No profile, flag, or override admits an exclusive workflow.
/// </summary>
internal sealed record OperationAdmissionProfile(
    string Name,
    OperationRisks AdmittedRisks,
    bool ReadOnlyEffectOnly)
{
    /// <summary>The packaged default profile.</summary>
    public const string DefaultName = "standard";

    private const OperationRisks ReadOnlyRisks = OperationRisks.FilesystemMetadata;

    private const OperationRisks StandardRisks = ReadOnlyRisks |
        OperationRisks.FilesystemRead |
        OperationRisks.FilesystemWrite |
        OperationRisks.Destructive;

    private const OperationRisks DeviceRisks = StandardRisks |
        OperationRisks.DeviceSession |
        OperationRisks.DeviceConfig |
        OperationRisks.PhysicalMotion;

    private const OperationRisks FullRisks = DeviceRisks |
        OperationRisks.ExternalIo |
        OperationRisks.CodeExecution |
        OperationRisks.FilesystemDelete;

    /// <summary>Inspection: read-only effect and filesystem metadata only.</summary>
    public static OperationAdmissionProfile ReadOnly { get; } = new("read-only", ReadOnlyRisks, ReadOnlyEffectOnly: true);

    /// <summary>Document automation, including file reads and writes and destructive model edits.</summary>
    public static OperationAdmissionProfile Standard { get; } = new("standard", StandardRisks, ReadOnlyEffectOnly: false);

    /// <summary>Standard plus one-call device actions and device configuration.</summary>
    public static OperationAdmissionProfile Device { get; } = new("device", DeviceRisks, ReadOnlyEffectOnly: false);

    /// <summary>Device plus external IO, code execution, and file deletion.</summary>
    public static OperationAdmissionProfile Full { get; } = new("full", FullRisks, ReadOnlyEffectOnly: false);

    /// <summary>Every profile, narrowest first.</summary>
    public static IReadOnlyList<OperationAdmissionProfile> All { get; } = [ReadOnly, Standard, Device, Full];

    /// <summary>Returns the profile with exactly this name, or null.</summary>
    public static OperationAdmissionProfile? Find(string name) =>
        All.FirstOrDefault(profile => string.Equals(profile.Name, name, StringComparison.Ordinal));

    /// <summary>
    /// True when <paramref name="risks"/> are all admitted by this profile or by
    /// <paramref name="additionallyAllowed"/>, and the effect filter passes.
    /// </summary>
    public bool Admits(
        OperationRisks risks,
        global::Briosa.OperationEffect effect,
        OperationRisks additionallyAllowed = OperationRisks.None) =>
        (risks & ~(AdmittedRisks | additionallyAllowed)) == OperationRisks.None &&
        (!ReadOnlyEffectOnly || effect == global::Briosa.OperationEffect.ReadOnly);
}
