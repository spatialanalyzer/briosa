using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using Briosa.Server.Operations.AnalysisOperations;
using Briosa.Server.Operations.FileOperations;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog.Events;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

/// <summary>
/// How the <c>briosa-caller-restriction</c> header reaches the server on the wire
/// (#443). A raw HTTP/2 request can carry header fields that a gRPC client library
/// never sends: two separate fields of one name, or a binary header that is not valid
/// base64. The server reads the restriction from the request's raw headers, so two
/// fields are always two values, and it never decodes a header it does not own.
/// </summary>
public sealed class CallerRestrictionTransportTests
{
    private const string HeaderName = OperationCallerRestriction.HeaderName;
    private const string GetWorkingDirectoryPath = "/briosa.FileOperations/GetWorkingDirectory";
    private const string QueryPointsToObjects = "analysis_operations.query_points_to_objects";

    public static TheoryData<string, string> SeparateFields() => new()
    {
        // Their concatenation with a comma, "...,filesystem_write", is grammatical.
        { "profile=read-only; deny=interactive_ui", "filesystem_write" },
        { "deny=interactive_ui", "destructive" },
        { "profile=read-only", "profile=read-only" }
    };

    [Theory]
    [MemberData(nameof(SeparateFields))]
    public async Task TwoSeparateHeaderFieldsAreRejectedBeforeDispatch(string first, string second)
    {
        var host = await RawHost.StartAsync(OperationPolicyTests.CreatePolicy()).ConfigureAwait(true);
        await using var running = host.ConfigureAwait(true);

        // One field in the request headers and one in the content headers: HttpClient
        // writes them as two HTTP/2 header fields, as any other HTTP/2 client can.
        var response = await host.SendAsync(GetWorkingDirectoryPath, new Api.GetWorkingDirectoryRequest(), request =>
        {
            Assert.True(request.Headers.TryAddWithoutValidation(HeaderName, first));
            Assert.True(request.Content!.Headers.TryAddWithoutValidation(HeaderName, second));
        }).ConfigureAwait(true);
        await host.StopAsync().ConfigureAwait(true);

        // The server received two values, not one joined value.
        Assert.Equal([2], host.RawValueCounts);
        response.AssertInvalidCallerRestriction("file_operations.get_working_directory", Api.ReplaySafety.Safe);
        Assert.Equal(0, host.Dispatcher.Submissions);
        host.AssertRejectionAudited();
    }

    [Fact]
    public async Task AnUndecodableBinaryRestrictionIsRejectedAndAudited()
    {
        var host = await RawHost.StartAsync(OperationPolicyTests.CreatePolicy()).ConfigureAwait(true);
        await using var running = host.ConfigureAwait(true);

        var response = await host.SendAsync(GetWorkingDirectoryPath, new Api.GetWorkingDirectoryRequest(), request =>
            Assert.True(request.Headers.TryAddWithoutValidation(HeaderName + "-bin", "!!not base64!!")))
            .ConfigureAwait(true);
        await host.StopAsync().ConfigureAwait(true);

        response.AssertInvalidCallerRestriction("file_operations.get_working_directory", Api.ReplaySafety.Safe);
        Assert.Equal(0, host.Dispatcher.Submissions);
        host.AssertRejectionAudited();
    }

