using System.Text.Json.Nodes;

namespace Briosa.Cli.Engine;

/// <summary>
/// What <c>GetServerInfo</c> says about the server, checked against this build's
/// exact target and compatibility. It reports the server's own claims and infers
/// no readiness from them.
/// </summary>
internal sealed class ServerAssessment
{
    private ServerAssessment(GetServerInfoResponse info, IReadOnlyList<string> problems)
    {
        Info = info;
        Problems = problems;
    }

    public GetServerInfoResponse Info { get; }

    /// <summary>Value-free codes for each reason the server is not usable at all.</summary>
    public IReadOnlyList<string> Problems { get; }

    /// <summary>The exact target, protocol package, and compatibility major all match.</summary>
    public bool Verified => Problems.Count == 0;

    public uint Revision => Info.Compatibility?.Revision ?? 0;

    /// <summary>
    /// The server enforces the caller restriction header, so an operation may be
    /// sent with it. An older server ignores the header.
    /// </summary>
    public bool SupportsExecution => Verified && Revision >= BridgeContract.MinimumExecutionRevision;

    /// <summary>Server 0.10.1 and later describe request options and denial detail.</summary>
    public bool DescribesRequestOptions => Verified && Revision >= 1;

    /// <summary>Server 0.10.2 and later report <c>mp_step</c> and request validation detail.</summary>
    public bool ReportsMpStep => Verified && Revision >= 2;

    public static ServerAssessment Assess(GetServerInfoResponse info)
    {
        ArgumentNullException.ThrowIfNull(info);
        List<string> problems = [];
        if (!string.Equals(info.Version?.SpatialAnalyzerTarget, BridgeContract.SpatialAnalyzerTarget, StringComparison.Ordinal))
        {
            problems.Add("server-target-mismatch");
        }

        if (!string.Equals(info.Version?.ProtocolPackage, BridgeContract.ProtocolPackage, StringComparison.Ordinal))
        {
            problems.Add("server-protocol-package-mismatch");
        }

        if (info.Compatibility is null)
        {
            problems.Add("server-compatibility-missing");
        }
        else if (info.Compatibility.Major != BridgeContract.CompatibilityMajor)
        {
            problems.Add("server-compatibility-major-unsupported");
        }

        return new ServerAssessment(info, problems);
    }

    /// <summary>The identity block of the envelope: the server's claims, and the verification result.</summary>
    public JsonObject ToJson()
    {
        var version = Info.Version;
        return new JsonObject
        {
            ["briosa_version"] = version is { HasBriosaVersion: true } ? version.BriosaVersion : null,
            ["source_revision"] = version is { HasSourceRevision: true } ? version.SourceRevision : null,
            ["protocol_package"] = version is { HasProtocolPackage: true } ? version.ProtocolPackage : null,
            ["spatial_analyzer_target"] = version is { HasSpatialAnalyzerTarget: true } ? version.SpatialAnalyzerTarget : null,
            ["interop_fingerprint"] = version is { HasInteropFingerprint: true } ? version.InteropFingerprint : null,
            ["compatibility"] = Info.Compatibility is null
                ? null
                : new JsonObject { ["major"] = Info.Compatibility.Major, ["revision"] = Info.Compatibility.Revision },
            ["expected_spatial_analyzer_target"] = BridgeContract.SpatialAnalyzerTarget,
            ["expected_compatibility_major"] = BridgeContract.CompatibilityMajor,
            ["verified"] = Verified,
            ["verification_problems"] = new JsonArray(Problems.Select(problem => (JsonNode)problem).ToArray()),
            ["execution_supported"] = SupportsExecution
        };
    }

    /// <summary>What this server does not report, by revision, as fixed warnings.</summary>
    public void AddFeatureWarnings(BridgeEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        if (!Verified)
        {
            return;
        }

        if (Revision < BridgeContract.MinimumExecutionRevision)
        {
            envelope.Warn("server-predates-caller-restriction",
                "This server predates Server 0.10.2. It ignores the caller restriction header, so call refuses " +
                "to execute operations against it, and it does not report mp_step or request validation detail.");
        }

        if (Revision < 1)
        {
            envelope.Warn("server-predates-request-options",
                "This server predates Server 0.10.1. It does not describe request options, typed denial detail, " +
                "lifecycle bounds, or its configured interaction mode.");
        }
    }
}
