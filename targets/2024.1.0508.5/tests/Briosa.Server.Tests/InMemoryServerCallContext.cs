using Grpc.Core;

namespace Briosa.Server.Tests;

// Lets portable outcome tests invoke the actual gRPC override without adding a
// test-only execution entry point to a production service.
internal sealed class InMemoryServerCallContext(
    string method, DateTime? deadline, CancellationToken cancellationToken) : ServerCallContext
{
    protected override string MethodCore => method;
    protected override string HostCore => "localhost";
    protected override string PeerCore => "ipv4:127.0.0.1:12345";
    protected override DateTime DeadlineCore => deadline ?? DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
    protected override Metadata RequestHeadersCore { get; } = [];
    protected override CancellationToken CancellationTokenCore => cancellationToken;
    protected override Metadata ResponseTrailersCore { get; } = [];
    protected override Status StatusCore { get; set; }
    protected override WriteOptions? WriteOptionsCore { get; set; }
    protected override AuthContext AuthContextCore { get; } = new("", new Dictionary<string, List<AuthProperty>>());
    protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
    protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) =>
        throw new NotSupportedException();
}
