using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.RegularExpressions;
using Briosa.Server.Services;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

// #450: a connection capture records what ASP.NET Core's own HTTP stack logs about the
// HTTP/2 connection, reduced to fixed names and validated fields. It is off unless its
// categories are named at startup, which only a conformance soak package does.
public sealed class ConnectionCaptureTests
{
    private const string HeaderMarker = "connection-capture-header-marker";
    private const string RequestMarker = "connection-capture-request-marker";
    private const string UserAgentMarker = "connection-capture-agent-marker";

    private static readonly string[] CommonProperties =
        ["SchemaVersion", "SourceContext", "EventId", "ServerInstanceId", "SpatialAnalyzerTarget", "TraceId", "SpanId"];
    private static readonly string[] AuditProperties = ["Rpc", "OperationId", "GrpcStatus", "ElapsedMilliseconds"];

    // The categories the runner turns up are exactly the ones the projection reads, and
    // the names it prints are exactly the projection's.
    [Fact]
    public void TheConformanceRunnerNamesTheProjectionsCategoriesAndRecordNames()
    {
        var runner = RunnerText();
        var levels = RunnerCaptureLevels(runner);
        Assert.Equal(
            new[]
            {
                ConnectionLogProjection.Http2Category, ConnectionLogProjection.ConnectionsCategory,
                ConnectionLogProjection.BadRequestsCategory, ConnectionLogProjection.GeneralCategory,
                ConnectionLogProjection.SocketsCategory, ConnectionLogProjection.HostingCategory
            }.Order(StringComparer.Ordinal),
            levels.Keys.Order(StringComparer.Ordinal));
        Assert.Equal("Trace", levels[ConnectionLogProjection.Http2Category]);
        Assert.Equal("Information", levels[ConnectionLogProjection.HostingCategory]);

        var list = Regex.Match(runner, @"\$connectionEventNames = New-OrdinalSet @\((?<names>[^)]*)\)",
            RegexOptions.CultureInvariant, TimeSpan.FromSeconds(5));
        Assert.True(list.Success, "The conformance runner has no connection record list.");
        var names = Regex.Matches(list.Groups["names"].Value, "\"(?<name>[A-Za-z0-9]+)\"",
                RegexOptions.CultureInvariant, TimeSpan.FromSeconds(5))
            .Select(match => match.Groups["name"].Value)
            .Order(StringComparer.Ordinal);
        Assert.Equal(ConnectionLogProjection.Labels.Order(StringComparer.Ordinal), names);
    }

    // Off by default: with the server's own defaults no projected category is enabled at
    // any level the projection reads, so ASP.NET Core never creates these records.
    [Fact]
    public void TheDefaultLevelsLeaveEveryProjectedCategoryOffAndTheCaptureLevelsTurnThemOn()
    {
        var directory = Path.Combine(Path.GetTempPath(), "briosa-connection-capture-" + Guid.NewGuid().ToString("N"));
        try
        {
            var defaults = new ConfigurationBuilder()
                .AddJsonFile(ServerAppSettings(), optional: false)
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Briosa:Logging:ConsoleEnabled"] = "false",
                    ["Briosa:Logging:File:Directory"] = directory
                })
                .Build();
            using (var off = Loggers(defaults))
            {
                foreach (var category in RunnerCaptureLevels(RunnerText()).Keys)
                {
                    Assert.False(Logger(off, category).IsEnabled(LogLevel.Information), category);
                }

                Assert.True(Logger(off, ConnectionLogProjection.Http2Category).IsEnabled(LogLevel.Warning));
            }

