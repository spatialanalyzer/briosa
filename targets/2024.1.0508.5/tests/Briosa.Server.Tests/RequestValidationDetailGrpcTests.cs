using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Net;
using System.Text;
using Briosa.Server.Operations;
using Briosa.Server.Operations.ConstructionOperations;
using Briosa.Server.Operations.UtilityOperations;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog.Events;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

/// <summary>
/// #442 through the real gRPC pipeline: a validation failure names its field and
/// constraint in the existing error trailer, and every other part of the failure is
/// byte for byte what Server 0.10.0 sent. Request values reach none of the trailers, the
/// status text, the logs, the audit records, or the telemetry.
/// </summary>
public sealed class RequestValidationDetailGrpcTests
{
    private const string Sentinel = RequestValidationDetailTests.TextSentinel;
    private const double NumberSentinel = RequestValidationDetailTests.NumberSentinel + 0.25;
    private static readonly string[] TransportHeaders = ["date", "server"];

    public static TheoryData<string> Cases() => [.. InvalidRequests().Keys];

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task ValidationFailureAddsOnlyTheFieldDetail(string name)
    {
        var (operation, call, path, constraint) = InvalidRequests()[name];
        var worker = new RecordingWorker();
        var host = await GrpcTestHost.StartAsync<UtilityOperationsService, ConstructionOperationsService>(worker)
            .ConfigureAwait(true);
        await using var running = host.ConfigureAwait(true);

        var exception = await Assert.ThrowsAsync<RpcException>(() => call(host.Channel)).ConfigureAwait(true);

        Assert.Empty(worker.Commands);
        // Kestrel adds date and server to a trailers-only response; Briosa sends one entry.
        var trailer = Assert.Single(exception.Trailers, entry => entry.Key.StartsWith("briosa", StringComparison.Ordinal));
        Assert.Equal(GrpcOperationOutcomeMapper.ErrorTrailerName, trailer.Key);
        Assert.All(exception.Trailers.Where(entry => entry != trailer), entry => Assert.Contains(entry.Key, TransportHeaders));
        var error = Api.OperationError.Parser.ParseFrom(trailer.ValueBytes);
        Assert.Equal(new Api.RequestValidationDetails { FieldPath = path, Constraint = constraint }, error.RequestValidation);

        // The failure the server sent before the detail existed.
        var before = GrpcOperationOutcomeMapper.CreateValidationFailure(
            operation.OperationId, "request-validation-failed", replaySafety: operation.ReplaySafety);
        Assert.Equal(before.StatusCode, exception.StatusCode);
        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
        Assert.Equal(before.Status.Detail, exception.Status.Detail);
        Assert.Equal("The request is invalid.", exception.Status.Detail);
        var beforeBytes = Assert.Single(before.Trailers).ValueBytes;
        Assert.Equal(beforeBytes, trailer.ValueBytes[..beforeBytes.Length]);
        var remainder = Api.OperationError.Parser.ParseFrom(trailer.ValueBytes[beforeBytes.Length..]);
        Assert.Equal(new Api.OperationError { RequestValidation = error.RequestValidation }, remainder);

        var unchanged = error.Clone();
        unchanged.RequestValidation = null;
        Assert.Equal(new Api.OperationError
        {
            OperationId = operation.OperationId,
            Kind = Api.OperationFailureKind.Validation,
            DiagnosticCode = "request-validation-failed",
            WorkerGeneration = 0,
            ExecutionDisposition = Api.ExecutionDisposition.NotStarted,
            RecoveryGuidance = Api.RecoveryGuidance.None,
            ReplayGuidance = Api.ReplayGuidance.DoNotReplay,
            ReplaySafety = operation.ReplaySafety
        }, unchanged);
        Assert.Null(error.PolicyDenial);
        Assert.Null(error.MpExecution);
    }

