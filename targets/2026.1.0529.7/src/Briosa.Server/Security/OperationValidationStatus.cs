namespace Briosa.Server.Security;

/// <summary>
/// The recorded validation gap of an operation (D1, #242). It is reported, not
/// enforced: the decision keeps at-risk and fixture-pending operations admitted
/// by their profile.
/// </summary>
internal enum OperationValidationStatus
{
    /// <summary>Not reviewed; treated as unreviewed.</summary>
    Unspecified = 0,

    /// <summary>No validation gap is recorded for the operation.</summary>
    NoRecordedGap,

    /// <summary>Formerly the <c>fixture_validation_pending</c> descriptor flag: a licensed fixture is still needed.</summary>
    FixturePending,

    /// <summary>Formerly the 2024 <c>at-risk-no-runtime-validation</c> descriptor flag.</summary>
    AtRiskUnvalidated
}
