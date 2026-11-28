using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using Briosa.Cli.Engine;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging;

namespace Briosa.Cli.Tests;

/// <summary>
/// An in-process loopback HTTP/2 server that speaks gRPC framing by hand, so a test
/// controls every byte the CLI sees: the discovery answers, the MP reply, and its
/// trailers, including missing, duplicated, and undecodable ones. It records every
/// RPC it receives with its caller restriction header values.
/// </summary>
internal sealed class FakeBriosaServer : IAsyncDisposable
{
    private readonly WebApplication _app;

    private FakeBriosaServer(WebApplication app, string endpoint)
    {
        _app = app;
        Endpoint = endpoint;
    }

    public string Endpoint { get; }

    public GetServerInfoResponse ServerInfo { get; set; } = DefaultServerInfo();

    public ListCapabilitiesResponse AllCapabilities { get; set; } = ServerProjection.AllCapabilities;

    /// <summary>The reply to the next MP call. Discovery is answered from the two properties above.</summary>
    public Func<ReceivedCall, FakeReply> Reply { get; set; } = _ => FakeReply.Status(StatusCode.Unimplemented);

    public ConcurrentQueue<ReceivedCall> Calls { get; } = new();

    public IReadOnlyList<ReceivedCall> MpCalls => Calls.Where(call => !call.Path.StartsWith(
        "/briosa.DiscoveryService/", StringComparison.Ordinal)).ToArray();

    /// <summary>Completes when an MP call has been received, before it is answered.</summary>
    public TaskCompletionSource MpCallReceived { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public static GetServerInfoResponse DefaultServerInfo(uint revision = 2) => new()
    {
        Version = new VersionCoordinates
        {
            BriosaVersion = "0.10.2",
            ProtocolPackage = "briosa",
            SpatialAnalyzerTarget = BridgeContract.SpatialAnalyzerTarget,
            InteropFingerprint = "sha256:fake",
            SourceRevision = new string('a', 40)
        },
        Compatibility = new CompatibilityContract { Major = 3, Revision = revision },
        WorkerState = WorkerRuntimeState.Ready,
        SpatialAnalyzerConnectionState = SpatialAnalyzerConnectionState.Connected,
        SpatialAnalyzerExecutionReadinessState = SpatialAnalyzerExecutionReadinessState.ExecutionReady,
        ReadyForMp = true,
        TargetIsolationMode = TargetIsolationMode.SingleTenant,
        ActivatedSdkIdentity = new RuntimeIdentityEvidence
        {
            Version = BridgeContract.SpatialAnalyzerTarget,
            Source = RuntimeIdentityEvidenceSource.RuntimeVerification,
            MatchState = RuntimeIdentityMatchState.ExactMatch
        },
        ConnectedSpatialAnalyzerIdentity = new RuntimeIdentityEvidence
        {
            Version = BridgeContract.SpatialAnalyzerTarget,
            Source = RuntimeIdentityEvidenceSource.OperatorAttestation,
            MatchState = RuntimeIdentityMatchState.ExactMatch
        },
        ConfiguredSpatialAnalyzerInteractionMode = ConfiguredSpatialAnalyzerInteractionMode.Silent
    };

    public static async Task<FakeBriosaServer> StartAsync(int port = 0)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, port,
            listener => listener.Protocols = HttpProtocols.Http2));
        var app = builder.Build();
        FakeBriosaServer? server = null;
        app.Run(context => server!.HandleAsync(context));
        await app.StartAsync().ConfigureAwait(true);
        var address = new Uri(app.Urls.Single());
        server = new FakeBriosaServer(app, $"http://127.0.0.1:{address.Port}");
        return server;
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync().ConfigureAwait(true);
        await _app.DisposeAsync().ConfigureAwait(true);
    }

    private async Task HandleAsync(HttpContext context)
    {
        using var body = new MemoryStream();
        await context.Request.Body.CopyToAsync(body, context.RequestAborted).ConfigureAwait(false);
        var frame = body.ToArray();
        var payload = frame.Length >= 5 ? frame[5..] : [];
        var path = context.Request.Path.Value ?? string.Empty;
        var call = new ReceivedCall(
            path,
            context.Request.Headers[BridgeContract.CallerRestrictionHeader].Select(value => value ?? string.Empty).ToArray(),
            context.Request.Headers.ContainsKey(BridgeContract.CallerRestrictionHeader + "-bin"),
            payload);
        Calls.Enqueue(call);
        FakeReply reply;
        switch (path)
        {
            case "/briosa.DiscoveryService/GetServerInfo":
                reply = FakeReply.Ok(ServerInfo);
                break;
            case "/briosa.DiscoveryService/ListCapabilities":
                var request = ListCapabilitiesRequest.Parser.ParseFrom(payload);
                var response = AllCapabilities.Clone();
                if (!request.IncludeDenied)
                {
                    var admitted = response.Operations.Where(operation =>
                        operation.Admission == OperationAdmission.Admitted).ToArray();
                    response.Operations.Clear();
                    response.Operations.AddRange(admitted);
                }

                reply = FakeReply.Ok(response);
                break;
            default:
                MpCallReceived.TrySetResult();
                reply = Reply(call);
                break;
        }

        await reply.WriteAsync(context).ConfigureAwait(false);
    }
}

