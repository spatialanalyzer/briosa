using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace Briosa.Desktop;

public static class SupportBundle
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    public static void Export(string path, DesktopState state, IEnumerable<ActivityEntry> events,
        bool historicalUnknownOutcome, string packageDirectory)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(events);
        // Explicit field projection: never serialize the private reply, launch settings,
        // ownership file, raw protobuf, manifest, environment, or original log records.
        var info = state.Info;
        var summary = new
        {
            schemaVersion = 1, capturedAt = DateTimeOffset.UtcNow, target = DesktopProtocol.Target,
            available = state.Available, readyForMp = state.Ready, observedAt = state.ObservedAt,
            historicalUnknownOutcome,
            briosaVersion = SafeText.Version(info?.Version?.BriosaVersion),
            protocol = SafeText.Version(info?.Version?.ProtocolPackage),
            sourceRevision = Hex(info?.Version?.SourceRevision, 40),
            interopFingerprint = Fingerprint(info?.Version?.InteropFingerprint),
            packageManifestSha256 = ManifestHash(packageDirectory),
            sdk = state.Sdk?.SdkState.ToString(), connection = state.Sdk?.ConnectionState.ToString(),
            readiness = state.Sdk?.ExecutionReadinessState.ToString(), recovery = state.Sdk?.RecoveryState.ToString(),
            sdkGeneration = state.Sdk?.HasSdkGeneration == true ? state.Sdk.SdkGeneration : (int?)null,
            application = state.Application?.ApplicationState.ToString(), ownership = state.Application?.Ownership.ToString(),
            sdkIdentity = Identity(info?.ActivatedSdkIdentity), saIdentity = Identity(info?.ConnectedSpatialAnalyzerIdentity),
            diagnostic = SafeText.Code(state.Sdk?.DiagnosticCode),
            incident = state.Sdk?.LastIncident is { } incident ? new
            {
                generation = incident.SdkGeneration, termination = incident.TerminationKind.ToString(),
                disposition = incident.HasExecutionDisposition ? incident.ExecutionDisposition.ToString() : "Unavailable",
                diagnostic = SafeText.Code(incident.DiagnosticCode)
            } : null
        };
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var archive = ZipFile.Open(temporary, ZipArchiveMode.Create))
            {
                Write(archive, "status.json", summary);
                Write(archive, "activity.json", events.TakeLast(2000).ToArray());
            }
            File.Move(temporary, path, overwrite: true);
        }
        finally { File.Delete(temporary); }
    }

    private static object Identity(RuntimeIdentityEvidence? identity) => new
    {
        version = SafeText.Version(identity?.Version), source = identity?.Source.ToString(), match = identity?.MatchState.ToString()
    };

    private static string? ManifestHash(string directory)
    {
        var path = Path.Combine(directory, "manifest.json");
        if (!File.Exists(path)) return null;
        using var file = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(file));
    }

    private static string? Hex(string? text, int length) => text?.Length == length && text.All(char.IsAsciiHexDigit) ? text : null;
    private static string? Fingerprint(string? text) => text?.StartsWith("sha256:", StringComparison.Ordinal) == true && Hex(text[7..], 64) is { } hash
        ? "sha256:" + hash : null;

    private static void Write<T>(ZipArchive archive, string name, T value)
    {
        using var stream = archive.CreateEntry(name).Open();
        JsonSerializer.Serialize(stream, value, Options);
    }
}
