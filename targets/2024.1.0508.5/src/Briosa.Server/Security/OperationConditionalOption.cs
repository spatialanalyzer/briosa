namespace Briosa.Server.Security;

/// <summary>
/// What a caller-controlled request option changes when it is enabled (#293).
/// </summary>
internal enum OperationOptionEffect
{
    /// <summary>Not reviewed; never valid in a table row.</summary>
    Unspecified = 0,

    /// <summary>
    /// The option opens or blocks on operator UI. The request gains
    /// <see cref="OperationRisks.InteractiveUi"/> and the
    /// <see cref="OperationDurationClass.Interactive"/> duration class.
    /// </summary>
    InteractiveUi,

    /// <summary>
    /// The option lets the call return while device or operator work keeps
    /// running. The request becomes an exclusive workflow, which nothing admits.
    /// </summary>
    BackgroundWork
}

/// <summary>How a reviewed conditional option entry came about (#293).</summary>
internal enum OperationOptionTrigger
{
    /// <summary>Not reviewed; never valid in a table row.</summary>
    Unspecified = 0,

    /// <summary>The caller turns the behavior on with a request option.</summary>
    CallerOption,

    /// <summary>
    /// The breaking release flipped Briosa's default for this option, so the
    /// behavior now needs the caller to ask for it.
    /// </summary>
    DefaultFlip
}

/// <summary>Which request values enable a conditional option.</summary>
internal enum OperationOptionCondition
{
    /// <summary>Not reviewed; never valid in a table row.</summary>
    Unspecified = 0,

    /// <summary>A Boolean option set to <c>true</c>.</summary>
    WhenTrue,

    /// <summary>A Boolean option set to <c>false</c>.</summary>
    WhenFalse,

    /// <summary>A message option, such as a file reference, that is present at all.</summary>
    WhenPresent,

    /// <summary>A string option that is present and not empty.</summary>
    WhenNonEmpty,

    /// <summary>An enum option set to one of the row's listed protobuf value names.</summary>
    WhenOneOf
}

/// <summary>What Briosa sends when the caller omits the option.</summary>
internal enum OperationOptionAbsence
{
    /// <summary>Not reviewed; never valid in a table row.</summary>
    Unspecified = 0,

    /// <summary>Briosa sends <c>true</c>.</summary>
    SendsTrue,

    /// <summary>Briosa sends <c>false</c>.</summary>
    SendsFalse,

    /// <summary>Briosa sends nothing, or an empty value, that does not enable the option.</summary>
    SendsNothing,

    /// <summary>The option is required; the request mapping rejects its absence.</summary>
    Rejected
}

/// <summary>
/// One reviewed (operation, request option) pair whose value changes the
/// operation's effective classification. Transcribed from
/// <c>eng/classification/conditional-ui.json</c> and
/// <c>eng/classification/conditional-background.json</c>.
/// </summary>
internal sealed record OperationConditionalOption(
    string OperationId,
    string Field,
    string MpArgument,
    OperationOptionEffect Effect,
    OperationOptionTrigger Trigger,
    OperationOptionCondition Condition,
    OperationOptionAbsence Absence,
    params string[] EnablingValues)
{
    /// <summary>
    /// True when every field holds a reviewed value, the absence fits the
    /// condition, and enabling values are listed exactly for
    /// <see cref="OperationOptionCondition.WhenOneOf"/>.
    /// </summary>
    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(OperationId) &&
        !string.IsNullOrWhiteSpace(Field) &&
        !string.IsNullOrWhiteSpace(MpArgument) &&
        Effect is OperationOptionEffect.InteractiveUi or OperationOptionEffect.BackgroundWork &&
        Trigger is OperationOptionTrigger.CallerOption or OperationOptionTrigger.DefaultFlip &&
        Condition switch
        {
            OperationOptionCondition.WhenTrue or OperationOptionCondition.WhenFalse =>
                Absence is OperationOptionAbsence.SendsTrue or OperationOptionAbsence.SendsFalse or
                    OperationOptionAbsence.Rejected,
            OperationOptionCondition.WhenPresent or OperationOptionCondition.WhenNonEmpty or
                OperationOptionCondition.WhenOneOf =>
                Absence is OperationOptionAbsence.SendsNothing or OperationOptionAbsence.Rejected,
            _ => false
        } &&
        (Condition == OperationOptionCondition.WhenOneOf) == (EnablingValues.Length > 0) &&
        EnablingValues.All(value => !string.IsNullOrWhiteSpace(value));
}
