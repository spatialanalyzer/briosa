using System.Text.Json.Serialization;

namespace Briosa.Worker.Control;

// The private protocol uses numeric discriminators. All reachable types are
// generated at build time; channel serialization has no reflection fallback.
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow)]
[JsonSerializable(typeof(WorkerControlMessage))]
internal sealed partial class WorkerControlJsonContext : JsonSerializerContext;
