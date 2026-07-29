using Briosa.Worker.Control;

namespace Briosa.Server.Workers;

internal enum RuntimeIdentityEvidenceSource
{
    Unavailable,
    RuntimeVerification,
    OperatorAttestation
}

internal enum RuntimeIdentityMatchState
{
    Unavailable,
    ExactMatch,
    Mismatch
}

internal sealed record RuntimeIdentityEvidence(
    string? Version,
    RuntimeIdentityEvidenceSource Source,
    RuntimeIdentityMatchState MatchState);

internal sealed record ExactTargetIdentitySnapshot(
    RuntimeIdentityEvidence ActivatedSdk,
    RuntimeIdentityEvidence ConnectedSpatialAnalyzer)
{
    public bool AllowsExecution =>
        ActivatedSdk.MatchState == RuntimeIdentityMatchState.ExactMatch &&
        ConnectedSpatialAnalyzer.MatchState == RuntimeIdentityMatchState.ExactMatch;
}

internal sealed class ExactTargetIdentityPolicy
{
    internal const string ActivatedSdkVersionKey =
        "Briosa:SpatialAnalyzer:Identity:ActivatedSdk:OperatorAttestation:Version";
    internal const string ActivatedSdkReferenceKey =
        "Briosa:SpatialAnalyzer:Identity:ActivatedSdk:OperatorAttestation:Reference";
    internal const string ConnectedSpatialAnalyzerVersionKey =
        "Briosa:SpatialAnalyzer:Identity:ConnectedSpatialAnalyzer:OperatorAttestation:Version";
    internal const string ConnectedSpatialAnalyzerReferenceKey =
        "Briosa:SpatialAnalyzer:Identity:ConnectedSpatialAnalyzer:OperatorAttestation:Reference";

    private readonly OperatorAttestation? _activatedSdkAttestation;
    private readonly OperatorAttestation? _connectedSpatialAnalyzerAttestation;

    private ExactTargetIdentityPolicy(
        string targetVersion,
        OperatorAttestation? activatedSdkAttestation,
        OperatorAttestation? connectedSpatialAnalyzerAttestation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetVersion);
        TargetVersion = targetVersion;
        _activatedSdkAttestation = activatedSdkAttestation;
        _connectedSpatialAnalyzerAttestation = connectedSpatialAnalyzerAttestation;
    }

    public string TargetVersion { get; }

    public static ExactTargetIdentityPolicy Create(
        IConfiguration configuration,
        string targetVersion)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return new ExactTargetIdentityPolicy(
            targetVersion,
            ReadAttestation(
                configuration,
                ActivatedSdkVersionKey,
                ActivatedSdkReferenceKey),
            ReadAttestation(
                configuration,
                ConnectedSpatialAnalyzerVersionKey,
                ConnectedSpatialAnalyzerReferenceKey));
    }

    internal static ExactTargetIdentityPolicy CreateForTesting(
        string targetVersion,
        string? activatedSdkVersion = null,
        string? connectedSpatialAnalyzerVersion = null) =>
        new(
            targetVersion,
            activatedSdkVersion is null
                ? null
                : new OperatorAttestation(activatedSdkVersion, "portable-test"),
            connectedSpatialAnalyzerVersion is null
                ? null
                : new OperatorAttestation(connectedSpatialAnalyzerVersion, "portable-test"));

    internal static ExactTargetIdentityPolicy CreateRuntimeOnly(string targetVersion) =>
        new(targetVersion, activatedSdkAttestation: null, connectedSpatialAnalyzerAttestation: null);

    public ExactTargetIdentitySnapshot Evaluate(WorkerRuntimeIdentitySnapshot? runtimeIdentity) =>
        new(
            Evaluate(
                runtimeIdentity?.ActivatedSdk,
                _activatedSdkAttestation),
            Evaluate(
                runtimeIdentity?.ConnectedSpatialAnalyzer,
                _connectedSpatialAnalyzerAttestation));

    public static bool IsWellFormed(WorkerRuntimeIdentitySnapshot? runtimeIdentity) =>
        runtimeIdentity is null ||
        IsWellFormed(runtimeIdentity.ActivatedSdk) &&
        IsWellFormed(runtimeIdentity.ConnectedSpatialAnalyzer);

    private RuntimeIdentityEvidence Evaluate(
        WorkerRuntimeIdentityEvidence? runtimeEvidence,
        OperatorAttestation? attestation)
    {
        if (runtimeEvidence?.Source == WorkerRuntimeIdentityEvidenceSource.RuntimeVerified)
        {
            return CreateEvidence(
                runtimeEvidence.Version!,
                RuntimeIdentityEvidenceSource.RuntimeVerification);
        }

        return attestation is null
            ? new RuntimeIdentityEvidence(
                Version: null,
                RuntimeIdentityEvidenceSource.Unavailable,
                RuntimeIdentityMatchState.Unavailable)
            : CreateEvidence(
                attestation.Version,
                RuntimeIdentityEvidenceSource.OperatorAttestation);
    }

    private RuntimeIdentityEvidence CreateEvidence(
        string version,
        RuntimeIdentityEvidenceSource source) =>
        new(
            version,
            source,
            string.Equals(version, TargetVersion, StringComparison.Ordinal)
                ? RuntimeIdentityMatchState.ExactMatch
                : RuntimeIdentityMatchState.Mismatch);

    private static bool IsWellFormed(WorkerRuntimeIdentityEvidence? evidence) =>
        evidence is not null && evidence.Source switch
        {
            WorkerRuntimeIdentityEvidenceSource.Unavailable => evidence.Version is null,
            WorkerRuntimeIdentityEvidenceSource.RuntimeVerified =>
                IsValidVersion(evidence.Version),
            _ => false
        };

    private static bool IsValidVersion(string? version) =>
        !string.IsNullOrWhiteSpace(version) &&
        version.Length <= 128 &&
        !version.Contains('\r', StringComparison.Ordinal) &&
        !version.Contains('\n', StringComparison.Ordinal);

    private static OperatorAttestation? ReadAttestation(
        IConfiguration configuration,
        string versionKey,
        string referenceKey)
    {
        var version = configuration[versionKey];
        var reference = configuration[referenceKey];
        if (string.IsNullOrWhiteSpace(version) && string.IsNullOrWhiteSpace(reference))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(version) || string.IsNullOrWhiteSpace(reference))
        {
            throw new InvalidOperationException(
                $"Operator attestation requires both '{versionKey}' and '{referenceKey}'.");
        }

        if (!IsValidVersion(version) || reference.Length > 256 ||
            reference.Contains('\r', StringComparison.Ordinal) ||
            reference.Contains('\n', StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Operator attestation values for '{versionKey}' and '{referenceKey}' have an invalid shape.");
        }

        return new OperatorAttestation(version, reference);
    }

    private sealed record OperatorAttestation(string Version, string Reference);
}
