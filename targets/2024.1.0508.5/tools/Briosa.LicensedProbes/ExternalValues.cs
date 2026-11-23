using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Briosa.LicensedProbes;

/// <summary>
/// Checks every value that reaches a record from outside the harness: the server's and the
/// worker's diagnostic codes, the server's version and revision, and output field names. A
/// value that is not on a reviewed closed list or in a strict shape is recorded as
/// <see cref="Withheld"/>, so a machine name, account, path or license text sent back by a
/// server, a worker or a log can never reach evidence (#402).
/// </summary>
internal static partial class ExternalValues
{
    public const string Withheld = "withheld";

    // The conformance digest's reviewed closed list of the codes the server, the worker and
    // the fake worker log (conformance/client/v1/digest-vocabulary.json), compiled in.
    private const string VocabularyResource = "Briosa.LicensedProbes.digest-vocabulary.json";

    private static readonly Lazy<IReadOnlySet<string>> Codes = new(static () => LoadCodes());

    public static IReadOnlySet<string> DiagnosticCodes => Codes.Value;

    /// <summary>A listed code, <see cref="Withheld"/> for any other value, or null.</summary>
    public static string? DiagnosticCode(string? code) =>
        code is null ? null : Codes.Value.Contains(code) ? code : Withheld;

    /// <summary>A release version such as <c>0.10.1-probe-s4</c>, or <see cref="Withheld"/>.</summary>
    public static string? Version(string? version) =>
        version is null ? null : VersionShape().IsMatch(version) ? version : Withheld;

    /// <summary>A full lowercase commit SHA, or <see cref="Withheld"/>.</summary>
    public static string? Revision(string? revision) =>
        revision is null ? null : revision.Length == 40 && revision.All(char.IsAsciiHexDigitLower) ? revision : Withheld;

    /// <summary>A protobuf field name such as <c>resultant_point_name_list</c>, or <see cref="Withheld"/>.</summary>
    public static string FieldName(string? name) =>
        name is not null && FieldShape().IsMatch(name) ? name : Withheld;

    [GeneratedRegex("^[0-9]{1,4}\\.[0-9]{1,4}\\.[0-9]{1,6}(-[0-9A-Za-z][0-9A-Za-z.-]{0,39})?$", RegexOptions.CultureInvariant)]
    private static partial Regex VersionShape();

    [GeneratedRegex("^[a-z][a-z0-9_]{0,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex FieldShape();

    private static HashSet<string> LoadCodes()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(VocabularyResource)
            ?? throw new InvalidOperationException("The diagnostic code vocabulary is missing from the harness.");
        using var document = JsonDocument.Parse(stream);
        var codes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var code in document.RootElement.GetProperty("diagnostic_codes").EnumerateArray())
        {
            // Even a listed code must have the stable lowercase shape.
            if (code.ValueKind == JsonValueKind.String && SafeCode.OrNull(code.GetString()) is { } safe)
            {
                codes.Add(safe);
            }
        }

        return codes;
    }
}
