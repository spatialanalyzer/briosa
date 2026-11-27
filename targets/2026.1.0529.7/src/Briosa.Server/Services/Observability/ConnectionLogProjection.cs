using System.Collections.Frozen;
using System.Globalization;

namespace Briosa.Server.Services;

/// <summary>
/// Keeps a small, reviewed part of what ASP.NET Core's own HTTP stack logs about the
/// HTTP/2 connection, so a stalled call can be followed below Briosa's call records
/// (#450): each frame Kestrel received or sent (type, stream, length and flags), the
/// connection, socket and stream lifecycle, the start and end of each request body
/// read, and the hosting layer's request start and finish.
/// </summary>
/// <remarks>
/// <para>
/// Every one of these records is Trace, Debug or Information in its category, and the
/// server's default filter holds every <c>Microsoft</c> category at Warning, so by
/// default ASP.NET Core never creates them and none reaches this code. A record at
/// Warning or above is never projected, so a default installation logs exactly what it
/// logged before. They are turned on only by naming their categories at startup; the
/// conformance soak does so (see docs/operations/server-observability.md).
/// </para>
/// <para>
/// Each record is matched by its category, event ID and event name, all from ASP.NET
/// Core 10's own definitions, and written under a fixed name. Only the fields below are
/// kept, each validated or mapped to a fixed value: a connection ID, a stream ID, a
/// frame type, length and flags, an HTTP/2 error code, a count, the protocol and method,
/// the routed RPC name or operation ID, the HTTP status code and the elapsed time. No
/// frame payload, header value, host, path text, query, content type, reason text,
/// exception or message is kept. A request path is reduced to the name of one of the
/// server's own methods, or <c>unrecognized</c>.
/// </para>
/// </remarks>
internal static class ConnectionLogProjection
{
    internal const string Http2Category = "Microsoft.AspNetCore.Server.Kestrel.Http2";
    internal const string ConnectionsCategory = "Microsoft.AspNetCore.Server.Kestrel.Connections";
    internal const string BadRequestsCategory = "Microsoft.AspNetCore.Server.Kestrel.BadRequests";
    internal const string GeneralCategory = "Microsoft.AspNetCore.Server.Kestrel";
    internal const string SocketsCategory = "Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets";
    internal const string HostingCategory = "Microsoft.AspNetCore.Hosting.Diagnostics";

    /// <summary>The recorded value of a field that is present but does not validate.</summary>
    internal const string Unrecognized = "unrecognized";

    // Category, event ID and ASP.NET Core event name (null where ASP.NET Core gives
    // none), and the fixed name Briosa writes. Every entry is below Warning.
    private static readonly FrozenDictionary<(string Category, int Id), (string? Name, string Label)> Events =
        new Dictionary<(string Category, int Id), (string? Name, string Label)>
        {
            [(Http2Category, 29)] = ("Http2ConnectionError", "Http2ConnectionError"),
            [(Http2Category, 30)] = ("Http2StreamError", "Http2StreamError"),
            [(Http2Category, 31)] = ("HPackDecodingError", "Http2HPackDecodingError"),
            [(Http2Category, 35)] = ("Http2StreamResetAbort", "Http2StreamResetAbort"),
            [(Http2Category, 36)] = ("Http2ConnectionClosing", "Http2ConnectionClosing"),
            [(Http2Category, 37)] = ("Http2FrameReceived", "Http2FrameReceived"),
            [(Http2Category, 38)] = ("HPackEncodingError", "Http2HPackEncodingError"),
            [(Http2Category, 40)] = ("Http2MaxConcurrentStreamsReached", "Http2MaxConcurrentStreamsReached"),
            [(Http2Category, 48)] = ("Http2ConnectionClosed", "Http2ConnectionClosed"),
            [(Http2Category, 49)] = ("Http2FrameSending", "Http2FrameSending"),
            [(Http2Category, 62)] = ("Http2ConnectionQueueProcessingCompleted", "Http2ConnectionQueueProcessingCompleted"),
            [(Http2Category, 64)] = ("Http2TooManyEnhanceYourCalms", "Http2TooManyEnhanceYourCalms"),
            [(Http2Category, 65)] = ("Http2FlowControlQueueOperationsExceeded", "Http2FlowControlQueueOperationsExceeded"),
            [(ConnectionsCategory, 1)] = ("ConnectionStart", "ConnectionStart"),
            [(ConnectionsCategory, 2)] = ("ConnectionStop", "ConnectionStop"),
            [(ConnectionsCategory, 4)] = ("ConnectionPause", "ConnectionPause"),
            [(ConnectionsCategory, 5)] = ("ConnectionResume", "ConnectionResume"),
            [(ConnectionsCategory, 9)] = ("ConnectionKeepAlive", "ConnectionKeepAlive"),
            [(ConnectionsCategory, 10)] = ("ConnectionDisconnect", "ConnectionDisconnect"),
            [(ConnectionsCategory, 34)] = ("ApplicationAbortedConnection", "ConnectionAbortedByApplication"),
            [(ConnectionsCategory, 39)] = ("ConnectionAccepted", "ConnectionAccepted"),
            [(BadRequestsCategory, 17)] = ("ConnectionBadRequest", "ConnectionBadRequest"),
            [(BadRequestsCategory, 20)] = ("RequestProcessingError", "RequestProcessingError"),
            [(BadRequestsCategory, 27)] = ("RequestBodyMinimumDataRateNotSatisfied", "RequestBodyMinimumDataRateNotSatisfied"),
            [(BadRequestsCategory, 28)] = ("ResponseMinimumDataRateNotSatisfied", "ResponseMinimumDataRateNotSatisfied"),
            [(GeneralCategory, 25)] = ("RequestBodyStart", "RequestBodyStart"),
            [(GeneralCategory, 26)] = ("RequestBodyDone", "RequestBodyDone"),
            [(GeneralCategory, 32)] = ("RequestBodyNotEntirelyRead", "RequestBodyNotEntirelyRead"),
            [(GeneralCategory, 33)] = ("RequestBodyDrainTimedOut", "RequestBodyDrainTimedOut"),
            [(GeneralCategory, 66)] = ("RequestAborted", "RequestAborted"),
            [(SocketsCategory, 4)] = ("ConnectionPause", "SocketConnectionPause"),
            [(SocketsCategory, 5)] = ("ConnectionResume", "SocketConnectionResume"),
            [(SocketsCategory, 6)] = ("ConnectionReadFin", "SocketConnectionReadFin"),
            [(SocketsCategory, 7)] = ("ConnectionWriteFin", "SocketConnectionWriteFin"),
            [(SocketsCategory, 8)] = ("ConnectionWriteRst", "SocketConnectionWriteRst"),
            [(SocketsCategory, 14)] = ("ConnectionError", "SocketConnectionError"),
            [(SocketsCategory, 19)] = ("ConnectionReset", "SocketConnectionReset"),
            [(HostingCategory, 1)] = (null, "HostingRequestStarting"),
            [(HostingCategory, 2)] = (null, "HostingRequestFinished")
        }.ToFrozenDictionary();

