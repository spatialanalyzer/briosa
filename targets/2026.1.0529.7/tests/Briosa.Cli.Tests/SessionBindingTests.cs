using Briosa.Cli.Engine;
using Grpc.Core;

namespace Briosa.Cli.Tests;

/// <summary>
/// A session is bound to the one connection it verified. A server that replaces
/// the verified one on the same port never receives an RPC from that session.
/// </summary>
public sealed class SessionBindingTests
{
    [Fact]
    public async Task AServerThatReplacedTheVerifiedOneOnTheSamePortReceivesNothing()
    {
        var first = await FakeBriosaServer.StartAsync();
        var endpoint = first.Endpoint;
        Assert.True(LoopbackEndpoint.TryParse(endpoint, out var parsed));
        using var connection = new BriosaConnection(parsed!);
        _ = await connection.GetServerInfoAsync(CancellationToken.None);
        await first.DisposeAsync();

        await using var replacement = await FakeBriosaServer.StartAsync(parsed!.Port);
        replacement.ServerInfo = FakeBriosaServer.DefaultServerInfo(revision: 0);
        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            connection.ListCapabilitiesAsync(includeDenied: true, CancellationToken.None));

        Assert.Equal(StatusCode.Unavailable, exception.StatusCode);
        Assert.True(connection.ReconnectRefused);
        Assert.Empty(replacement.Calls);
    }
}
