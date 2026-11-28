using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using Grpc.Core;
using Grpc.Net.Client;

namespace Briosa.Cli.Engine;

/// <summary>
/// One gRPC channel to one loopback server, over exactly one TCP connection. The
/// session is bound to the server it verified: if that connection is lost, the
/// channel never opens another, so a server that restarted on the same port, which
/// could be older and ignore the caller restriction, never receives a call that was
/// checked against its predecessor. It never retries: no service config is set, so
/// the client has no retry or hedging policy, and a <see cref="SubmissionGuard"/>
/// stops the HTTP stack from resending a request on the same connection. It never
/// calls a lifecycle RPC; the only RPCs it sends are the two discovery reads and one
/// MP call.
/// </summary>
internal sealed class BriosaConnection : IDisposable
{
    private readonly GrpcChannel _channel;
    private readonly DiscoveryService.DiscoveryServiceClient _discovery;
    private readonly SubmissionGuard _submissions = new();
    private int _connectAttempts;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "The channel owns the handler chain and disposes it with itself (DisposeHttpClient).")]
    public BriosaConnection(LoopbackEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        Endpoint = endpoint;
        var handler = new SocketsHttpHandler
        {
            // Loopback only: never route through a system proxy and never follow a
            // redirect somewhere else.
            UseProxy = false,
            AllowAutoRedirect = false,
            ConnectTimeout = BridgeContract.ConnectTimeout,
            EnableMultipleHttp2Connections = false,
            // Connect to the parsed loopback address itself, never to a resolved name,
            // and at most once per session.
            ConnectCallback = ConnectOnceAsync
        };
        _channel = GrpcChannel.ForAddress(endpoint.Uri, new GrpcChannelOptions
        {
            HttpHandler = new SubmissionHandler(_submissions, handler),
            DisposeHttpClient = true,
            MaxReceiveMessageSize = BridgeContract.ClientReceiveLimitBytes,
            MaxSendMessageSize = BridgeContract.ServerReceiveLimitBytes,
            ThrowOperationCanceledOnCancellation = false
        });
        _discovery = new DiscoveryService.DiscoveryServiceClient(_channel);
        Invoker = _channel.CreateCallInvoker();
    }

    public LoopbackEndpoint Endpoint { get; }

    public CallInvoker Invoker { get; }

    /// <summary>The verified connection was lost and the channel refused to open another.</summary>
    public bool ReconnectRefused => Volatile.Read(ref _connectAttempts) > 1;

    /// <summary>The HTTP stack tried to submit a request a second time and was stopped.</summary>
    public bool ResendBlocked => _submissions.ResendBlocked;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "The returned stream owns the socket; on failure the socket is disposed here.")]
    private async ValueTask<Stream> ConnectOnceAsync(SocketsHttpConnectionContext context,
        CancellationToken cancellationToken)
    {
        if (Interlocked.Increment(ref _connectAttempts) > 1)
        {
            throw new IOException("The verified connection is gone, and this session does not reconnect.");
        }

        var socket = new Socket(Endpoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(new IPEndPoint(Endpoint.Address, Endpoint.Port), cancellationToken)
                .ConfigureAwait(false);
            return new SubmissionGuardStream(new NetworkStream(socket, ownsSocket: true), _submissions);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    public async Task<GetServerInfoResponse> GetServerInfoAsync(CancellationToken cancellationToken)
    {
        using var call = _discovery.GetServerInfoAsync(new GetServerInfoRequest(),
            deadline: DateTime.UtcNow.Add(BridgeContract.DiscoveryDeadline),
            cancellationToken: cancellationToken);
        return await call.ResponseAsync.ConfigureAwait(false);
    }

    public async Task<ListCapabilitiesResponse> ListCapabilitiesAsync(bool includeDenied,
        CancellationToken cancellationToken)
    {
        using var call = _discovery.ListCapabilitiesAsync(new ListCapabilitiesRequest { IncludeDenied = includeDenied },
            deadline: DateTime.UtcNow.Add(BridgeContract.DiscoveryDeadline),
            cancellationToken: cancellationToken);
        return await call.ResponseAsync.ConfigureAwait(false);
    }

    public void Dispose() => _channel.Dispose();
}