            var captured = new ConfigurationBuilder()
                .AddConfiguration(defaults)
                .AddInMemoryCollection(RunnerCaptureLevels(RunnerText())
                    .ToDictionary(entry => $"Logging:LogLevel:{entry.Key}", entry => (string?)entry.Value))
                .Build();
            using var on = Loggers(captured);
            foreach (var (category, level) in RunnerCaptureLevels(RunnerText()))
            {
                Assert.True(Logger(on, category).IsEnabled(Enum.Parse<LogLevel>(level)), category);
            }
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void AFrameRecordKeepsOnlyItsTypeStreamLengthAndFlags()
    {
        var state = Kestrel(("ConnectionId", "0HNQCDAST4U3F"), ("type", (byte)1), ("id", 9), ("length", 188),
            ("flags", (byte)5), ("{OriginalFormat}", "Connection id \"{ConnectionId}\" received {type} frame"));
        Assert.True(ConnectionLogProjection.TryProject(ConnectionLogProjection.Http2Category, LogLevel.Trace,
            new EventId(37, "Http2FrameReceived"), state, out var label, out var fields));
        Assert.Equal("Http2FrameReceived", label);
        Assert.Equal(
            new (string, object?)[] { ("ConnectionId", "0HNQCDAST4U3F"), ("FrameType", "HEADERS"), ("StreamId", 9),
                ("FrameLength", 188), ("FrameFlags", 5) },
            Pairs(fields));

        // Kestrel writes unnamed flags as hex text; anything else is never written.
        Assert.True(ConnectionLogProjection.TryProject(ConnectionLogProjection.Http2Category, LogLevel.Trace,
            new EventId(49, "Http2FrameSending"),
            Kestrel(("ConnectionId", "C:/Users/someone"), ("type", (byte)200), ("id", -1), ("length", 1L << 40),
                ("flags", "0x1ff")),
            out _, out var invalid));
        Assert.All(invalid, field => Assert.Equal(ConnectionLogProjection.Unrecognized, field.Value));
        Assert.True(ConnectionLogProjection.TryProject(ConnectionLogProjection.Http2Category, LogLevel.Trace,
            new EventId(49, "Http2FrameSending"), Kestrel(("flags", "0x0")), out _, out var hexFlags));
        Assert.Equal(0, Assert.IsType<int>(Assert.Single(hexFlags).Value));
    }

    [Fact]
    public void ARequestRecordKeepsItsStreamAndTheRoutedNameButNoHostPathOrContent()
    {
        Assert.True(ConnectionLogProjection.TryProject(ConnectionLogProjection.GeneralCategory, LogLevel.Debug,
            new EventId(25, "RequestBodyStart"),
            Kestrel(("ConnectionId", "0HNQCDAST4U3F"), ("TraceIdentifier", "0HNQCDAST4U3F:0000000B")),
            out var label, out var fields));
        Assert.Equal("RequestBodyStart", label);
        Assert.Equal(new (string, object?)[] { ("ConnectionId", "0HNQCDAST4U3F"), ("StreamId", 11) }, Pairs(fields));

        var start = Kestrel(("Protocol", "HTTP/2"), ("Method", "POST"), ("ContentType", "application/grpc+" + HeaderMarker),
            ("ContentLength", 5L), ("Scheme", "http"), ("Host", HeaderMarker + ":50051"), ("PathBase", ""),
            ("Path", "/briosa.DiscoveryService/GetServerInfo"), ("QueryString", "?" + RequestMarker));
        Assert.True(ConnectionLogProjection.TryProject(ConnectionLogProjection.HostingCategory, LogLevel.Information,
            new EventId(1), start, out label, out fields));
        Assert.Equal("HostingRequestStarting", label);
        Assert.Equal(new (string, object?)[] { ("HttpProtocol", "HTTP/2"), ("HttpMethod", "POST"), ("Rpc", "GetServerInfo") },
            Pairs(fields));

        // The caller's spelling of a path is never written: only an exact server method.
        foreach (var path in new[] { "/briosa.discoveryservice/getserverinfo", "/" + RequestMarker, "" })
        {
            Assert.True(ConnectionLogProjection.TryProject(ConnectionLogProjection.HostingCategory, LogLevel.Information,
                new EventId(2), Kestrel(("ElapsedMilliseconds", 1.23456), ("StatusCode", 200), ("Path", path)),
                out _, out fields));
            Assert.Equal(new (string, object?)[] { ("RequestMilliseconds", 1.235), ("HttpStatusCode", 200), ("Rpc", "unrecognized") },
                Pairs(fields));
        }

        Assert.True(ConnectionLogProjection.TryProject(ConnectionLogProjection.HostingCategory, LogLevel.Information,
            new EventId(1), Kestrel(("Path", "/briosa.FileOperations/GetWorkingDirectory")), out _, out fields));
        Assert.Equal(("OperationId", (object?)"file_operations.get_working_directory"), Pairs(fields).Single());
    }

