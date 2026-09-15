using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json.Nodes;
using Briosa.Desktop;

namespace Briosa.ControlCenter.Smoke;

internal static class PackageSmoke
{
    // Only a disposable extracted package and the repository's fake worker are accepted by the caller.
    public static async Task<int> RunAsync(string package, string worker)
    {
        package = Path.GetFullPath(package);
        worker = Path.GetFullPath(worker);
        if (Path.GetFileName(worker) != "Briosa.Worker.TestHost.exe" || !File.Exists(worker)) throw new ArgumentException("A built fake worker is required.");
        if (DesktopStore.Find(package) is not null) throw new InvalidOperationException("The test package already has a server.");
        var settingsPath = Path.Combine(package, "appsettings.json");
        var original = await File.ReadAllTextAsync(settingsPath);
        const string workerVariable = "Briosa__Worker__ExecutablePath";
        var previousWorker = Environment.GetEnvironmentVariable(workerVariable);
        // Override inherited configuration as well as the disposable file: this harness must use its fake.
        Environment.SetEnvironmentVariable(workerVariable, worker);
        var settings = JsonNode.Parse(original)!;
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        settings["Briosa"]!["Endpoint"]!["Port"] = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        settings["Briosa"]!["Worker"]!["ExecutablePath"] = worker;
        await File.WriteAllTextAsync(settingsPath, settings.ToJsonString());
        string? instance = null;
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        try
        {
            using (var first = new DesktopSession(package))
            {
                await first.StartAsync(timeout.Token);
                instance = first.Instance;
                Require(first.State.CanManage && first.State.Available && !first.State.Ready, "Owned API startup must leave SDK stopped.");
                Require(first.State.Sdk?.SdkState == SpatialAnalyzerSdkState.Stopped, "SDK started implicitly.");
                var monitor = await DesktopProtocol.SendAsync(new(instance!), timeout.Token);
                Require(!monitor.CanManage, "Status granted ownership.");
                var rejected = await DesktopProtocol.SendAsync(new(instance!, DesktopAction.StopServer, DesktopProtocol.NewCredential()), timeout.Token);
                Require(!rejected.Accepted, "Wrong ownership credential accepted.");
                await first.ActAsync(DesktopAction.StartSdk, timeout.Token);
                Require(first.State.Sdk?.HasSdkGeneration == true, "Fake SDK did not start.");
                // No Connect or MP request is made. Application observation is read-only.
            }
            using (var reopened = new DesktopSession(package))
            {
                Require(reopened.Instance == instance, "Reopening lost the existing server.");
                await reopened.RefreshAsync(timeout.Token);
                Require(reopened.State.CanManage, "Reopening did not recover ownership.");
                await reopened.RestartAsync(timeout.Token);
                Require(reopened.Instance != instance, "Restart reused the old server instance.");
                Require(DesktopStore.ReadExit(instance!) is { Clean: true }, "Graceful exit was not recorded.");
                instance = reopened.Instance;
                Require(reopened.State.Sdk?.SdkState == SpatialAnalyzerSdkState.Stopped, "Restart implicitly started SDK.");
                await reopened.StopAsync(timeout.Token);
                Require(!reopened.HasServer && DesktopStore.Find(package) is null, "Stop did not confirm process exit.");
            }
            Console.WriteLine("Packaged desktop ownership, fake SDK startup, detach/reopen, restart, and graceful shutdown passed.");
            return 0;
        }
        finally
        {
            // Cleanup only the exact test process proven by its own registration; never a name-wide kill.
            if (instance is not null && DesktopStore.ReadRegistration(instance) is { } registration && DesktopStore.IsAlive(registration))
            {
                using var process = Process.GetProcessById(registration.ProcessId);
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(CancellationToken.None);
            }
            await File.WriteAllTextAsync(settingsPath, original);
            Environment.SetEnvironmentVariable(workerVariable, previousWorker);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
