using Briosa.Server.Services;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

#if BRIOSA_DEVELOPMENT_REFLECTION
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Briosa.Server.Operations;
using Google.Protobuf.Reflection;
using Grpc.Core;
using Grpc.Health.V1;
using Grpc.Net.Client;
using Grpc.Reflection.V1Alpha;
using Api = Briosa;
#endif

namespace Briosa.Server.Tests;

public sealed class DevelopmentGrpcReflectionTests
{
    [Fact]
    public void ReflectionRequiresTheCompileTimeAndRuntimeDevelopmentGates()
    {
        var development = new TestHostEnvironment(Environments.Development);
        var production = new TestHostEnvironment(Environments.Production);

#if BRIOSA_DEVELOPMENT_REFLECTION
        Assert.True(DevelopmentGrpcReflectionHosting.IsEnabled(development));
#else
        Assert.False(DevelopmentGrpcReflectionHosting.IsEnabled(development));
#endif
        Assert.False(DevelopmentGrpcReflectionHosting.IsEnabled(production));
    }

#if BRIOSA_DEVELOPMENT_REFLECTION
    [Fact]
    public async Task DevelopmentHostReflectsEveryMappedServiceAndPreservesAdmissionPolicy()
    {
        var host = await RunningServer.StartAsync(Environments.Development)
            .ConfigureAwait(true);
        await using var configuredHost = host.ConfigureAwait(true);
        var reflection = new ServerReflection.ServerReflectionClient(host.Channel);

        var services = await ListServicesAsync(reflection).ConfigureAwait(true);
        Assert.Contains("grpc.reflection.v1alpha.ServerReflection", services);

        var expectedMethods = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["grpc.health.v1.Health"] = ["Check", "Watch"],
            ["briosa.DiscoveryService"] =
                ["GetServerInfo", "ListCapabilities"]
        };
        foreach (var service in SpatialAnalyzerApi.Operations
            .GroupBy(operation => operation.GrpcService, StringComparer.Ordinal))
        {
            expectedMethods.Add(
                service.Key,
                service.Select(operation => operation.Rpc)
                    .Order(StringComparer.Ordinal)
                    .ToArray());
        }

        foreach (var (service, methods) in expectedMethods)
        {
            Assert.Contains(service, services);
            var reflectedMethods = await DescribeServiceMethodsAsync(reflection, service)
                .ConfigureAwait(true);
            Assert.Equal(methods.Order(StringComparer.Ordinal), reflectedMethods);
        }

        var health = new Health.HealthClient(host.Channel);
        var liveness = await health.CheckAsync(new HealthCheckRequest
        {
            Service = "briosa.liveness"
        }).ResponseAsync.ConfigureAwait(true);
        var readiness = await health.CheckAsync(new HealthCheckRequest
        {
            Service = "briosa.readiness"
        }).ResponseAsync.ConfigureAwait(true);
        Assert.Equal(HealthCheckResponse.Types.ServingStatus.Serving, liveness.Status);
        Assert.Equal(HealthCheckResponse.Types.ServingStatus.NotServing, readiness.Status);

        var discovery = new Api.DiscoveryService.DiscoveryServiceClient(host.Channel);
        var serverInfo = await discovery.GetServerInfoAsync(
            new Api.GetServerInfoRequest()).ResponseAsync.ConfigureAwait(true);
        Assert.Equal("2026.1.0529.7", serverInfo.Version.SpatialAnalyzerTarget);
        Assert.False(serverInfo.ReadyForMp);
        Assert.Equal(
            Api.RuntimeIdentityEvidenceSource.Unavailable,
            serverInfo.ActivatedSdkIdentity.Source);
        Assert.Equal(
            Api.RuntimeIdentityEvidenceSource.Unavailable,
            serverInfo.ConnectedSpatialAnalyzerIdentity.Source);
        var capabilities = await discovery.ListCapabilitiesAsync(
            new Api.ListCapabilitiesRequest()).ResponseAsync.ConfigureAwait(true);
        Assert.Equal(
            SpatialAnalyzerApi.Operations
                .Select(operation => operation.OperationId)
                .Order(StringComparer.Ordinal),
            capabilities.Operations
                .Select(operation => operation.OperationId)
                .Order(StringComparer.Ordinal));
        Assert.DoesNotContain(
            capabilities.Operations,
            operation => operation.Effect == Api.OperationEffect.Mutating);