    // The mapping throws each failure for a real ScaleObjectsRequest, so only the server's
    // own path check stands between a bad path and the trailer.
    [Fact]
    public async Task AnArgumentFailureThatNamesNoFieldOfTheRequestHasNoDetailAndNoText()
    {
        var operation = ScaleObjectsOperation.Descriptor;
        var logs = new RecordingLogger();
        var executor = new OperationExecutor(new RecordingWorker(), new OperationAuditLogger(logs), TimeProvider.System);
        Exception[] failures =
        [
            new ArgumentException(Sentinel, Sentinel),
            new ArgumentOutOfRangeException(Sentinel, NumberSentinel, Sentinel),
            new ArgumentNullException(Sentinel, Sentinel),
            // Value-shaped paths, such as text built from a request value.
            RequestFieldViolation.Required(Sentinel),
            RequestFieldViolation.NotBlank($"objects.{Sentinel}"),
            RequestFieldViolation.Range("objects.object_name\n"),
            // Identifier-shaped paths that do not resolve to a field of this request: a
            // caller's object name, an unknown nested field, a field of a scalar, the
            // C# property name.
            RequestFieldViolation.Required("project_alpha"),
            RequestFieldViolation.NotBlank("objects.project_alpha"),
            RequestFieldViolation.Required("scale_factor.value"),
            RequestFieldViolation.NotEmpty("Objects"),
            // Real fields with a constraint that cannot apply to them.
            RequestFieldViolation.NotBlank("scale_factor"),
            RequestFieldViolation.Required("objects"),
            RequestFieldViolation.ElementCount("objects.object_name"),
            RequestFieldViolation.OneofRequired("objects")
        ];

        foreach (var failure in failures)
        {
            var exception = await Assert.ThrowsAsync<RpcException>(() => executor.ExecuteAsync(
                new Api.ScaleObjectsRequest { ScaleFactor = 2 }, operation, _ => throw failure, [],
                _ => new Api.ScaleObjectsResult(), CancellationToken.None)).ConfigureAwait(true);

            Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
            Assert.Equal("The request is invalid.", exception.Status.Detail);
            var trailer = Assert.Single(exception.Trailers);
            var error = Api.OperationError.Parser.ParseFrom(trailer.ValueBytes);
            Assert.Null(error.RequestValidation);
            Assert.Equal(
                Assert.Single(GrpcOperationOutcomeMapper.CreateValidationFailure(operation.OperationId,
                    "request-validation-failed", replaySafety: operation.ReplaySafety).Trailers).ValueBytes,
                trailer.ValueBytes);
            AssertNoSentinel(Encoding.UTF8.GetString(trailer.ValueBytes));
        }

        AssertNoSentinel(logs.AllText);
    }

    [Fact]
    public void AViolationMessageIsFixedText()
    {
        foreach (var violation in new[]
                 {
                     RequestFieldViolation.Required("project_alpha"),
                     RequestFieldViolation.NotBlank(Sentinel),
                     RequestFieldViolation.SupportedChoice("objects.object_type")
                 })
        {
            Assert.Equal("A request field failed validation.", violation.Message);
            Assert.Null(violation.ParamName);
            Assert.DoesNotContain(violation.FieldPath, violation.ToString(), StringComparison.Ordinal);
        }
    }

    // Detail follows the origin, not the exception type: only the violation that the
    // request mapping threw names a field.
    [Fact]
    public async Task AViolationTheRequestMappingDidNotThrowHasNoFieldDetail()
    {
        var operation = ScaleObjectsOperation.Descriptor;
        var valid = new Api.ScaleObjectsRequest
        {
            Objects = { new Api.CollectionObjectName { ObjectName = "Plane" } },
            ScaleFactor = 2
        };
        Task<Api.ScaleObjectsResult> Run(IWorkerCommandExecutor worker, Api.ScaleObjectsRequest request) =>
            new OperationExecutor(worker, new OperationAuditLogger(new RecordingLogger()), TimeProvider.System)
                .ExecuteAsync(request, operation, ScaleObjectsOperation.CreateCommand,
                    ScaleObjectsOperation.OutputContracts, ScaleObjectsOperation.CreateResult, CancellationToken.None);

        // Thrown after the command was mapped and handed on: possibly started, so neither
        // a validation failure nor NOT_STARTED.
        var afterDispatch = new ThrowingWorker(mapFirst: true, RequestFieldViolation.Required("scale_factor"));
        var late = await Assert.ThrowsAsync<RpcException>(() => Run(afterDispatch, valid)).ConfigureAwait(true);
        Assert.True(afterDispatch.Mapped);
        Assert.Equal(StatusCode.Internal, late.StatusCode);
        var lateError = Api.OperationError.Parser.ParseFrom(Assert.Single(late.Trailers).ValueBytes);
        Assert.Equal(Api.OperationFailureKind.Internal, lateError.Kind);
        Assert.Equal("request-violation-after-mapping", lateError.DiagnosticCode);
        Assert.Equal(Api.ExecutionDisposition.StartedOutcomeUnknown, lateError.ExecutionDisposition);
        Assert.NotEqual(Api.ReplayGuidance.MayReplay, lateError.ReplayGuidance);
        Assert.Null(lateError.RequestValidation);

        // Thrown before the request was mapped: provably not started, but no field detail.
        var beforeMapping = new ThrowingWorker(mapFirst: false, RequestFieldViolation.Required("scale_factor"));
        var early = await Assert.ThrowsAsync<RpcException>(() => Run(beforeMapping, valid)).ConfigureAwait(true);
        Assert.False(beforeMapping.Mapped);
        Assert.Equal(StatusCode.InvalidArgument, early.StatusCode);
        Assert.Equal(
            Assert.Single(GrpcOperationOutcomeMapper.CreateValidationFailure(operation.OperationId,
                "request-validation-failed", replaySafety: operation.ReplaySafety).Trailers).ValueBytes,
            Assert.Single(early.Trailers).ValueBytes);

        // The mapping's own violation, with the same worker shape, does name the field.
        var own = await Assert.ThrowsAsync<RpcException>(() => Run(new ThrowingWorker(mapFirst: true, null),
            new Api.ScaleObjectsRequest { Objects = { valid.Objects } })).ConfigureAwait(true);
        Assert.Equal(StatusCode.InvalidArgument, own.StatusCode);
        Assert.Equal(new Api.RequestValidationDetails
        {
            FieldPath = "scale_factor",
            Constraint = Api.RequestFieldConstraint.Required
        }, Api.OperationError.Parser.ParseFrom(Assert.Single(own.Trailers).ValueBytes).RequestValidation);
    }

