using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class ViewControlColorMapper
{
    public static WorkerRgbColorValue WithDefault(Api.Color? value)
    {
        value ??= new Api.Color { Red = byte.MaxValue };
        if (value.Red > byte.MaxValue || value.Green > byte.MaxValue || value.Blue > byte.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Color channels must be in 0..255.");
        }

        return new((byte)value.Red, (byte)value.Green, (byte)value.Blue);
    }
}
