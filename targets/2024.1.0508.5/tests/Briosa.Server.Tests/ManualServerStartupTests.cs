#if BRIOSA_DEVELOPMENT_REFLECTION
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;

namespace Briosa.Server.Tests;

public sealed class ManualServerStartupTests
{
    private const string SdkErrorMetadataKey =
        "briosa-spatial-analyzer-sdk-lifecycle-error-bin";

    [Fact]
    public async Task ManualServerStartupIsInertAndLifecycleRpcErrorsAreTyped()
    {
        var workerProcessesBefore = ProcessIds("Briosa.Worker");
        var sdkProcessesBefore = ProcessIds("SpatialAnalyzerSDK");
        var port = ReserveLoopbackPort();
        using var server = Process.Start(new ProcessStartInfo
        {
            FileName = Path.Combine(AppContext.BaseDirectory, "Briosa.Server.exe"),
            WorkingDirectory = AppContext.BaseDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            Arguments = $"--Briosa:Endpoint:Port={port}"
        }) ?? throw new InvalidOperationException("The Briosa test server did not start.");
        var standardOutput = server.StandardOutput.ReadToEndAsync();
        var standardError = server.StandardError.ReadToEndAsync();
        try
        {
            await WaitForListener(server, port).ConfigureAwait(true);
            using var channel = GrpcChannel.ForAddress($"http://127.0.0.1:{port}");
            var sdk = new global::Briosa.SpatialAnalyzerSdkLifecycle
                .SpatialAnalyzerSdkLifecycleClient(channel);
            var application = new global::Briosa.SpatialAnalyzerLifecycle
                .SpatialAnalyzerLifecycleClient(channel);

            var sdkState = await sdk.GetSpatialAnalyzerSdkStateAsync(
                new global::Briosa.GetSpatialAnalyzerSdkStateRequest());
            _ = await application.GetSpatialAnalyzerStateAsync(
                new global::Briosa.GetSpatialAnalyzerStateRequest());
            var invalid = await Assert.ThrowsAsync<RpcException>(async () =>
                await sdk.ConnectToSpatialAnalyzerAsync(
                    new global::Briosa.ConnectToSpatialAnalyzerRequest
                    {
                        ExpectedSdkGeneration = 0
                    }));

            Assert.Equal(
                global::Briosa.SpatialAnalyzerSdkState.Stopped,
                sdkState.State.SdkState);
            Assert.False(sdkState.State.HasSdkGeneration);
            Assert.False(sdkState.State.ReadyForMp);
            Assert.Equal(workerProcessesBefore, ProcessIds("Briosa.Worker"));
            Assert.Equal(sdkProcessesBefore, ProcessIds("SpatialAnalyzerSDK"));
            Assert.Equal(StatusCode.InvalidArgument, invalid.StatusCode);
            var detail = global::Briosa.SpatialAnalyzerSdkLifecycleError.Parser
                .ParseFrom(Assert.Single(
                    invalid.Trailers,
                    entry => entry.Key == SdkErrorMetadataKey).ValueBytes);
            Assert.Equal(
                global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.Validation,
                detail.Kind);
            Assert.Equal("sdk-generation-required", detail.DiagnosticCode);
            Assert.Equal("ConnectToSpatialAnalyzer", detail.Rpc);
        }
        finally
        {
            if (!server.HasExited)
            {
                server.Kill(entireProcessTree: true);
                await server.WaitForExitAsync().ConfigureAwait(true);
            }

            await Task.WhenAll(standardOutput, standardError).ConfigureAwait(true);
        }
    }

    private static int[] ProcessIds(string processName) =>
        [.. Process.GetProcessesByName(processName)
            .Select(process =>
            {
                using (process)
                {
                    return process.Id;
                }
            })
            .Order()];

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

    private static async Task WaitForListener(Process server, int port)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (true)
        {
            if (server.HasExited)
            {
                throw new InvalidOperationException(
                    $"The Briosa test server exited with code {server.ExitCode}.");
            }

            using var client = new TcpClient();
            try
            {
                await client.ConnectAsync(
                    IPAddress.Loopback,
                    port,
                    timeout.Token).ConfigureAwait(true);
                return;
            }
            catch (SocketException)
            {
                await Task.Delay(
                    TimeSpan.FromMilliseconds(50),
                    timeout.Token).ConfigureAwait(true);
            }
        }
    }
}
#endif