    // A mapping that runs twice breaks the single-mapping assumption, so the server can
    // no longer prove that nothing started: no field detail and no NOT_STARTED once a
    // command was mapped. With no command ever mapped, NOT_STARTED still holds.
    [Fact]
    public async Task ARepeatedMappingNeverClaimsNotStartedWithFieldDetail()
    {
        foreach (var second in new Exception[]
                 {
                     RequestFieldViolation.Required("scale_factor"),
                     new ArgumentException("The second mapping failed.")
                 })
        {
            var (late, lateAudit) = await RunAudited(new TwiceMappingWorker(swallowFirst: false), Twice(null, second))
                .ConfigureAwait(true);
            Assert.Equal(StatusCode.Internal, late.StatusCode);
            var error = Api.OperationError.Parser.ParseFrom(Assert.Single(late.Trailers).ValueBytes);
            Assert.Equal("request-violation-after-mapping", error.DiagnosticCode);
            Assert.Equal(Api.ExecutionDisposition.StartedOutcomeUnknown, error.ExecutionDisposition);
            Assert.Null(error.RequestValidation);
            AssertAuditAgrees(late, lateAudit);
        }

        // The first mapping fails and the worker maps again, which fails too: no command
        // exists, so the generic validation failure stands, without field detail.
        var (neither, neitherAudit) = await RunAudited(new TwiceMappingWorker(swallowFirst: true),
            Twice(RequestFieldViolation.Required("scale_factor"), RequestFieldViolation.NotEmpty("objects")))
            .ConfigureAwait(true);
        Assert.Equal(StatusCode.InvalidArgument, neither.StatusCode);
        var neitherError = Api.OperationError.Parser.ParseFrom(Assert.Single(neither.Trailers).ValueBytes);
        Assert.Equal(Api.ExecutionDisposition.NotStarted, neitherError.ExecutionDisposition);
        Assert.Null(neitherError.RequestValidation);
        AssertAuditAgrees(neither, neitherAudit);
    }

