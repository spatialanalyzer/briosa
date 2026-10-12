namespace Briosa.Server.Services;

// What a direct check found for one previously observed process.
internal enum SpatialAnalyzerProcessPresence
{
    // The check could not read the process, so it found nothing either way.
    Unknown = 0,

    // The same process, by ID and start time, is still running.
    Running,

    // The process has exited. Its ID is unused, used by another program, or used by
    // a process with another start time.
    Exited
}
