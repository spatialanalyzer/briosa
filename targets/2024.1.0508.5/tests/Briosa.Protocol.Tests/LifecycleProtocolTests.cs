using global::Briosa;

namespace Briosa.Protocol.Tests;

public sealed class LifecycleProtocolTests
{
    [Fact]
    public void LifecycleServicesRetainTheDocumentedMethodSurface()
    {
        Assert.Equal(
            [
                "GetSpatialAnalyzerState",
                "LaunchSpatialAnalyzer",
                "CloseOwnedSpatialAnalyzer"
            ],
            SpatialAnalyzerLifecycle.Descriptor.Methods.Select(method => method.Name));
        Assert.Equal(
            [
                "GetSpatialAnalyzerSdkState",
                "StartSpatialAnalyzerSdk",
                "ConnectToSpatialAnalyzer",
                "ReconnectToSpatialAnalyzer",
                "StopSpatialAnalyzerSdk",
                "RecoverSpatialAnalyzerSdk"
            ],
            SpatialAnalyzerSdkLifecycle.Descriptor.Methods.Select(method => method.Name));
    }

    [Fact]
    public void LaunchRequestExposesOnlyControlledSpatialAnalyzerInputs()
    {
        var fields = LaunchSpatialAnalyzerRequest.Descriptor.Fields
            .InFieldNumberOrder()
            .Select(field => (field.Name, field.FieldNumber))
            .ToArray();

        Assert.Equal(
            [
                ("job_file_path", 1),
                ("quick_start_instrument_name", 2),
                ("start_minimized", 3)
            ],
            fields);
        Assert.Equal(
            "initial_content",
            Assert.Single(LaunchSpatialAnalyzerRequest.Descriptor.Oneofs).Name);
    }

    [Fact]
    public void LifecycleTransitionsUseGenerationGuards()
    {
        Assert.Equal(
            1,
            CloseOwnedSpatialAnalyzerRequest.Descriptor
                .FindFieldByName("expected_application_generation")
                .FieldNumber);
        AssertGenerationGuard<ConnectToSpatialAnalyzerRequest>();
        AssertGenerationGuard<ReconnectToSpatialAnalyzerRequest>();
        AssertGenerationGuard<StopSpatialAnalyzerSdkRequest>();
        AssertGenerationGuard<RecoverSpatialAnalyzerSdkRequest>();
    }

    private static void AssertGenerationGuard<T>()
        where T : Google.Protobuf.IMessage<T>, new() =>
        Assert.Equal(
            1,
            new T().Descriptor.FindFieldByName("expected_sdk_generation").FieldNumber);
}