    // The audit record of a failed call states the same disposition and outcome as its
    // typed error, on every path that the request mapping can take through the executor.
    [Fact]
    public async Task TheAuditAgreesWithTheTypedErrorOnEveryMappingPath()
    {
        var valid = new Api.ScaleObjectsRequest
        {
            Objects = { new Api.CollectionObjectName { ObjectName = "Plane" } },
            ScaleFactor = 2
        };
        Func<Api.ScaleObjectsRequest, WorkerMpCommand> map = ScaleObjectsOperation.CreateCommand;
        (IWorkerCommandExecutor Worker, Func<Api.ScaleObjectsRequest, WorkerMpCommand> Map, Api.ScaleObjectsRequest Request,
            Api.ExecutionDisposition Disposition)[] paths =
        [
            // The mapping's own violation.
            (new RecordingWorker(), map, new Api.ScaleObjectsRequest(), Api.ExecutionDisposition.NotStarted),
            // An untyped argument failure from the mapping.
            (new RecordingWorker(), _ => throw new ArgumentException("untyped"), valid, Api.ExecutionDisposition.NotStarted),
            // A typed violation from the dispatch side, before and after mapping.
            (new ThrowingWorker(mapFirst: false, RequestFieldViolation.Required("scale_factor")), map, valid,
                Api.ExecutionDisposition.NotStarted),
            (new ThrowingWorker(mapFirst: true, RequestFieldViolation.Required("scale_factor")), map, valid,
                Api.ExecutionDisposition.StartedOutcomeUnknown),
            // A repeated mapping whose second run fails.
            (new TwiceMappingWorker(swallowFirst: false), Twice(null, RequestFieldViolation.Required("scale_factor")),
                valid, Api.ExecutionDisposition.StartedOutcomeUnknown)
        ];

        foreach (var (worker, mapping, request, disposition) in paths)
        {
            var (exception, audit) = await RunAudited(worker, mapping, request).ConfigureAwait(true);
            Assert.Equal(disposition,
                Api.OperationError.Parser.ParseFrom(Assert.Single(exception.Trailers).ValueBytes).ExecutionDisposition);
            AssertAuditAgrees(exception, audit);
        }
    }

    // The failure audit record matches the typed error: the same status and diagnostic
    // code, and an execution disposition and MP outcome that say the same thing.
    // Validation failures have no worker outcome, and their audit has recorded the
    // disposition as "unspecified" with MP outcome "not_started" since before #442.
    private static void AssertAuditAgrees(RpcException exception, IReadOnlyDictionary<string, object?> audit)
    {
        var error = Api.OperationError.Parser.ParseFrom(Assert.Single(exception.Trailers).ValueBytes);
        Assert.Equal(exception.StatusCode.ToString(), audit["GrpcStatus"]?.ToString());
        Assert.Equal(error.DiagnosticCode, audit["DiagnosticCode"]?.ToString());
        var recorded = audit["ExecutionDisposition"]?.ToString();
        var mpOutcome = audit["MpOutcome"]?.ToString();
        switch (error.ExecutionDisposition)
        {
            case Api.ExecutionDisposition.StartedOutcomeUnknown:
                Assert.Equal("started_outcome_unknown", recorded);
                Assert.Equal("outcome_unknown", mpOutcome);
                break;
            case Api.ExecutionDisposition.NotStarted:
                Assert.True(recorded is "not_started" or "unspecified", recorded);
                Assert.Equal("not_started", mpOutcome);
                break;
            default:
                Assert.Fail($"Unexpected disposition {error.ExecutionDisposition}.");
                break;
        }
    }

    private static async Task<(RpcException Exception, IReadOnlyDictionary<string, object?> Audit)> RunAudited(
        IWorkerCommandExecutor worker, Func<Api.ScaleObjectsRequest, WorkerMpCommand> map,
        Api.ScaleObjectsRequest? request = null)
    {
        var logger = new StructuredLogger();
        var executor = new OperationExecutor(worker, new OperationAuditLogger(logger), TimeProvider.System);
        var exception = await Assert.ThrowsAsync<RpcException>(() => executor.ExecuteAsync(
            request ?? new Api.ScaleObjectsRequest
            {
                Objects = { new Api.CollectionObjectName { ObjectName = "Plane" } },
                ScaleFactor = 2
            },
            ScaleObjectsOperation.Descriptor, map, ScaleObjectsOperation.OutputContracts,
            ScaleObjectsOperation.CreateResult, CancellationToken.None)).ConfigureAwait(true);
        // Event 2005 is the failure audit record (RpcFailed).
        return (exception, Assert.Single(logger.Entries, entry => entry.EventId == 2005).Values);
    }

    // A mapping that maps the request on its first run, or throws `first`, and throws
    // `second` on its next run.
    private static Func<Api.ScaleObjectsRequest, WorkerMpCommand> Twice(Exception? first, Exception second)
    {
        var runs = 0;
        return request => ++runs == 1
            ? first is null ? ScaleObjectsOperation.CreateCommand(request) : throw first
            : throw second;
    }

