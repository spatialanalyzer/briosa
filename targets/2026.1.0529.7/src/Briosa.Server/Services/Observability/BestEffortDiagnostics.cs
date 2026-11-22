using System.Diagnostics.CodeAnalysis;

namespace Briosa.Server.Services;

/// <summary>
/// Lifecycle timing records are diagnostics only (#371). A failing clock or logger is
/// contained here, so the record can never skip cleanup, replace an RPC's response, or
/// mask the exception the operation itself raised. Like the supervisor, it lets
/// OutOfMemoryException through.
/// </summary>
[SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Justification = "Diagnostics must not change the outcome of the lifecycle operation they describe.")]
internal static class BestEffortDiagnostics
{
    /// <summary>Elapsed milliseconds reported when the time could not be measured.</summary>
    internal const long Unmeasured = -1;

    public static long? Timestamp(TimeProvider timeProvider)
    {
        try
        {
            return timeProvider.GetTimestamp();
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return null;
        }
    }

    public static long ElapsedMilliseconds(TimeProvider timeProvider, long? startTimestamp)
    {
        if (startTimestamp is not { } start) return Unmeasured;
        try
        {
            return (long)timeProvider.GetElapsedTime(start).TotalMilliseconds;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return Unmeasured;
        }
    }

    public static void Record(Action record)
    {
        try
        {
            record();
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // The record is lost; the operation it describes is unaffected.
        }
    }
}
