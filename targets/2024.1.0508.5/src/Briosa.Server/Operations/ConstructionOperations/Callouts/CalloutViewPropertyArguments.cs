using Briosa.Server.Operations.Values;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class CalloutViewPropertyArguments
{
    public static IReadOnlyList<WorkerMpInputArgument> Create(Api.CalloutViewProperties? properties)
    {
        properties ??= new();
        return
        [
            new("Lock View Point?", WorkerMpValueKind.Logical, new WorkerBooleanValue(properties.LockViewPoint), "SetBoolArg"),
            new("Recall Working Frame?", WorkerMpValueKind.Logical, new WorkerBooleanValue(properties.RecallWorkingFrame), "SetBoolArg"),
            new("Recall Visible Layer?", WorkerMpValueKind.Logical, new WorkerBooleanValue(properties.RecallVisibleLayer), "SetBoolArg"),
            new("Callout Leader Thickness", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(properties.HasCalloutLeaderThickness ? properties.CalloutLeaderThickness : 2), "SetIntegerArg"),
            new("Callout Leader Color", WorkerMpValueKind.RgbColor,
                Color(properties.CalloutLeaderColor, 128, 128, 128), "SetColorArg"),
            new("Callout Border Thickness", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(properties.HasCalloutBorderThickness ? properties.CalloutBorderThickness : 2), "SetIntegerArg"),
            new("Callout Border Color", WorkerMpValueKind.RgbColor,
                Color(properties.CalloutBorderColor, 0, 0, 255), "SetColorArg"),
            new("Divide Text with Lines?", WorkerMpValueKind.Logical, new WorkerBooleanValue(properties.DivideTextWithLines), "SetBoolArg"),
            new("Font", WorkerMpValueKind.Font, FontMapper.WithDefaults(properties.Font), "SetFontTypeArg")
        ];
    }

    private static WorkerRgbColorValue Color(Api.Color? color, byte red, byte green, byte blue)
    {
        if (color is null)
            return new(red, green, blue);
        if (color.Red > byte.MaxValue || color.Green > byte.MaxValue || color.Blue > byte.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(color), "Color channels must be in 0..255.");
        return new((byte)color.Red, (byte)color.Green, (byte)color.Blue);
    }
}
