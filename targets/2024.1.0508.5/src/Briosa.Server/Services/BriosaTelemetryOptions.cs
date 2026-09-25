using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Briosa.Server.Operations;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Briosa.Server.Services;

internal sealed record BriosaTelemetryOptions(bool Enabled, Uri? Endpoint, double SampleRatio)
{
    public static BriosaTelemetryOptions Bind(IConfiguration configuration)
    {
        try
        {
            var section = configuration.GetSection("Briosa:Telemetry");
            var enabled = section.GetValue("Enabled", false);
            var ratio = section.GetValue("SampleRatio", 0.1);
            Uri? endpoint = null;
            if (!double.IsFinite(ratio) || ratio is < 0 or > 1 ||
                (enabled && (!Uri.TryCreate(section["Endpoint"], UriKind.Absolute, out endpoint) ||
                    endpoint.Scheme is not ("http" or "https") || endpoint.UserInfo.Length != 0 ||
                    endpoint.Query.Length != 0 || endpoint.Fragment.Length != 0)))
                throw new InvalidOperationException();
            return new(enabled, endpoint, ratio);
        }
        catch (Exception exception) when (exception is FormatException or InvalidOperationException or ArgumentException)
        {
            throw new InvalidOperationException("Invalid Briosa telemetry configuration.");
        }
    }
}
