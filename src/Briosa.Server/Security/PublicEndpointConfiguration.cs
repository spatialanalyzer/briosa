using System.Globalization;
using System.Net;

namespace Briosa.Server.Security;

internal sealed record PublicEndpoint(IPAddress Address, int Port);

internal static class PublicEndpointConfiguration
{
    internal const string AddressKey = "Briosa:Endpoint:Address";
    internal const string PortKey = "Briosa:Endpoint:Port";
    internal const string DefaultAddress = "127.0.0.1";
    internal const int DefaultPort = 50051;

    private static readonly string[] AlternativeBindingKeys =
    [
        "urls",
        "http_ports",
        "https_ports"
    ];

    public static PublicEndpoint Resolve(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        RejectAlternativeBindingConfiguration(configuration);

        var configuredAddress = configuration[AddressKey] ?? DefaultAddress;
        if (!IPAddress.TryParse(configuredAddress, out var address))
        {
            throw new InvalidOperationException(
                $"{AddressKey} must be an IP address literal.");
        }

        if (!IPAddress.IsLoopback(address))
        {
            throw new InvalidOperationException(
                $"{AddressKey} must be a loopback address. " +
                "Remote Briosa endpoints are not supported.");
        }

        var configuredPort = configuration[PortKey];
        var port = DefaultPort;
        if (configuredPort is not null &&
            (!int.TryParse(
                configuredPort,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out port) ||
             port is < 1 or > 65535))
        {
            throw new InvalidOperationException(
                $"{PortKey} must be an integer from 1 through 65535.");
        }

        return new PublicEndpoint(address, port);
    }

    private static void RejectAlternativeBindingConfiguration(
        IConfiguration configuration)
    {
        foreach (var key in AlternativeBindingKeys)
        {
            if (configuration[key] is not null)
            {
                throw new InvalidOperationException(
                    $"The endpoint setting '{key}' is not supported. " +
                    $"Use {AddressKey} and {PortKey}.");
            }
        }

        if (configuration.GetSection("Kestrel:Endpoints").GetChildren().Any())
        {
            throw new InvalidOperationException(
                "Kestrel:Endpoints is not supported. " +
                $"Use {AddressKey} and {PortKey}.");
        }
    }
}
