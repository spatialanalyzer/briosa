using global::Briosa;
using Google.Protobuf;

namespace Briosa.Protocol.Tests;

public sealed class DiscoveryProtocolTests
{
    [Fact]
    public void DiscoveryServiceHasStableUnaryMethods()
    {
        var service = DiscoveryService.Descriptor;

        Assert.Equal("briosa.DiscoveryService", service.FullName);
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
    public void CapabilityClassificationFieldsKeepTheirReviewedNumbers()
    {
        Assert.Equal(1, ListCapabilitiesRequest.IncludeDeniedFieldNumber);
        Assert.Equal(6, ListCapabilitiesResponse.AdmissionProfileFieldNumber);
        Assert.Equal(7, ListCapabilitiesResponse.PolicyFingerprintFieldNumber);
        Assert.Equal(8, OperationCapability.RiskFlagsFieldNumber);
        Assert.Equal(9, OperationCapability.DurationClassFieldNumber);
        Assert.Equal(10, OperationCapability.ValidationStatusFieldNumber);
        Assert.Equal(11, OperationCapability.AdmissionFieldNumber);
        Assert.True(OperationCapability.Descriptor.FindFieldByNumber(8).IsPacked);
        // Retired catalog fields stay reserved.
        Assert.Null(ListCapabilitiesResponse.Descriptor.FindFieldByNumber(1));
        Assert.Null(ListCapabilitiesResponse.Descriptor.FindFieldByNumber(2));
    }

    [Fact]
    public void AnEmptyCapabilitiesRequestListsOnlyAdmittedOperations()
    {
        // Clients built before include_denied send an empty request.
        var request = ListCapabilitiesRequest.Parser.ParseFrom(Array.Empty<byte>());

        Assert.False(request.IncludeDenied);
        Assert.Empty(request.ToByteArray());
    }

    [Fact]
    public void CapabilityClassificationEnumsAreClosedWithAnUnspecifiedZero()
    {
        var capability = new OperationCapability();
        Assert.Empty(capability.RiskFlags);
        Assert.Equal(OperationDurationClass.Unspecified, capability.DurationClass);
        Assert.Equal(OperationValidationStatus.Unspecified, capability.ValidationStatus);
        Assert.Equal(OperationAdmission.Unspecified, capability.Admission);

        var expected = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [nameof(OperationRiskFlag)] =
            [
                "FILESYSTEM_METADATA", "FILESYSTEM_READ", "FILESYSTEM_WRITE", "FILESYSTEM_DELETE",
                "DESTRUCTIVE", "CODE_EXECUTION", "PHYSICAL_MOTION", "DEVICE_SESSION", "DEVICE_CONFIG",
                "INTERACTIVE_UI", "EXTERNAL_IO"
            ],
            [nameof(OperationDurationClass)] = ["QUICK", "LONG_RUNNING", "INTERACTIVE"],
            [nameof(OperationValidationStatus)] = ["NO_RECORDED_GAP", "FIXTURE_PENDING", "AT_RISK_UNVALIDATED"],
            [nameof(OperationAdmission)] =
            [
                "ADMITTED", "DENIED_PROFILE", "DENIED_FLAG", "DENIED_OVERRIDE", "DENIED_EXCLUSIVE",
                "DENIED_UNREVIEWED"
            ]
        };
        foreach (var (name, values) in expected)
        {
            var type = DiscoveryReflection.Descriptor.EnumTypes.Single(candidate => candidate.Name == name);
            var prefix = string.Concat(name.Select((character, index) =>
                index > 0 && char.IsUpper(character) ? $"_{character}" : $"{char.ToUpperInvariant(character)}"));
            Assert.Equal(
                new[] { $"{prefix}_UNSPECIFIED" }.Concat(values.Select(value => $"{prefix}_{value}")),
                type.Values.OrderBy(value => value.Number).Select(value => value.Name));
            Assert.Equal(
                Enumerable.Range(0, values.Length + 1),
                type.Values.Select(value => value.Number).Order());
        }
    }

    [Fact]
    public void CapabilityClassificationRoundTripsWithoutChangingEarlierFields()
    {
        var earlier = new OperationCapability
        {
            OperationId = "file_operations.get_working_directory",
            GrpcService = "briosa.FileOperations",
            Rpc = "GetWorkingDirectory",
            FullyQualifiedMethod = "/briosa.FileOperations/GetWorkingDirectory",
            Effect = OperationEffect.ReadOnly,
            ReplaySafety = ReplaySafety.Safe,
            ExecutionScope = OperationExecutionScope.GlobalStateRead
        };
        var classified = earlier.Clone();
        classified.RiskFlags.Add(OperationRiskFlag.FilesystemMetadata);
        classified.DurationClass = OperationDurationClass.Quick;
        classified.ValidationStatus = OperationValidationStatus.NoRecordedGap;
        classified.Admission = OperationAdmission.Admitted;

        var bytes = classified.ToByteArray();

        Assert.Equal(classified, OperationCapability.Parser.ParseFrom(bytes));
        // The added fields follow the earlier ones on the wire, so an earlier
        // reader sees the same leading bytes and skips the rest as unknown fields.
        Assert.Equal(earlier.ToByteArray(), bytes.Take(earlier.CalculateSize()));
    }

    [Fact]
    public void DiscoveryMessagesCannotExposeSensitiveOperationalDetails()
    {
        var fieldNames = GetServerInfoResponse.Descriptor.Fields.InFieldNumberOrder()
            .Concat(ListCapabilitiesRequest.Descriptor.Fields.InFieldNumberOrder())
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
