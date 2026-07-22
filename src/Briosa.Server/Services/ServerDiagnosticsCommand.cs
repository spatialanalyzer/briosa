using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Briosa.Server.Generated.Sa.V2026_1_0529_7.V1Alpha1;

namespace Briosa.Server.Services;

internal static class ServerDiagnosticsCommand
{
    private const string WorkerFileName = "Briosa.Worker.exe";
    private const string InteropFileName = "Briosa.SpatialAnalyzer.Interop.dll";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public static int Run(
        TextWriter output,
        string baseDirectory,
        Assembly? serverAssembly = null)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);
        var coordinates = new AssemblyServerBuildIdentityProvider(
            serverAssembly ?? typeof(Program).Assembly)
            .CreateVersionCoordinates();
        var workerPresent = File.Exists(Path.Combine(baseDirectory, WorkerFileName));
        var interopPresent = File.Exists(Path.Combine(baseDirectory, InteropFileName));
        var supportedPlatform = OperatingSystem.IsWindows() &&
            RuntimeInformation.ProcessArchitecture == Architecture.X64;
        var report = new DiagnosticsReport(
            SchemaVersion: 1,
            coordinates.BriosaVersion,
            coordinates.HasSourceRevision ? coordinates.SourceRevision : null,
            coordinates.CoreProtocolPackage,
            coordinates.TargetProtocolPackage,
            TargetCatalogMetadata.CatalogId,
            coordinates.CatalogRevision,
            coordinates.SpatialAnalyzerTarget,
            coordinates.InteropFingerprint,
            RuntimeInformation.ProcessArchitecture.ToString().ToUpperInvariant(),
            workerPresent,
            interopPresent,
            SpatialAnalyzerRequired: true,
            SpatialAnalyzerBundled: false,
            ReadyToLaunch: supportedPlatform && workerPresent && interopPresent);
        output.WriteLine(JsonSerializer.Serialize(report, JsonOptions));
        return report.ReadyToLaunch ? 0 : 2;
    }

    private sealed record DiagnosticsReport(
        [property: JsonPropertyName("schema_version")] int SchemaVersion,
        [property: JsonPropertyName("briosa_version")] string BriosaVersion,
        [property: JsonPropertyName("source_revision")] string? SourceRevision,
        [property: JsonPropertyName("core_protocol_package")] string CoreProtocolPackage,
        [property: JsonPropertyName("target_protocol_package")] string TargetProtocolPackage,
        [property: JsonPropertyName("catalog_id")] string CatalogId,
        [property: JsonPropertyName("catalog_revision")] string CatalogRevision,
        [property: JsonPropertyName("spatial_analyzer_target")] string SpatialAnalyzerTarget,
        [property: JsonPropertyName("interop_fingerprint")] string InteropFingerprint,
        [property: JsonPropertyName("process_architecture")] string ProcessArchitecture,
        [property: JsonPropertyName("worker_executable_present")] bool WorkerExecutablePresent,
        [property: JsonPropertyName("interop_assembly_present")] bool InteropAssemblyPresent,
        [property: JsonPropertyName("spatial_analyzer_required")] bool SpatialAnalyzerRequired,
        [property: JsonPropertyName("spatial_analyzer_bundled")] bool SpatialAnalyzerBundled,
        [property: JsonPropertyName("ready_to_launch")] bool ReadyToLaunch);
}
