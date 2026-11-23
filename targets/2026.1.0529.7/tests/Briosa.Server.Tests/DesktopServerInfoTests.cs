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

    // An unfamiliar disposition must not read as "no incident": the strict reader refused it.
    [Fact]
    public void AnUnfamiliarIncidentDispositionIsNotRecognizedAndNeverReady()
    {
        var sdk = ReadySdk();
        sdk.LastIncident = new SpatialAnalyzerSdkIncident { ExecutionDisposition = ExecutionDisposition.StartedOutcomeUnknown };
        var sdkJson = WithValue(JsonFormatter.Default.Format(sdk), "lastIncident.executionDisposition",
            "EXECUTION_DISPOSITION_FROM_A_LATER_SERVER");

        // Read as omitted, the same reply would claim readiness with no warning.
        Assert.Equal("Ready for commands", DesktopState.FromReply(Reply(
            JsonFormatter.Default.Format(new GetServerInfoResponse { ReadyForMp = true }),
            JsonFormatter.Default.Format(ReadySdk()))).Heading);

        var state = DesktopState.FromReply(Reply(JsonFormatter.Default.Format(new GetServerInfoResponse { ReadyForMp = true }),
            sdkJson) with { CanManage = true });

        AssertNotRecognized(state);
    }

    // An unfamiliar match state must not read as unset, which would offer Connect.
    [Fact]
    public void AnUnfamiliarIdentityMatchStateIsNotRecognizedAndOffersNoConnect()
    {
        var info = new GetServerInfoResponse { ActivatedSdkIdentity = new() { MatchState = RuntimeIdentityMatchState.Mismatch } };
        var infoJson = WithValue(JsonFormatter.Default.Format(info), "activatedSdkIdentity.matchState",
            "RUNTIME_IDENTITY_MATCH_STATE_FROM_A_LATER_SERVER");
        var connectable = JsonFormatter.Default.Format(new SpatialAnalyzerSdkLifecycleState
        {
            SdkState = SpatialAnalyzerSdkState.Running, SdkGeneration = 1,
            ConnectionState = SpatialAnalyzerConnectionState.Disconnected,
            RecoveryState = SpatialAnalyzerSdkRecoveryState.NotRequired
        });
        // Read as omitted, the same reply would offer Connect.
        Assert.True(DesktopState.FromReply(Reply(JsonFormatter.Default.Format(new GetServerInfoResponse()), connectable)
            with { CanManage = true }).CanConnect);

        var state = DesktopState.FromReply(Reply(infoJson, connectable) with { CanManage = true });

        AssertNotRecognized(state);
    }

    // Every enum a known field carries in the three payloads, by name and by number.
    [Theory]
    [InlineData("info", "activatedSdkIdentity.source")]
    [InlineData("info", "connectedSpatialAnalyzerIdentity.matchState")]
    [InlineData("info", "connectedSpatialAnalyzerIdentity.source")]
    [InlineData("info", "workerState")]
    [InlineData("info", "spatialAnalyzerConnectionState")]
    [InlineData("info", "spatialAnalyzerExecutionReadinessState")]
    [InlineData("info", "connectedSpatialAnalyzerVersionState")]
    [InlineData("info", "targetIsolationMode")]
    [InlineData("sdk", "sdkState")]
    [InlineData("sdk", "connectionState")]
    [InlineData("sdk", "recoveryState")]
    [InlineData("sdk", "executionReadinessState")]
    [InlineData("sdk", "lastIncident.terminationKind")]
    [InlineData("application", "applicationState")]
    [InlineData("application", "ownership")]
    public void AnUnfamiliarValueInAnyKnownEnumFieldIsNotRecognized(string payload, string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        foreach (System.Text.Json.Nodes.JsonNode value in new System.Text.Json.Nodes.JsonNode[] { "VALUE_FROM_A_LATER_SERVER", 99 })
        {
            var info = JsonFormatter.Default.Format(new GetServerInfoResponse { ReadyForMp = true });
            var sdk = JsonFormatter.Default.Format(ReadySdk());
            var application = JsonFormatter.Default.Format(new SpatialAnalyzerLifecycleState
            {
                ApplicationState = SpatialAnalyzerApplicationState.Running
            });
            var reply = Reply(payload == "info" ? WithValue(info, path, value) : info,
                payload == "sdk" ? WithValue(sdk, path, value) : sdk) with
            {
                CanManage = true,
                ApplicationStateJson = payload == "application" ? WithValue(application, path, value) : application
            };

            AssertNotRecognized(DesktopState.FromReply(reply));
        }
    }

    // The configured mode is reported only, so an unfamiliar one stays unknown.
    [Fact]
    public void AnUnfamiliarConfiguredInteractionModeStaysUnknown()
    {
        var info = JsonFormatter.Default.Format(new GetServerInfoResponse
        {
            ConfiguredSpatialAnalyzerInteractionMode = ConfiguredSpatialAnalyzerInteractionMode.Silent
        });
        var stopped = JsonFormatter.Default.Format(new SpatialAnalyzerSdkLifecycleState { SdkState = SpatialAnalyzerSdkState.Stopped });

        var state = DesktopState.FromReply(Reply(
            WithValue(info, "configuredSpatialAnalyzerInteractionMode", "CONFIGURED_SPATIAL_ANALYZER_INTERACTION_MODE_FROM_A_LATER_SERVER"),
            stopped) with { CanManage = true });

        Assert.True(state.Available);
        Assert.True(state.CanStartSdk);
        Assert.Equal(ConfiguredSpatialAnalyzerInteractionMode.Unspecified, state.Info!.ConfiguredSpatialAnalyzerInteractionMode);
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

    private static SpatialAnalyzerSdkLifecycleState ReadySdk() => new()
    {
        SdkState = SpatialAnalyzerSdkState.Ready, ReadyForMp = true, SdkGeneration = 1,
        ConnectionState = SpatialAnalyzerConnectionState.Connected,
        ExecutionReadinessState = SpatialAnalyzerExecutionReadinessState.ExecutionReady,
        RecoveryState = SpatialAnalyzerSdkRecoveryState.NotRequired
    };

    // Nothing is offered or claimed from a reply that this Control Center cannot read in full.
    private static void AssertNotRecognized(DesktopState state)
    {
        Assert.Equal("Status not recognized", state.Heading);
        Assert.False(state.Available);
        Assert.False(state.CanManage);
        Assert.False(state.Ready);
        Assert.True(state.NeedsAttention);
        Assert.False(state.CanStartSdk || state.CanConnect || state.CanReconnect || state.CanRecover || state.CanStopSdk);
        Assert.False(state.OutcomeUnknown);
        Assert.Null(state.Info);
        Assert.Null(state.Sdk);
        Assert.Null(state.Application);
    }

    // Sets a value at a dotted path, creating the objects on the way.
    private static string WithValue(string json, string path, System.Text.Json.Nodes.JsonNode value)
    {
        var root = System.Text.Json.Nodes.JsonNode.Parse(json)!.AsObject();
        var node = root;
        var names = path.Split('.');
        foreach (var name in names[..^1])
        {
            if (node[name] is not System.Text.Json.Nodes.JsonObject child)
            {
                child = new System.Text.Json.Nodes.JsonObject();
                node[name] = child;
            }

            node = child;
        }

        node[names[^1]] = value.DeepClone();
        return root.ToJsonString();
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
