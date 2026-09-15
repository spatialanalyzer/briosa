using Google.Protobuf;

namespace Briosa.Desktop;

public sealed record DesktopState
{
    public bool Available { get; init; }
    public bool CanManage { get; init; }
    public bool Ready { get; init; }
    public bool NeedsAttention { get; init; }
    public string Heading { get; init; } = "Server stopped";
    public string Guidance { get; init; } = "Start the API host when you are ready to use Briosa.";
    public string Host { get; init; } = "Stopped";
    public string Endpoint { get; init; } = "";
    public DateTimeOffset? ObservedAt { get; init; }
    public GetServerInfoResponse? Info { get; init; }
    public SpatialAnalyzerSdkLifecycleState? Sdk { get; init; }
    public SpatialAnalyzerLifecycleState? Application { get; init; }

    public bool CanStartSdk => CanManage && Available && Sdk?.SdkState == SpatialAnalyzerSdkState.Stopped;
    public bool CanConnect => CanManage && Available && Sdk is { SdkState: SpatialAnalyzerSdkState.Running,
        ConnectionState: SpatialAnalyzerConnectionState.Disconnected, RecoveryState: SpatialAnalyzerSdkRecoveryState.NotRequired };
    public bool CanReconnect => CanManage && Available && !Ready && Sdk is { SdkState: SpatialAnalyzerSdkState.Running,
        ConnectionState: SpatialAnalyzerConnectionState.Connected, RecoveryState: SpatialAnalyzerSdkRecoveryState.NotRequired };
    public bool CanRecover => CanManage && Available && Sdk?.RecoveryState is SpatialAnalyzerSdkRecoveryState.RecoveryAvailable
        or SpatialAnalyzerSdkRecoveryState.OperatorActionRequired;
    public bool CanStopSdk => CanManage && Available && Sdk is { HasSdkGeneration: true } &&
        Sdk.SdkState is SpatialAnalyzerSdkState.Running or SpatialAnalyzerSdkState.Ready or SpatialAnalyzerSdkState.Faulted;
    public bool OutcomeUnknown => Sdk?.LastIncident is { HasExecutionDisposition: true,
        ExecutionDisposition: ExecutionDisposition.StartedOutcomeUnknown };

    public static DesktopState FromReply(DesktopReply reply)
    {
        ArgumentNullException.ThrowIfNull(reply);
        var info = GetServerInfoResponse.Parser.ParseJson(reply.ServerInfoJson);
        var sdk = SpatialAnalyzerSdkLifecycleState.Parser.ParseJson(reply.SdkStateJson);
        var application = SpatialAnalyzerLifecycleState.Parser.ParseJson(reply.ApplicationStateJson);
        bool available = reply.HostState == "Running";
        bool ready = available && info.ReadyForMp && sdk.ReadyForMp;
        var (heading, guidance, attention) = Describe(reply.HostState, info, sdk, application, ready);
        return new DesktopState
        {
            Available = available, CanManage = reply.CanManage, Ready = ready, NeedsAttention = attention,
            Heading = heading, Guidance = guidance, Host = reply.HostState, Endpoint = reply.Endpoint,
            ObservedAt = DateTimeOffset.Now, Info = info, Sdk = sdk, Application = application
        };
    }

    public DesktopState Unavailable(string heading = "Status unavailable") => this with
    {
        Available = false, CanManage = false, Ready = false, NeedsAttention = true,
        Heading = heading, Host = "Unavailable", Guidance =
            "The last observation is shown below. Refresh after checking the server; no action has been retried."
    };

    private static (string, string, bool) Describe(string host, GetServerInfoResponse info,
        SpatialAnalyzerSdkLifecycleState sdk, SpatialAnalyzerLifecycleState application, bool ready)
    {
        if (host != "Running") return ("Server · " + host, "Waiting for the API host. No SDK connection is started automatically.", host == "Failed");
        if (sdk.LastIncident is { HasExecutionDisposition: true, ExecutionDisposition: ExecutionDisposition.StartedOutcomeUnknown })
            return (ready ? "Ready — earlier command outcome unknown" : "Command outcome unknown",
                "An earlier command may have run. Inspect SpatialAnalyzer before deciding what to do. Recovery does not replay it or establish its result.", true);
        if (sdk.RecoveryState is SpatialAnalyzerSdkRecoveryState.OperatorActionRequired or SpatialAnalyzerSdkRecoveryState.Blocked)
            return ("Operator action required", "Establish a clean SDK/SpatialAnalyzer execution owner before recovery. Repeated reconnects can leave competing SDK clients.", true);
        if (sdk.RecoveryState == SpatialAnalyzerSdkRecoveryState.RecoveryAvailable)
            return ("SDK recovery required", "Recover the stopped or faulted SDK generation, then connect explicitly. Commands are not replayed.", true);
        if (ready) return ("Ready for commands", "The SDK connection, both version identities, and execution channel meet readiness requirements for this generation.", false);
        if (sdk.SdkState == SpatialAnalyzerSdkState.Stopped)
            return ("Server running · SDK stopped", "Start the SDK, then connect to the separately running SpatialAnalyzer application.", false);
        if (info.ActivatedSdkIdentity?.MatchState == RuntimeIdentityMatchState.Mismatch ||
            info.ConnectedSpatialAnalyzerIdentity?.MatchState == RuntimeIdentityMatchState.Mismatch)
            return ("Version mismatch", "The activated SDK and connected SpatialAnalyzer must both match this exact target. Correct the environment before connecting again.", true);
        if (sdk.ConnectionState == SpatialAnalyzerConnectionState.Connected &&
            (info.ActivatedSdkIdentity?.MatchState != RuntimeIdentityMatchState.ExactMatch ||
             info.ConnectedSpatialAnalyzerIdentity?.MatchState != RuntimeIdentityMatchState.ExactMatch))
            return ("Version identity unavailable", "Review independent SDK and SpatialAnalyzer identity evidence in server configuration. An installed package alone does not establish runtime identity.", true);
        if (application.ApplicationState == SpatialAnalyzerApplicationState.NotRunning)
            return ("Waiting for SpatialAnalyzer", "Open the exact-target SpatialAnalyzer application, then connect the SDK.", false);
        if (sdk.ConnectionState == SpatialAnalyzerConnectionState.Disconnected)
            return ("SDK disconnected", "Connect to the local SpatialAnalyzer application when its environment is ready.", false);
        return ("Commands unavailable", "Review SDK state, identity evidence, and the latest diagnostic before taking another action.", sdk.SdkState == SpatialAnalyzerSdkState.Faulted);
    }

    public static string IdentityText(RuntimeIdentityEvidence? identity) => identity is null ? "Unavailable" :
        $"{SafeText.Version(identity.Version)} · {SafeText.Label(identity.Source)} · {SafeText.Label(identity.MatchState)}";
}

public static class SafeText
{
    public static string Label(Enum? value)
    {
        if (value is null || !Enum.IsDefined(value.GetType(), value)) return "Unavailable";
        var text = value.ToString();
        var result = new System.Text.StringBuilder();
        for (int i = 0; i < text.Length; i++)
        {
            if (i > 0 && char.IsUpper(text[i]) && char.IsLower(text[i - 1])) result.Append(' ');
            result.Append(i == 0 ? text[i] : char.ToLowerInvariant(text[i]));
        }
        return result.ToString();
    }
    public static string Code(string? value) => value is { Length: > 0 and <= 128 } &&
        value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_') ? value : "unavailable";
    public static string Version(string? value) => value is { Length: > 0 and <= 96 } &&
        value.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '-' or '+') ? value : "Unavailable";
}
