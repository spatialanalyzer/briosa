using System.Globalization;

namespace Briosa.Server.Workers;

internal sealed record OperatorAttestationOptions(string Version, string Reference)
{
    public static OperatorAttestationOptions? BindAndValidate(
        IConfiguration configuration,
        string versionKey,
        string referenceKey)
    {
        var version = configuration[versionKey];
        var reference = configuration[referenceKey];
        if (string.IsNullOrWhiteSpace(version) && string.IsNullOrWhiteSpace(reference))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(version) || string.IsNullOrWhiteSpace(reference))
        {
            throw new InvalidOperationException(
                $"Operator attestation requires both '{versionKey}' and '{referenceKey}'.");
        }

        if (!IsValidVersion(version) ||
            reference.Length > 256 ||
            reference.Contains('\r', StringComparison.Ordinal) ||
            reference.Contains('\n', StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Operator attestation values for '{versionKey}' and '{referenceKey}' have an invalid shape.");
        }

        return new OperatorAttestationOptions(version, reference);
    }

    private static bool IsValidVersion(string version) =>
        version.Length <= 128 &&
        !version.Contains('\r', StringComparison.Ordinal) &&
        !version.Contains('\n', StringComparison.Ordinal);
}
