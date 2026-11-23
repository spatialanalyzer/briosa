namespace Briosa.LicensedProbes;

/// <summary>Exact-target identity of this harness build.</summary>
internal static class ProbeTarget
{
    public const string SpatialAnalyzerTarget = "2024.1.0508.5";

    // A licensed run refuses to start without this exact-target confirmation.
    public const string ConfirmFlag = "--confirm-licensed-sa2024";

    // Any other target's confirmation is a refusal, never a substitute.
    public const string ConfirmFlagPrefix = "--confirm-licensed-sa";

    public const string Issue = "spatialanalyzer/briosa#277";

    // Licensed session 4: result sizes, cold starts and the corrected bindings.
    public const string Session4Issue = "spatialanalyzer/briosa#402";
}
