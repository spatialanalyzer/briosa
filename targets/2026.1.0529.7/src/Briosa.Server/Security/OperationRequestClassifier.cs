using System.Collections.Frozen;
using System.Globalization;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace Briosa.Server.Security;

/// <summary>
/// The conditional options one typed request enables (#293). A request that
/// cannot be read against the reviewed table is unclassified and fails closed.
/// </summary>
internal sealed record OperationRequestClassification(
    bool IsClassified,
    IReadOnlyList<OperationConditionalOption> EnabledOptions)
{
    public static OperationRequestClassification NoOptions { get; } = new(true, []);

    public static OperationRequestClassification Unclassified { get; } = new(false, []);

    /// <summary>The first enabled option with this effect, in table order, or null.</summary>
    public OperationConditionalOption? First(OperationOptionEffect effect) =>
        EnabledOptions.FirstOrDefault(option => option.Effect == effect);
}

/// <summary>
/// Reads a typed protobuf request against the reviewed
/// <see cref="OperationConditionalOptions"/> before any mapping, reservation, or
/// dispatch (#293). It reads only field presence and the reviewed enabling
/// values; values never leave this class.
/// </summary>
internal sealed class OperationRequestClassifier
{
    private readonly FrozenDictionary<string, OperationConditionalOption[]> _options;

    public OperationRequestClassifier(IReadOnlyList<OperationConditionalOption> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var incomplete = options.FirstOrDefault(option => option is null || !option.IsComplete);
        if (options.Any(option => option is null) || incomplete is not null)
        {
            throw new InvalidOperationException(
                $"The conditional option table has an incomplete entry for '{incomplete?.OperationId}'.");
        }

        var duplicate = options
            .GroupBy(option => (option.OperationId, option.Field))
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                $"The conditional option table lists '{duplicate.Key.OperationId}' field '{duplicate.Key.Field}' more than once.");
        }

        _options = options
            .GroupBy(option => option.OperationId, StringComparer.Ordinal)
            .ToFrozenDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
    }

    /// <summary>The classifier for this target's reviewed table.</summary>
    public static OperationRequestClassifier Default { get; } = new(OperationConditionalOptions.Entries);

    /// <summary>The reviewed options of one operation, in table order.</summary>
    public IReadOnlyList<OperationConditionalOption> OptionsFor(string operationId) =>
        _options.TryGetValue(operationId, out var options) ? options : [];

    /// <summary>The operations that have at least one reviewed option.</summary>
    public IEnumerable<string> OperationIds => _options.Keys;

    /// <summary>
    /// Returns the options <paramref name="request"/> enables. An operation without
    /// reviewed options needs no request. Otherwise a missing request, a field the
    /// request type does not declare with presence, or a field of an unexpected type
    /// is unclassified.
    /// </summary>
    public OperationRequestClassification Classify(string operationId, IMessage? request)
    {
        ArgumentNullException.ThrowIfNull(operationId);
        if (!_options.TryGetValue(operationId, out var options))
        {
            return OperationRequestClassification.NoOptions;
        }

        if (request is null)
        {
            return OperationRequestClassification.Unclassified;
        }

        var enabled = new List<OperationConditionalOption>();
        foreach (var option in options)
        {
            var state = IsEnabled(option, request);
            if (state is null)
            {
                return OperationRequestClassification.Unclassified;
            }

            if (state.Value)
            {
                enabled.Add(option);
            }
        }

        return enabled.Count == 0
            ? OperationRequestClassification.NoOptions
            : new OperationRequestClassification(true, enabled);
    }

    /// <summary>
    /// True when the request enables the option, false when it does not, and null
    /// when the request cannot be read against the option.
    /// </summary>
    internal static bool? IsEnabled(OperationConditionalOption option, IMessage request)
    {
        ArgumentNullException.ThrowIfNull(option);
        ArgumentNullException.ThrowIfNull(request);

        var field = request.Descriptor.FindFieldByName(option.Field);
        if (field is null || !field.HasPresence || field.IsRepeated || field.IsMap)
        {
            return null;
        }

        var present = field.Accessor.HasValue(request);
        switch (option.Condition)
        {
            case OperationOptionCondition.WhenTrue or OperationOptionCondition.WhenFalse:
            {
                if (field.FieldType != FieldType.Bool)
                {
                    return null;
                }

                bool? sent = present
                    ? (bool)field.Accessor.GetValue(request)
                    : option.Absence switch
                    {
                        OperationOptionAbsence.SendsTrue => true,
                        OperationOptionAbsence.SendsFalse => false,
                        // The request mapping rejects the missing value before dispatch.
                        _ => null
                    };
                return sent is { } value && value == (option.Condition == OperationOptionCondition.WhenTrue);
            }

            case OperationOptionCondition.WhenPresent:
                return field.FieldType == FieldType.Message ? present : null;

            case OperationOptionCondition.WhenNonEmpty:
                return field.FieldType == FieldType.String
                    ? present && ((string)field.Accessor.GetValue(request)).Length > 0
                    : null;

            case OperationOptionCondition.WhenOneOf:
            {
                if (field.FieldType != FieldType.Enum)
                {
                    return null;
                }

                if (!present)
                {
                    return false;
                }

                var number = Convert.ToInt32(field.Accessor.GetValue(request), CultureInfo.InvariantCulture);
                // An unknown enum number is treated as enabling: fail closed.
                return field.EnumType.FindValueByNumber(number) is not { } value ||
                    option.EnablingValues.Contains(value.Name, StringComparer.Ordinal);
            }

            default:
                return null;
        }
    }
}
