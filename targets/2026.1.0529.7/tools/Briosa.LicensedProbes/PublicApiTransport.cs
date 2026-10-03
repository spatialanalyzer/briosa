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
    private readonly GrpcChannel _channel;
    private readonly TimeSpan _stepTimeout;
    private readonly TimeSpan _operatorTimeout;
    private readonly IProcessCensus _census;

    public PublicApiTransport(Uri address, TimeSpan stepTimeout, TimeSpan operatorTimeout, IProcessCensus census)
    {
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(census);
        if (!address.IsLoopback || address.Scheme != Uri.UriSchemeHttp)
        {
            throw new ArgumentException("The probe address must be loopback HTTP.", nameof(address));
        }

        _channel = GrpcChannel.ForAddress(address);
        _stepTimeout = stepTimeout;
        _operatorTimeout = operatorTimeout;
        _census = census;
    }

    public ProbePhase Phase => ProbePhase.PublicApi;

    public async Task<SessionIdentity> StartAsync(ProbePlan plan, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plan);

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
            info = await discovery.GetServerInfoAsync(new Api.GetServerInfoRequest(), deadline: deadline,
                cancellationToken: cancellationToken).ResponseAsync.ConfigureAwait(false);
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
            ServerVersion = info.Version.HasBriosaVersion ? info.Version.BriosaVersion : null,
            ServerSourceRevision = info.Version.HasSourceRevision ? info.Version.SourceRevision : null
        };
    }

    public async Task<ProbeOutcome> ExecuteAsync(ProbeStep step, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(step);
        var timeout = step.RequiresOperator ? _operatorTimeout : _stepTimeout;
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

    public ValueTask DisposeAsync()
    {
        _channel.Dispose();
        return ValueTask.CompletedTask;
    }
}
