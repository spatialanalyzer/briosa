namespace Briosa.Server.Security;

/// <summary>
/// The reviewed isolation class of an operation (D1, #242).
/// <see cref="OperationPolicy"/> treats <see cref="ExclusiveWorkflow"/> as the
/// operation's effective execution scope and denies it under every profile,
/// flag, and override.
/// </summary>
internal enum OperationIsolationClass
{
    /// <summary>Not reviewed; treated as unreviewed.</summary>
    Unspecified = 0,

    /// <summary>Completes within one RPC; a profile or override may admit it.</summary>
    Admissible,

    /// <summary>
    /// Starts, stops, or depends on a session that spans RPCs. No profile or
    /// override may admit it until a lease design is accepted (invariant 13).
    /// </summary>
    ExclusiveWorkflow
}
