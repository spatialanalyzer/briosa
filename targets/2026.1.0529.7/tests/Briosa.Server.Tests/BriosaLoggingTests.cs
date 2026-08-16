using Briosa.Server.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Briosa.Server.Tests;

public sealed class BriosaLoggingTests
{
    [Fact]
    public void DefaultLoggingUsesOnlyTheNonPrivilegedConsoleProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddBriosaLogging());

        using var provider = services.BuildServiceProvider();
        var loggingProviders = provider.GetServices<ILoggerProvider>().ToArray();

        Assert.Single(loggingProviders);
        Assert.IsType<ConsoleLoggerProvider>(loggingProviders[0]);
        Assert.DoesNotContain(
            loggingProviders,
            item => item.GetType().FullName?.Contains(
                "EventLog",
                StringComparison.OrdinalIgnoreCase) == true);
    }
}