    [Fact]
    public async Task AnUnrelatedUndecodableBinaryHeaderChangesNothing()
    {
        var host = await RawHost.StartAsync(OperationPolicyTests.CreatePolicy()).ConfigureAwait(true);
        await using var running = host.ConfigureAwait(true);

        // The same admitted call, without and then with unrelated binary headers that are
        // not valid base64. The server owns neither, so it never decodes them.
        var plain = await host.SendAsync(GetWorkingDirectoryPath, new Api.GetWorkingDirectoryRequest(), _ => { })
            .ConfigureAwait(true);
        var noisy = await host.SendAsync(GetWorkingDirectoryPath, new Api.GetWorkingDirectoryRequest(), request =>
        {
            Assert.True(request.Headers.TryAddWithoutValidation("x-unrelated-bin", "!!not base64!!"));
            Assert.True(request.Content!.Headers.TryAddWithoutValidation("x-unrelated-bin", "%%"));
        }).ConfigureAwait(true);
        await host.StopAsync().ConfigureAwait(true);

        // Admitted, mapped, and handed to the worker queue, which reports it full.
        Assert.Equal("8", plain.GrpcStatus);
        Assert.Equal(plain.GrpcStatus, noisy.GrpcStatus);
        Assert.Equal(plain.GrpcMessage, noisy.GrpcMessage);
        Assert.Equal(plain.ErrorTrailer, noisy.ErrorTrailer);
        Assert.Equal(2, host.Dispatcher.Mapped);
        Assert.Equal(2, host.Events.Count(logEvent => EventId(logEvent) == 2002));
    }

    /// <summary>
    /// The second evaluation, after mapping and before dispatch, also applies the
    /// restriction: a request that becomes one the caller denies during mapping never
    /// reaches the worker.
    /// </summary>
    [Fact]
    public async Task TheSecondEvaluationAppliesTheRestrictionAfterMapping()
    {
        var dispatcher = new MappingDispatcher();
        var executor = new OperationExecutor(
            new PolicyEnforcingWorkerCommandExecutor(dispatcher, dispatcher,
                OperationPolicyTests.CreatePolicy(settings: new() { ["Flags:interactive_ui"] = "allow" }),
                new OperationAuditLogger(NullLogger<OperationAuditLogger>.Instance)),
            new OperationAuditLogger(NullLogger<OperationAuditLogger>.Instance),
            TimeProvider.System);
        var request = OperationConditionalOptionTests.RequestPopulator.Create<Api.QueryPointsToObjectsRequest>();
        request.ShowResultsDialog = false;
        var restriction = OperationCallerRestrictionHeader.Of(CallerRestrictionPolicyTests.Parse("deny=interactive_ui"));

        // The first evaluation admits the request without the dialog; the mapping then
        // turns the dialog on, so the second evaluation must deny it.
        var exception = await Assert.ThrowsAsync<RpcException>(() => executor.ExecuteAsync(
            request,
            QueryPointsToObjectsOperation.Descriptor,
            mapped =>
            {
                mapped.ShowResultsDialog = true;
                return QueryPointsToObjectsOperation.CreateCommand(mapped);
            },
            QueryPointsToObjectsOperation.OutputContracts,
            QueryPointsToObjectsOperation.CreateResult,
            CancellationToken.None,
            callerRestriction: restriction)).ConfigureAwait(true);

        Assert.Equal(1, dispatcher.Submissions);
        Assert.Equal(1, dispatcher.Mapped);
        Assert.Equal(0, dispatcher.Dispatched);
        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
        var error = Api.OperationError.Parser.ParseFrom(Assert.Single(exception.Trailers).ValueBytes);
        Assert.Equal(Api.OperationFailureKind.Validation, error.Kind);
        Assert.Equal("request-validation-failed", error.DiagnosticCode);
        Assert.Equal(Api.ExecutionDisposition.NotStarted, error.ExecutionDisposition);
        Assert.Equal(Api.ReplayGuidance.DoNotReplay, error.ReplayGuidance);
        Assert.Null(error.RequestValidation);
        Assert.Equal(QueryPointsToObjects, error.OperationId);
    }

    private static int EventId(LogEvent logEvent) =>
        logEvent.Properties.TryGetValue("EventId", out var value) && value is StructureValue structure &&
        structure.Properties.FirstOrDefault(property => property.Name == "Id")?.Value is ScalarValue { Value: int id }
            ? id
            : -1;

    private static string? Text(LogEvent logEvent, string name) =>
        logEvent.Properties.TryGetValue(name, out var value) && value is ScalarValue scalar
            ? scalar.Value?.ToString()
            : null;

