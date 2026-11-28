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
/// the client has no retry or hedging policy. It never calls a lifecycle RPC; the
/// only RPCs it sends are the two discovery reads and one MP call.
/// </summary>
internal sealed class BriosaConnection : IDisposable
{
    private readonly GrpcChannel _channel;
    private readonly DiscoveryService.DiscoveryServiceClient _discovery;
    private int _connectAttempts;

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
            HttpHandler = handler,
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

    private async ValueTask<Stream> ConnectOnceAsync(SocketsHttpConnectionContext context,
        CancellationToken cancellationToken)
    {
        if (Interlocked.Increment(ref _connectAttempts) > 1)
        {
            throw new IOException("The verified connection is gone, and this session does not reconnect.");
        }

        Socket? socket = new(Endpoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            socket.NoDelay = true;
            await socket.ConnectAsync(new IPEndPoint(Endpoint.Address, Endpoint.Port), cancellationToken)
                .ConfigureAwait(false);
            var stream = new NetworkStream(socket, ownsSocket: true);
            socket = null;
            return stream;
        }
        finally
        {
            socket?.Dispose();
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