    // A record at Warning or above keeps its pre-capture form, so a default installation
    // logs what it logged before. So does any record that is not one of the reviewed ones.
    [Theory]
    [InlineData(ConnectionLogProjection.Http2Category, 37, "Http2FrameReceived", LogLevel.Warning)]
    [InlineData(ConnectionLogProjection.Http2Category, 60, "Http2QueueOperationsExceeded", LogLevel.Critical)]
    [InlineData(ConnectionLogProjection.Http2Category, 37, "SomethingElse", LogLevel.Trace)]
    [InlineData(ConnectionLogProjection.ConnectionsCategory, 24, "ConnectionRejected", LogLevel.Warning)]
    [InlineData(ConnectionLogProjection.GeneralCategory, 13, "ApplicationError", LogLevel.Error)]
    [InlineData("Microsoft.AspNetCore.Server.Kestrel.Http3", 37, "Http2FrameReceived", LogLevel.Trace)]
    [InlineData("Briosa.Server.Services.UnaryCallAuditInterceptor", 1600, "UnaryCallStarted", LogLevel.Debug)]
    public void OtherRecordsAreNotProjected(string category, int id, string name, LogLevel level)
    {
        var state = Kestrel(("ConnectionId", "0HNQCDAST4U3F"), ("TraceIdentifier", "0HNQCDAST4U3F:00000001"));
        Assert.False(ConnectionLogProjection.TryProject(category, level, new EventId(id, name), state, out _, out _));
        var safe = SafeLogState.Create(category, level, new EventId(id, name), state, "instance");
        Assert.Equal(SafeLogState.Create(category, new EventId(id, name), state, "instance").ToString(), safe.ToString());
        Assert.DoesNotContain(safe, pair => pair.Key is "ConnectionId" or "StreamId");
    }