    /// <summary>The gRPC outcome of a raw HTTP/2 call, read from the response headers and trailers.</summary>
    private sealed record RawResponse(string? GrpcStatus, string? GrpcMessage, byte[]? ErrorTrailer)
    {
        public void AssertInvalidCallerRestriction(string operationId, Api.ReplaySafety replaySafety)
        {
            Assert.Equal("3", GrpcStatus);
            Assert.Equal("The request is invalid.", GrpcMessage);
            Assert.Equal(
                Assert.Single(GrpcOperationOutcomeMapper.CreateValidationFailure(
                    operationId, "caller-restriction-invalid", replaySafety: replaySafety).Trailers).ValueBytes,
                ErrorTrailer);
            var error = Api.OperationError.Parser.ParseFrom(ErrorTrailer);
            Assert.Equal(Api.ExecutionDisposition.NotStarted, error.ExecutionDisposition);
            Assert.Equal(Api.OperationFailureKind.Validation, error.Kind);
        }
    }

    /// <summary>
    /// A loopback gRPC server with the production registration, the policy enforcer, the
    /// production log provider, and a middleware that records how many raw values the
    /// restriction header has before gRPC reads the request.
    /// </summary>
    private sealed class RawHost : IAsyncDisposable
    {
        private readonly WebApplication _app;
        private readonly BriosaLogProvider _logs;
        private readonly ObservabilityTests.CaptureSink _sink;
        private readonly ConcurrentQueue<int> _rawValueCounts;

        private RawHost(WebApplication app, BriosaLogProvider logs, ObservabilityTests.CaptureSink sink,
            MappingDispatcher dispatcher, ConcurrentQueue<int> rawValueCounts)
        {
            _app = app;
            _logs = logs;
            _sink = sink;
            _rawValueCounts = rawValueCounts;
            Dispatcher = dispatcher;
            Address = new Uri(Assert.Single(app.Urls));
        }

        public Uri Address { get; }

        public MappingDispatcher Dispatcher { get; }

        public int[] RawValueCounts => [.. _rawValueCounts];

        public IReadOnlyList<LogEvent> Events => [.. _sink.Events];

