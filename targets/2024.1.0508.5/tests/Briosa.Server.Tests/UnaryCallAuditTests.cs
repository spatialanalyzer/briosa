using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Briosa.Server.Operations;
using Briosa.Server.Services;
using Google.Protobuf.Reflection;
using Grpc.AspNetCore.Server;
using Grpc.Core;
using Grpc.Health.V1;
using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

// #450: every unary call records its arrival, its handler's exit and the end of its
// response at Debug, so a stalled conformance fixture's server log shows which call
// arrived and which was answered.
public sealed class UnaryCallAuditTests
{
    private const string Sentinel = "unary-audit-sentinel-argument";
    private static readonly string[] AllowedProperties =
        ["SchemaVersion", "SourceContext", "EventId", "ServerInstanceId", "SpatialAnalyzerTarget", "Rpc", "OperationId",
            "GrpcStatus", "ElapsedMilliseconds", "TraceId", "SpanId"];
    private static readonly string[] CallSequence = ["UnaryCallStarted", "UnaryCallCompleted", "UnaryCallResponseEnded"];
    private static readonly string[] EventNames =
        ["UnaryCallStarted", "UnaryCallCompleted", "UnaryCallFaulted", "UnaryCallResponseEnded"];

    // A new RPC is recorded by the interceptor without any edit; this fails when one
    // has no name that the conformance runner's digest prints.
    [Fact]
    public void EveryUnaryRpcHasOneIdentityThatTheConformanceDigestPrints()
    {
        var operations = SpatialAnalyzerApi.Operations.ToDictionary(
            operation => operation.FullyQualifiedMethod, operation => operation.OperationId, StringComparer.Ordinal);
        var digestRpcs = DigestRpcNames();
        var unary = UnaryMethods();
        Assert.Contains("/briosa.DiscoveryService/GetServerInfo", unary);
        Assert.Contains("/grpc.health.v1.Health/Check", unary);
        foreach (var method in unary)
        {
            var (rpc, operationId) = UnaryCallAuditInterceptor.Identify(method);
            if (operations.TryGetValue(method, out var expected))
            {
                Assert.Null(rpc);
                Assert.Equal(expected, operationId);
                continue;
            }

            Assert.Null(operationId);
            Assert.NotNull(rpc);
            Assert.True(digestRpcs.Contains(rpc),
                $"Add '{rpc}' ({method}) to the Rpc list of the conformance runner's digest in both targets.");
        }

        Assert.All(operations.Keys, method => Assert.Contains(method, unary));
        Assert.Equal((UnaryCallAuditInterceptor.UnrecognizedRpc, (string?)null),
            UnaryCallAuditInterceptor.Identify("/briosa.Other/Not-An-Identifier"));
        Assert.Equal((UnaryCallAuditInterceptor.UnrecognizedRpc, (string?)null),
            UnaryCallAuditInterceptor.Identify(null));
    }

