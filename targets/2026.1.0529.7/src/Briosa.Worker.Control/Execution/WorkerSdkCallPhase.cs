namespace Briosa.Worker.Control;

/// <summary>
/// The MP sequence phase in which one SDK call faulted. Each phase proves a
/// different execution disposition. <see cref="None"/> is never valid, so a
/// missing or unknown phase on the private channel fails closed instead of
/// claiming that execution did not start.
/// </summary>
public enum WorkerSdkCallPhase
{
    /// <summary>Not a phase; a result carrying it is rejected.</summary>
    None = 0,

    /// <summary>SetStep or an input setter faulted; ExecuteStep was never called.</summary>
    BeforeExecute = 1,

    /// <summary>ExecuteStep faulted; the MP may have started.</summary>
    ExecuteStep = 2,

    /// <summary>GetMPStepResult faulted after ExecuteStep returned true.</summary>
    MpResultRetrieval = 3,

    /// <summary>An output getter faulted after retrieved MP code 2.</summary>
    OutputGetter = 4
}
