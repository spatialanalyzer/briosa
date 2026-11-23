using System.Text.Json;
using System.Text.Json.Serialization;

namespace Briosa.Worker.Control;

/// <summary>
/// Writes long strings in segments, so the JSON writer's scratch for escaping
/// and transcoding stays proportional to one segment rather than to the whole
/// value. The encoded bytes are identical to a single string write, and reading
/// is unchanged.
/// </summary>
/// <remarks>
/// The writer rents escaping scratch inside a string write and returns it only
/// when the write returns. Each write is therefore marked on the frame being
/// encoded (<see cref="BoundedFrameBuffer.Current"/>), which does not throw during
/// it, and an over-budget frame is rejected here, after the write.
/// </remarks>
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
        var frame = BoundedFrameBuffer.Current;
        if (value.Length <= SegmentLength)
        {
            Write(writer, frame, value, isFinalSegment: null);
            return;
        }

        var remaining = value.AsSpan();
        while (remaining.Length > SegmentLength)
        {
            Write(writer, frame, remaining[..SegmentLength], isFinalSegment: false);
            remaining = remaining[SegmentLength..];
        }

        Write(writer, frame, remaining, isFinalSegment: true);
    }

    private static void Write(
        Utf8JsonWriter writer,
        BoundedFrameBuffer? frame,
        ReadOnlySpan<char> value,
        bool? isFinalSegment)
    {
        frame?.BeginStringWrite();
        try
        {
            if (isFinalSegment is { } isFinal)
            {
                writer.WriteStringValueSegment(value, isFinal);
            }
            else
            {
                writer.WriteStringValue(value);
            }
        }
        finally
        {
            frame?.EndStringWrite();
        }

        // The writer has returned its scratch; stop before the next segment.
        if (frame is { Exceeded: true })
        {
            throw new InvalidDataException("The worker control message exceeds the size limit.");
        }
    }
}
