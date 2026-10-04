namespace Briosa.Server.Security;

/// <summary>
/// The reviewed execution-duration class of an operation (D1, #242). There is no
/// default class: <see cref="Unspecified"/> means the row is unreviewed.
/// </summary>
internal enum OperationDurationClass
{
    /// <summary>Not reviewed; treated as unreviewed.</summary>
    Unspecified = 0,

    /// <summary>Completes within the default execution watchdog.</summary>
    Quick,

    /// <summary>Device actions, sessions, motion, and heavy document operations.</summary>
    LongRunning,

    /// <summary>Waits for an unbounded amount of operator time.</summary>
    Interactive
}
