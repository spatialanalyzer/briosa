using Briosa.Server.Security;
using Api = global::Briosa;

namespace Briosa.Server.Services;

/// <summary>
/// Describes the reviewed <see cref="OperationConditionalOptions"/> of one
/// operation for discovery (#397): the request field, its effect, what turns it
/// on, what omitting it does, and the active policy's decision for a request that
/// turns on that option alone. It describes; the server still decides every
/// actual request.
/// </summary>
internal static class RequestOptionDescription
{
    /// <summary>
    /// Returns the operation's described options. The container is present for
    /// every operation and empty when no option applies.
    /// </summary>
    public static Api.OperationRequestOptions Describe(OperationPolicy policy, string operationId)
    {
        ArgumentNullException.ThrowIfNull(policy);
        var options = new Api.OperationRequestOptions();
        foreach (var (option, decision) in policy.EvaluateRequestOptions(operationId))
        {
            options.Options.Add(Describe(option, decision));
        }

        return options;
    }

    internal static Api.OperationRequestOption Describe(
        OperationConditionalOption option,
        OperationPolicyDecision decision)
    {
        ArgumentNullException.ThrowIfNull(option);
        ArgumentNullException.ThrowIfNull(decision);
        var described = new Api.OperationRequestOption
        {
            FieldName = option.Field,
            Effect = ToEffect(option.Effect),
            Condition = ToCondition(option.Condition),
            Omission = ToOmission(option),
            Admission = ServerDiscoveryService.ToAdmission(decision),
            PolicyDenial = decision.Denial?.ToDetails()
        };
        described.EnablingEnumValues.AddRange(option.EnablingValues);
        return described;
    }

    internal static Api.OperationRequestOptionEffect ToEffect(OperationOptionEffect effect) => effect switch
    {
        OperationOptionEffect.InteractiveUi => Api.OperationRequestOptionEffect.InteractiveUi,
        OperationOptionEffect.BackgroundWork => Api.OperationRequestOptionEffect.BackgroundWork,
        _ => throw new ArgumentOutOfRangeException(nameof(effect), effect, "The request option effect is not reviewed.")
    };

    internal static Api.OperationRequestOptionCondition ToCondition(OperationOptionCondition condition) =>
        condition switch
        {
            OperationOptionCondition.WhenTrue => Api.OperationRequestOptionCondition.WhenTrue,
            OperationOptionCondition.WhenFalse => Api.OperationRequestOptionCondition.WhenFalse,
            OperationOptionCondition.WhenPresent => Api.OperationRequestOptionCondition.WhenPresent,
            OperationOptionCondition.WhenNonEmpty => Api.OperationRequestOptionCondition.WhenNonEmpty,
            OperationOptionCondition.WhenOneOf => Api.OperationRequestOptionCondition.WhenOneOf,
            _ => throw new ArgumentOutOfRangeException(nameof(condition), condition,
                "The request option condition is not reviewed.")
        };

    /// <summary>
    /// What omitting the field does, in public terms rather than the mapper's
    /// <see cref="OperationOptionAbsence"/>. An omitted Boolean is decided by the
    /// value Briosa sends instead; an omitted message, string, or enum turns
    /// nothing on; a required field fails request validation.
    /// </summary>
    internal static Api.OperationRequestOptionOmission ToOmission(OperationConditionalOption option)
    {
        ArgumentNullException.ThrowIfNull(option);
        return option.Absence switch
        {
            OperationOptionAbsence.Rejected => Api.OperationRequestOptionOmission.Required,
            OperationOptionAbsence.SendsNothing => Api.OperationRequestOptionOmission.DoesNotActivate,
            OperationOptionAbsence.SendsTrue => option.Condition == OperationOptionCondition.WhenTrue
                ? Api.OperationRequestOptionOmission.Activates
                : Api.OperationRequestOptionOmission.DoesNotActivate,
            OperationOptionAbsence.SendsFalse => option.Condition == OperationOptionCondition.WhenFalse
                ? Api.OperationRequestOptionOmission.Activates
                : Api.OperationRequestOptionOmission.DoesNotActivate,
            _ => throw new ArgumentOutOfRangeException(nameof(option), option.Absence,
                "The request option absence is not reviewed.")
        };
    }
}
