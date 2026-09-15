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

internal sealed partial class BriosaTelemetryExport(
    BriosaTelemetryOptions options, BriosaLogHealth health,
    ILogger<BriosaTelemetryExport> logger) : BackgroundService
{
    private readonly ILogger<BriosaTelemetryExport> _logger = logger;
    private TracerProvider? _traces;
    private MeterProvider? _metrics;

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "The provider owns its processor/reader, which owns its exporter.")]
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        if (options.Enabled)
        {
            // Empty resource avoids environment/process/host detectors. Only our
            // manual source and meter are exported; no payload/exception enrichment.
            var resource = ResourceBuilder.CreateEmpty().AddService("Briosa.Server")
                .AddAttributes([new KeyValuePair<string, object>(
                    "briosa.target", SpatialAnalyzerApi.TargetVersion)]);
            _traces = Sdk.CreateTracerProviderBuilder().SetResourceBuilder(resource)
                .AddSource(BriosaTelemetry.InstrumentationName)
                .SetSampler(new TraceIdRatioBasedSampler(options.SampleRatio))
                .AddProcessor(new BatchActivityExportProcessor(
                    new OtlpTraceExporter(ExporterOptions()),
                    maxQueueSize: 2048, scheduledDelayMilliseconds: 5000,
                    exporterTimeoutMilliseconds: 2000, maxExportBatchSize: 256))
                .Build();
            _metrics = Sdk.CreateMeterProviderBuilder().SetResourceBuilder(resource)
                .AddMeter(BriosaTelemetry.InstrumentationName)
                .AddView(instrument => instrument is Histogram<double>
                    ? new ExplicitBucketHistogramConfiguration
                    {
                        CardinalityLimit = 4096,
                        Boundaries = [0.0001, 0.00025, 0.0005, 0.001, 0.0025, 0.005, 0.01,
                            0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10, 30, 60, 120]
                    }
                    : new MetricStreamConfiguration { CardinalityLimit = 4096 })
                .AddReader(new PeriodicExportingMetricReader(
                    new OtlpMetricExporter(ExporterOptions()), 30000, 2000))
                .Build();
        }
        return base.StartAsync(cancellationToken);
    }
    private OtlpExporterOptions ExporterOptions() => new()
    {
        Endpoint = options.Endpoint!,
        Protocol = OtlpExportProtocol.Grpc,
        TimeoutMilliseconds = 2000,
        Headers = null
    };
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        long dropped = 0, failures = 0;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                var currentDropped = health.Dropped;
                var currentFailures = health.Failures;
                if (currentDropped != dropped || currentFailures != failures)
                {
                    dropped = currentDropped;
                    failures = currentFailures;
                    LogSinkDegraded(dropped, failures);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
        _traces?.Shutdown(2000);
        _metrics?.Shutdown(2000);
    }
    public override void Dispose()
    {
        _traces?.Dispose();
        _metrics?.Dispose();
        base.Dispose();
    }
    [LoggerMessage(EventId = 1401, Level = LogLevel.Warning,
        Message = "Logging degraded: dropped records {DroppedRecords}, sink failures {SinkFailures}.")]
    private partial void LogSinkDegraded(long droppedRecords, long sinkFailures);
}