/// <summary>One RPC as the fake received it.</summary>
internal sealed record ReceivedCall(string Path, string[] RestrictionValues, bool BinaryRestrictionPresent, byte[] Payload);

/// <summary>How the fake answers one RPC.</summary>
internal sealed class FakeReply
{
    private readonly Func<HttpContext, Task> _write;

    private FakeReply(Func<HttpContext, Task> write) => _write = write;

    public Task WriteAsync(HttpContext context) => _write(context);

    public static FakeReply Ok(IMessage message) => new(async context =>
    {
        context.Response.ContentType = "application/grpc";
        await context.Response.StartAsync().ConfigureAwait(false);
        var bytes = message.ToByteArray();
        var header = new byte[5];
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(1), (uint)bytes.Length);
        await context.Response.Body.WriteAsync(header).ConfigureAwait(false);
        await context.Response.Body.WriteAsync(bytes).ConfigureAwait(false);
        context.Response.AppendTrailer("grpc-status", "0");
    });

    public static FakeReply Error(StatusCode status, OperationError error) =>
        WithTrailers(status, (BridgeContract.OperationErrorTrailer, Convert.ToBase64String(error.ToByteArray())));

    public static FakeReply Status(StatusCode status) => WithTrailers(status);

    /// <summary>Raw trailer entries, so a test can send a malformed or duplicated error trailer.</summary>
    public static FakeReply WithTrailers(StatusCode status, params (string Name, string Value)[] trailers) =>
        new(context =>
        {
            context.Response.ContentType = "application/grpc";
            context.Response.AppendTrailer("grpc-status", ((int)status).ToString(CultureInfo.InvariantCulture));
            context.Response.AppendTrailer("grpc-message", "The request is invalid.");
            foreach (var (name, value) in trailers)
            {
                context.Response.AppendTrailer(name, value);
            }

            return Task.CompletedTask;
        });

    /// <summary>Exactly the status, text, and trailers a server-side exception carries.</summary>
    public static FakeReply FromRpcException(RpcException exception) => new(context =>
    {
        context.Response.ContentType = "application/grpc";
        context.Response.AppendTrailer("grpc-status", ((int)exception.StatusCode).ToString(CultureInfo.InvariantCulture));
        context.Response.AppendTrailer("grpc-message", exception.Status.Detail);
        foreach (var entry in exception.Trailers)
        {
            context.Response.AppendTrailer(entry.Key, entry.IsBinary ? Convert.ToBase64String(entry.ValueBytes) : entry.Value);
        }

        return Task.CompletedTask;
    });

    /// <summary>An OK response that also carries extra trailers, such as an error trailer.</summary>
    public static FakeReply OkWithTrailers(IMessage message, params (string Name, string Value)[] trailers) => new(async context =>
    {
        context.Response.ContentType = "application/grpc";
        await context.Response.StartAsync().ConfigureAwait(false);
        var bytes = message.ToByteArray();
        var header = new byte[5];
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(1), (uint)bytes.Length);
        await context.Response.Body.WriteAsync(header).ConfigureAwait(false);
        await context.Response.Body.WriteAsync(bytes).ConfigureAwait(false);
        context.Response.AppendTrailer("grpc-status", "0");
        foreach (var (name, value) in trailers)
        {
            context.Response.AppendTrailer(name, value);
        }
    });

    /// <summary>
    /// Refuses the HTTP/2 stream with RST_STREAM REFUSED_STREAM before reading the
    /// request, as a server does that has not processed it.
    /// </summary>
    public static FakeReply RefuseStream() => new(context =>
    {
        context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResetFeature>()!.Reset(0x7);
        return Task.CompletedTask;
    });

    /// <summary>Holds the call open until the client gives up.</summary>
    public static FakeReply Hang() => new(async context =>
    {
        try
        {
            await Task.Delay(Timeout.Infinite, context.RequestAborted).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
    });

    /// <summary>Drops the stream after the request arrived, with no status and no trailers.</summary>
    public static FakeReply Drop() => new(context =>
    {
        context.Abort();
        return Task.CompletedTask;
    });
}
