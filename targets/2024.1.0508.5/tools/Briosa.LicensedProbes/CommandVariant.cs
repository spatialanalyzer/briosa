using Briosa.Worker.Control;

namespace Briosa.LicensedProbes;

internal enum VariantKind
{
    // The exact sequence the shipped server sends for the request.
    Shipped,

    // The named setter is not called at all.
    OmitArgument,

    // The named setter is called with a blank value of the same SDK binding.
    BlankArgument,

    // The named setter is called with a label that differs only by letter case.
    RecaseArgumentLabel,

    // SetStep receives different step text; every argument is unchanged.
    ReplaceStepText
}

/// <summary>
/// One explicit, reviewable difference from a shipped command. A variant never
/// changes more than one setter or the step text, and never changes a binding.
/// </summary>
internal sealed record CommandVariant
{
    private CommandVariant(VariantKind kind, string? argumentLabel, string? replacement)
    {
        Kind = kind;
        ArgumentLabel = argumentLabel;
        Replacement = replacement;
    }

    public VariantKind Kind { get; }

    public string? ArgumentLabel { get; }

    public string? Replacement { get; }

    public static CommandVariant Shipped { get; } = new(VariantKind.Shipped, null, null);

    public static CommandVariant Omit(string label) => new(VariantKind.OmitArgument, Require(label), null);

    public static CommandVariant Blank(string label) => new(VariantKind.BlankArgument, Require(label), null);

    public static CommandVariant Recase(string label, string recased)
    {
        Require(label);
        Require(recased);
        if (string.Equals(label, recased, StringComparison.Ordinal) ||
            !string.Equals(label, recased, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("A recased label must differ from the shipped label only by letter case.", nameof(recased));
        }

        return new(VariantKind.RecaseArgumentLabel, label, recased);
    }

    public static CommandVariant StepText(string stepText) => new(VariantKind.ReplaceStepText, null, Require(stepText));

    public WorkerMpCommand Apply(WorkerMpCommand shipped)
    {
        ArgumentNullException.ThrowIfNull(shipped);
        return Kind switch
        {
            VariantKind.Shipped => shipped,
            VariantKind.ReplaceStepText => new WorkerMpCommand(
                shipped.OperationId, Replacement!, shipped.InputArguments, shipped.OutputArguments),
            _ => new WorkerMpCommand(
                shipped.OperationId, shipped.StepName, ReplaceArgument(shipped), shipped.OutputArguments)
        };
    }

    /// <summary>How the variant treats a shipped input label, for planning and records.</summary>
    public string TreatmentOf(string shippedLabel) =>
        !string.Equals(shippedLabel, ArgumentLabel, StringComparison.Ordinal)
            ? "value"
            : Kind switch
            {
                VariantKind.OmitArgument => "omitted",
                VariantKind.BlankArgument => "blank",
                VariantKind.RecaseArgumentLabel => "recased",
                _ => "value"
            };

    private List<WorkerMpInputArgument> ReplaceArgument(WorkerMpCommand shipped)
    {
        var matches = shipped.InputArguments.Count(argument =>
            string.Equals(argument.Name, ArgumentLabel, StringComparison.Ordinal));
        if (matches != 1)
        {
            throw new InvalidOperationException("A variant must name exactly one shipped input argument.");
        }

        var arguments = new List<WorkerMpInputArgument>(shipped.InputArguments.Count);
        foreach (var argument in shipped.InputArguments)
        {
            if (!string.Equals(argument.Name, ArgumentLabel, StringComparison.Ordinal))
            {
                arguments.Add(argument);
                continue;
            }

            switch (Kind)
            {
                case VariantKind.OmitArgument:
                    break;
                case VariantKind.BlankArgument:
                    arguments.Add(new WorkerMpInputArgument(
                        argument.Name, argument.Kind, BlankValue(argument), argument.SdkBinding));
                    break;
                case VariantKind.RecaseArgumentLabel:
                    arguments.Add(new WorkerMpInputArgument(
                        Replacement!, argument.Kind, argument.Value, argument.SdkBinding));
                    break;
                default:
                    throw new InvalidOperationException("The variant does not replace an argument.");
            }
        }

        return arguments;
    }

    // Blank keeps the SDK binding and any embedded object type; only text becomes empty.
    private static WorkerMpValue BlankValue(WorkerMpInputArgument argument) => argument.Value switch
    {
        WorkerCollectionObjectNameValue value => new WorkerCollectionObjectNameValue(string.Empty, string.Empty, value.ObjectType),
        WorkerFileReferenceValue => new WorkerFileReferenceValue(string.Empty, EmbeddedFile: false),
        WorkerTextValue => new WorkerTextValue(string.Empty),
        _ => throw new InvalidOperationException("No reviewed blank value exists for this argument kind.")
    };

    private static string Require(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value;
    }
}
