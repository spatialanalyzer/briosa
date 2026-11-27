using Grpc.Core;

namespace Briosa.Server.Security;

/// <summary>
/// A per-call restriction a caller asserts with the <c>briosa-caller-restriction</c>
/// request header (#443): an optional admission profile and a set of denied risk
/// flags. <see cref="OperationPolicy"/> applies it after the server's own
/// configuration has admitted a request, on the same effective risks and effect,
/// so it can only narrow what the server admits.
/// </summary>
/// <remarks>
/// The header value is one or more clauses separated by a semicolon and one
/// space. A clause is <c>profile=&lt;profile&gt;</c> or
/// <c>deny=&lt;flag&gt;[,&lt;flag&gt;...]</c>; each clause appears at most once and
/// each flag at most once. Names are the exact, case-sensitive profile and risk
/// flag names. No other character, whitespace, or key is accepted. For example:
/// <c>profile=read-only; deny=interactive_ui,filesystem_write</c>.
/// </remarks>
internal sealed record OperationCallerRestriction
{
    /// <summary>The gRPC request metadata key.</summary>
    public const string HeaderName = "briosa-caller-restriction";

    /// <summary>
    /// The longest value accepted. The longest valid value, the read-only profile
    /// with every flag denied, has 193 characters.
    /// </summary>
    public const int MaximumLength = 256;

    internal const string ProfileKey = "profile";
    internal const string DenyKey = "deny";
    private const string ClauseSeparator = "; ";

    private OperationCallerRestriction(OperationAdmissionProfile? profile, OperationRisks deniedFlags)
    {
        Profile = profile;
        DeniedFlags = deniedFlags;
    }

    /// <summary>The caller's admission profile, or null when the caller named none.</summary>
    public OperationAdmissionProfile? Profile { get; }

    /// <summary>The risk flags the caller denies; <see cref="OperationRisks.None"/> when it named none.</summary>
    public OperationRisks DeniedFlags { get; }

    /// <summary>
    /// Creates a restriction from a known profile and vocabulary flags. A
    /// restriction names a profile, at least one denied flag, or both.
    /// </summary>
    public static OperationCallerRestriction Create(OperationAdmissionProfile? profile, OperationRisks deniedFlags)
    {
        if ((deniedFlags & ~OperationRiskVocabulary.All) != OperationRisks.None)
        {
            throw new ArgumentOutOfRangeException(nameof(deniedFlags), deniedFlags,
                "The value contains bits outside the operation risk vocabulary.");
        }

        if (profile is not null && !OperationAdmissionProfile.All.Contains(profile))
        {
            throw new ArgumentException("The profile is not a reviewed admission profile.", nameof(profile));
        }

        if (profile is null && deniedFlags == OperationRisks.None)
        {
            throw new ArgumentException("A caller restriction names a profile, a denied flag, or both.");
        }

        return new(profile, deniedFlags);
    }

    /// <summary>
    /// Reads the caller restriction from request metadata. No entry is
    /// <see cref="OperationCallerRestrictionHeader.Absent"/>. More than one entry,
    /// a binary entry, or a value that does not parse is malformed.
    /// </summary>
    public static OperationCallerRestrictionHeader Read(Metadata? headers)
    {
        if (headers is null)
        {
            return OperationCallerRestrictionHeader.Absent;
        }

        Metadata.Entry? found = null;
        var count = 0;
        foreach (var entry in headers)
        {
            // Metadata keys are lowercase; a binary variant of the key is not a restriction.
            if (string.Equals(entry.Key, HeaderName, StringComparison.Ordinal) ||
                string.Equals(entry.Key, HeaderName + Metadata.BinaryHeaderSuffix, StringComparison.Ordinal))
            {
                found = entry;
                count++;
            }
        }

        if (count == 0)
        {
            return OperationCallerRestrictionHeader.Absent;
        }

        return count == 1 && !found!.IsBinary && Parse(found.Value) is { } restriction
            ? OperationCallerRestrictionHeader.Of(restriction)
            : OperationCallerRestrictionHeader.Malformed;
    }