    [Fact]
    public async Task AWorkerRejectionHasNoDetail()
    {
        var operation = ScaleObjectsOperation.Descriptor;
        var executor = new OperationExecutor(new RejectingWorker(), new OperationAuditLogger(new RecordingLogger()),
            TimeProvider.System);

        var exception = await Assert.ThrowsAsync<RpcException>(() => executor.ExecuteAsync(
            new Api.ScaleObjectsRequest { Objects = { new Api.CollectionObjectName { ObjectName = "Plane" } }, ScaleFactor = 2 },
            operation, ScaleObjectsOperation.CreateCommand, ScaleObjectsOperation.OutputContracts,
            ScaleObjectsOperation.CreateResult, CancellationToken.None)).ConfigureAwait(true);

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
        var error = Api.OperationError.Parser.ParseFrom(Assert.Single(exception.Trailers).ValueBytes);
        Assert.Equal(Api.OperationFailureKind.Validation, error.Kind);
        Assert.Equal("request-encoding-rejected", error.DiagnosticCode);
        Assert.Null(error.RequestValidation);
    }

    [Fact]
    public void TheMapperSetsTheDetailOnlyWhereItIsGiven()
    {
        var details = new Api.RequestValidationDetails
        {
            FieldPath = "scale_factor",
            Constraint = Api.RequestFieldConstraint.Required
        };
        var withDetail = GrpcOperationOutcomeMapper.CreateValidationFailure(
            "utility_operations.scale_objects", "request-validation-failed", 3, Api.ReplaySafety.Unsafe, details);
        var without = GrpcOperationOutcomeMapper.CreateValidationFailure(
            "utility_operations.scale_objects", "request-validation-failed", 3, Api.ReplaySafety.Unsafe);

        Assert.Equal(details, Api.OperationError.Parser.ParseFrom(Assert.Single(withDetail.Trailers).ValueBytes).RequestValidation);
        Assert.Null(Api.OperationError.Parser.ParseFrom(Assert.Single(without.Trailers).ValueBytes).RequestValidation);
        Assert.Equal(without.Status, withDetail.Status);
        Assert.Equal("request-validation-failed", GrpcOperationOutcomeMapper.GetDiagnosticCode(withDetail));
    }

