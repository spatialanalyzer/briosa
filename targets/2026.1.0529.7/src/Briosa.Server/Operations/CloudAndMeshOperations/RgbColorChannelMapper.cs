using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.CloudAndMeshOperations;

internal static class RgbColorChannelMapper
{
    public static WorkerTextValue ToMpValue(Api.RGBColorChannel channel) => new(channel switch
    {
        Api.RGBColorChannel.Red => "Red",
        Api.RGBColorChannel.Green => "Green",
        Api.RGBColorChannel.Blue => "Blue",
        Api.RGBColorChannel.Intensity => "Intensity",
        _ => throw new ArgumentException("Unsupported RGB color channel.", nameof(channel))
    });
}
