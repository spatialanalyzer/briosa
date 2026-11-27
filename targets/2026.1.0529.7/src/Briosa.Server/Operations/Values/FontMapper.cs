using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class FontMapper
{
    public static WorkerFontValue WithDefaults(Api.Font? value, string fieldName)
    {
        if (value is null)
        {
            return new("MS Shell Dlg", 8, new(0, 0, 0));
        }

        if (value.Size > byte.MaxValue)
        {
            throw RequestFieldViolation.Range(RequestFieldViolation.Path(fieldName, "size"));
        }

        var color = value.Color;
        if (color is not null)
        {
            ColorChannelRange.Require(color, RequestFieldViolation.Path(fieldName, "color"));
        }

        return new(value.HasFontName ? value.FontName : "MS Shell Dlg",
            (byte)(value.HasSize ? value.Size : 8),
            color is null ? new(0, 0, 0) : new((byte)color.Red, (byte)color.Green, (byte)color.Blue));
    }
}
