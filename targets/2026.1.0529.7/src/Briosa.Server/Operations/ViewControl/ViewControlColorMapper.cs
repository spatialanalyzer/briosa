using Briosa.Server.Operations.Values;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class ViewControlColorMapper
{
    public static WorkerRgbColorValue WithDefault(Api.Color? value, string fieldName)
    {
        value ??= new Api.Color { Red = byte.MaxValue };
        ColorChannelRange.Require(value, fieldName);

        return new((byte)value.Red, (byte)value.Green, (byte)value.Blue);
    }
}