    // Sentinel text and numbers in every value the requests carry, through the production
    // gRPC registration, the policy enforcer, the audit logger and the telemetry, with
    // every log category at Trace and the log pipeline drained before it is read.
    [Fact]
    public async Task NoRequestValueReachesTheTrailersStatusLogsAuditOrTelemetry()
    {
        var sink = new ObservabilityTests.CaptureSink();
        var logHealth = new BriosaLogHealth();
        using var provider = new BriosaLogProvider(ObservabilityTests.Options(), logHealth, sink);
        using var telemetry = new BriosaTelemetry(new BriosaLogHealth());
        var activities = new ConcurrentQueue<Activity>();
        using var activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == BriosaTelemetry.InstrumentationName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activities.Enqueue
        };
        ActivitySource.AddActivityListener(activityListener);
        var measurements = new ConcurrentQueue<string>();
        using var meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter == telemetry.Meter)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };
        meterListener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
            measurements.Enqueue(Render(instrument.Name, tags)));
        meterListener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
            measurements.Enqueue(Render(instrument.Name, tags)));
        meterListener.Start();

        var dispatcher = new MappingDispatcher();
        var builder = WebApplication.CreateSlimBuilder();
        // The server's appsettings.json in the test output would hold categories at
        // Information; the check needs every category, Debug audit records included.
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Logging:LogLevel:Default"] = "Trace"
        });
        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(LogLevel.Trace);
        builder.Logging.AddProvider(provider);
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0,
            listener => listener.Protocols = HttpProtocols.Http2));
        builder.Services.AddBriosaGrpc();
        builder.Services.AddSingleton(services =>
        {
            var audit = new OperationAuditLogger(services.GetRequiredService<ILogger<OperationAuditLogger>>());
            return new OperationExecutor(
                new PolicyEnforcingWorkerCommandExecutor(dispatcher, dispatcher,
                    OperationPolicyTests.CreatePolicy(profile: "full"), audit),
                audit, TimeProvider.System, telemetry);
        });
        var app = builder.Build();
        app.MapGrpcService<UtilityOperationsService>();
        app.MapGrpcService<ConstructionOperationsService>();
        await app.StartAsync().ConfigureAwait(true);
        await using var running = app.ConfigureAwait(true);
        using var channel = Grpc.Net.Client.GrpcChannel.ForAddress(Assert.Single(app.Urls));

        var failures = new List<RpcException>();
        foreach (var (_, call, _, _) in SentinelRequests().Values)
        {
            failures.Add(await Assert.ThrowsAsync<RpcException>(() => call(channel)).ConfigureAwait(true));
        }

        // A valid request with the same values maps and completes, so the values were read.
        await new Api.UtilityOperations.UtilityOperationsClient(channel).ScaleObjectsAsync(new()
        {
            Objects = { SentinelObject() },
            ScaleFactor = NumberSentinel
        }).ResponseAsync.ConfigureAwait(true);
        await app.StopAsync().ConfigureAwait(true);
        meterListener.RecordObservableInstruments();
        // Drain the asynchronous log pipeline before reading what it wrote, so the check
        // below cannot pass on a partial log.
        provider.Dispose();
        Assert.Equal(0, logHealth.Dropped);
        Assert.Equal(0, logHealth.Failures);
        var logEvents = sink.Events.ToArray();

        Assert.Equal(SentinelRequests().Count, failures.Count);
        Assert.Equal(SentinelRequests().Count + 1, dispatcher.Submissions);
        Assert.Equal(1, dispatcher.Mapped);
        foreach (var (failure, expected) in failures.Zip(SentinelRequests().Values))
        {
            Assert.Equal(StatusCode.InvalidArgument, failure.StatusCode);
            var error = Api.OperationError.Parser.ParseFrom(Assert.Single(failure.Trailers,
                entry => entry.Key == GrpcOperationOutcomeMapper.ErrorTrailerName).ValueBytes);
            Assert.Equal(expected.Path, error.RequestValidation.FieldPath);
            Assert.Equal(expected.Constraint, error.RequestValidation.Constraint);
            AssertNoSentinel(failure.Status.Detail);
            AssertNoSentinel(failure.Message);
            foreach (var entry in failure.Trailers)
            {
                AssertNoSentinel(entry.Key);
                AssertNoSentinel(entry.IsBinary ? Encoding.UTF8.GetString(entry.ValueBytes) : entry.Value);
                AssertNoSentinel(entry.IsBinary ? Encoding.Latin1.GetString(entry.ValueBytes) : entry.Value);
            }
        }

        // Every audit record of these calls arrived and belongs to its own call. Grouped by
        // correlation ID, each call has exactly one start, one policy decision and one
        // terminal record, all for one operation; the invalid calls end in a validation
        // failure and the valid one in a completion.
        string? Property(LogEvent logEvent, string name) =>
            logEvent.Properties.TryGetValue(name, out var value) ? (value as ScalarValue)?.Value?.ToString() : null;
        string[] audited = ["RequestStarted", "PolicyAllowed", "RpcFailed", "RpcCompleted"];
        var calls = logEvents
            .Where(logEvent => audited.Contains(logEvent.MessageTemplate.Text))
            .GroupBy(logEvent => Property(logEvent, "CorrelationId") ?? string.Empty)
            .ToArray();
        Assert.DoesNotContain(calls, call => call.Key.Length == 0);
        var endings = new List<string>();
        foreach (var call in calls)
        {
            var terminal = Assert.Single(call, logEvent => logEvent.MessageTemplate.Text is "RpcFailed" or "RpcCompleted");
            Assert.Equal(
                new[] { "PolicyAllowed", "RequestStarted", terminal.MessageTemplate.Text }.Order(StringComparer.Ordinal),
                call.Select(logEvent => logEvent.MessageTemplate.Text).Order(StringComparer.Ordinal));
            var operationId = Assert.Single(call.Select(logEvent => Property(logEvent, "OperationId")).Distinct());
            endings.Add(terminal.MessageTemplate.Text == "RpcFailed"
                ? $"{operationId} failed {Property(terminal, "DiagnosticCode")}"
                : $"{operationId} completed");
        }

        Assert.Equal(
            SentinelRequests().Values.Select(expected => $"{expected.Operation.OperationId} failed request-validation-failed")
                .Append($"{ScaleObjectsOperation.Descriptor.OperationId} completed")
                .Order(StringComparer.Ordinal),
            endings.Order(StringComparer.Ordinal));
        foreach (var logEvent in logEvents)
        {
            AssertNoSentinel(Render(logEvent));
        }

        Assert.NotEmpty(activities);
        foreach (var activity in activities)
        {
            AssertNoSentinel(activity.DisplayName);
            AssertNoSentinel(activity.OperationName);
            foreach (var (key, value) in activity.TagObjects)
            {
                AssertNoSentinel($"{key}={value}");
            }

            foreach (var activityEvent in activity.Events)
            {
                AssertNoSentinel(activityEvent.Name);
                foreach (var (key, value) in activityEvent.Tags)
                {
                    AssertNoSentinel($"{key}={value}");
                }
            }
        }

        Assert.NotEmpty(measurements);
        foreach (var measurement in measurements)
        {
            AssertNoSentinel(measurement);
        }
    }

    private static void AssertNoSentinel(string? text)
    {
        text ??= string.Empty;
        Assert.DoesNotContain(Sentinel, text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(RequestValidationDetailTests.NumberSentinel.ToString(CultureInfo.InvariantCulture),
            text, StringComparison.Ordinal);
    }

    private static string Render(LogEvent logEvent)
    {
        var text = new StringBuilder(logEvent.RenderMessage(CultureInfo.InvariantCulture));
        text.Append(' ').Append(logEvent.MessageTemplate.Text);
        foreach (var (key, value) in logEvent.Properties)
        {
            text.Append(' ').Append(key).Append('=').Append(value.ToString());
        }

        return text.Append(' ').Append(logEvent.Exception).ToString();
    }

    private static string Render(string name, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var text = new StringBuilder(name);
        foreach (var (key, value) in tags)
        {
            text.Append(' ').Append(key).Append('=').Append(value);
        }

        return text.ToString();
    }

    // Every value these requests send is a sentinel that the leak checks search for; the
    // object type is left to the operation's default instead of a fixed choice.
    private static Api.CollectionObjectName SentinelObject() => new()
    {
        CollectionName = Sentinel,
        ObjectName = Sentinel
    };

    private static Api.Transform SentinelTransform(int count)
    {
        var transform = new Api.Transform();
        transform.Values.AddRange(Enumerable.Repeat(NumberSentinel, count));
        return transform;
    }

    private static Dictionary<string, Case> InvalidRequests() => new(StringComparer.Ordinal)
    {
        ["absent scalar"] = new(ScaleObjectsOperation.Descriptor,
            channel => Utility(channel).ScaleObjectsAsync(new() { Objects = { SentinelObject() } }).ResponseAsync,
            "scale_factor", Api.RequestFieldConstraint.Required),
        ["empty list"] = new(ScaleObjectsOperation.Descriptor,
            channel => Utility(channel).ScaleObjectsAsync(new() { ScaleFactor = 2 }).ResponseAsync,
            "objects", Api.RequestFieldConstraint.NotEmpty),
        ["unknown enum in a list element"] = new(ScaleObjectsOperation.Descriptor,
            channel => Utility(channel).ScaleObjectsAsync(new()
            {
                Objects = { new Api.CollectionObjectName { ObjectName = "Plane", ObjectType = (Api.ObjectType)9999 } },
                ScaleFactor = 2
            }).ResponseAsync,
            "objects.object_type", Api.RequestFieldConstraint.SupportedChoice),
        ["absent message"] = new(GetPointNotesOperation.Descriptor,
            channel => Utility(channel).GetPointNotesAsync(new()).ResponseAsync,
            "point", Api.RequestFieldConstraint.Required),
        ["blank nested text"] = new(GetPointNotesOperation.Descriptor,
            channel => Utility(channel).GetPointNotesAsync(new()
            {
                Point = new Api.PointName { CollectionName = "A", GroupName = "G", TargetName = "  " }
            }).ResponseAsync,
            "point.target_name", Api.RequestFieldConstraint.NotBlank),
        ["wrong element count"] = new(ConstructFrameOperation.Descriptor,
            channel => Construction(channel).ConstructFrameAsync(new()
            {
                NewFrameName = new Api.CollectionObjectName { CollectionName = "A", ObjectName = "F" },
                TransformInWorkingCoordinates = SentinelTransform(15)
            }).ResponseAsync,
            "transform_in_working_coordinates.values", Api.RequestFieldConstraint.ElementCount)
    };

    private static Dictionary<string, Case> SentinelRequests() => new(StringComparer.Ordinal)
    {
        ["blank nested text"] = new(GetPointNotesOperation.Descriptor,
            channel => Utility(channel).GetPointNotesAsync(new()
            {
                Point = new Api.PointName { CollectionName = Sentinel, GroupName = Sentinel, TargetName = " " }
            }).ResponseAsync,
            "point.target_name", Api.RequestFieldConstraint.NotBlank),
        ["unknown enum"] = new(ScaleObjectsOperation.Descriptor,
            channel => Utility(channel).ScaleObjectsAsync(new()
            {
                // The rejected enum value is itself the numeric sentinel.
                Objects =
                {
                    new Api.CollectionObjectName
                    {
                        CollectionName = Sentinel,
                        ObjectName = Sentinel,
                        ObjectType = (Api.ObjectType)RequestValidationDetailTests.NumberSentinel
                    }
                },
                ScaleFactor = NumberSentinel
            }).ResponseAsync,
            "objects.object_type", Api.RequestFieldConstraint.SupportedChoice),
        ["absent scalar"] = new(ScaleObjectsOperation.Descriptor,
            channel => Utility(channel).ScaleObjectsAsync(new() { Objects = { SentinelObject() } }).ResponseAsync,
            "scale_factor", Api.RequestFieldConstraint.Required),
        ["wrong element count"] = new(ConstructFrameOperation.Descriptor,
            channel => Construction(channel).ConstructFrameAsync(new()
            {
                NewFrameName = SentinelObject(),
                TransformInWorkingCoordinates = SentinelTransform(17)
            }).ResponseAsync,
            "transform_in_working_coordinates.values", Api.RequestFieldConstraint.ElementCount)
    };

    private static Api.UtilityOperations.UtilityOperationsClient Utility(Grpc.Net.Client.GrpcChannel channel) => new(channel);

    private static Api.ConstructionOperations.ConstructionOperationsClient Construction(Grpc.Net.Client.GrpcChannel channel) =>
        new(channel);

    private sealed record Case(
        OperationDescriptor Operation,
        Func<Grpc.Net.Client.GrpcChannel, Task> Call,
        string Path,
        Api.RequestFieldConstraint Constraint);

    /// <summary>Maps each admitted request the way the supervisor does, and completes it.</summary>
    private sealed class MappingDispatcher : IWorkerCommandDispatcher, IWorkerStatusProvider
    {
        private int _submissions;
        private int _mapped;

        public int Submissions => _submissions;

        public int Mapped => _mapped;

        public WorkerLifecycleSnapshot Current { get; } = new(
            WorkerLifecycleState.Ready, Generation: 1, ProcessId: null, RecoveryCount: 0,
            WorkerTerminationKind.None, "ready", Connection: null, DateTimeOffset.UnixEpoch);

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerCommandSubmission submission, Guid correlationId, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _submissions);
            submission.CreateCommand();
            Interlocked.Increment(ref _mapped);
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, [], "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1, correlationId));
        }
    }

    /// <summary>
    /// Throws a typed violation from the dispatch side, after mapping the request or
    /// without mapping it; with no violation it only maps.
    /// </summary>
    private sealed class ThrowingWorker(bool mapFirst, RequestFieldViolation? violation) : IWorkerCommandExecutor
    {
        public bool Mapped { get; private set; }

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("The submission overload is the one under test.");

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerCommandSubmission submission, Guid correlationId, CancellationToken cancellationToken = default)
        {
            if (mapFirst)
            {
                submission.CreateCommand();
                Mapped = true;
            }

            throw (Exception?)violation ?? new InvalidOperationException("The request mapped.");
        }
    }

    /// <summary>
    /// Runs the submission's mapping twice, which the pipeline never does, optionally
    /// swallowing a failure of the first run.
    /// </summary>
    private sealed class TwiceMappingWorker(bool swallowFirst) : IWorkerCommandExecutor
    {
        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("The submission overload is the one under test.");

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerCommandSubmission submission, Guid correlationId, CancellationToken cancellationToken = default)
        {
            try
            {
                submission.CreateCommand();
            }
            catch (ArgumentException) when (swallowFirst)
            {
            }

            submission.CreateCommand();
            throw new InvalidOperationException("Both mappings succeeded.");
        }
    }

    /// <summary>Records each audit event by id with its structured values.</summary>
    private sealed class StructuredLogger : ILogger<OperationAuditLogger>
    {
        public ConcurrentQueue<(int EventId, IReadOnlyDictionary<string, object?> Values)> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Entries.Enqueue((eventId.Id, (state as IReadOnlyList<KeyValuePair<string, object?>> ?? [])
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal)));
    }

    private sealed class RejectingWorker : IWorkerCommandExecutor
    {
        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.RequestRejected,
                WorkerExecutionDisposition.NotStarted, null, null, "request-encoding-rejected", 4));
    }

    private sealed class RecordingLogger : ILogger<OperationAuditLogger>
    {
        private readonly ConcurrentQueue<string> _entries = new();

        public string AllText => string.Join('\n', _entries);

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var text = new StringBuilder(formatter(state, exception));
            if (state is IReadOnlyList<KeyValuePair<string, object?>> values)
            {
                foreach (var (key, value) in values)
                {
                    text.Append(' ').Append(key).Append('=').Append(value);
                }
            }

            _entries.Enqueue(text.Append(' ').Append(exception).ToString());
        }
    }
}
