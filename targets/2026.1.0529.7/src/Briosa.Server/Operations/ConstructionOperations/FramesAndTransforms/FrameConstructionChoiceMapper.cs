using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class FrameConstructionChoiceMapper
{
    public static WorkerChoiceValue<WorkerAxisIdentifierValue> Axis(Api.AxisIdentifier value)
    {
        if (value == Api.AxisIdentifier.Unspecified || !Enum.IsDefined(value))
            throw new ArgumentException("A supported axis is required.", nameof(value));
        return new((WorkerAxisIdentifierValue)((int)value - 1));
    }

    public static WorkerChoiceValue<WorkerAxisIdentifierValue> FrameAxis(Api.FrameAxis value)
    {
        if (value == Api.FrameAxis.Unspecified || !Enum.IsDefined(value))
            throw new ArgumentException("A supported frame axis is required.", nameof(value));
        return new((WorkerAxisIdentifierValue)(((int)value - 1) * 2));
    }

    public static string Method(Api.FrameConstructionMethod value) => (int)value switch
    {
        1 => "Origin X XY",
        2 => "Origin X XZ",
        3 => "Origin Y Yx",
        4 => "Origin Y YZ",
        5 => "Origin Z Zx",
        6 => "Origin X Zy",
        _ => throw new ArgumentException("A supported frame construction method is required.", nameof(value))
    };
}
