namespace Briosa.Server.Workers;

internal enum WorkerTerminationKind
{
    None,
    Graceful,
    Crash,
    Forced
}