        var fileOperations = new Api.FileOperations.FileOperationsClient(host.Channel);
        var unavailable = await Assert.ThrowsAsync<RpcException>(async () =>
            await fileOperations.GetWorkingDirectoryAsync(
                new Api.GetWorkingDirectoryRequest()).ResponseAsync.ConfigureAwait(true))
            .ConfigureAwait(true);
        Assert.Equal(StatusCode.Unavailable, unavailable.StatusCode);
    }

    [Fact]
    public async Task NonDevelopmentHostDoesNotMapReflection()
    {
        var host = await RunningServer.StartAsync(Environments.Production)
            .ConfigureAwait(true);
        await using var configuredHost = host.ConfigureAwait(true);
        var reflection = new ServerReflection.ServerReflectionClient(host.Channel);

        var exception = await Assert.ThrowsAsync<RpcException>(async () =>
            await ListServicesAsync(reflection).ConfigureAwait(true)).ConfigureAwait(true);

        Assert.Equal(StatusCode.Unimplemented, exception.StatusCode);
    }

    private static async Task<string[]> ListServicesAsync(
        ServerReflection.ServerReflectionClient client)
    {
        var response = await ReflectAsync(
            client,
            new ServerReflectionRequest { ListServices = string.Empty }).ConfigureAwait(true);
        Assert.NotNull(response.ListServicesResponse);
        return response.ListServicesResponse.Service
            .Select(service => service.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static async Task<string[]> DescribeServiceMethodsAsync(
        ServerReflection.ServerReflectionClient client,
        string serviceName)
    {
        var response = await ReflectAsync(
            client,
            new ServerReflectionRequest { FileContainingSymbol = serviceName }).ConfigureAwait(true);
        Assert.NotNull(response.FileDescriptorResponse);
        var service = response.FileDescriptorResponse.FileDescriptorProto
            .Select(FileDescriptorProto.Parser.ParseFrom)
            .SelectMany(file => file.Service.Select(descriptor => (file.Package, descriptor)))
            .Single(candidate => string.Equals(
                $"{candidate.Package}.{candidate.descriptor.Name}",
                serviceName,
                StringComparison.Ordinal));
        return service.descriptor.Method
            .Select(method => method.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static async Task<ServerReflectionResponse> ReflectAsync(
        ServerReflection.ServerReflectionClient client,
        ServerReflectionRequest request)
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var call = client.ServerReflectionInfo(cancellationToken: cancellation.Token);
        await call.RequestStream.WriteAsync(request, cancellation.Token).ConfigureAwait(true);
        await call.RequestStream.CompleteAsync().ConfigureAwait(true);
        Assert.True(await call.ResponseStream.MoveNext(cancellation.Token).ConfigureAwait(true));
        return call.ResponseStream.Current;
    }

    private sealed class RunningServer : IAsyncDisposable
    {
        private readonly Process _process;
        private readonly StringBuilder _output;

        private RunningServer(Process process, GrpcChannel channel, StringBuilder output)
        {
            _process = process;
            Channel = channel;
            _output = output;
        }

        public GrpcChannel Channel { get; }

        public static async Task<RunningServer> StartAsync(string environmentName)
        {
            var repositoryRoot = FindRepositoryRoot();
            var serverOutput = Path.Combine(
                repositoryRoot.FullName,
                "src",
                "Briosa.Server",
                "bin",
                "Debug",
                "net10.0-windows");
            var executable = Path.Combine(serverOutput, "Briosa.Server.exe");
            Assert.True(File.Exists(executable), $"Debug server was not built at '{executable}'.");
            var smokeWorker = Path.Combine(
                repositoryRoot.FullName,
                "tests",
                "Briosa.SmokeWorker",
                "bin",
                "Debug",
                "net10.0-windows",
                "Briosa.SmokeWorker.exe");
            Assert.True(
                File.Exists(smokeWorker),
                $"Portable smoke worker was not built at '{smokeWorker}'.");

            var port = ReserveLoopbackPort();
            var output = new StringBuilder();
            var startInfo = new ProcessStartInfo(executable)
            {
                WorkingDirectory = serverOutput,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            startInfo.ArgumentList.Add($"--Briosa:Endpoint:Port={port}");
            startInfo.ArgumentList.Add($"--Briosa:Worker:ExecutablePath={smokeWorker}");
            startInfo.ArgumentList.Add(
                "--Briosa:SpatialAnalyzer:Identity:ActivatedSdk:OperatorAttestation:Version=");
            startInfo.ArgumentList.Add(
                "--Briosa:SpatialAnalyzer:Identity:ActivatedSdk:OperatorAttestation:Reference=");
            startInfo.ArgumentList.Add(
                "--Briosa:SpatialAnalyzer:Identity:ConnectedSpatialAnalyzer:OperatorAttestation:Version=");
            startInfo.ArgumentList.Add(
                "--Briosa:SpatialAnalyzer:Identity:ConnectedSpatialAnalyzer:OperatorAttestation:Reference=");
            startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = environmentName;
            startInfo.Environment["DOTNET_ENVIRONMENT"] = environmentName;
            startInfo.Environment["BRIOSA_TEST_WORKER_SCENARIO"] = "disconnected";
            foreach (var inheritedKey in new[]
            {
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
            process.OutputDataReceived += (_, args) => AppendOutput(output, args.Data);
            process.ErrorDataReceived += (_, args) => AppendOutput(output, args.Data);
            Assert.True(process.Start());
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            try
            {
                await WaitForListenerAsync(process, port, output).ConfigureAwait(true);
                var channel = GrpcChannel.ForAddress($"http://127.0.0.1:{port}");
                return new RunningServer(process, channel, output);
            }
            catch
            {
                Stop(process);
                throw;
            }
        }

        public ValueTask DisposeAsync()
        {
            Channel.Dispose();
            Stop(_process);
            GC.KeepAlive(_output);
            return ValueTask.CompletedTask;
        }

        private static void AppendOutput(StringBuilder output, string? line)
        {
            if (line is null)
            {
                return;
            }

            lock (output)
            {
                output.AppendLine(line);
            }
        }

        private static async Task WaitForListenerAsync(
            Process process,
            int port,
            StringBuilder output)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            while (!timeout.IsCancellationRequested)
            {
                if (process.HasExited)
                {
                    Assert.Fail($"Server exited before listening. Output:{Environment.NewLine}{output}");
                }

                using var client = new TcpClient();
                try
                {
                    await client.ConnectAsync(IPAddress.Loopback, port, timeout.Token)
                        .ConfigureAwait(true);
                    return;
                }
                catch (SocketException)
                {
                    await Task.Delay(50, timeout.Token).ConfigureAwait(true);
                }
            }

            Assert.Fail($"Server did not listen before timeout. Output:{Environment.NewLine}{output}");
        }

        private static int ReserveLoopbackPort()
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }

        private static void Stop(Process process)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(10_000);
            }

            process.Dispose();
        }

        private static DirectoryInfo FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Briosa.slnx")))
            {
                directory = directory.Parent;
            }

            return directory ?? throw new InvalidOperationException("Could not locate the repository root.");
        }
    }
#endif

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "Briosa.Server.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
