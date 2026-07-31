using System.Net;
using Briosa.Server.Security;
using Microsoft.Extensions.Configuration;

namespace Briosa.Server.Tests;

public sealed class PublicEndpointConfigurationTests
{
    [Fact]
    public void EmptyConfigurationUsesIpv4LoopbackDefault()
    {
        var endpoint = PublicEndpointConfiguration.Resolve(CreateConfiguration());

        Assert.Equal(IPAddress.Loopback, endpoint.Address);
        Assert.Equal(50051, endpoint.Port);
    }

    [Fact]
    public void ExplicitIpv6LoopbackAndPortAreAccepted()
    {
        var endpoint = PublicEndpointConfiguration.Resolve(CreateConfiguration(
            (PublicEndpointConfiguration.AddressKey, "::1"),
            (PublicEndpointConfiguration.PortKey, "43117")));

        Assert.Equal(IPAddress.IPv6Loopback, endpoint.Address);
        Assert.Equal(43117, endpoint.Port);
    }

    [Theory]
    [InlineData("0.0.0.0")]
    [InlineData("::")]
    [InlineData("192.0.2.1")]
    [InlineData("localhost")]
    [InlineData("")]
    public void NonLoopbackOrNonLiteralAddressIsRejected(string address)
    {
        var configuration = CreateConfiguration(
            (PublicEndpointConfiguration.AddressKey, address));

        var exception = Assert.Throws<InvalidOperationException>(
            () => PublicEndpointConfiguration.Resolve(configuration));

        Assert.Contains(
            PublicEndpointConfiguration.AddressKey,
            exception.Message,
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("65536")]
    [InlineData("-1")]
    [InlineData("not-a-port")]
    [InlineData("")]
    public void InvalidPortIsRejected(string port)
    {
        var configuration = CreateConfiguration(
            (PublicEndpointConfiguration.PortKey, port));

        var exception = Assert.Throws<InvalidOperationException>(
            () => PublicEndpointConfiguration.Resolve(configuration));

        Assert.Contains(
            PublicEndpointConfiguration.PortKey,
            exception.Message,
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("urls", "http://0.0.0.0:50051")]
    [InlineData("http_ports", "50051")]
    [InlineData("https_ports", "50051")]
    [InlineData("Kestrel:Endpoints:Remote:Url", "http://0.0.0.0:50051")]
    public void AlternativeEndpointConfigurationIsRejected(
        string key,
        string value)
    {
        var configuration = CreateConfiguration((key, value));

        Assert.Throws<InvalidOperationException>(
            () => PublicEndpointConfiguration.Resolve(configuration));
    }

    private static IConfiguration CreateConfiguration(
        params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(
                static value => value.Key,
                static value => value.Value,
                StringComparer.OrdinalIgnoreCase))
            .Build();
}