    /// <summary>Parses one header value, or returns null when it is malformed.</summary>
    public static OperationCallerRestriction? Parse(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > MaximumLength)
        {
            return null;
        }

        foreach (var character in value)
        {
            if (character is not ((>= 'a' and <= 'z') or '_' or '-' or '=' or ';' or ',' or ' '))
            {
                return null;
            }
        }

        OperationAdmissionProfile? profile = null;
        OperationRisks? denied = null;
        foreach (var clause in value.Split(ClauseSeparator))
        {
            // The separator is consumed, so any remaining semicolon or space is stray.
            var equals = clause.IndexOf('=', StringComparison.Ordinal);
            if (equals <= 0 || clause.AsSpan().ContainsAny(';', ' '))
            {
                return null;
            }

            var key = clause[..equals];
            var argument = clause[(equals + 1)..];
            if (argument.Contains('=', StringComparison.Ordinal))
            {
                return null;
            }

            switch (key)
            {
                case ProfileKey when profile is null:
                    profile = OperationAdmissionProfile.Find(argument);
                    if (profile is null)
                    {
                        return null;
                    }

                    break;
                case DenyKey when denied is null:
                    denied = ParseFlags(argument);
                    if (denied is null)
                    {
                        return null;
                    }

                    break;
                default:
                    // An unknown key, or a repeated one.
                    return null;
            }
        }

        return new(profile, denied ?? OperationRisks.None);
    }

    /// <summary>
    /// The canonical header value: the profile clause first, then the denied flags
    /// in vocabulary order. It holds only reviewed profile and flag names.
    /// </summary>
    public string Format()
    {
        var clauses = new List<string>(2);
        if (Profile is { } profile)
        {
            clauses.Add($"{ProfileKey}={profile.Name}");
        }

        if (DeniedFlags != OperationRisks.None)
        {
            clauses.Add($"{DenyKey}={string.Join(',', OperationRiskVocabulary.GetNames(DeniedFlags))}");
        }

        return string.Join(ClauseSeparator, clauses);
    }

    private static OperationRisks? ParseFlags(string argument)
    {
        if (argument.Length == 0)
        {
            return null;
        }

        var flags = OperationRisks.None;
        foreach (var name in argument.Split(','))
        {
            var flag = OperationRiskVocabulary.Members.Cast<OperationRisks?>().FirstOrDefault(member =>
                string.Equals(OperationRiskVocabulary.GetName(member!.Value), name, StringComparison.Ordinal));
            if (flag is not { } known || flags.HasFlag(known))
            {
                // An empty, unknown, or repeated flag.
                return null;
            }

            flags |= known;
        }

        return flags;
    }
}

/// <summary>
/// What a call's request metadata says about a caller restriction: absent,
/// a parsed restriction, or malformed. A malformed header rejects the call
/// before policy evaluation or dispatch.
/// </summary>
internal readonly record struct OperationCallerRestrictionHeader
{
    private OperationCallerRestrictionHeader(OperationCallerRestriction? restriction, bool isMalformed)
    {
        Restriction = restriction;
        IsMalformed = isMalformed;
    }

    /// <summary>The value-free diagnostic code of a call rejected for a malformed header.</summary>
    public const string InvalidDiagnosticCode = "caller-restriction-invalid";

    public static OperationCallerRestrictionHeader Absent => default;

    public static OperationCallerRestrictionHeader Malformed { get; } = new(null, isMalformed: true);

    /// <summary>The parsed restriction; null when the header is absent or malformed.</summary>
    public OperationCallerRestriction? Restriction { get; }

    public bool IsMalformed { get; }

    public static OperationCallerRestrictionHeader Of(OperationCallerRestriction restriction) =>
        new(restriction ?? throw new ArgumentNullException(nameof(restriction)), isMalformed: false);
}
