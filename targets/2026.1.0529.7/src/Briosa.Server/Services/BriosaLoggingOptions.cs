using System.Globalization;
using Briosa.Server.Operations;

namespace Briosa.Server.Services;

internal sealed record BriosaLoggingOptions
{
    public bool ConsoleEnabled { get; init; } = true;
    public bool FileEnabled { get; init; } = true;
    public string Directory { get; init; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Briosa", "logs", SpatialAnalyzerApi.TargetVersion);
    public int MaxFileSizeMiB { get; init; } = 20;
    public int RetainedFileCount { get; init; } = 10;
    public int MaxAgeDays { get; init; } = 7;
    public int MaxTotalSizeMiB { get; init; } = 200;
    public int QueueCapacity { get; init; } = 4096;
    public int ShutdownTimeoutMilliseconds { get; init; } = 2000;

    public static BriosaLoggingOptions Bind(IConfiguration configuration)
    {
        try
        {
            var section = configuration.GetSection("Briosa:Logging");
            var defaults = new BriosaLoggingOptions();
            var options = defaults with
            {
                ConsoleEnabled = section.GetValue("ConsoleEnabled", true),
                FileEnabled = section.GetValue("File:Enabled", true),
                Directory = section["File:Directory"] ?? defaults.Directory,
                MaxFileSizeMiB = section.GetValue("File:MaxFileSizeMiB", 20),
                RetainedFileCount = section.GetValue("File:RetainedFileCount", 10),
                MaxAgeDays = section.GetValue("File:MaxAgeDays", 7),
                MaxTotalSizeMiB = section.GetValue("File:MaxTotalSizeMiB", 200),
                QueueCapacity = section.GetValue("QueueCapacity", 4096),
                ShutdownTimeoutMilliseconds = section.GetValue("ShutdownTimeoutMilliseconds", 2000)
            };
            options.Validate();
            return options;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or FormatException)
        {
            throw new InvalidOperationException("Invalid Briosa logging configuration.");
        }
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Directory) || !Path.IsPathFullyQualified(Directory) ||
            Directory.Any(char.IsControl) ||
            MaxFileSizeMiB is < 1 or > 1024 || RetainedFileCount is < 1 or > 1000 ||
            MaxAgeDays is < 1 or > 365 || MaxTotalSizeMiB < MaxFileSizeMiB ||
            MaxTotalSizeMiB > 10240 || QueueCapacity is < 16 or > 65536 ||
            ShutdownTimeoutMilliseconds is < 1 or > 10000)
        {
            throw new InvalidOperationException("Invalid Briosa logging configuration.");
        }
    }

    public static LogLevel ParseLevel(string? value)
    {
        if (value is null || !Enum.TryParse<LogLevel>(value, true, out var level) ||
            !Enum.IsDefined(level) || int.TryParse(value, CultureInfo.InvariantCulture, out _))
        {
            throw new InvalidOperationException("Invalid logging level.");
        }
        return level;
    }
}
