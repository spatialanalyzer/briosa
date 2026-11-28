using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace Briosa.Cli.Engine;

/// <summary>
/// A cleartext HTTP/2 endpoint on an IP loopback address, the only posture the
/// server supports. Hostnames, including <c>localhost</c>, are rejected as the
/// server rejects them, so name resolution can never send a call elsewhere.
/// </summary>
internal sealed partial class LoopbackEndpoint
{
    public const string DefaultText = "http://127.0.0.1:50051";
    private const int MaximumLength = 64;

    private LoopbackEndpoint(IPAddress address, int port)
    {
        Address = address;
        Port = port;
        // The text names the port explicitly, even 80, so it parses again as itself.
        Text = string.Create(CultureInfo.InvariantCulture,
            $"http://{(address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 ? "[::1]" : address.ToString())}:{port}");
        Uri = new Uri(Text);
    }

    public static LoopbackEndpoint Default { get; } = new(IPAddress.Loopback, 50051);

    public IPAddress Address { get; }

    public int Port { get; }

    public Uri Uri { get; }

    /// <summary>The normalized text form, for example <c>http://127.0.0.1:50051</c>.</summary>
    public string Text { get; }

    /// <summary>
    /// Accepts exactly <c>http://</c>, an IPv4 address in 127.0.0.0/8 or <c>[::1]</c>,
    /// an explicit port from 1 to 65535, and an optional trailing slash. Never
    /// returns the rejected text.
    /// </summary>
    public static bool TryParse(string? text, out LoopbackEndpoint? endpoint)
    {
        endpoint = null;
        if (text is null || text.Length > MaximumLength)
        {
            return false;
        }

        var match = EndpointPattern().Match(text);
        if (!match.Success ||
            !int.TryParse(match.Groups["port"].ValueSpan, NumberStyles.None, CultureInfo.InvariantCulture, out var port) ||
            port is < 1 or > 65535)
        {
            return false;
        }

        var host = match.Groups["host"].Value;
        if (host == "[::1]")
        {
            endpoint = new LoopbackEndpoint(IPAddress.IPv6Loopback, port);
            return true;
        }

        // Each octet is checked by the parser; the pattern already fixed four
        // decimal groups, so no shorthand form such as 127.1 reaches it.
        if (!IPAddress.TryParse(host, out var address) ||
            address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork ||
            !string.Equals(address.ToString(), host, StringComparison.Ordinal) ||
            !IPAddress.IsLoopback(address))
        {
            return false;
        }

        endpoint = new LoopbackEndpoint(address, port);
        return true;
    }

    [GeneratedRegex(@"\Ahttp://(?<host>\[::1\]|[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}):(?<port>[0-9]{1,5})/?\z",
        RegexOptions.CultureInvariant)]
    private static partial Regex EndpointPattern();
}
