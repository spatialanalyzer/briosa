using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Briosa.Cli.Engine;

/// <summary>
/// The one result envelope every command returns. Every top-level member is always
/// present, null when it does not apply, so a reader never has to guess between
/// "absent" and "not applicable". The CLI writes it as one line of UTF-8 JSON; the
/// MCP server will return the same object.
/// </summary>
internal sealed class BridgeEnvelope
{
    /// <summary>Raised when a member is added, removed, or changes meaning.</summary>
    public const int Version = 1;

    // The envelope is JSON for programs, never embedded in HTML, so HTML-sensitive
    // characters and non-ASCII text stay readable. Quotes, backslashes, and every
    // control character are still escaped, so text from SpatialAnalyzer cannot break
    // out of its string value or add a line.
    private static readonly JsonWriterOptions WriterOptions = new()
    {
        Indented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public BridgeEnvelope(string command, BridgeOutcome outcome)
    {
        Command = command;
        Outcome = outcome;
    }

    public string Command { get; }

    public BridgeOutcome Outcome { get; set; }

    /// <summary>The normalized loopback endpoint, when one was selected.</summary>
    public string? Endpoint { get; set; }

    /// <summary>The server's identity and compatibility as it reported them, and whether they were verified.</summary>
    public JsonObject? Server { get; set; }

    /// <summary>The gRPC method and canonical status of the last RPC the command depended on.</summary>
    public JsonObject? Transport { get; set; }

    /// <summary>Why the command did not succeed, kept apart from the execution dimensions.</summary>
    public JsonObject? Failure { get; set; }

    /// <summary>For <c>call</c>: what is known about whether and how the MP ran.</summary>
    public JsonObject? Execution { get; set; }

    /// <summary>The command's own payload.</summary>
    public JsonNode? Data { get; set; }

    public List<BridgeNotice> Warnings { get; } = [];

    public List<string> Guidance { get; } = [];

    public JsonObject ToJson(int? exitCode)
    {
        var root = new JsonObject
        {
            ["envelope_version"] = Version,
            ["command"] = Command,
            ["outcome"] = BridgeOutcomes.Name(Outcome)
        };
        if (exitCode is { } code)
        {
            root["exit_code"] = code;
        }

        root["endpoint"] = Endpoint;
        root["server"] = Server;
        root["transport"] = Transport;
        root["failure"] = Failure;
        root["execution"] = Execution;
        root["data"] = Data;
        root["warnings"] = new JsonArray(Warnings.Select(warning => (JsonNode)new JsonObject
        {
            ["code"] = warning.Code,
            ["message"] = warning.Message
        }).ToArray());
        root["guidance"] = new JsonArray(Guidance.Select(text => (JsonNode)text).ToArray());
        return root;
    }

    /// <summary>One line of UTF-8 JSON without a byte order mark, ending in a newline.</summary>
    public byte[] ToUtf8(int? exitCode)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, WriterOptions))
        {
            ToJson(exitCode).WriteTo(writer);
        }

        buffer.WriteByte((byte)'\n');
        return buffer.ToArray();
    }

    public void Warn(string code, string message) => Warnings.Add(new BridgeNotice(code, message));

    /// <summary>A value-free one-line summary for standard error.</summary>
    public string Diagnostic()
    {
        var builder = new StringBuilder("briosa-cli: ").Append(BridgeOutcomes.Name(Outcome));
        if (DiagnosticVocabulary.Printable(Failure?["source"]?.GetValue<string>(),
                Failure?["diagnostic_code"]?.GetValue<string>()) is { } code)
        {
            builder.Append(" (").Append(code).Append(')');
        }

        return builder.ToString();
    }
}

/// <summary>A fixed warning. Its message never contains text from SpatialAnalyzer or the caller.</summary>
internal sealed record BridgeNotice(string Code, string Message);
