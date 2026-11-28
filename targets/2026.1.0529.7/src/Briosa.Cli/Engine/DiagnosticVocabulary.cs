using System.Collections.Frozen;

namespace Briosa.Cli.Engine;

/// <summary>
/// The diagnostic codes standard error may name. A code the CLI itself produced
/// (failure source <c>cli</c> or <c>transport</c>) is one of its own fixed literals.
/// A code the server sent is printed only when it is one of the server's documented
/// codes on this target; any other server text, however it is spelled, is replaced by
/// <see cref="ServerFallback"/>. The envelope on standard output still carries the
/// server's code as data.
/// </summary>
internal static class DiagnosticVocabulary
{
    public const string ServerFallback = "server-diagnostic";

    private static readonly FrozenSet<string> ServerCodes = new[]
    {
        "request-validation-failed", "caller-restriction-invalid", "request-encoding-rejected",
        "operation-policy-denied", "operation-option-denied", "caller-restriction-denied",
        "caller-restriction-option-denied", "operation-unsupported", "operation-binding-mismatch",
        "operation-risk-unreviewed", "operation-isolation-unreviewed", "operation-request-unclassified",
        "operation-isolation-unsupported", "operation-option-isolation-unsupported", "operation-duration-unreviewed",
        "worker-admission-full", "queued-request-abandoned", "client-wait-cancelled", "mp-command-failed",
        "execute-step-rejected", "sdk-argument-rejected", "sdk-mp-result-retrieval-failed",
        "sdk-output-retrieval-failed", "sdk-call-faulted-before-execute", "sdk-execute-step-faulted",
        "sdk-mp-result-retrieval-faulted", "sdk-output-getter-faulted", "worker-output-encoding-rejected",
        "worker-output-shape-invalid", "worker-result-missing", "result-mapping-failed",
        "request-violation-after-mapping", "internal-operation-failure", "worker-execution-failed",
        "worker-watchdog-timeout", "sa-modal-dialog-open"
    }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>The code to show on standard error for a failure, or null for none.</summary>
    public static string? Printable(string? source, string? code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return null;
        }

        var local = source is "cli" or "transport";
        return (local && IsWellFormed(code)) || (!local && ServerCodes.Contains(code)) ? code : ServerFallback;
    }

    private static bool IsWellFormed(string code) =>
        code.Length is > 0 and <= 80 && code.All(character => character is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-');
}
