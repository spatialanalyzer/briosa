using System.Text.Json;
using System.Text.Json.Serialization;

namespace Briosa.Worker.Control;

/// <summary>
/// Writes long strings in segments, so the JSON writer's scratch for escaping
/// and transcoding stays proportional to one segment rather than to the whole
/// value. The encoded bytes are identical to a single string write, and reading
/// is unchanged.
/// </summary>
internal sealed class WorkerControlStringConverter : JsonConverter<string>
{
    /// <summary>
    /// Characters per segment. A segment that escapes every character (6 bytes
    /// each) asks the buffer for at most 1,024 * 6 * 3 + 3 = 18,435 bytes, well
    /// within one <see cref="BoundedFrameBuffer.ChunkBytes"/> chunk.
    /// </summary>
    internal const int SegmentLength = 1024;

    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.GetString();

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length <= SegmentLength)
        {
            writer.WriteStringValue(value);
            return;
        }

        var remaining = value.AsSpan();
        while (remaining.Length > SegmentLength)
        {
            writer.WriteStringValueSegment(remaining[..SegmentLength], isFinalSegment: false);
            remaining = remaining[SegmentLength..];
        }

        writer.WriteStringValueSegment(remaining, isFinalSegment: true);
    }
}
