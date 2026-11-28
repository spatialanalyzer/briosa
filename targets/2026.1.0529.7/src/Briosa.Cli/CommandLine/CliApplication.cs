using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text.Json.Nodes;
using Briosa.Cli.Engine;

namespace Briosa.Cli.CommandLine;

/// <summary>Where the CLI writes, and the engine environment it runs with.</summary>
internal sealed record CliHost(Stream StandardOutput, TextWriter StandardError, BridgeEnvironment Environment);

/// <summary>
/// The JSON command line. It only turns arguments into engine requests and an
/// envelope into an exit code: every other behavior lives in the shared engine.
/// Standard output carries exactly one envelope; standard error carries at most
/// one value-free line.
/// </summary>
internal static class CliApplication
{
    public static IReadOnlyList<string> Usage { get; } =
        [
            "Briosa.Cli <command> [options]",
            "  status [--endpoint URL]",
            "  ops categories [--endpoint URL]",
            "  ops search [--query TEXT] [--category NAME] [--effect read_only|mutating|unknown]",
            "             [--risk-flag FLAG]... [--tier read_only|read_only_restricted|mutating|excluded]",
            "             [--include-denied] [--validated-only] [--offset N] [--limit N] [--endpoint URL]",
            "  ops describe OPERATION_ID [--endpoint URL]",
            "  call OPERATION_ID (--json TEXT | --input FILE) [--output FILE] [--max-inline-bytes N]",
            "       [--deadline-seconds N] [--endpoint URL]",
            "  version",
            "  help",
            "The endpoint defaults to " + LoopbackEndpoint.DefaultText + " and must be an IP loopback address.",
            "Standard output is one JSON envelope; see docs/operations/briosa-cli.md for its members and exit codes."
        ];

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "An unexpected fault before any operation is sent still produces a value-free envelope.")]
    public static async Task<int> RunAsync(IReadOnlyList<string> arguments, CliHost host,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(host);
        BridgeEnvelope envelope;
        try
        {
            envelope = await ExecuteAsync(arguments, host.Environment, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // The engine catches every fault after a call is sent, so nothing ran.
            envelope = new BridgeEnvelope("unknown", BridgeOutcome.InternalError)
            {
                Failure = BridgeEngine.Failure("cli", "client-internal-error")
            };
        }

        var exitCode = ExitCodes.For(envelope.Outcome);
        var bytes = envelope.ToUtf8(exitCode);
        await host.StandardOutput.WriteAsync(bytes, cancellationToken: CancellationToken.None).ConfigureAwait(false);
        await host.StandardOutput.FlushAsync(CancellationToken.None).ConfigureAwait(false);
        if (exitCode != 0)
        {
            await host.StandardError.WriteLineAsync(envelope.Diagnostic()).ConfigureAwait(false);
            await host.StandardError.FlushAsync(CancellationToken.None).ConfigureAwait(false);
        }

        return exitCode;
    }

    private static async Task<BridgeEnvelope> ExecuteAsync(IReadOnlyList<string> arguments,
        BridgeEnvironment environment, CancellationToken cancellationToken)
    {
        var command = CommandParser.Parse(arguments, out var usageCode, out var recognized);
        if (command is null)
        {
            return UsageError(recognized, usageCode!);
        }

        switch (command.Name)
        {
            case "help":
                return new BridgeEnvelope("help", BridgeOutcome.Succeeded)
                {
                    Data = new JsonObject { ["usage"] = new JsonArray(Usage.Select(line => (JsonNode)line).ToArray()) }
                };
            case "version":
                return Version();
        }

        var endpointText = command.Option(CommandParser.EndpointOption) ?? LoopbackEndpoint.DefaultText;
        if (!LoopbackEndpoint.TryParse(endpointText, out var endpoint))
        {
            var refused = new BridgeEnvelope(command.Name, BridgeOutcome.RejectedLocally)
            {
                Failure = BridgeEngine.Failure("cli", "endpoint-not-loopback"),
                Execution = NotSentFor(command.Name)
            };
            refused.Guidance.Add("The endpoint must be http:// with an IP loopback address (127.0.0.0/8 or [::1]) " +
                "and an explicit port. Hostnames, including localhost, are rejected.");
            return refused;
        }

        var engine = new BridgeEngine(endpoint!, environment);
        switch (command.Name)
        {
            case "status":
                return await engine.StatusAsync(cancellationToken).ConfigureAwait(false);
            case "ops categories":
                return await engine.CategoriesAsync(cancellationToken).ConfigureAwait(false);
            case "ops describe":
                return await engine.DescribeAsync(command.Arguments[0], cancellationToken).ConfigureAwait(false);
            case "ops search":
                var query = ReadQuery(command, out var queryCode);
                return query is null
                    ? UsageError(command.Name, queryCode!)
                    : await engine.SearchAsync(query, cancellationToken).ConfigureAwait(false);
            default:
                var call = ReadCall(command, out var callCode);
                return call is null
                    ? UsageError(command.Name, callCode!)
                    : await engine.CallAsync(call, cancellationToken).ConfigureAwait(false);
        }
    }

    private static OperationQuery? ReadQuery(ParsedCommand command, out string? code)
    {
        code = null;
        var text = command.Option("--query");
        if (text is { Length: 0 or > 200 })
        {
            code = "usage-invalid-value";
            return null;
        }

        var category = command.Option("--category");
        if (category is not null && !IsCategory(category))
        {
            code = "usage-invalid-value";
            return null;
        }

        OperationEffect? effect = command.Option("--effect") switch
        {
            null => null,
            "read_only" => OperationEffect.ReadOnly,
            "mutating" => OperationEffect.Mutating,
            "unknown" => OperationEffect.Unknown,
            _ => OperationEffect.Unspecified
        };
        BridgeTier? tier = null;
        if (command.Option("--tier") is { } tierText)
        {
            if (!BridgeTiers.TryParse(tierText, out var parsedTier))
            {
                code = "usage-invalid-value";
                return null;
            }

            tier = parsedTier;
        }

        List<OperationRiskFlag> flags = [];
        foreach (var flagText in command.Values("--risk-flag"))
        {
            if (!ProtocolJson.TryParseRiskFlag(flagText, out var flag) || flags.Contains(flag))
            {
                code = "usage-invalid-value";
                return null;
            }

            flags.Add(flag);
        }

        if (effect == OperationEffect.Unspecified ||
            !TryReadInteger(command.Option("--offset"), 0, int.MaxValue, 0, out var offset) ||
            !TryReadInteger(command.Option("--limit"), 1, OperationQuery.MaximumLimit, OperationQuery.DefaultLimit,
                out var limit))
        {
            code = "usage-invalid-value";
            return null;
        }

        return new OperationQuery(text, category, effect, flags, tier,
            command.Switches.Contains("--include-denied"), command.Switches.Contains("--validated-only"), offset, limit);
    }

    private static CallRequest? ReadCall(ParsedCommand command, out string? code)
    {
        code = null;
        var json = command.Option("--json");
        var input = command.Option("--input");
        if (json is null == input is null)
        {
            code = json is null ? "usage-input-required" : "usage-input-conflict";
            return null;
        }

        if (!TryReadInteger(command.Option("--max-inline-bytes"), 0, ResultDelivery.MaximumInlineBytesLimit,
                ResultDelivery.DefaultMaximumInlineBytes, out var maximumInline) ||
            !TryReadInteger(command.Option("--deadline-seconds"), 1, 86_400, 0, out var deadlineSeconds) ||
            command.Option("--output") is { Length: 0 } ||
            input is { Length: 0 })
        {
            code = "usage-invalid-value";
            return null;
        }

        return new CallRequest(command.Arguments[0], json, input, command.Option("--output"), maximumInline,
            deadlineSeconds == 0 ? null : TimeSpan.FromSeconds(deadlineSeconds));
    }

    private static bool TryReadInteger(string? text, int minimum, int maximum, int fallback, out int value)
    {
        if (text is null)
        {
            value = fallback;
            return true;
        }

        return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value) &&
            value >= minimum && value <= maximum && text.Length <= 10;
    }

    private static bool IsCategory(string text) =>
        text.Length is > 0 and <= 64 && text[0] is >= 'a' and <= 'z' &&
        text.All(character => character is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '_');

    private static BridgeEnvelope UsageError(string? command, string code)
    {
        var envelope = new BridgeEnvelope(command ?? "unknown", BridgeOutcome.UsageError)
        {
            Failure = BridgeEngine.Failure("cli", code),
            Execution = NotSentFor(command),
            Data = new JsonObject { ["usage"] = new JsonArray(Usage.Select(line => (JsonNode)line).ToArray()) }
        };
        return envelope;
    }

    // A call refused before any RPC reports, like every local rejection of a call,
    // that nothing was sent.
    private static JsonObject? NotSentFor(string? command) =>
        string.Equals(command, "call", StringComparison.Ordinal) ? MpOutcome.NotSent() : null;

    private static BridgeEnvelope Version()
    {
        var assembly = typeof(CliApplication).Assembly;
        return new BridgeEnvelope("version", BridgeOutcome.Succeeded)
        {
            Data = new JsonObject
            {
                ["cli_version"] = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
                ["spatial_analyzer_target"] = BridgeContract.SpatialAnalyzerTarget,
                ["compatibility_major"] = BridgeContract.CompatibilityMajor,
                ["minimum_revision_for_call"] = BridgeContract.MinimumExecutionRevision,
                ["envelope_version"] = BridgeEnvelope.Version
            }
        };
    }
}
