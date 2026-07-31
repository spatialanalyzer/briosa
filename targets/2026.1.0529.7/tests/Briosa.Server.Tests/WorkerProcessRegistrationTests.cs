using Briosa.Server.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Briosa.Worker.Control;
using ServerIdentitySource = Briosa.Server.Workers.RuntimeIdentityEvidenceSource;

namespace Briosa.Server.Tests;

public sealed class WorkerProcessRegistrationTests
{
    [Theory]
    [InlineData("")]
    [InlineData("not-a-duration")]
    [InlineData("00:00:00")]
    [InlineData("-00:00:01")]
    [InlineData("00:11:00")]
    public void InvalidExecutionWatchdogConfigurationFailsStartup(string value)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Briosa:Worker:ExecutionWatchdogTimeout"] = value
            })
            .Build();
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddWorkerProcessLifecycle(configuration));

        Assert.Contains(
            "Briosa:Worker:ExecutionWatchdogTimeout",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ValidExecutionWatchdogConfigurationRegistersSupervisor()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Briosa:Worker:ExecutionWatchdogTimeout"] = "00:00:00.250"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddWorkerProcessLifecycle(configuration);

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(WorkerProcessSupervisor));
    }

    [Fact]
    public void OmittedConfigurationBindsExistingDefaultsOnce()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        services.AddWorkerProcessLifecycle(configuration);
        using var provider = services.BuildServiceProvider();
        var worker = provider.GetRequiredService<WorkerProcessOptions>();
        var spatialAnalyzer =
            provider.GetRequiredService<SpatialAnalyzerConnectionOptions>();

        Assert.Equal(
            Path.Combine(AppContext.BaseDirectory, "Briosa.Worker.exe"),
            worker.ExecutablePath);
        Assert.Equal(TimeSpan.FromSeconds(30), worker.ExecutionWatchdogTimeout);
        Assert.Equal("localhost", spatialAnalyzer.Host);
        Assert.Null(spatialAnalyzer.Identity.ActivatedSdk);
        Assert.Null(spatialAnalyzer.Identity.ConnectedSpatialAnalyzer);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" localhost")]
    [InlineData("localhost\rforged")]
    public void InvalidSpatialAnalyzerHostFailsStartup(string value)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [SpatialAnalyzerConnectionOptions.HostKey] = value
            })
            .Build();
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddWorkerProcessLifecycle(configuration));

        Assert.Contains(
            SpatialAnalyzerConnectionOptions.HostKey,
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ExplicitMissingWorkerExecutableFailsStartupWithoutEchoingPath()
    {
        var configuredPath = $"missing-worker-{Guid.NewGuid():N}.exe";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [WorkerProcessOptions.ExecutablePathKey] = configuredPath
            })
            .Build();
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddWorkerProcessLifecycle(configuration));

        Assert.Contains(
            WorkerProcessOptions.ExecutablePathKey,
            exception.Message,
            StringComparison.Ordinal);
        Assert.DoesNotContain(configuredPath, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ExplicitExistingWorkerExecutableIsResolvedAtStartup()
    {
        var configuredPath = Environment.ProcessPath!;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [WorkerProcessOptions.ExecutablePathKey] = configuredPath
            })
            .Build();
        var services = new ServiceCollection();

        services.AddWorkerProcessLifecycle(configuration);
        using var provider = services.BuildServiceProvider();

        Assert.Equal(
            Path.GetFullPath(configuredPath),
            provider.GetRequiredService<WorkerProcessOptions>().ExecutablePath);
    }

    [Fact]
    public void PartialRuntimeIdentityAttestationFailsStartupRegistration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ExactTargetIdentityPolicy.ActivatedSdkVersionKey] = "2026.1.0529.7"
            })
            .Build();
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddWorkerProcessLifecycle(configuration));

        Assert.Contains(
            ExactTargetIdentityPolicy.ActivatedSdkReferenceKey,
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void InvalidAttestationDiagnosticDoesNotEchoConfiguredValues()
    {
        const string version = "2026.1.0529.7\rforged";
        const string reference = "sensitive-evidence-reference";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ExactTargetIdentityPolicy.ActivatedSdkVersionKey] = version,
                [ExactTargetIdentityPolicy.ActivatedSdkReferenceKey] = reference
            })
            .Build();
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddWorkerProcessLifecycle(configuration));

        Assert.Contains(
            ExactTargetIdentityPolicy.ActivatedSdkVersionKey,
            exception.Message,
            StringComparison.Ordinal);
        Assert.DoesNotContain(version, exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(reference, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void IndependentAttestationConfigurationPreservesMissingClaim()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ExactTargetIdentityPolicy.ActivatedSdkVersionKey] = "2026.1.0529.7",
                [ExactTargetIdentityPolicy.ActivatedSdkReferenceKey] =
                    "deployment-record:sdk"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddWorkerProcessLifecycle(configuration);
        using var provider = services.BuildServiceProvider();
        var policy = provider.GetRequiredService<ExactTargetIdentityPolicy>();
        var identity = policy.Evaluate(new WorkerRuntimeIdentitySnapshot(
            new WorkerRuntimeIdentityEvidence(
                Version: null,
                WorkerRuntimeIdentityEvidenceSource.Unavailable),
            new WorkerRuntimeIdentityEvidence(
                Version: null,
                WorkerRuntimeIdentityEvidenceSource.Unavailable)));

        Assert.Equal(
            ServerIdentitySource.OperatorAttestation,
            identity.ActivatedSdk.Source);
        Assert.Equal(
            ServerIdentitySource.Unavailable,
            identity.ConnectedSpatialAnalyzer.Source);
        Assert.False(identity.AllowsExecution);
    }
}
