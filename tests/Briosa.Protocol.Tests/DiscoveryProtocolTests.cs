using Briosa.Core.V1Alpha1;

namespace Briosa.Protocol.Tests;

public sealed class DiscoveryProtocolTests
{
    [Fact]
    public void DiscoveryServiceHasStableUnaryMethods()
    {
        var service = DiscoveryService.Descriptor;

        Assert.Equal("briosa.core.v1alpha1.DiscoveryService", service.FullName);
        Assert.Collection(
            service.Methods,
            method =>
            {
                Assert.Equal("GetServerInfo", method.Name);
                Assert.Equal(GetServerInfoRequest.Descriptor, method.InputType);
                Assert.Equal(GetServerInfoResponse.Descriptor, method.OutputType);
            },
            method =>
            {
                Assert.Equal("ListCapabilities", method.Name);
                Assert.Equal(ListCapabilitiesRequest.Descriptor, method.InputType);
                Assert.Equal(ListCapabilitiesResponse.Descriptor, method.OutputType);
            });
    }

    [Fact]
    public void ConnectedVersionPresenceIsDistinctFromVerificationState()
    {
        var response = new GetServerInfoResponse
        {
            ConnectedSpatialAnalyzerVersionState =
                ConnectedSpatialAnalyzerVersionState.Unavailable
        };

        Assert.False(response.HasConnectedSpatialAnalyzerVersion);
        Assert.Equal(
            ConnectedSpatialAnalyzerVersionState.Unavailable,
            response.ConnectedSpatialAnalyzerVersionState);

        response.ConnectedSpatialAnalyzerVersion = "2026.1.0529.7";

        Assert.True(response.HasConnectedSpatialAnalyzerVersion);
    }

    [Fact]
    public void RuntimeIdentitiesKeepEvidenceSourceAndMatchStateIndependent()
    {
        var response = new GetServerInfoResponse
        {
            ActivatedSdkIdentity = new RuntimeIdentityEvidence
            {
                Version = "2025.0",
                Source = RuntimeIdentityEvidenceSource.RuntimeVerification,
                MatchState = RuntimeIdentityMatchState.Mismatch
            },
            ConnectedSpatialAnalyzerIdentity = new RuntimeIdentityEvidence
            {
                Version = "2026.1.0529.7",
                Source = RuntimeIdentityEvidenceSource.OperatorAttestation,
                MatchState = RuntimeIdentityMatchState.ExactMatch
            }
        };

        Assert.Equal(
            RuntimeIdentityEvidenceSource.RuntimeVerification,
            response.ActivatedSdkIdentity.Source);
        Assert.Equal(
            RuntimeIdentityMatchState.Mismatch,
            response.ActivatedSdkIdentity.MatchState);
        Assert.Equal(
            RuntimeIdentityEvidenceSource.OperatorAttestation,
            response.ConnectedSpatialAnalyzerIdentity.Source);
        Assert.Equal(
            RuntimeIdentityMatchState.ExactMatch,
            response.ConnectedSpatialAnalyzerIdentity.MatchState);
    }

    [Fact]
    public void AttachmentAndExecutionReadinessAreIndependentStates()
    {
        var response = new GetServerInfoResponse
        {
            SpatialAnalyzerConnectionState = SpatialAnalyzerConnectionState.Connected,
            SpatialAnalyzerExecutionReadinessState =
                SpatialAnalyzerExecutionReadinessState.Unverified,
            ReadyForMp = false
        };

        Assert.Equal(
            SpatialAnalyzerConnectionState.Connected,
            response.SpatialAnalyzerConnectionState);
        Assert.Equal(
            SpatialAnalyzerExecutionReadinessState.Unverified,
            response.SpatialAnalyzerExecutionReadinessState);
        Assert.False(response.ReadyForMp);
    }

    [Fact]
    public void CapabilityReportsReviewedReplaySafety()
    {
        var capability = new OperationCapability
        {
            OperationId = "file_operations.get_working_directory",
            ReplaySafety = ReplaySafety.Safe
        };

        Assert.Equal(ReplaySafety.Safe, capability.ReplaySafety);
    }

    [Fact]
    public void DiscoveryReportsTargetIsolationAndOperationExecutionScope()
    {
        var server = new GetServerInfoResponse
        {
            TargetIsolationMode = TargetIsolationMode.SingleTenant
        };
        var capability = new OperationCapability
        {
            ExecutionScope = OperationExecutionScope.GlobalStateRead
        };

        Assert.Equal(TargetIsolationMode.SingleTenant, server.TargetIsolationMode);
        Assert.Equal(
            OperationExecutionScope.GlobalStateRead,
            capability.ExecutionScope);
    }

    [Fact]
    public void DiscoveryMessagesCannotExposeSensitiveOperationalDetails()
    {
        var fieldNames = GetServerInfoResponse.Descriptor.Fields.InFieldNumberOrder()
            .Concat(ListCapabilitiesResponse.Descriptor.Fields.InFieldNumberOrder())
            .Concat(OperationCapability.Descriptor.Fields.InFieldNumberOrder())
            .Select(field => field.Name)
            .ToArray();
        var prohibitedFragments = new[]
        {
            "host",
            "port",
            "process",
            "license",
            "credential",
            "diagnostic",
            "status_code"
        };

        Assert.All(
            fieldNames,
            field => Assert.DoesNotContain(
                prohibitedFragments,
                fragment => field.Contains(fragment, StringComparison.OrdinalIgnoreCase)));
    }
}
