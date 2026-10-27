using System.Globalization;

namespace Briosa.Server.Workers;

internal sealed record SpatialAnalyzerConnectionOptions(
    string Host,
    SpatialAnalyzerIdentityOptions Identity)
{
    internal const string HostKey = "Briosa:SpatialAnalyzer:Host";
    internal const string DefaultHost = "localhost";
    internal const string InteractionModeKey = "Briosa:SpatialAnalyzer:InteractionMode";

    public SpatialAnalyzerInteractionMode InteractionMode { get; init; } =
        SpatialAnalyzerInteractionMode.Unchanged;

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
            SpatialAnalyzerIdentityOptions.BindAndValidate(configuration))
        {
            InteractionMode = ReadInteractionMode(configuration)
        };
    }

    // Only the exact names are accepted, and a rejected value is not echoed.
    private static SpatialAnalyzerInteractionMode ReadInteractionMode(
        IConfiguration configuration) =>
        configuration[InteractionModeKey] switch
        {
            null or "Unchanged" => SpatialAnalyzerInteractionMode.Unchanged,
            "Silent" => SpatialAnalyzerInteractionMode.Silent,
            "Automatic" => SpatialAnalyzerInteractionMode.Automatic,
            "Manual" => SpatialAnalyzerInteractionMode.Manual,
            _ => throw new InvalidOperationException(
                $"Configuration value '{InteractionModeKey}' must be Unchanged, Silent, Automatic, or Manual.")
        };
}