    /// <summary>Every name a projected record can carry.</summary>
    internal static IReadOnlyCollection<string> Labels { get; } =
        Events.Values.Select(entry => entry.Label).ToFrozenSet(StringComparer.Ordinal);

    /// <summary>Every field a projected record can carry, besides the common ones.</summary>
    internal static IReadOnlyCollection<string> Fields { get; } = new[]
    {
        "ConnectionId", "StreamId", "FrameType", "FrameLength", "FrameFlags", "HighestOpenedStreamId",
        "Http2ErrorCode", "Count", "HttpProtocol", "HttpMethod", "Rpc", "OperationId", "HttpStatusCode",
        "RequestMilliseconds"
    }.ToFrozenSet(StringComparer.Ordinal);

    // RFC 9113 section 6 frame types, by their wire value.
    private static readonly string[] FrameTypes =
        ["DATA", "HEADERS", "PRIORITY", "RST_STREAM", "SETTINGS", "PUSH_PROMISE", "PING", "GOAWAY", "WINDOW_UPDATE",
            "CONTINUATION"];

    // RFC 9113 section 7 error codes, by their wire value.
    private static readonly string[] ErrorCodes =
        ["NO_ERROR", "PROTOCOL_ERROR", "INTERNAL_ERROR", "FLOW_CONTROL_ERROR", "SETTINGS_TIMEOUT", "STREAM_CLOSED",
            "FRAME_SIZE_ERROR", "REFUSED_STREAM", "CANCEL", "COMPRESSION_ERROR", "CONNECT_ERROR", "ENHANCE_YOUR_CALM",
            "INADEQUATE_SECURITY", "HTTP_1_1_REQUIRED"];

    private static readonly FrozenSet<string> Protocols =
        new[] { "HTTP/1.0", "HTTP/1.1", "HTTP/2", "HTTP/3" }.ToFrozenSet(StringComparer.Ordinal);

    private static readonly FrozenSet<string> Methods =
        new[] { "GET", "HEAD", "POST", "PUT", "DELETE", "CONNECT", "OPTIONS", "TRACE", "PATCH" }
            .ToFrozenSet(StringComparer.Ordinal);

