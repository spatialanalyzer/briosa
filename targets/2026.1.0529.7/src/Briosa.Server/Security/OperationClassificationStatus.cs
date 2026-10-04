namespace Briosa.Server.Security;

/// <summary>
/// Whether an operation has a complete reviewed classification row.
/// </summary>
internal enum OperationClassificationStatus
{
    /// <summary>No row, a duplicated row, or a row with an unreviewed value. Fail closed.</summary>
    Unreviewed = 0,

    /// <summary>Exactly one complete row exists.</summary>
    Reviewed
}
