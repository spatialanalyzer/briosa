using Briosa.Server.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Briosa.Server.Tests;

public sealed class BriosaLoggingTests
{
    [Fact]
    public void DefaultLoggingUsesOnlyTheContainedNonPrivilegedProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddBriosaLogging());

        using var provider = services.BuildServiceProvider();
        var loggingProviders = provider.GetServices<ILoggerProvider>().ToArray();

        Assert.Single(loggingProviders);
        Assert.IsType<BriosaLogProvider>(loggingProviders[0]);
        Assert.DoesNotContain(
            loggingProviders,
            item => item.GetType().FullName?.Contains(
                "EventLog",
                StringComparison.OrdinalIgnoreCase) == true);
    }
}
