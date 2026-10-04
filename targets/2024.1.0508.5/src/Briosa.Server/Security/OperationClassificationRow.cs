namespace Briosa.Server.Security;

/// <summary>
/// One reviewed row of <see cref="OperationClassification"/>.
/// </summary>
internal sealed record OperationClassificationRow(
    string OperationId,
    OperationRisks Risks,
    OperationDurationClass Duration,
    OperationValidationStatus ValidationStatus,
    OperationIsolationClass Isolation)
{
    /// <summary>
    /// True when every field holds a reviewed value: risks only from the
    /// vocabulary and no <c>Unspecified</c> class or status.
    /// </summary>
    public bool IsComplete =>
        (Risks & ~OperationRiskVocabulary.All) == OperationRisks.None &&
        Duration is OperationDurationClass.Quick or OperationDurationClass.LongRunning or
            OperationDurationClass.Interactive &&
        ValidationStatus is OperationValidationStatus.NoRecordedGap or
            OperationValidationStatus.FixturePending or OperationValidationStatus.AtRiskUnvalidated &&
        Isolation is OperationIsolationClass.Admissible or OperationIsolationClass.ExclusiveWorkflow;
}
