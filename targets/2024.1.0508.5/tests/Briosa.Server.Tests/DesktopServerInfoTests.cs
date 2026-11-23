using Briosa.Desktop;
using Google.Protobuf;

namespace Briosa.Server.Tests;

/// <summary>
/// #400: Control Center reads server info that carries fields it does not know, and
/// sizes its stop observation from the host graceful shutdown bound the server publishes.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Tests retain xUnit synchronization behavior.")]
public sealed class DesktopServerInfoTests
{
    [Fact]
    public void ServerInfoWithFieldsControlCenterDoesNotKnowStillParses()
    {
        var info = new GetServerInfoResponse
        {
            ReadyForMp = true,
            LifecycleBounds = new LifecycleBounds
            {
                StartSdkMilliseconds = 45_000,
                HostGracefulShutdown = new GracefulShutdownBound { FiniteMilliseconds = 105_000 }
            },
            ConfiguredSpatialAnalyzerInteractionMode = ConfiguredSpatialAnalyzerInteractionMode.Silent
        };
        var sdk = new SpatialAnalyzerSdkLifecycleState { SdkState = SpatialAnalyzerSdkState.Ready, ReadyForMp = true };
        var reply = Reply(
            WithUnknownFields(JsonFormatter.Default.Format(info)),
            WithUnknownFields(JsonFormatter.Default.Format(sdk)));

        var state = DesktopState.FromReply(reply);

        Assert.True(state.Ready);
        Assert.Equal(info, state.Info);
        Assert.Equal(SpatialAnalyzerSdkState.Ready, state.Sdk!.SdkState);
        Assert.Equal(TimeSpan.FromSeconds(110), DesktopStopWait.For(state.Info));
    }

    [Theory]
    // In range: the published bound plus the 5 s exit margin.
    [InlineData(105_000UL, 110_000L)]
    [InlineData(1UL, 5_001L)]
    [InlineData(4_294_962_294UL, 4_294_967_294L)]
    // Out of the local timer range once the margin is added, or zero: the fixed 45 s.
    [InlineData(4_294_962_295UL, 45_000L)]
    [InlineData(ulong.MaxValue, 45_000L)]
    [InlineData(0UL, 45_000L)]
    public void AFiniteBoundSizesTheWaitWithinTheTimerRange(ulong finite, long expectedMilliseconds)
    {
        var info = new GetServerInfoResponse
        {
            LifecycleBounds = new LifecycleBounds
            {
                HostGracefulShutdown = new GracefulShutdownBound { FiniteMilliseconds = finite }
            }
        };

        Assert.Equal(TimeSpan.FromMilliseconds(expectedMilliseconds), DesktopStopWait.For(info));
    }

    [Fact]
    public void AnAbsentOrUnrecognizedBoundFallsBackAndAnUnboundedOneHasNoLimit()
    {
        Assert.Equal(DesktopStopWait.Fallback, DesktopStopWait.For(null));
        // Server 0.10.0 sends no bounds.
        Assert.Equal(DesktopStopWait.Fallback, DesktopStopWait.For(new GetServerInfoResponse()));
        Assert.Equal(DesktopStopWait.Fallback, DesktopStopWait.For(new GetServerInfoResponse
        {
            LifecycleBounds = new LifecycleBounds { StartSdkMilliseconds = 45_000 }
        }));
        // A limit case this reader does not know arrives as an unknown field.
        var future = GracefulShutdownBound.Parser.ParseFrom([0x18, 0x01]);
        Assert.Equal(DesktopStopWait.Fallback, DesktopStopWait.For(new GetServerInfoResponse
        {
            LifecycleBounds = new LifecycleBounds { HostGracefulShutdown = future }
        }));
        Assert.Null(DesktopStopWait.For(new GetServerInfoResponse
        {
            LifecycleBounds = new LifecycleBounds
            {
                HostGracefulShutdown = new GracefulShutdownBound { Unbounded = new UnboundedShutdownWait() }
            }
        }));
        Assert.Equal(TimeSpan.FromSeconds(45), DesktopStopWait.Fallback);
    }

    // An unbounded wait observes the exit under the caller's own token until it cancels.
    [Fact]
    public async Task AnUnboundedWaitEndsOnlyWhenTheCallerCancels()
    {
        using var caller = new CancellationTokenSource();
        var exit = new TaskCompletionSource();
        CancellationToken observed = default;
        var wait = DesktopStopWait.ObserveAsync(token =>
        {
            observed = token;
            return exit.Task.WaitAsync(token);
        }, wait: null, caller.Token);

        await Task.Delay(50);
        Assert.False(wait.IsCompleted);
        Assert.Equal(caller.Token, observed);
        await caller.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => wait);
        Assert.False(exit.Task.IsCompleted);
    }

    [Fact]
    public async Task AFiniteWaitEndsAtExpiryOrWhenTheCallerCancels()
    {
        using var caller = new CancellationTokenSource();
        var neverExits = new TaskCompletionSource();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => DesktopStopWait.ObserveAsync(
            token => neverExits.Task.WaitAsync(token), TimeSpan.FromMilliseconds(50), caller.Token));
        Assert.False(caller.IsCancellationRequested);

        var cancelled = DesktopStopWait.ObserveAsync(
            token => neverExits.Task.WaitAsync(token), TimeSpan.FromMinutes(5), caller.Token);
        await caller.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled);

        var exited = new TaskCompletionSource();
        var observing = DesktopStopWait.ObserveAsync(
            token => exited.Task.WaitAsync(token), TimeSpan.FromMinutes(5), CancellationToken.None);
        exited.SetResult();
        await observing;
    }

    // Adds a top-level and a nested property that the reader's schema does not have.
    private static string WithUnknownFields(string json)
    {
        var node = System.Text.Json.Nodes.JsonNode.Parse(json)!.AsObject();
        node["fieldFromALaterServer"] = 7;
        if (node["lifecycleBounds"] is System.Text.Json.Nodes.JsonObject bounds)
            bounds["allowanceFromALaterServer"] = "1";
        return node.ToJsonString();
    }

    private static DesktopReply Reply(string serverInfoJson, string sdkStateJson) => new()
    {
        Instance = DesktopProtocol.NewInstance(),
        Target = DesktopProtocol.Target,
        Endpoint = "http://127.0.0.1:50051",
        HostState = "Running",
        ServerInfoJson = serverInfoJson,
        SdkStateJson = sdkStateJson,
        ApplicationStateJson = WithUnknownFields(JsonFormatter.Default.Format(new SpatialAnalyzerLifecycleState
        {
            ApplicationState = SpatialAnalyzerApplicationState.Running
        }))
    };
}