    /// <summary>
    /// The fixed name and validated fields of one of ASP.NET Core's connection records,
    /// or false for any other record, any record at Warning or above, or a state that is
    /// not a list of named values.
    /// </summary>
    internal static bool TryProject<TState>(string category, LogLevel level, EventId eventId, TState state,
        out string label, out List<KeyValuePair<string, object?>> fields)
    {
        label = string.Empty;
        fields = [];
        if (level >= LogLevel.Warning || level == LogLevel.None ||
            !Events.TryGetValue((category, eventId.Id), out var entry) ||
            !string.Equals(entry.Name, eventId.Name, StringComparison.Ordinal) ||
            state is not IEnumerable<KeyValuePair<string, object?>> values)
        {
            return false;
        }

        label = entry.Label;
        string? path = null;
        string? pathBase = null;
        foreach (var (key, value) in values)
        {
            switch (key)
            {
                case "ConnectionId":
                    fields.Add(new("ConnectionId", ConnectionId(value)));
                    break;
                case "TraceIdentifier":
                    // ASP.NET Core's HTTP/2 request identifier is the connection ID and
                    // the stream ID in hexadecimal, separated by a colon.
                    if (value is string trace && trace.Length == 22 && trace[13] == ':' &&
                        IsConnectionId(trace.AsSpan(0, 13)) &&
                        int.TryParse(trace.AsSpan(14), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture,
                            out var stream) && stream >= 0)
                    {
                        if (!fields.Exists(field => field.Key == "ConnectionId"))
                            fields.Add(new("ConnectionId", trace[..13]));
                        fields.Add(new("StreamId", stream));
                    }
                    else
                    {
                        fields.Add(new("StreamId", Unrecognized));
                    }

                    break;
                case "id" or "StreamId":
                    fields.Add(new("StreamId", NonNegative(value)));
                    break;
                case "type":
                    fields.Add(new("FrameType", Code(value) is { } type && type < FrameTypes.Length
                        ? FrameTypes[type] : Unrecognized));
                    break;
                case "length":
                    fields.Add(new("FrameLength", NonNegative(value)));
                    break;
                case "flags":
                    fields.Add(new("FrameFlags", Flags(value)));
                    break;
                case "HighestOpenedStreamId":
                    fields.Add(new("HighestOpenedStreamId", NonNegative(value)));
                    break;
                case "error":
                    fields.Add(new("Http2ErrorCode", Code(value) is { } error && error < ErrorCodes.Length
                        ? ErrorCodes[error] : Unrecognized));
                    break;
                case "Count":
                    fields.Add(new("Count", NonNegative(value)));
                    break;
                case "Protocol" when entry.Name is null:
                    fields.Add(new("HttpProtocol", value is string protocol && Protocols.Contains(protocol)
                        ? protocol : Unrecognized));
                    break;
                case "Method" when entry.Name is null:
                    fields.Add(new("HttpMethod", value is string method && Methods.Contains(method)
                        ? method : Unrecognized));
                    break;
                case "Path" when entry.Name is null:
                    path = value as string ?? string.Empty;
                    break;
                case "PathBase" when entry.Name is null:
                    pathBase = value as string ?? string.Empty;
                    break;
                case "StatusCode" when entry.Name is null:
                    fields.Add(new("HttpStatusCode", value is int status && status is >= 100 and <= 599
                        ? status : Unrecognized));
                    break;
                case "ElapsedMilliseconds" when entry.Name is null:
                    fields.Add(new("RequestMilliseconds", value is double elapsed && double.IsFinite(elapsed) &&
                        elapsed >= 0 ? Math.Round(elapsed, 3) : Unrecognized));
                    break;
            }
        }

        if (path is not null)
        {
            // The same names the unary call records use, from an exact match with one
            // of the server's own methods; any other path is never written.
            var (rpc, operationId) = UnaryCallAuditInterceptor.Identify(
                string.IsNullOrEmpty(pathBase) ? path : null);
            if (operationId is not null)
                fields.Add(new("OperationId", operationId));
            else
                fields.Add(new("Rpc", rpc));
        }

        return true;
    }

    private static string ConnectionId(object? value) =>
        value is string text && IsConnectionId(text) ? text : Unrecognized;

    // Kestrel's connection IDs are 13 characters of base 32: 0-9 and A-V.
    private static bool IsConnectionId(ReadOnlySpan<char> text)
    {
        if (text.Length != 13)
            return false;
        foreach (var character in text)
        {
            if (character is not (>= '0' and <= '9' or >= 'A' and <= 'V'))
                return false;
        }

        return true;
    }

    private static object NonNegative(object? value) => value switch
    {
        int number when number >= 0 => number,
        long number when number is >= 0 and <= int.MaxValue => (int)number,
        _ => Unrecognized
    };

    private static int? Code(object? value)
    {
        if (value is null || (value is not Enum && value is not byte && value is not int))
            return null;
        try
        {
            var number = Convert.ToInt64(value, CultureInfo.InvariantCulture);
            return number is >= 0 and <= int.MaxValue ? (int)number : null;
        }
        catch (Exception exception) when (exception is InvalidCastException or OverflowException)
        {
            return null;
        }
    }

    // Kestrel names known flags with an enum and writes any other as "0x" and hex.
    private static object Flags(object? value)
    {
        if (Code(value) is { } flags && flags <= byte.MaxValue)
            return flags;
        return value is string text && text.Length is > 2 and <= 4 && text.StartsWith("0x", StringComparison.Ordinal) &&
            byte.TryParse(text.AsSpan(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var parsed)
                ? (int)parsed
                : Unrecognized;
    }
}
