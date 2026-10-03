using System.Globalization;

namespace Briosa.LicensedProbes;

/// <summary>
/// Parsed command line. Parsing is strict: unknown, repeated, or contradictory
/// options are refused before any plan is built or any client is created.
/// </summary>
internal sealed record ProbeOptions
{
    private static readonly HashSet<string> Flags = new(StringComparer.Ordinal) { "--dry-run", ProbeTarget.ConfirmFlag };

    private static readonly HashSet<string> Valued = new(StringComparer.Ordinal)
    {
        "--phase", "--fixtures", "--output-directory", "--address", "--worker-path",
        "--connected-sa-attested-version", "--connected-sa-attestation-reference",
        "--activated-sdk-attestation-reference", "--step-timeout-seconds", "--operator-timeout-seconds",
        "--harness-revision"
    };

    public ProbePhase Phase { get; init; }

    public bool DryRun { get; init; }

    public string? FixturesPath { get; init; }

    public string? OutputDirectory { get; init; }

    public Uri? Address { get; init; }

    public string? WorkerPath { get; init; }

    public string? ConnectedSaAttestedVersion { get; init; }

    public string? ConnectedSaAttestationReference { get; init; }

    public string? ActivatedSdkAttestationReference { get; init; }

    public string? HarnessRevision { get; init; }

    public TimeSpan StepTimeout { get; init; } = TimeSpan.FromSeconds(120);

    public TimeSpan OperatorTimeout { get; init; } = TimeSpan.FromSeconds(300);

    public static ProbeOptions Parse(IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        var flags = new HashSet<string>(StringComparer.Ordinal);
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 0; index < arguments.Count; index++)
        {
            var argument = arguments[index];
            if (argument.StartsWith(ProbeTarget.ConfirmFlagPrefix, StringComparison.Ordinal) &&
                !string.Equals(argument, ProbeTarget.ConfirmFlag, StringComparison.Ordinal))
            {
                throw new ProbeUsageException("This harness serves only its exact target; another target's confirmation is refused.");
            }

            if (Flags.Contains(argument))
            {
                if (!flags.Add(argument))
                    throw new ProbeUsageException($"Option '{argument}' is repeated.");
                continue;
            }

            if (!Valued.Contains(argument))
                throw new ProbeUsageException("An unknown option was supplied.");
            if (index + 1 >= arguments.Count)
                throw new ProbeUsageException($"Option '{argument}' needs a value.");
            if (!values.TryAdd(argument, arguments[++index]))
                throw new ProbeUsageException($"Option '{argument}' is repeated.");
        }

        var dryRun = flags.Contains("--dry-run");
        var confirmed = flags.Contains(ProbeTarget.ConfirmFlag);
        var phase = Value(values, "--phase") switch
        {
            "public-api" => ProbePhase.PublicApi,
            "worker" => ProbePhase.Worker,
            null => throw new ProbeUsageException("--phase public-api|worker is required."),
            _ => throw new ProbeUsageException("--phase must be public-api or worker.")
        };

        var options = new ProbeOptions
        {
            Phase = phase,
            DryRun = dryRun,
            FixturesPath = Value(values, "--fixtures"),
            OutputDirectory = Value(values, "--output-directory"),
            ConnectedSaAttestedVersion = Value(values, "--connected-sa-attested-version"),
            ConnectedSaAttestationReference = Value(values, "--connected-sa-attestation-reference"),
            ActivatedSdkAttestationReference = Value(values, "--activated-sdk-attestation-reference"),
            HarnessRevision = Value(values, "--harness-revision"),
            WorkerPath = Value(values, "--worker-path"),
            StepTimeout = Seconds(values, "--step-timeout-seconds", 120, 5, 600),
            OperatorTimeout = Seconds(values, "--operator-timeout-seconds", 300, 30, 1800)
        };

        foreach (var reference in new[] { options.ConnectedSaAttestationReference, options.ActivatedSdkAttestationReference })
        {
            if (reference is not null && !IsSafeReference(reference))
                throw new ProbeUsageException("Attestation references must be short, non-sensitive identifiers.");
        }

        if (options.HarnessRevision is not null &&
            (options.HarnessRevision.Length != 40 || !options.HarnessRevision.All(char.IsAsciiHexDigitLower)))
            throw new ProbeUsageException("--harness-revision must be a 40-character lowercase commit SHA.");

        if (dryRun)
        {
            // A dry run never connects; connection options would only mislead.
            if (confirmed || values.ContainsKey("--address") || options.WorkerPath is not null)
                throw new ProbeUsageException("--dry-run never connects; remove the confirmation and connection options.");
            return options;
        }

        if (!confirmed)
            throw new ProbeUsageException($"A licensed session requires {ProbeTarget.ConfirmFlag} or --dry-run.");
        if (options.FixturesPath is null || options.OutputDirectory is null)
            throw new ProbeUsageException("A licensed session requires --fixtures and --output-directory.");

        if (phase == ProbePhase.PublicApi)
        {
            if (options.WorkerPath is not null || options.ConnectedSaAttestedVersion is not null)
                throw new ProbeUsageException("Worker options are not valid for the public-api phase.");
            var address = new Uri(Value(values, "--address") ?? "http://127.0.0.1:50051", UriKind.Absolute);
            if (!address.IsLoopback || address.Scheme != Uri.UriSchemeHttp)
                throw new ProbeUsageException("--address must be loopback HTTP.");
            return options with { Address = address };
        }

        if (values.ContainsKey("--address"))
            throw new ProbeUsageException("--address is not valid for the worker phase.");
        if (options.WorkerPath is null)
            throw new ProbeUsageException("The worker phase requires --worker-path.");

        // The worker cannot runtime-verify the connected SA release; the operator attests it.
        if (!string.Equals(options.ConnectedSaAttestedVersion, ProbeTarget.SpatialAnalyzerTarget, StringComparison.Ordinal) ||
            options.ConnectedSaAttestationReference is null)
            throw new ProbeUsageException("The worker phase requires an exact connected-SA attestation and reference.");
        return options;
    }

    internal static bool IsSafeReference(string value) =>
        value.Length is > 0 and <= 128 &&
        value.All(static c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.');

    private static string? Value(Dictionary<string, string> values, string key) =>
        values.TryGetValue(key, out var value) ? value : null;

    private static TimeSpan Seconds(Dictionary<string, string> values, string key, int fallback, int minimum, int maximum)
    {
        var text = Value(values, key);
        if (text is null)
            return TimeSpan.FromSeconds(fallback);
        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds) || seconds < minimum || seconds > maximum)
            throw new ProbeUsageException($"{key} must be between {minimum} and {maximum}.");
        return TimeSpan.FromSeconds(seconds);
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1032:Implement standard exception constructors",
    Justification = "Usage errors carry one reviewed message and never wrap other exceptions.")]
internal sealed class ProbeUsageException(string message) : Exception(message);
