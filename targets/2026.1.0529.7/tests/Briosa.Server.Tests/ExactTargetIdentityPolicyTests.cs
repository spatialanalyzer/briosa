using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Microsoft.Extensions.Configuration;
using ServerIdentityEvidence = Briosa.Server.Workers.RuntimeIdentityEvidence;
using ServerIdentityMatchState = Briosa.Server.Workers.RuntimeIdentityMatchState;
using ServerIdentitySource = Briosa.Server.Workers.RuntimeIdentityEvidenceSource;

namespace Briosa.Server.Tests;

public sealed class ExactTargetIdentityPolicyTests
{
    private const string Target = "2026.1.0529.7";

    [Fact]
    public void MissingEvidenceFailsClosedWithoutInventingVersions()
    {
        var policy = CreatePolicy(new Dictionary<string, string?>());

        var identity = policy.Evaluate(runtimeIdentity: null);

        Assert.False(identity.AllowsExecution);
        AssertUnavailable(identity.ActivatedSdk);
        AssertUnavailable(identity.ConnectedSpatialAnalyzer);
    }

    [Fact]
    public void EachUnavailableClaimCanUseItsOwnOperatorAttestation()
    {
        var policy = CreatePolicy(new Dictionary<string, string?>
        {
            [ExactTargetIdentityPolicy.ConnectedSpatialAnalyzerVersionKey] = Target,
            [ExactTargetIdentityPolicy.ConnectedSpatialAnalyzerReferenceKey] =
                "deployment-record:connected-sa"
        });
        var runtime = new WorkerRuntimeIdentitySnapshot(
            RuntimeVerified(Target),
            Unavailable());

        var identity = policy.Evaluate(runtime);

        Assert.True(identity.AllowsExecution);
        Assert.Equal(
            ServerIdentitySource.RuntimeVerification,
            identity.ActivatedSdk.Source);
        Assert.Equal(
            ServerIdentitySource.OperatorAttestation,
            identity.ConnectedSpatialAnalyzer.Source);
        Assert.Equal(Target, identity.ActivatedSdk.Version);
        Assert.Equal(Target, identity.ConnectedSpatialAnalyzer.Version);
    }

    [Fact]
    public void RuntimeMismatchWinsOverMatchingAttestation()
    {
        var policy = CreatePolicy(new Dictionary<string, string?>
        {
            [ExactTargetIdentityPolicy.ActivatedSdkVersionKey] = Target,
            [ExactTargetIdentityPolicy.ActivatedSdkReferenceKey] =
                "deployment-record:sdk",
            [ExactTargetIdentityPolicy.ConnectedSpatialAnalyzerVersionKey] = Target,
            [ExactTargetIdentityPolicy.ConnectedSpatialAnalyzerReferenceKey] =
                "deployment-record:connected-sa"
        });
        var runtime = new WorkerRuntimeIdentitySnapshot(
            RuntimeVerified("2025.0"),
            Unavailable());

        var identity = policy.Evaluate(runtime);

        Assert.False(identity.AllowsExecution);
        Assert.Equal(
            ServerIdentitySource.RuntimeVerification,
            identity.ActivatedSdk.Source);
        Assert.Equal(ServerIdentityMatchState.Mismatch, identity.ActivatedSdk.MatchState);
        Assert.Equal("2025.0", identity.ActivatedSdk.Version);
        Assert.Equal(
            ServerIdentitySource.OperatorAttestation,
            identity.ConnectedSpatialAnalyzer.Source);
    }

    [Theory]
    [InlineData(
        ExactTargetIdentityPolicy.ActivatedSdkVersionKey,
        ExactTargetIdentityPolicy.ActivatedSdkReferenceKey)]
    [InlineData(
        ExactTargetIdentityPolicy.ConnectedSpatialAnalyzerVersionKey,
        ExactTargetIdentityPolicy.ConnectedSpatialAnalyzerReferenceKey)]
    public void PartialOperatorAttestationFailsConfiguration(
        string versionKey,
        string referenceKey)
    {
        var versionOnly = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [versionKey] = Target
            })
            .Build();
        var referenceOnly = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [referenceKey] = "deployment-record:test"
            })
            .Build();

        Assert.Throws<InvalidOperationException>(() =>
            SpatialAnalyzerIdentityOptions.BindAndValidate(versionOnly));
        Assert.Throws<InvalidOperationException>(() =>
            SpatialAnalyzerIdentityOptions.BindAndValidate(referenceOnly));
    }

    [Fact]
    public void WorkerEvidenceShapeMustBeExplicitAndConsistent()
    {
        Assert.True(ExactTargetIdentityPolicy.IsWellFormed(runtimeIdentity: null));
        Assert.True(ExactTargetIdentityPolicy.IsWellFormed(
            new WorkerRuntimeIdentitySnapshot(
                RuntimeVerified(Target),
                Unavailable())));
        Assert.False(ExactTargetIdentityPolicy.IsWellFormed(
            new WorkerRuntimeIdentitySnapshot(
                new WorkerRuntimeIdentityEvidence(
                    Version: null,
                    WorkerRuntimeIdentityEvidenceSource.RuntimeVerified),
                Unavailable())));
        Assert.False(ExactTargetIdentityPolicy.IsWellFormed(
            new WorkerRuntimeIdentitySnapshot(
                RuntimeVerified("2026.1\rforged"),
                Unavailable())));
        Assert.False(ExactTargetIdentityPolicy.IsWellFormed(
            new WorkerRuntimeIdentitySnapshot(
                RuntimeVerified(new string('x', 129)),
                Unavailable())));
        Assert.False(ExactTargetIdentityPolicy.IsWellFormed(
            new WorkerRuntimeIdentitySnapshot(
                new WorkerRuntimeIdentityEvidence(
                    Target,
                    WorkerRuntimeIdentityEvidenceSource.Unavailable),
                Unavailable())));
    }

    private static ExactTargetIdentityPolicy CreatePolicy(
        IReadOnlyDictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        return ExactTargetIdentityPolicy.Create(
            SpatialAnalyzerIdentityOptions.BindAndValidate(configuration),
            Target);
    }

    private static WorkerRuntimeIdentityEvidence RuntimeVerified(string version) =>
        new(version, WorkerRuntimeIdentityEvidenceSource.RuntimeVerified);

    private static WorkerRuntimeIdentityEvidence Unavailable() =>
        new(Version: null, WorkerRuntimeIdentityEvidenceSource.Unavailable);

    private static void AssertUnavailable(ServerIdentityEvidence evidence)
    {
        Assert.Null(evidence.Version);
        Assert.Equal(ServerIdentitySource.Unavailable, evidence.Source);
        Assert.Equal(ServerIdentityMatchState.Unavailable, evidence.MatchState);
    }
}
