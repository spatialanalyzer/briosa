using System.Text.Json;
using Briosa.Server.Services;

namespace Briosa.Server.Tests;

public sealed class ServerDiagnosticsCommandTests
{
    [Fact]
    public void CompletePackageReportsSafeBuildIdentity()
    {
        var directory = CreatePackageDirectory(includeRequiredFiles: true);
        try
        {
            using var output = new StringWriter();

            var exitCode = ServerDiagnosticsCommand.Run(
                output,
                directory,
                typeof(Program).Assembly);

            using var report = JsonDocument.Parse(output.ToString());
            var root = report.RootElement;
            Assert.Equal(0, exitCode);
            Assert.Equal(1, root.GetProperty("schema_version").GetInt32());
            Assert.Equal("2026.1.0529.7", root.GetProperty("spatial_analyzer_target").GetString());
            Assert.Equal("2", root.GetProperty("catalog_revision").GetString());
            Assert.True(root.GetProperty("worker_executable_present").GetBoolean());
            Assert.True(root.GetProperty("interop_assembly_present").GetBoolean());
            Assert.True(root.GetProperty("spatial_analyzer_required").GetBoolean());
            Assert.False(root.GetProperty("spatial_analyzer_bundled").GetBoolean());
            Assert.Equal(OperatingSystem.IsWindows(), root.GetProperty("ready_to_launch").GetBoolean());
            Assert.DoesNotContain(directory, output.ToString(), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("license_key", output.ToString(), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("hostname", output.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void MissingWorkerFailsWithoutStartingHostOrSdk()
    {
        var directory = CreatePackageDirectory(includeRequiredFiles: false);
        try
        {
            using var output = new StringWriter();

            var exitCode = ServerDiagnosticsCommand.Run(
                output,
                directory,
                typeof(Program).Assembly);

            using var report = JsonDocument.Parse(output.ToString());
            Assert.Equal(2, exitCode);
            Assert.False(report.RootElement
                .GetProperty("worker_executable_present")
                .GetBoolean());
            Assert.False(report.RootElement.GetProperty("ready_to_launch").GetBoolean());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreatePackageDirectory(bool includeRequiredFiles)
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            $"briosa-diagnostics-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        File.WriteAllText(
            Path.Combine(directory, "Briosa.SpatialAnalyzer.Interop.dll"),
            string.Empty);
        if (includeRequiredFiles)
        {
            File.WriteAllText(
                Path.Combine(directory, "Briosa.Worker.exe"),
                string.Empty);
        }

        return directory;
    }
}
