namespace Briosa.Cli.Engine;

/// <summary>
/// The fixed facts of the one exact target this build serves. Each is a reviewed
/// fact of this target's server, not a setting.
/// </summary>
internal static class BridgeContract
{
    /// <summary>The exact SpatialAnalyzer release this build attaches to.</summary>
    public const string SpatialAnalyzerTarget = "2026.1.0529.7";

    /// <summary>The stable protobuf package of every Briosa target.</summary>
    public const string ProtocolPackage = "briosa";

    /// <summary>The compatibility major this build understands.</summary>
    public const uint CompatibilityMajor = 3;

    /// <summary>
    /// The first revision whose server enforces the caller restriction header
    /// (Server 0.10.2). An older server ignores the header, so the bridge never
    /// executes an operation against it.
    /// </summary>
    public const uint MinimumExecutionRevision = 2;

    /// <summary>The binary trailer that carries the typed <c>OperationError</c>.</summary>
    public const string OperationErrorTrailer = "briosa-operation-error-bin";

    /// <summary>The request header that narrows admission for one call.</summary>
    public const string CallerRestrictionHeader = "briosa-caller-restriction";

    /// <summary>
    /// The restriction every read-only call sends: the read-only profile, with
    /// operator UI denied, so the server enforces the promise even when its own
    /// configuration admits more.
    /// </summary>
    public const string ReadOnlyCallerRestriction = "profile=read-only; deny=interactive_ui";

    /// <summary>
    /// The server's public receive limit for one request message
    /// (<c>PublicGrpcHosting.MaximumReceiveMessageBytes</c>). A larger request is
    /// rejected by the transport before the service runs, without a typed trailer.
    /// </summary>
    public const int ServerReceiveLimitBytes = 64 * 1024;

    /// <summary>
    /// The receive allowance for one response. The server's largest public response
    /// stays below it, so the CLI never lowers what the server can return.
    /// </summary>
    public const int ClientReceiveLimitBytes = 4 * 1024 * 1024;

    /// <summary>The bound on each discovery RPC. MP calls have no default deadline.</summary>
    public static readonly TimeSpan DiscoveryDeadline = TimeSpan.FromSeconds(15);

    /// <summary>The bound on opening a TCP connection to the loopback endpoint.</summary>
    public static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(5);

    /// <summary>The server versions that declare a given compatibility revision.</summary>
    public static string ServerVersionForRevision(uint revision) => revision switch
    {
        0 => "0.10.0",
        1 => "0.10.1",
        2 => "0.10.2",
        _ => "0.10.2 or later"
    };
}
