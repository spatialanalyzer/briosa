using Grpc.Core;
using Grpc.Net.Client;
using Api = global::Briosa;

namespace Briosa.LicensedProbes;

/// <summary>
/// Public-API path. It follows the SmokeClient licensed-scenario pattern:
/// loopback-only address, exact target identity, MP readiness required, typed
/// errors parsed from the trailer, and no retry. It never connects or stops the
/// SDK; the operator owns server lifecycle with the maintained lifecycle client.
/// </summary>
internal sealed class PublicApiTransport : IProbeTransport
{
    // Above the published clients' 4 MiB limit, so that probe 34 can measure and record a
    // response the published clients could not receive instead of losing it as an untyped
    // transport failure (#402). Every response the server sends today is far smaller.
    public const int MeasurementReceiveLimitBytes = 64 * 1024 * 1024;

    private readonly GrpcChannel _channel;
    private readonly TimeSpan _stepTimeout;
    private readonly TimeSpan _operatorTimeout;
    private readonly IProcessCensus _census;
    private LicenseAbortSignal _licenseAbort = LicenseAbortSignal.Never;
    private bool _abandoned;

    public PublicApiTransport(Uri address, TimeSpan stepTimeout, TimeSpan operatorTimeout, IProcessCensus census)
    {
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(census);
        if (!address.IsLoopback || address.Scheme != Uri.UriSchemeHttp)
        {
            throw new ArgumentException("The probe address must be loopback HTTP.", nameof(address));
        }

        _channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions { MaxReceiveMessageSize = MeasurementReceiveLimitBytes });
        _stepTimeout = stepTimeout;
        _operatorTimeout = operatorTimeout;
        _census = census;
    }

    public ProbePhase Phase => ProbePhase.PublicApi;

    public async Task<SessionIdentity> StartAsync(ProbePlan plan, LicenseAbortSignal licenseAbort, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(licenseAbort);
        _licenseAbort = licenseAbort;

        // Exactly one SA and one Briosa worker: the server's worker is the only SDK client.
        if (_census.Count(ProcessCensus.SpatialAnalyzer) != 1)
            throw new ProbeRefusedException("exactly-one-spatial-analyzer-required");
        if (_census.Count(ProcessCensus.BriosaWorker) != 1 || _census.Count(ProcessCensus.SpatialAnalyzerSdk) > 1)
            throw new ProbeRefusedException("exactly-one-sdk-client-required");

        var deadline = DateTime.UtcNow.Add(_stepTimeout);
        var discovery = new Api.DiscoveryService.DiscoveryServiceClient(_channel);
        Api.GetServerInfoResponse info;
        Api.ListCapabilitiesResponse capabilities;
        try
        {
            // Each read-only discovery call is sent only while no license prompt is signalled.
            RefuseIfAbandoning();
            info = await discovery.GetServerInfoAsync(new Api.GetServerInfoRequest(), deadline: deadline,
                cancellationToken: cancellationToken).ResponseAsync.ConfigureAwait(false);
            RefuseIfAbandoning();
            capabilities = await discovery.ListCapabilitiesAsync(new Api.ListCapabilitiesRequest(), deadline: deadline,
                cancellationToken: cancellationToken).ResponseAsync.ConfigureAwait(false);
        }
        catch (RpcException)
        {
            throw new ProbeRefusedException("server-discovery-unavailable");
        }

        if (info.Version?.SpatialAnalyzerTarget != ProbeTarget.SpatialAnalyzerTarget ||
            capabilities.SpatialAnalyzerTarget != ProbeTarget.SpatialAnalyzerTarget)
            throw new ProbeRefusedException("server-target-identity-mismatch");
        if (!info.ReadyForMp || info.WorkerState != Api.WorkerRuntimeState.Ready ||
            info.SpatialAnalyzerConnectionState != Api.SpatialAnalyzerConnectionState.Connected)
            throw new ProbeRefusedException("server-not-ready-for-mp");

        var admitted = capabilities.Operations.Select(static operation => operation.FullyQualifiedMethod).ToHashSet(StringComparer.Ordinal);
        if (!plan.FullyQualifiedMethods.All(admitted.Contains))
            throw new ProbeRefusedException("probe-operations-not-admitted");

        return new SessionIdentity
        {
            // External values: only a release-version shape and a full commit SHA reach a record.
            ServerVersion = info.Version.HasBriosaVersion ? ExternalValues.Version(info.Version.BriosaVersion) : null,
            ServerSourceRevision = info.Version.HasSourceRevision ? ExternalValues.Revision(info.Version.SourceRevision) : null
        };
    }

    public async Task<ProbeOutcome> ExecuteAsync(ProbeStep step, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(step);
        if (Abandoning())
        {
            return ProbeOutcome.Refused(ProbeSession.LicensePromptStop);
        }

        var timeout = step.ClientTimeout ?? (step.RequiresOperator ? _operatorTimeout : _stepTimeout);
        var options = new CallOptions(deadline: DateTime.UtcNow.Add(timeout), cancellationToken: cancellationToken);
        try
        {
            return await step.Operation.InvokePublicAsync(_channel.CreateCallInvoker(), step.Request, options).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return ProbeOutcome.Unknown("grpc:client-cancelled", "client-cancelled");
        }
    }

    // The public path sends nothing on disposal; after a license prompt it sends nothing at all.
    public void Abandon() => _abandoned = true;

    private bool Abandoning()
    {
        if (!_abandoned && _licenseAbort.Raised)
        {
            _abandoned = true;
        }

        return _abandoned;
    }

    private void RefuseIfAbandoning()
    {
        if (Abandoning())
            throw new ProbeRefusedException(ProbeSession.LicensePromptStop);
    }

    public ValueTask DisposeAsync()
    {
        _channel.Dispose();
        return ValueTask.CompletedTask;
    }
}
