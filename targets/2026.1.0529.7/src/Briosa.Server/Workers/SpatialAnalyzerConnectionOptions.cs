using System.Globalization;

namespace Briosa.Server.Workers;

internal sealed record SpatialAnalyzerConnectionOptions(
    string Host,
    SpatialAnalyzerIdentityOptions Identity)
{
    internal const string HostKey = "Briosa:SpatialAnalyzer:Host";
    internal const string DefaultHost = "localhost";

    public static SpatialAnalyzerConnectionOptions BindAndValidate(
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var configuredHost = configuration[HostKey];
        var host = configuredHost is null ? DefaultHost : configuredHost;
        if (!string.Equals(host, DefaultHost, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Configuration value '{HostKey}' must be 'localhost' for the local-only lifecycle contract.");
        }

        return new SpatialAnalyzerConnectionOptions(
            DefaultHost,
            SpatialAnalyzerIdentityOptions.BindAndValidate(configuration));
    }
}