    [Fact]
    public async Task TheServerRecordsTheHttp2ConnectionOnlyWhenTheCaptureLevelsAreSet()
    {
        var directory = Path.Combine(Path.GetTempPath(), "briosa-connection-capture-" + Guid.NewGuid().ToString("N"));
        try
        {
            var captured = await RunServerAsync(Path.Combine(directory, "capture"), capture: true).ConfigureAwait(true);
            var records = captured.Records;
            var allowed = CommonProperties.Concat(AuditProperties).Concat(ConnectionLogProjection.Fields).ToArray();
            var connection = records.Where(record => ConnectionLogProjection.Labels.Contains(record.Name)).ToArray();
            Assert.All(connection, record =>
            {
                Assert.Empty(record.Properties.Except(allowed, StringComparer.Ordinal));
                Assert.NotEqual("Warning", record.Level);
            });
            Assert.Contains(connection, record => record.Name == "ConnectionAccepted");
            Assert.Contains(connection, record => record.Name == "HostingRequestStarting" && record.Text("Rpc") == "unrecognized");

            // Each call: its HEADERS and DATA frames in, its request body read on the same
            // stream with the same trace as its call records and its hosting records, and a
            // HEADERS frame out that ends the stream.
            var calls = records.Where(record => record.Name == "UnaryCallStarted").ToArray();
            Assert.Equal(4, calls.Length);
            foreach (var call in calls)
            {
                var body = Assert.Single(records, record => record.Name == "RequestBodyStart" && record.TraceId == call.TraceId);
                var connectionId = body.Text("ConnectionId");
                var stream = body.Number("StreamId");
                Assert.Matches("^[0-9A-V]{13}$", connectionId);
                Assert.Contains(records, record => record.Name == "RequestBodyDone" && record.TraceId == call.TraceId);
                Assert.Contains(records, record => record.Name == "HostingRequestStarting" && record.TraceId == call.TraceId);
                Assert.Contains(records, record => record.Name == "HostingRequestFinished" && record.TraceId == call.TraceId &&
                    record.Number("HttpStatusCode") == 200);
                Frame(records, "Http2FrameReceived", connectionId, stream, "HEADERS");
                Frame(records, "Http2FrameReceived", connectionId, stream, "DATA");
                Assert.Contains(records, record => record.Name == "Http2FrameSending" && record.Text("ConnectionId") == connectionId &&
                    record.Number("StreamId") == stream && record.Text("FrameType") == "HEADERS" &&
                    (record.Number("FrameFlags") & 1) == 1);
            }

            // Frame metadata only: no header, metadata or request value, no host or port, no
            // content type and no path text.
            foreach (var marker in new[] { HeaderMarker, RequestMarker, UserAgentMarker, "x-capture", "127.0.0.1", "localhost",
                         "application/grpc", "/briosa.", "getserverinfo",
                         ":" + captured.Port.ToString(System.Globalization.CultureInfo.InvariantCulture) })
            {
                Assert.DoesNotContain(marker, captured.LogText, StringComparison.Ordinal);
            }

            Assert.DoesNotContain(directory, captured.LogText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(directory.Replace("\\", "\\\\", StringComparison.Ordinal), captured.LogText,
                StringComparison.OrdinalIgnoreCase);

            // Off by default: the same calls leave no connection record and no record from
            // these categories at all.
            var quiet = await RunServerAsync(Path.Combine(directory, "default"), capture: false).ConfigureAwait(true);
            Assert.Contains(quiet.Records, record => record.Name == "PolicyLoaded");
            Assert.DoesNotContain(quiet.Records, record => ConnectionLogProjection.Labels.Contains(record.Name));
            Assert.DoesNotContain(quiet.Records, record =>
                record.Text("SourceContext")?.StartsWith("Microsoft.AspNetCore.Server.Kestrel", StringComparison.Ordinal) == true ||
                record.Text("SourceContext")?.StartsWith("Microsoft.AspNetCore.Hosting", StringComparison.Ordinal) == true);
            Assert.Equal(quiet.Statuses, captured.Statuses);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private static void Frame(List<LogRecord> records, string name, string? connectionId, long? stream, string type) =>
        Assert.Contains(records, record => record.Name == name && record.Text("ConnectionId") == connectionId &&
            record.Number("StreamId") == stream && record.Text("FrameType") == type);

    private static (string, object?)[] Pairs(List<KeyValuePair<string, object?>> fields) =>
        fields.Select(field => (field.Key, field.Value)).ToArray();

    private static List<KeyValuePair<string, object?>> Kestrel(params (string Key, object? Value)[] values) =>
        values.Select(value => new KeyValuePair<string, object?>(value.Key, value.Value)).ToList();

    private static ServiceProvider Loggers(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddBriosaLogging(configuration));
        return services.BuildServiceProvider();
    }

    private static ILogger Logger(ServiceProvider provider, string category) =>
        provider.GetRequiredService<ILoggerFactory>().CreateLogger(category);

    private static async Task<ServerRun> RunServerAsync(string logDirectory, bool capture)
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

        // The environment the conformance runner sets for a capture.
        const string audit = "Logging__LogLevel__Briosa.Server.Services.UnaryCallAuditInterceptor";
        start.Environment[audit] = "Debug";
        foreach (var (category, level) in RunnerCaptureLevels(RunnerText()))
        {
            var key = $"Logging__LogLevel__{category}";
            if (capture) start.Environment[key] = level;
            else start.Environment.Remove(key);
        }

        using var server = Process.Start(start) ?? throw new InvalidOperationException("The server did not start.");
        var output = server.StandardOutput.ReadToEndAsync();
        var error = server.StandardError.ReadToEndAsync();
        try
        {
            await WaitForListenerAsync(server, port).ConfigureAwait(true);
            using var channel = GrpcChannel.ForAddress($"http://127.0.0.1:{port}");
            CallOptions Options() => new(new Metadata { { "x-capture-marker", HeaderMarker }, { "x-capture-agent", UserAgentMarker } },
                DateTime.UtcNow.AddSeconds(30));
            var statuses = new List<string>();
            var discovery = new Api.DiscoveryService.DiscoveryServiceClient(channel);
            await discovery.GetServerInfoAsync(new Api.GetServerInfoRequest(), Options()).ResponseAsync.ConfigureAwait(true);
            statuses.Add("OK");
            await discovery.ListCapabilitiesAsync(new Api.ListCapabilitiesRequest { IncludeDenied = true }, Options())
                .ResponseAsync.ConfigureAwait(true);
            statuses.Add("OK");
            var invoker = channel.CreateCallInvoker();
            await invoker.AsyncUnaryCall(new Method<Api.GetServerInfoRequest, Api.GetServerInfoResponse>(
                    MethodType.Unary, "briosa.DiscoveryService", "getserverinfo",
                    Marshallers.Create<Api.GetServerInfoRequest>(message => Google.Protobuf.MessageExtensions.ToByteArray(message),
                        _ => throw new NotSupportedException()),
                    Marshallers.Create(message => Google.Protobuf.MessageExtensions.ToByteArray(message),
                        Api.GetServerInfoResponse.Parser.ParseFrom)), null, Options(), new Api.GetServerInfoRequest())
                .ResponseAsync.ConfigureAwait(true);
            statuses.Add("OK");
            var unserved = await Assert.ThrowsAsync<RpcException>(async () =>
                await new Api.Variables.VariablesClient(channel)
                    .GetDoubleVariableAsync(new Api.GetDoubleVariableRequest { Name = RequestMarker }, Options())
                    .ResponseAsync.ConfigureAwait(true)).ConfigureAwait(true);
            statuses.Add(unserved.StatusCode.ToString());

            var records = await ReadRecordsAsync(logDirectory, capture).ConfigureAwait(true);
            return new(records.Records, records.Text, statuses, port);
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

    // The file sink writes on its own consumer; wait for the last call's records.
    private static async Task<(List<LogRecord> Records, string Text)> ReadRecordsAsync(string directory, bool capture)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        while (true)
        {
            var text = string.Concat(Directory.Exists(directory)
                ? Directory.GetFiles(directory, "*.jsonl").Order(StringComparer.Ordinal).Select(ReadShared)
                : []);
            var records = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(Parse).OfType<LogRecord>().ToList();
            var ended = records.Count(record => record.Name == "UnaryCallResponseEnded");
            var finished = records.Count(record => record.Name == "HostingRequestFinished");
            if ((ended >= 4 && (!capture || finished >= 4)) || timeout.IsCancellationRequested)
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
            var values = properties.EnumerateObject().ToDictionary(property => property.Name,
                property => property.Value.Clone(), StringComparer.Ordinal);
            return new(root.GetProperty("MessageTemplate").GetString() ?? string.Empty,
                root.GetProperty("Level").GetString() ?? string.Empty, values);
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

    private static Dictionary<string, string> RunnerCaptureLevels(string runner)
    {
        var block = Regex.Match(runner, @"\$connectionCaptureLevels = \[ordered\]@\{(?<entries>[^}]*)\}",
            RegexOptions.CultureInvariant, TimeSpan.FromSeconds(5));
        Assert.True(block.Success, "The conformance runner has no connection capture levels.");
        return Regex.Matches(block.Groups["entries"].Value, "\"(?<category>[A-Za-z0-9.]+)\" = \"(?<level>[A-Za-z]+)\"",
                RegexOptions.CultureInvariant, TimeSpan.FromSeconds(5))
            .ToDictionary(match => match.Groups["category"].Value, match => match.Groups["level"].Value,
                StringComparer.Ordinal) is { Count: 6 } levels
                ? levels
                : throw new InvalidOperationException("The conformance runner's connection capture levels did not parse.");
    }

    private static string RunnerText() => File.ReadAllText(Path.Combine(TargetRoot(), "conformance", "client", "v1",
        "Invoke-BriosaClientConformance.ps1"));

    private static string ServerAppSettings() => Path.Combine(TargetRoot(), "src", "Briosa.Server", "appsettings.json");

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

    private sealed record ServerRun(List<LogRecord> Records, string LogText, List<string> Statuses, int Port);

    private sealed record LogRecord(string Name, string Level, Dictionary<string, JsonElement> Values)
    {
        public IEnumerable<string> Properties => Values.Keys;

        public string? TraceId => Text("TraceId");

        public string? Text(string name) =>
            Values.TryGetValue(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

        public long? Number(string name) =>
            Values.TryGetValue(name, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetInt64() : null;
    }
}
