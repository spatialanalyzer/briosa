namespace Briosa.LicensedProbes;

/// <summary>Exact-target identity of this harness build.</summary>
internal static class ProbeTarget
{
    public const string SpatialAnalyzerTarget = "2026.1.0529.7";

    // A licensed run refuses to start without this exact-target confirmation.
    public const string ConfirmFlag = "--confirm-licensed-sa2026";

    // Any other target's confirmation is a refusal, never a substitute.
    public const string ConfirmFlagPrefix = "--confirm-licensed-sa";

    public const string Issue = "spatialanalyzer/briosa#277";
}