        public static async Task<RawHost> StartAsync(OperationPolicy policy)
        {
            var sink = new ObservabilityTests.CaptureSink();
            var logs = new BriosaLogProvider(ObservabilityTests.Options(), new BriosaLogHealth(), sink);
            var dispatcher = new MappingDispatcher();
            var counts = new ConcurrentQueue<int>();
            var builder = WebApplication.CreateSlimBuilder();
            builder.Logging.ClearProviders();
            builder.Logging.SetMinimumLevel(LogLevel.Trace);
            builder.Logging.AddFilter(typeof(OperationAuditLogger).FullName, LogLevel.Trace);
            builder.Logging.AddProvider(logs);
            builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0,
                listener => listener.Protocols = HttpProtocols.Http2));
            builder.Services.AddBriosaGrpc();
            builder.Services.AddSingleton(services =>
            {
                var audit = new OperationAuditLogger(services.GetRequiredService<ILogger<OperationAuditLogger>>());
                return new OperationExecutor(
                    new PolicyEnforcingWorkerCommandExecutor(dispatcher, dispatcher, policy, audit),
                    audit, TimeProvider.System);
            });
            var app = builder.Build();
            app.Use(async (context, next) =>
            {
                if (context.Request.Headers.TryGetValue(HeaderName, out var values))
                {
                    counts.Enqueue(values.Count);
                }

                await next(context).ConfigureAwait(true);
            });
            app.MapGrpcService<FileOperationsService>();
            app.MapGrpcService<AnalysisOperationsService>();
            await app.StartAsync().ConfigureAwait(true);
            return new RawHost(app, logs, sink, dispatcher, counts);
        }

        public async Task<RawResponse> SendAsync(string path, Api.GetWorkingDirectoryRequest message,
            Action<HttpRequestMessage> addHeaders)
        {
            var frame = new byte[5 + message.CalculateSize()];
            BinaryPrimitives.WriteUInt32BigEndian(frame.AsSpan(1, 4), (uint)(frame.Length - 5));
            message.WriteTo(frame.AsSpan(5));
            using var handler = new SocketsHttpHandler();
            using var client = new HttpClient(handler, disposeHandler: false);
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(Address, path))
            {
                Version = HttpVersion.Version20,
                VersionPolicy = HttpVersionPolicy.RequestVersionExact,
                Content = new ByteArrayContent(frame)
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/grpc");
            request.Headers.TE.Add(new TransferCodingWithQualityHeaderValue("trailers"));
            addHeaders(request);
            using var response = await client.SendAsync(request).ConfigureAwait(true);
            _ = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(true);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string? Read(string name) =>
                response.TrailingHeaders.TryGetValues(name, out var trailing) ? trailing.Single()
                : response.Headers.TryGetValues(name, out var leading) ? leading.Single()
                : null;
            var error = Read(GrpcOperationOutcomeMapper.ErrorTrailerName);
            return new RawResponse(Read("grpc-status"), Read("grpc-message") is { } text ? Uri.UnescapeDataString(text) : null,
                error is null ? null : Convert.FromBase64String(error.PadRight(error.Length + ((4 - (error.Length % 4)) % 4), '=')));
        }

        public async Task StopAsync()
        {
            await _app.StopAsync().ConfigureAwait(true);
            // The log queue is asynchronous; disposing the provider drains it.
            _logs.Dispose();
        }

        // RequestStarted, then one RpcFailed that agrees with the trailer, and no policy event.
        public void AssertRejectionAudited()
        {
            var events = Events;
            Assert.Single(events, logEvent => EventId(logEvent) == 2001);
            var failed = Assert.Single(events, logEvent => EventId(logEvent) == 2005);
            Assert.Equal("caller-restriction-invalid", Text(failed, "DiagnosticCode"));
            Assert.Equal(nameof(StatusCode.InvalidArgument), Text(failed, "GrpcStatus"));
            Assert.Equal("not_started", Text(failed, "ExecutionDisposition"));
            Assert.DoesNotContain(events, logEvent => EventId(logEvent) is 2002 or 2003);
        }

        public async ValueTask DisposeAsync()
        {
            await _app.DisposeAsync().ConfigureAwait(true);
            _logs.Dispose();
        }
    }

    /// <summary>
    /// Maps each admitted request, as the supervisor does before it enqueues, and counts a
    /// dispatch only when the mapping and the second evaluation succeed.
    /// </summary>
    private sealed class MappingDispatcher : IWorkerCommandDispatcher, IWorkerStatusProvider
    {
        private int _submissions;
        private int _mapped;
        private int _dispatched;

        public int Submissions => Volatile.Read(ref _submissions);

        public int Mapped => Volatile.Read(ref _mapped);

        public int Dispatched => Volatile.Read(ref _dispatched);

        public WorkerLifecycleSnapshot Current { get; } = new(
            WorkerLifecycleState.Stopped, Generation: 0, ProcessId: null, RecoveryCount: 0,
            WorkerTerminationKind.None, "caller-restriction-test", Connection: null, DateTimeOffset.UnixEpoch);

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerCommandSubmission submission,
            Guid correlationId,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _submissions);
            try
            {
                _ = submission.CreateCommand();
            }
            finally
            {
                Interlocked.Increment(ref _mapped);
            }

            Interlocked.Increment(ref _dispatched);
            return Task.FromResult(new WorkerExecutionOutcome(
                WorkerExecutionStatus.Overloaded, WorkerExecutionDisposition.NotStarted,
                Execution: null, Connection: null, "recorded", Generation: 0, correlationId));
        }
    }
}
