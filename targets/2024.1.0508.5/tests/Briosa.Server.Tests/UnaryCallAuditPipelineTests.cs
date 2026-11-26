using System.Globalization;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using Briosa.Server.Services;
using Google.Protobuf;
using Grpc.AspNetCore.Server;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog.Events;
using Xunit.Abstractions;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

// #450: the unary call audit records are diagnostics only. With their Debug level off,
// gRPC must build the call path it built before they existed; with it on, each record
// must describe what the call actually did.
public sealed class UnaryCallAuditPipelineTests(ITestOutputHelper output)
{
    private const string Category = "Briosa.Server.Services.UnaryCallAuditInterceptor";
    private const string Sentinel = "unary-audit-pipeline-sentinel";
    private const int WarmupCalls = 20_000;
    private const int MeasuredCalls = 200_000;

    // The framework's own unary invoker, from the endpoint gRPC routes to, called
    // directly with a handler that returns one cached, completed task.
    [Fact]
    public void WithDebugOffGrpcBuildsThePreChangeInvocationPath()
    {
        var before = Measure(Registration.PreChange, debug: false);
        var off = Measure(Registration.Current, debug: false);
        var on = Measure(Registration.Current, debug: true);
        var always = Measure(Registration.AlwaysRegistered, debug: false);
        foreach (var (name, measurement) in new[] { ("pre-change", before), ("debug off", off), ("debug on", on),
                     ("always registered, passing the call through", always) })
        {
            output.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"{name}: returns the handler's task {measurement.ReturnsHandlerTask}, {measurement.BytesPerCall:0.###} bytes per call, synchronous cancellation {measurement.SynchronousCancellation}"));
        }

        Assert.True(before.ReturnsHandlerTask);
        Assert.True(off.ReturnsHandlerTask);
        Assert.Equal(before.SynchronousCancellation, off.SynchronousCancellation);
        Assert.InRange(off.BytesPerCall, 0, before.BytesPerCall + 0.01);
        // The check sees the pipeline that an unconditional registration builds.
        Assert.False(always.ReturnsHandlerTask);
        Assert.True(always.BytesPerCall > before.BytesPerCall + 1);
        Assert.False(on.ReturnsHandlerTask);
    }

    [Fact]
    public void TheInterceptorIsRegisteredOnlyWhenItsDebugLevelIsEnabled()
    {
        using var quiet = Services(debug: false);
        // The logging provider is owned by each service provider.
        using var enabled = Services(debug: true);
        Assert.Empty(quiet.GetRequiredService<Microsoft.Extensions.Options.IOptions<GrpcServiceOptions>>().Value.Interceptors);
        Assert.Contains(enabled.GetRequiredService<Microsoft.Extensions.Options.IOptions<GrpcServiceOptions>>().Value.Interceptors,
            registration => registration.Type == typeof(UnaryCallAuditInterceptor));
    }

    // Each status is the one the call ended with at that boundary: a status the
    // handler assigned before returning, a handler fault, a deadline and a client
    // cancellation, over HTTP/2 on loopback. A request path in another case reaches
    // the same endpoint and is recorded by the endpoint's name.
    [Fact]
    public async Task RecordsTheStatusAtEachBoundaryOverHttp2()
    {
        var sink = new ObservabilityTests.CaptureSink();
        using var provider = new BriosaLogProvider(ObservabilityTests.Options(), new BriosaLogHealth(), sink);
        var app = await StartAsync(provider).ConfigureAwait(true);
        await using var running = app.ConfigureAwait(true);
        using var channel = GrpcChannel.ForAddress(Assert.Single(app.Urls));
        var client = new Api.SpatialAnalyzerSdkLifecycle.SpatialAnalyzerSdkLifecycleClient(channel);
        CallOptions Bounded() => new(deadline: DateTime.UtcNow.AddSeconds(30));

        await client.GetSpatialAnalyzerSdkStateAsync(new(), Bounded()).ResponseAsync.ConfigureAwait(true);
        var lowercase = new Method<Api.GetSpatialAnalyzerSdkStateRequest, Api.GetSpatialAnalyzerSdkStateResponse>(
            MethodType.Unary, "briosa.SpatialAnalyzerSdkLifecycle", "getspatialanalyzersdkstate",
            Marshallers.Create(message => message.ToByteArray(), Api.GetSpatialAnalyzerSdkStateRequest.Parser.ParseFrom),
            Marshallers.Create(message => message.ToByteArray(), Api.GetSpatialAnalyzerSdkStateResponse.Parser.ParseFrom));
        await channel.CreateCallInvoker().AsyncUnaryCall(lowercase, null, Bounded(), new())
            .ResponseAsync.ConfigureAwait(true);
        var assigned = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.StartSpatialAnalyzerSdkAsync(new(), Bounded()).ResponseAsync.ConfigureAwait(true)).ConfigureAwait(true);
        Assert.Equal(StatusCode.FailedPrecondition, assigned.StatusCode);
        var faulted = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConnectToSpatialAnalyzerAsync(new() { ExpectedSdkGeneration = 424242 }, Bounded())
                .ResponseAsync.ConfigureAwait(true)).ConfigureAwait(true);
        Assert.Equal(StatusCode.Unknown, faulted.StatusCode);
        var deadline = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ReconnectToSpatialAnalyzerAsync(new(), new CallOptions(deadline: DateTime.UtcNow.AddMilliseconds(300)))
                .ResponseAsync.ConfigureAwait(true)).ConfigureAwait(true);
        Assert.Equal(StatusCode.DeadlineExceeded, deadline.StatusCode);
        await Until(() => Records(sink, "UnaryCallStarted", "ReconnectToSpatialAnalyzer").Length == 1).ConfigureAwait(true);
        await Until(() => Records(sink, "UnaryCallResponseEnded", "ReconnectToSpatialAnalyzer").Length +
            Records(sink, "UnaryCallResponseAbandoned", "ReconnectToSpatialAnalyzer").Length == 1).ConfigureAwait(true);
        using (var cancellation = new CancellationTokenSource())
        {
            var call = client.StopSpatialAnalyzerSdkAsync(new(), new CallOptions(deadline: DateTime.UtcNow.AddSeconds(30),
                cancellationToken: cancellation.Token));
            await Until(() => Records(sink, "UnaryCallStarted", "StopSpatialAnalyzerSdk").Length == 1).ConfigureAwait(true);
            await cancellation.CancelAsync().ConfigureAwait(true);
            var cancelled = await Assert.ThrowsAsync<RpcException>(async () =>
                await call.ResponseAsync.ConfigureAwait(true)).ConfigureAwait(true);
            Assert.Equal(StatusCode.Cancelled, cancelled.StatusCode);
        }

        await Until(() => Records(sink, "UnaryCallResponseAbandoned", "StopSpatialAnalyzerSdk").Length == 1 ||
            Records(sink, "UnaryCallResponseEnded", "StopSpatialAnalyzerSdk").Length == 1).ConfigureAwait(true);
        await app.StopAsync().ConfigureAwait(true);
        foreach (var entry in sink.Events)
        {
            output.WriteLine($"{entry.MessageTemplate.Text} {Text(entry, "Rpc")} {Text(entry, "GrpcStatus")}");
        }

        // Both spellings of the state read: the routed endpoint's name, twice.
        Assert.Equal(2, Records(sink, "UnaryCallStarted", "GetSpatialAnalyzerSdkState").Length);
        Assert.All(Records(sink, "UnaryCallCompleted", "GetSpatialAnalyzerSdkState"), entry => Assert.Equal("OK", Text(entry, "GrpcStatus")));
        Assert.All(Records(sink, "UnaryCallResponseEnded", "GetSpatialAnalyzerSdkState"), entry => Assert.Equal("OK", Text(entry, "GrpcStatus")));
        Assert.Equal(2, Records(sink, "UnaryCallResponseEnded", "GetSpatialAnalyzerSdkState").Length);
        Assert.DoesNotContain(sink.Events, entry =>
            string.Equals(Text(entry, "Rpc"), "getspatialanalyzersdkstate", StringComparison.Ordinal));
        // The status the handler assigned, at both boundaries.
        Assert.Equal("FailedPrecondition", Text(Assert.Single(Records(sink, "UnaryCallCompleted", "StartSpatialAnalyzerSdk")), "GrpcStatus"));
        Assert.Equal("FailedPrecondition", Text(Assert.Single(Records(sink, "UnaryCallResponseEnded", "StartSpatialAnalyzerSdk")), "GrpcStatus"));
        // A handler fault: one 1602 at Debug, no 1601, and the status the server sent.
        var fault = Assert.Single(Records(sink, "UnaryCallFaulted", "ConnectToSpatialAnalyzer"));
        Assert.Equal(LogEventLevel.Debug, fault.Level);
        Assert.Equal(1602, EventId(fault));
        Assert.Empty(Records(sink, "UnaryCallCompleted", "ConnectToSpatialAnalyzer"));
        Assert.Equal("Unknown", Text(Assert.Single(Records(sink, "UnaryCallResponseEnded", "ConnectToSpatialAnalyzer")), "GrpcStatus"));
        // A deadline: the handler unwound without a status, and the server sent the
        // deadline's status.
        Assert.Single(Records(sink, "UnaryCallFaulted", "ReconnectToSpatialAnalyzer"));
        Assert.Equal("DeadlineExceeded", Text(Assert.Single(Records(sink, "UnaryCallResponseEnded", "ReconnectToSpatialAnalyzer")), "GrpcStatus"));
        // A client cancellation: the response ended without a status.
        Assert.Single(Records(sink, "UnaryCallFaulted", "StopSpatialAnalyzerSdk"));
        var abandoned = Assert.Single(Records(sink, "UnaryCallResponseAbandoned", "StopSpatialAnalyzerSdk"));
        Assert.Equal(1604, EventId(abandoned));
        Assert.False(abandoned.Properties.ContainsKey("GrpcStatus"));
        Assert.Empty(Records(sink, "UnaryCallResponseEnded", "StopSpatialAnalyzerSdk"));
        // No exception detail, status message or argument reaches the log.
        var text = string.Join("\n", sink.Events.Select(Serialize));
        Assert.DoesNotContain(Sentinel, text, StringComparison.Ordinal);
        Assert.DoesNotContain("424242", text, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", text, StringComparison.Ordinal);
    }

    private static ServiceProvider Services(bool debug)
    {
        var services = new ServiceCollection();
        services.AddLogging(logging =>
        {
            logging.ClearProviders();
#pragma warning disable CA2000 // The logger factory owns and disposes its providers.
            logging.AddProvider(new BriosaLogProvider(ObservabilityTests.Options(), new BriosaLogHealth(),
                new ObservabilityTests.CaptureSink()));
#pragma warning restore CA2000
            logging.SetMinimumLevel(LogLevel.Information);
            if (debug) logging.AddFilter(Category, LogLevel.Debug);
        });
        services.AddBriosaGrpc();
        return services.BuildServiceProvider();
    }

    private static async Task<WebApplication> StartAsync(BriosaLogProvider provider)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(LogLevel.Information);
        builder.Logging.AddFilter(Category, LogLevel.Debug);
        builder.Logging.AddProvider(provider);
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0,
            listener => listener.Protocols = HttpProtocols.Http2));
        builder.Services.AddBriosaGrpc();
        builder.Services.AddSingleton(new ScriptedSdkLifecycle());
        var app = builder.Build();
        app.MapGrpcService<ScriptedSdkLifecycle>();
        await app.StartAsync().ConfigureAwait(true);
        return app;
    }

    // Production logging, writing files to a temporary directory. With Debug on, only
    // a few calls are made, since each one writes records.
    private static Measurement Measure(Registration registration, bool debug)
    {
        var directory = ObservabilityTests.NewDirectory();
        try
        {
            var builder = WebApplication.CreateSlimBuilder();
            builder.Configuration.Sources.Clear();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Briosa:Logging:ConsoleEnabled"] = "false",
                ["Briosa:Logging:File:Directory"] = directory,
                [$"Logging:LogLevel:{Category}"] = debug ? "Debug" : "Information"
            });
            builder.Logging.AddBriosaLogging(builder.Configuration);
            switch (registration)
            {
                case Registration.PreChange:
                    builder.Services.AddGrpc(options => options.MaxReceiveMessageSize = PublicGrpcHosting.MaximumReceiveMessageBytes);
                    break;
                case Registration.Current:
                    builder.Services.AddBriosaGrpc();
                    break;
                case Registration.AlwaysRegistered:
                    // How the first revision registered the interceptor: always, with a
                    // level check that passed the call through. The framework's own
                    // pipeline still wraps every call.
                    builder.Services.AddSingleton(new PassThroughInterceptor());
                    builder.Services.AddGrpc(options =>
                    {
                        options.MaxReceiveMessageSize = PublicGrpcHosting.MaximumReceiveMessageBytes;
                        options.Interceptors.Add<PassThroughInterceptor>();
                    });
                    break;
            }

            builder.Services.AddSingleton(new CachedDiscovery());
            var app = builder.Build();
            try
            {
                app.MapGrpcService<CachedDiscovery>();
                var invoke = Invoker(app, "GetServerInfo", new Api.GetServerInfoRequest());
                var cancel = Invoker(app, "ListCapabilities", new Api.ListCapabilitiesRequest());
                var (warmup, measured) = debug ? (10, 100) : (WarmupCalls, MeasuredCalls);
                for (var index = 0; index < warmup; index++) Assert.True(invoke().IsCompletedSuccessfully);
                var allocated = GC.GetAllocatedBytesForCurrentThread();
                for (var index = 0; index < measured; index++)
                {
                    if (!invoke().IsCompletedSuccessfully) throw new InvalidOperationException("A measured call did not complete.");
                }

                var bytesPerCall = (GC.GetAllocatedBytesForCurrentThread() - allocated) / (double)measured;
                return new(ReferenceEquals(invoke(), CachedDiscovery.Response), bytesPerCall, cancel().Status);
            }
            finally
            {
                ((IDisposable)app).Dispose();
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    // A delegate that calls gRPC's invoker for the named method, as its call handler
    // does, with a fixed context.
    private static Func<Task> Invoker(WebApplication app, string method, object request)
    {
        var endpoint = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints)
            .Single(candidate => candidate.Metadata.GetMetadata<GrpcMethodMetadata>()?.Method.Name == method);
        var handler = endpoint.RequestDelegate!.Target!;
        const BindingFlags Instance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        var methodInvoker = FindProperty(handler.GetType(), "MethodInvoker").GetValue(handler)!;
        var invoke = methodInvoker.GetType().GetMethods(Instance).Single(candidate => candidate.Name == "Invoke");
        var contextType = typeof(GrpcServiceOptions).Assembly.GetType(
            "Grpc.AspNetCore.Server.Internal.HttpContextServerCallContext", throwOnError: true)!;
        var httpContext = new DefaultHttpContext { RequestServices = app.Services };
        var options = FindProperty(methodInvoker.GetType(), "Options").GetValue(methodInvoker)!;
        var constructor = contextType.GetConstructors(Instance).OrderByDescending(candidate => candidate.GetParameters().Length).First();
        var types = new Queue<Type>([request.GetType(), invoke.ReturnType.GetGenericArguments()[0]]);
        var arguments = constructor.GetParameters().Select(parameter => parameter.ParameterType switch
        {
            var type when type == typeof(HttpContext) => httpContext,
            var type when type == typeof(Type) => (object)types.Dequeue(),
            var type when type == typeof(ILogger) => NullLogger.Instance,
            var type when type.IsInstanceOfType(options) => options,
            var type => type.IsValueType ? Activator.CreateInstance(type) : null
        }).ToArray();
        var context = constructor.Invoke(arguments);
        var call = Expression.Call(Expression.Constant(methodInvoker), invoke,
            Expression.Constant(httpContext, typeof(HttpContext)),
            Expression.Constant(context, invoke.GetParameters()[1].ParameterType),
            Expression.Constant(request, invoke.GetParameters()[2].ParameterType));
        return Expression.Lambda<Func<Task>>(Expression.Convert(call, typeof(Task))).Compile();
    }

    private static PropertyInfo FindProperty(Type type, string name)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            var property = current.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                BindingFlags.DeclaredOnly);
            if (property is not null) return property;
        }

        throw new InvalidOperationException($"{type.Name} has no {name} property.");
    }

    private static LogEvent[] Records(ObservabilityTests.CaptureSink sink, string name, string rpc) =>
        sink.Events.Where(entry => entry.MessageTemplate.Text == name && Text(entry, "Rpc") == rpc).ToArray();

    private static string? Text(LogEvent entry, string property) =>
        entry.Properties.TryGetValue(property, out var value) && value is ScalarValue { Value: { } scalar }
            ? Convert.ToString(scalar, CultureInfo.InvariantCulture)
            : null;

    private static int? EventId(LogEvent entry) =>
        entry.Properties.TryGetValue("EventId", out var value) && value is StructureValue structure
            ? structure.Properties.Where(property => property.Name == "Id")
                .Select(property => (int?)Convert.ToInt32(((ScalarValue)property.Value).Value, CultureInfo.InvariantCulture))
                .FirstOrDefault()
            : null;

    private static string Serialize(LogEvent entry)
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        new Serilog.Formatting.Json.JsonFormatter().Format(entry, writer);
        return writer.ToString();
    }

    private static async Task Until(Func<bool> predicate)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        while (!predicate()) await Task.Delay(10, timeout.Token).ConfigureAwait(true);
    }

    private enum Registration
    {
        PreChange,
        Current,
        AlwaysRegistered
    }

    private sealed record Measurement(bool ReturnsHandlerTask, double BytesPerCall, TaskStatus SynchronousCancellation);

    internal sealed class PassThroughInterceptor : Grpc.Core.Interceptors.Interceptor
    {
        public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request,
            ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
        {
            ArgumentNullException.ThrowIfNull(continuation);
            return continuation(request, context);
        }
    }

    internal sealed class CachedDiscovery : Api.DiscoveryService.DiscoveryServiceBase
    {
        internal static readonly Task<Api.GetServerInfoResponse> Response = Task.FromResult(new Api.GetServerInfoResponse());

        public override Task<Api.GetServerInfoResponse> GetServerInfo(Api.GetServerInfoRequest request,
            ServerCallContext context) => Response;

        public override Task<Api.ListCapabilitiesResponse> ListCapabilities(Api.ListCapabilitiesRequest request,
            ServerCallContext context) => throw new OperationCanceledException();
    }

    internal sealed class ScriptedSdkLifecycle : Api.SpatialAnalyzerSdkLifecycle.SpatialAnalyzerSdkLifecycleBase
    {
        public override Task<Api.GetSpatialAnalyzerSdkStateResponse> GetSpatialAnalyzerSdkState(
            Api.GetSpatialAnalyzerSdkStateRequest request, ServerCallContext context) =>
            Task.FromResult(new Api.GetSpatialAnalyzerSdkStateResponse());

        public override Task<Api.StartSpatialAnalyzerSdkResponse> StartSpatialAnalyzerSdk(
            Api.StartSpatialAnalyzerSdkRequest request, ServerCallContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            context.Status = new Status(StatusCode.FailedPrecondition, Sentinel);
            return Task.FromResult(new Api.StartSpatialAnalyzerSdkResponse());
        }

        public override Task<Api.ConnectToSpatialAnalyzerResponse> ConnectToSpatialAnalyzer(
            Api.ConnectToSpatialAnalyzerRequest request, ServerCallContext context) =>
            throw new InvalidOperationException(Sentinel);

        public override async Task<Api.ReconnectToSpatialAnalyzerResponse> ReconnectToSpatialAnalyzer(
            Api.ReconnectToSpatialAnalyzerRequest request, ServerCallContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            await Task.Delay(Timeout.Infinite, context.CancellationToken).ConfigureAwait(false);
            return new Api.ReconnectToSpatialAnalyzerResponse();
        }

        public override async Task<Api.StopSpatialAnalyzerSdkResponse> StopSpatialAnalyzerSdk(
            Api.StopSpatialAnalyzerSdkRequest request, ServerCallContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            await Task.Delay(Timeout.Infinite, context.CancellationToken).ConfigureAwait(false);
            return new Api.StopSpatialAnalyzerSdkResponse();
        }
    }
}
