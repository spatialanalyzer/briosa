using System.Diagnostics;

namespace Briosa.Desktop;

/// <summary>Explicit operator evidence for this package; never inferred from its target.</summary>
public sealed record DesktopIdentitySettings(string SdkVersion = "", string SdkReference = "",
    string ApplicationVersion = "", string ApplicationReference = "")
{
    private const string Prefix = "Briosa__SpatialAnalyzer__Identity__";

    public void Validate()
    {
        ValidateClaim(SdkVersion, SdkReference);
        ValidateClaim(ApplicationVersion, ApplicationReference);
    }

    private static void ValidateClaim(string version, string reference)
    {
        if (string.IsNullOrEmpty(version) && string.IsNullOrEmpty(reference)) return;
        if (string.IsNullOrWhiteSpace(version) || version.Length > 96 ||
            !version.All(c => char.IsAsciiDigit(c) || c == '.') ||
            string.IsNullOrWhiteSpace(reference) || reference.Length > 256 || reference.Any(char.IsControl))
            throw new ArgumentException("Each identity claim needs a version and a non-sensitive evidence reference. Leave both fields empty when no evidence is available.");
    }

    public void ApplyTo(ProcessStartInfo start)
    {
        ArgumentNullException.ThrowIfNull(start);
        Validate();
        ApplyClaim(start, "ActivatedSdk", SdkVersion, SdkReference);
        ApplyClaim(start, "ConnectedSpatialAnalyzer", ApplicationVersion, ApplicationReference);
    }

    private static void ApplyClaim(ProcessStartInfo start, string claim, string version, string reference)
    {
        // Empty fields preserve explicitly configured server/environment evidence.
        if (string.IsNullOrEmpty(version)) return;
        start.Environment[Prefix + claim + "__OperatorAttestation__Version"] = version;
        start.Environment[Prefix + claim + "__OperatorAttestation__Reference"] = reference;
    }
}
