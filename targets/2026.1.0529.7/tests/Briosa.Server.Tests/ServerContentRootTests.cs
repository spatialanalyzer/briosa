using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Briosa.Server.Operations;
using Briosa.Server.Security;
using Microsoft.Extensions.Configuration;

namespace Briosa.Server.Tests;

public sealed class ServerContentRootTests
{
    // An unknown admission profile fails startup if this file is ever loaded.
    private const string PoisonSettings =
        """
        {
          "Briosa": {
            "Security": {
              "Operations": {
                "Profile": "poison-profile"
              }
            }
          }
        }
        """;

    [Fact]
    public async Task ServerLoadsPackageSettingsWhenLaunchedFromAForeignWorkingDirectory()
    {
        var foreignDirectory = CreatePoisonDirectory();
        try
        {
            var port = ReserveLoopbackPort();
            var server = ServerProcess.Start(foreignDirectory, port);
            await using var serverScope = server.ConfigureAwait(true);

            await server.WaitForListenerAsync(port).ConfigureAwait(true);
            var (profile, admittedCount) = ReadPackagePolicy();
            // The packaged admission profile, not a cwd-supplied one, is active.
            await server.WaitForOutputAsync(string.Create(
                CultureInfo.InvariantCulture,
                $"AdmissionProfile={profile} AdmittedOperationCount={admittedCount} ")).ConfigureAwait(true);

            Assert.Equal("standard", profile);
            Assert.True(admittedCount > 1);
            Assert.False(server.HasExited);
        }
        finally
        {
            Directory.Delete(foreignDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ExplicitContentRootOverrideStillSelectsTheConfiguration()
    {
        var overrideDirectory = CreatePoisonDirectory();
        try
        {
            var port = ReserveLoopbackPort();
            var server = ServerProcess.Start(
                AppContext.BaseDirectory,
                port,
                $"--contentRoot={overrideDirectory}");
            await using var serverScope = server.ConfigureAwait(true);

            var exitCode = await server.WaitForExitAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(true);

            Assert.NotEqual(0, exitCode);
        }
        finally
        {
            Directory.Delete(overrideDirectory, recursive: true);
        }
    }

    private static string CreatePoisonDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"briosa-content-root-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "appsettings.json"), PoisonSettings);
        return directory;
    }

    private static (string Profile, int AdmittedCount) ReadPackagePolicy()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"))
            .Build();
        var policy = OperationPolicy.Create(configuration, SpatialAnalyzerApi.Operations);
        return (policy.Profile.Name, policy.AllowedOperations.Count);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "TcpListener is deterministically stopped in the finally block.")]
    private static int ReserveLoopbackPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    private sealed class ServerProcess : IAsyncDisposable
    {
        private readonly Process _process;
        private readonly StringBuilder _output = new();
        private readonly Lock _outputLock = new();

        private ServerProcess(Process process)
        {
            _process = process;
        }

        public bool HasExited => _process.HasExited;

        public static ServerProcess Start(
            string workingDirectory,
            int port,
            params string[] additionalArguments)
        {
            var startInfo = new ProcessStartInfo(
                Path.Combine(AppContext.BaseDirectory, "Briosa.Server.exe"))
            {
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            startInfo.ArgumentList.Add($"--Briosa:Endpoint:Port={port}");
            startInfo.ArgumentList.Add("--Briosa:Desktop:Mode=Disabled");
            startInfo.ArgumentList.Add("--Briosa:Logging:File:Enabled=false");
            foreach (var argument in additionalArguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            foreach (var inheritedKey in new[]
            {
                "ASPNETCORE_CONTENTROOT",
                "DOTNET_CONTENTROOT",
                "ASPNETCORE_URLS",
                "URLS",
                "ASPNETCORE_HTTP_PORTS",
                "HTTP_PORTS",
                "ASPNETCORE_HTTPS_PORTS",
                "HTTPS_PORTS"
            })
            {
                startInfo.Environment.Remove(inheritedKey);
            }

            var process = new Process { StartInfo = startInfo };
            var server = new ServerProcess(process);
            process.OutputDataReceived += (_, args) => server.Append(args.Data);
            process.ErrorDataReceived += (_, args) => server.Append(args.Data);
            try
            {
                Assert.True(process.Start());
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                return server;
            }
            catch
            {
                process.Dispose();
                throw;
            }
        }

        public async Task WaitForListenerAsync(int port)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            while (true)
            {
                Assert.False(_process.HasExited, $"The server exited during startup.{Environment.NewLine}{Output}");
                using var client = new TcpClient();
                try
                {
                    await client.ConnectAsync(IPAddress.Loopback, port, timeout.Token).ConfigureAwait(true);
                    return;
                }
                catch (SocketException)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(50), timeout.Token).ConfigureAwait(true);
                }
            }
        }

        public async Task WaitForOutputAsync(string expected)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            try
            {
                while (!Output.Contains(expected, StringComparison.Ordinal))
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(50), timeout.Token).ConfigureAwait(true);
                }
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested)
            {
                Assert.Fail($"The server did not log '{expected}'.{Environment.NewLine}{Output}");
            }
        }

        public async Task<int> WaitForExitAsync(TimeSpan timeout)
        {
            using var cancellation = new CancellationTokenSource(timeout);
            await _process.WaitForExitAsync(cancellation.Token).ConfigureAwait(true);
            return _process.ExitCode;
        }

        public async ValueTask DisposeAsync()
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
            }

            await _process.WaitForExitAsync().ConfigureAwait(true);
            _process.Dispose();
        }

        private string Output
        {
            get
            {
                lock (_outputLock)
                {
                    return _output.ToString();
                }
            }
        }

        private void Append(string? line)
        {
            if (line is null)
            {
                return;
            }

            lock (_outputLock)
            {
                _output.AppendLine(line);
            }
        }
    }
}
