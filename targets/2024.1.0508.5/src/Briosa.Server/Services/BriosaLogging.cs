using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Briosa.Server.Services;

internal static class BriosaLogging
{
    /// <summary>
    /// Keeps the default server log sink usable by an ordinary Windows user.
    /// The Windows Event Log provider can require privileges that a client-
    /// launched Briosa process does not have, and provider failures must never
    /// replace a typed gRPC operation outcome.
    /// </summary>
    public static ILoggingBuilder AddBriosaLogging(
        this ILoggingBuilder logging, IConfiguration? configuration = null)
    {
        ArgumentNullException.ThrowIfNull(logging);
        configuration ??= new ConfigurationBuilder().Build();
        var options = BriosaLoggingOptions.Bind(configuration);
        logging.ClearProviders();
        var levels = configuration.GetSection("Logging:LogLevel").GetChildren()
            .ToDictionary(section => section.Key, section => BriosaLoggingOptions.ParseLevel(section.Value),
                StringComparer.OrdinalIgnoreCase);
        // Capture startup filtering; logging policy is not reloaded.
        logging.Services.PostConfigure<LoggerFilterOptions>(filters =>
        {
            filters.Rules.Clear();
            filters.MinLevel = levels.GetValueOrDefault("Default", LogLevel.Information);
            filters.Rules.Add(new LoggerFilterRule(null, "Microsoft", LogLevel.Warning, null));
            foreach (var (category, level) in levels)
            {
                if (!category.Equals("Default", StringComparison.OrdinalIgnoreCase))
                    filters.Rules.Add(new LoggerFilterRule(null, category, level, null));
            }
        });
        logging.Services.AddSingleton(options);
        logging.Services.TryAddSingleton<BriosaLogHealth>();
        logging.Services.AddSingleton<ILoggerProvider, BriosaLogProvider>();
        return logging;
    }
}
