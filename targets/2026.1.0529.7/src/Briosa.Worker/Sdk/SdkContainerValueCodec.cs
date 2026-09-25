using Briosa.Worker.Control;
using System.Runtime.InteropServices;

namespace Briosa.Worker.Sdk;

// The exact SpatialAnalyzer Automation contract requires rectangular double[4,4]
// arrays. A jagged-array substitution would change the COM VARIANT shape.
#pragma warning disable CA1814
internal static class SdkContainerValueCodec
{
    public const int TransformElementCount = 16;

    public static object EmptyArrayBuffer() =>
        new VariantWrapper(Array.Empty<object>());

    public static object DoubleArrayBuffer(int size) =>
        new VariantWrapper(new double[size]);

    public static object TransformBuffer() =>
        new VariantWrapper(new double[4, 4]);

    public static object ToDoubleArrayComValue(WorkerDoubleArrayValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new VariantWrapper(value.Values.ToArray());
    }

    public static object ToEditTextComValue(WorkerStringListValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new VariantWrapper(value.Values.Select(item => (object)item).ToArray());
    }

    public static object ToTransformComValue(WorkerTransformValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Values.Count != TransformElementCount)
        {
            throw new ArgumentException(
                "A SpatialAnalyzer transform must contain exactly 16 values.",
                nameof(value));
        }

        var matrix = new double[4, 4];
        for (var index = 0; index < TransformElementCount; index++)
        {
            matrix[index / 4, index % 4] = value.Values[index];
        }

        return new VariantWrapper(matrix);
    }

    public static bool TryParseDoubleArray(
        object value,
        int reportedSize,
        out WorkerDoubleArrayValue? result)
    {
        if (reportedSize < 0 ||
            value is not double[] values ||
            values.Length != reportedSize)
        {
            result = null;
            return false;
        }

        result = new WorkerDoubleArrayValue(values);
        return true;
    }

    public static bool TryParseEditText(object value, out WorkerStringListValue? result)
    {
        if (value is not object[] values ||
            values.Any(item => item is not string))
        {
            result = null;
            return false;
        }

        result = new WorkerStringListValue([.. values.Cast<string>()]);
        return true;
    }

    public static bool TryParseTransform(object value, out WorkerTransformValue? result)
    {
        if (value is not double[,] matrix ||
            matrix.GetLength(0) != 4 ||
            matrix.GetLength(1) != 4)
        {
            result = null;
            return false;
        }

        var values = new double[TransformElementCount];
        for (var index = 0; index < TransformElementCount; index++)
        {
            values[index] = matrix[index / 4, index % 4];
        }

        result = new WorkerTransformValue(values);
        return true;
    }
}
#pragma warning restore CA1814
