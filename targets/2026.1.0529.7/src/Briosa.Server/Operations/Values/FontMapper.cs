using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class FontMapper
{
    public static WorkerFontValue WithDefaults(Api.Font? value)
    {
        if (value is null)
        {
            return new("MS Shell Dlg", 8, new(0, 0, 0));
        }

        if (value.Size > byte.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Font size must be in 0..255.");
        }

        var color = value.Color;
        if (color is not null && (color.Red > byte.MaxValue || color.Green > byte.MaxValue || color.Blue > byte.MaxValue))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Color channels must be in 0..255.");
        }

        return new(value.HasFontName ? value.FontName : "MS Shell Dlg",
            (byte)(value.HasSize ? value.Size : 8),
            color is null ? new(0, 0, 0) : new((byte)color.Red, (byte)color.Green, (byte)color.Blue));
    }
}
