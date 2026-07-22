using Briosa.Server.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Briosa.Server.Tests;

public sealed class WorkerProcessRegistrationTests
{
    [Theory]
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
}