    [Fact]
    public void TheAuditInterceptorIsGlobalAndNoServiceOptsOut()
    {
        using var provider = new ServiceCollection().AddLogging().AddBriosaGrpc().BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<GrpcServiceOptions>>().Value;
        Assert.Contains(options.Interceptors, registration => registration.Type == typeof(UnaryCallAuditInterceptor));
        Assert.NotNull(provider.GetService<UnaryCallAuditInterceptor>());

        // The server registers gRPC only through AddBriosaGrpc, and no service opts
        // out of its global interceptors.
        var sources = Directory.EnumerateFiles(Path.Combine(TargetRoot(), "src", "Briosa.Server"), "*.cs",
                SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase) && !path.Contains(
                $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Assert.Contains(sources, path => Path.GetFileName(path) == "Program.cs" &&
            File.ReadAllText(path).Contains("builder.Services.AddBriosaGrpc();", StringComparison.Ordinal));
        Assert.All(sources, path =>
        {
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("IgnoreGlobalInterceptors", text, StringComparison.Ordinal);
            if (Path.GetFileName(path) != "PublicGrpcHosting.cs")
                Assert.DoesNotContain(".AddGrpc(", text, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task TheServerRecordsEachUnaryCallAtDebugWithoutArgumentsOrPaths()
    {
        var directory = Path.Combine(Path.GetTempPath(), "briosa-unary-audit-" + Guid.NewGuid().ToString("N"));
        try
        {
            var calls = await RunServerAsync(Path.Combine(directory, "debug"), debug: true).ConfigureAwait(true);
            // Discovery, state reads, health, a denied MP operation and an admitted one
            // that no worker serves: each has exactly one record of each kind.
            var expected = new (string? Rpc, string? OperationId, string Status)[]
            {
                ("GetServerInfo", null, "OK"),
                ("ListCapabilities", null, "OK"),
                ("GetSpatialAnalyzerSdkState", null, "OK"),
                ("GetSpatialAnalyzerState", null, "OK"),
                ("Check", null, "OK"),
                (null, "file_operations.get_working_directory", "PermissionDenied"),
                (null, "variables.get_double_variable", calls.DoubleVariableStatus)
            };
            var records = calls.Records.Where(record => EventNames.Contains(record.Name)).ToArray();
            Assert.Equal(expected.Length * 3, records.Length);
            Assert.DoesNotContain(records, record => record.Name == "UnaryCallFaulted");
            foreach (var (rpc, operationId, status) in expected)
            {
                var call = records.Where(record => record.Rpc == rpc && record.OperationId == operationId).ToArray();
                Assert.Equal(CallSequence, call.Select(record => record.Name));
                Assert.All(call, record => Assert.Equal("Debug", record.Level));
                Assert.All(call.Skip(1), record =>
                {
                    Assert.Equal(status, record.GrpcStatus);
                    Assert.InRange(record.ElapsedMilliseconds!.Value, 0, 60_000);
                });
                Assert.Null(call[0].GrpcStatus);
                Assert.Null(call[0].ElapsedMilliseconds);
            }

            // Metadata only: identity, status and time, never a request value, a
            // status message or a path.
            Assert.All(records, record => Assert.Empty(record.Properties.Except(AllowedProperties, StringComparer.Ordinal)));
            Assert.DoesNotContain(Sentinel, calls.LogText, StringComparison.Ordinal);
            Assert.DoesNotContain(directory, calls.LogText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(directory.Replace("\\", "\\\\", StringComparison.Ordinal), calls.LogText,
                StringComparison.OrdinalIgnoreCase);

            // Off at the default level: the same calls leave no record.
            var quiet = await RunServerAsync(Path.Combine(directory, "default"), debug: false).ConfigureAwait(true);
            Assert.Contains(quiet.Records, record => record.Name == "PolicyLoaded");
            Assert.DoesNotContain(quiet.Records, record => EventNames.Contains(record.Name));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private static async Task<ServerCalls> RunServerAsync(string logDirectory, bool debug)
    {
        var port = ReserveLoopbackPort();
        var start = new ProcessStartInfo
        {
            FileName = Path.Combine(AppContext.BaseDirectory, "Briosa.Server.exe"),
            WorkingDirectory = AppContext.BaseDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in new[]
        {
            $"--Briosa:Endpoint:Port={port}",
            "--Briosa:Desktop:Mode=Disabled",
            "--Briosa:Logging:ConsoleEnabled=false",
            $"--Briosa:Logging:File:Directory={logDirectory}",
            "--Briosa:Security:Operations:Overrides:file_operations:get_working_directory=deny"
        })
        {
            start.ArgumentList.Add(argument);
        }

        // The environment form the conformance runner uses.
        const string level = "Logging__LogLevel__Briosa.Server.Services.UnaryCallAuditInterceptor";
        if (debug) start.Environment[level] = "Debug";
        else start.Environment.Remove(level);
        using var server = Process.Start(start) ?? throw new InvalidOperationException("The server did not start.");
        var output = server.StandardOutput.ReadToEndAsync();
        var error = server.StandardError.ReadToEndAsync();
        try
        {
            await WaitForListenerAsync(server, port).ConfigureAwait(true);
            using var channel = GrpcChannel.ForAddress($"http://127.0.0.1:{port}");
            CallOptions Options() => new(deadline: DateTime.UtcNow.AddSeconds(30));
            var discovery = new Api.DiscoveryService.DiscoveryServiceClient(channel);
            await discovery.GetServerInfoAsync(new Api.GetServerInfoRequest(), Options()).ResponseAsync.ConfigureAwait(true);
            await discovery.ListCapabilitiesAsync(new Api.ListCapabilitiesRequest { IncludeDenied = true }, Options())
                .ResponseAsync.ConfigureAwait(true);
            await new Api.SpatialAnalyzerSdkLifecycle.SpatialAnalyzerSdkLifecycleClient(channel)
                .GetSpatialAnalyzerSdkStateAsync(new Api.GetSpatialAnalyzerSdkStateRequest(), Options())
                .ResponseAsync.ConfigureAwait(true);
            await new Api.SpatialAnalyzerLifecycle.SpatialAnalyzerLifecycleClient(channel)
                .GetSpatialAnalyzerStateAsync(new Api.GetSpatialAnalyzerStateRequest(), Options())
                .ResponseAsync.ConfigureAwait(true);
            await new Health.HealthClient(channel).CheckAsync(
                new HealthCheckRequest { Service = "briosa.liveness" }, Options()).ResponseAsync.ConfigureAwait(true);
            var denied = await Assert.ThrowsAsync<RpcException>(async () =>
                await new Api.FileOperations.FileOperationsClient(channel)
                    .GetWorkingDirectoryAsync(new Api.GetWorkingDirectoryRequest(), Options()).ResponseAsync.ConfigureAwait(true)).ConfigureAwait(true);
            Assert.Equal(StatusCode.PermissionDenied, denied.StatusCode);
            var unserved = await Assert.ThrowsAsync<RpcException>(async () =>
                await new Api.Variables.VariablesClient(channel)
                    .GetDoubleVariableAsync(new Api.GetDoubleVariableRequest { Name = Sentinel }, Options())
                    .ResponseAsync.ConfigureAwait(true)).ConfigureAwait(true);
            var doubleVariableStatus = unserved.StatusCode.ToString();

            // The file sink writes on its own consumer; wait for the last response's
            // record before the server is stopped.
            var records = await ReadRecordsAsync(logDirectory, wanted: debug ? 21 : 0).ConfigureAwait(true);
            return new(records.Records, records.Text, doubleVariableStatus);
        }
        finally
        {
            if (!server.HasExited)
            {
                server.Kill(entireProcessTree: true);
                await server.WaitForExitAsync().ConfigureAwait(true);
            }

            await Task.WhenAll(output, error).ConfigureAwait(true);
        }
    }

    private static async Task<(List<LogRecord> Records, string Text)> ReadRecordsAsync(string directory, int wanted)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        while (true)
        {
            var text = string.Concat(Directory.Exists(directory)
                ? Directory.GetFiles(directory, "*.jsonl").Order(StringComparer.Ordinal).Select(ReadShared)
                : []);
            var records = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(Parse).OfType<LogRecord>().ToList();
            var audit = records.Count(record => EventNames.Contains(record.Name));
            if ((wanted == 0 && records.Any(record => record.Name == "PolicyLoaded")) || (wanted > 0 && audit >= wanted) ||
                timeout.IsCancellationRequested)
            {
                return (records, text);
            }

            await Task.Delay(50, CancellationToken.None).ConfigureAwait(true);
        }
    }

    private static string ReadShared(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static LogRecord? Parse(string line)
    {
        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;
            var properties = root.GetProperty("Properties");
            string? Text(string name) => properties.TryGetProperty(name, out var value) &&
                value.ValueKind == JsonValueKind.String ? value.GetString() : null;
            return new(
                root.GetProperty("MessageTemplate").GetString() ?? string.Empty,
                root.GetProperty("Level").GetString() ?? string.Empty,
                Text("Rpc"),
                Text("OperationId"),
                Text("GrpcStatus"),
                properties.TryGetProperty("ElapsedMilliseconds", out var elapsed) &&
                    elapsed.ValueKind == JsonValueKind.Number ? elapsed.GetInt64() : null,
                properties.EnumerateObject().Select(property => property.Name).ToArray());
        }
        catch (JsonException)
        {
            return null;
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
    }

    private static string[] UnaryMethods() =>
        typeof(Api.DiscoveryService).Assembly.GetTypes()
            .Select(type => type.GetProperty("Descriptor", BindingFlags.Public | BindingFlags.Static))
            .Where(property => property?.PropertyType == typeof(FileDescriptor))
            .Select(property => (FileDescriptor)property!.GetValue(null)!)
            .Append(HealthReflection.Descriptor)
            .SelectMany(file => file.Services)
            .SelectMany(service => service.Methods)
            .Where(method => !method.IsClientStreaming && !method.IsServerStreaming)
            .Select(method => $"/{method.Service.FullName}/{method.Name}")
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    private static HashSet<string> DigestRpcNames()
    {
        var runner = File.ReadAllText(Path.Combine(TargetRoot(), "conformance", "client", "v1",
            "Invoke-BriosaClientConformance.ps1"));
        var list = Regex.Match(runner, @"Rpc = @\{ Allowed = \(New-OrdinalSet @\((?<names>[^)]*)\)\) \}",
            RegexOptions.CultureInvariant, TimeSpan.FromSeconds(5));
        Assert.True(list.Success, "The conformance runner's digest has no Rpc list.");
        return Regex.Matches(list.Groups["names"].Value, "\"(?<name>[A-Za-z0-9]+)\"",
                RegexOptions.CultureInvariant, TimeSpan.FromSeconds(5))
            .Select(match => match.Groups["name"].Value)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static string TargetRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Briosa.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory.FullName;
    }

    private static int ReserveLoopbackPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
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

    private static async Task WaitForListenerAsync(Process server, int port)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        while (true)
        {
            if (server.HasExited)
                throw new InvalidOperationException($"The Briosa test server exited with code {server.ExitCode}.");
            using var client = new TcpClient();
            try
            {
                await client.ConnectAsync(IPAddress.Loopback, port, timeout.Token).ConfigureAwait(true);
                return;
            }
            catch (SocketException)
            {
                await Task.Delay(50, timeout.Token).ConfigureAwait(true);
            }
        }
    }

    private sealed record ServerCalls(List<LogRecord> Records, string LogText, string DoubleVariableStatus);

    private sealed record LogRecord(string Name, string Level, string? Rpc, string? OperationId, string? GrpcStatus,
        long? ElapsedMilliseconds, string[] Properties);
}
