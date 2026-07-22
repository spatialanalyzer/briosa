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
