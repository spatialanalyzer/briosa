namespace Briosa.Server.Security;

/// <summary>
/// The intended isolation class of an operation (D1, #242). It records the review
/// outcome only; the registered descriptor's execution scope still governs
/// admission until the reclassification step of #293.
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
