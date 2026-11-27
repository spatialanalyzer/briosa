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

    [Fact]
    public async Task AnArgumentFailureThatNamesNoFieldHasNoDetailAndNoText()
    {
        var operation = ScaleObjectsOperation.Descriptor;
        var logs = new RecordingLogger();
        var executor = new OperationExecutor(new RecordingWorker(), new OperationAuditLogger(logs), TimeProvider.System);
        Exception[] failures =
        [
            new ArgumentException(Sentinel, Sentinel),
            new ArgumentOutOfRangeException(Sentinel, NumberSentinel, Sentinel),
            new ArgumentNullException(Sentinel, Sentinel),
            // A typed violation whose path is not a schema path, such as one built from a value.
            RequestFieldViolation.Required(Sentinel),
            RequestFieldViolation.NotBlank($"objects.{Sentinel}"),
            RequestFieldViolation.Range("objects.object_name\n")
        ];

        foreach (var failure in failures)
        {
            var exception = await Assert.ThrowsAsync<RpcException>(() => executor.ExecuteAsync<object, object>(
                new object(), operation, _ => throw failure, [], _ => new object(), CancellationToken.None))
                .ConfigureAwait(true);

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
    // every log category at Debug.
    [Fact]
    public async Task NoRequestValueReachesTheTrailersStatusLogsAuditOrTelemetry()
    {
        var sink = new ObservabilityTests.CaptureSink();
        using var provider = new BriosaLogProvider(ObservabilityTests.Options(), new BriosaLogHealth(), sink);
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

        Assert.NotEmpty(sink.Events);
        Assert.Contains(sink.Events, logEvent => logEvent.Properties.TryGetValue("DiagnosticCode", out var code) &&
            code.ToString().Contains("request-validation-failed", StringComparison.Ordinal));
        foreach (var logEvent in sink.Events)
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

    private static Api.CollectionObjectName SentinelObject() => new()
    {
        CollectionName = Sentinel,
        ObjectName = Sentinel,
        ObjectType = Api.ObjectType.Plane
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
                Objects = { new Api.CollectionObjectName { CollectionName = Sentinel, ObjectName = Sentinel, ObjectType = (Api.ObjectType)9999 } },
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
