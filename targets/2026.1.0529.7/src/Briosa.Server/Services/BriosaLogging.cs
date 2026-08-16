namespace Briosa.Server.Services;

internal static class BriosaLogging
{
    /// <summary>
    /// Keeps the default server log sink usable by an ordinary Windows user.
    /// The Windows Event Log provider can require privileges that a client-
    /// launched Briosa process does not have, and provider failures must never
    /// replace a typed gRPC operation outcome.
    /// </summary>
    public static ILoggingBuilder AddBriosaLogging(this ILoggingBuilder logging)
    {
        ArgumentNullException.ThrowIfNull(logging);
        logging.ClearProviders();
        logging.AddSimpleConsole();
        return logging;
    }
}
