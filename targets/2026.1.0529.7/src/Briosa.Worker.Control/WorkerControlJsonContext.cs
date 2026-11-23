using System.Text.Json.Serialization;

namespace Briosa.Worker.Control;

// The private protocol uses numeric discriminators. All reachable types are
// generated at build time; channel serialization has no reflection fallback.
// Strings are written in segments so encoding scratch stays bounded; the bytes
// are the same as a single string write.
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    Converters = [typeof(WorkerControlStringConverter)])]
[JsonSerializable(typeof(WorkerControlMessage))]
internal sealed partial class WorkerControlJsonContext : JsonSerializerContext;
