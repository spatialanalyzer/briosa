using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text;
using Briosa.Server.Operations;
using Briosa.Server.Operations.AnalysisOperations;
using Briosa.Server.Operations.FileOperations;
using Briosa.Server.Operations.InstrumentOperations;
using Briosa.Server.Operations.ViewControl;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
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
/// The <c>briosa-caller-restriction</c> header through the real gRPC pipeline (#443):
/// a well-formed restriction narrows what the server admits, a malformed one fails
/// closed before dispatch, a call without one is unchanged, and no header text leaves
/// the server beyond reviewed profile and flag names.
/// </summary>
public sealed class CallerRestrictionGrpcTests
{
    private const string HeaderName = OperationCallerRestriction.HeaderName;
    private const string QueryPointsToObjects = "analysis_operations.query_points_to_objects";
    private const string GetWorkingDirectory = "file_operations.get_working_directory";
    private const string DeniedStatusText = "The operation is denied by the Briosa operation policy.";

    public static TheoryData<string> Servers() => ["interactive-flag-allow", "operation-allow-override"];

    [Theory]
    [MemberData(nameof(Servers))]
    public async Task AResultsDialogIsDeniedByTheCallerEvenWhereTheServerAdmitsIt(string server)
    {
        var host = await Host.StartAsync(ServerPolicy(server)).ConfigureAwait(true);
        await using var running = host.ConfigureAwait(true);
        var dialog = QueryRequest(showResultsDialog: true);

        // Without the header the server admits the dialog: this is what the caller narrows.
        await host.AssertAdmittedAsync(client => Query(client, dialog, null)).ConfigureAwait(true);

        foreach (var (header, expected) in new[]
        {
            ("profile=read-only; deny=interactive_ui",
                new Api.PolicyDenialDetails { Rule = Api.PolicyDenialRule.CallerFlag, CallerDeniedFlag = "interactive_ui" }),
            ("deny=interactive_ui",
                new Api.PolicyDenialDetails { Rule = Api.PolicyDenialRule.CallerFlag, CallerDeniedFlag = "interactive_ui" }),
            ("profile=read-only",
                new Api.PolicyDenialDetails { Rule = Api.PolicyDenialRule.CallerProfile, CallerAdmissionProfile = "read-only" }),
            ("profile=full",
                new Api.PolicyDenialDetails { Rule = Api.PolicyDenialRule.CallerProfile, CallerAdmissionProfile = "full" })
        })
        {
            expected.RequestOptionField = "show_results_dialog";
            var error = await host.AssertDeniedAsync(
                client => Query(client, dialog, header), QueryPointsToObjects,
                "caller-restriction-option-denied").ConfigureAwait(true);
            Assert.Equal(expected, error.PolicyDenial);
        }

        // The same restriction admits the request without the dialog.
        await host.AssertAdmittedAsync(client =>
            Query(client, QueryRequest(showResultsDialog: false), "profile=read-only; deny=interactive_ui")).ConfigureAwait(true);
        await host.AssertAdmittedAsync(client =>
            Query(client, QueryRequest(showResultsDialog: null), "profile=read-only; deny=interactive_ui")).ConfigureAwait(true);
    }

    [Theory]
    [MemberData(nameof(Servers))]
    public async Task OmittingDisplayViewControlStaysAnExclusiveWorkflowDenial(string server)
    {
        var host = await Host.StartAsync(ServerPolicy(server, profile: "full", extra: new()
        {
            ["Flags:device_session"] = "allow",
            ["Overrides:view_control:set_point_of_view_from_instrument_updates"] = "allow"
        })).ConfigureAwait(true);
        await using var running = host.ConfigureAwait(true);
        var omitted = OperationConditionalOptionTests.RequestPopulator.Create<Api.SetPointOfViewFromInstrumentUpdatesRequest>();
        omitted.ClearDisplayViewControl();

        // The reviewed row is an exclusive workflow, which the server denies before a
        // caller restriction is read: with or without one, the same bytes come back.
        var without = await host.DenyAsync(client => PointOfView(client, omitted, null)).ConfigureAwait(true);
        var with = await host.DenyAsync(client => PointOfView(client, omitted, "profile=read-only; deny=interactive_ui"))
            .ConfigureAwait(true);

        Assert.Equal(StatusCode.PermissionDenied, with.StatusCode);
        Assert.Equal(Trailer(without), Trailer(with));
        var error = Api.OperationError.Parser.ParseFrom(Trailer(with));
        Assert.Equal("operation-isolation-unsupported", error.DiagnosticCode);
        Assert.Equal(new Api.PolicyDenialDetails { Rule = Api.PolicyDenialRule.Exclusive }, error.PolicyDenial);
        Assert.Equal(0, host.Dispatcher.Submissions);
    }

    [Fact]
    public async Task AnOmittedOptionThatOpensUiIsDeniedByTheCallerOnTheWire()
    {
        // A test table whose option opens operator UI when omitted, on an admissible operation.
        var sendsTrue = new OperationConditionalOption(
            QueryPointsToObjects, "show_results_dialog", "Show Results Dialog?",
            OperationOptionEffect.InteractiveUi, OperationOptionTrigger.CallerOption,
            OperationOptionCondition.WhenTrue, OperationOptionAbsence.SendsTrue);
        var host = await Host.StartAsync(OperationPolicyTests.CreatePolicy(
            settings: new() { ["Flags:interactive_ui"] = "allow" },
            requestClassifier: new OperationRequestClassifier([sendsTrue]))).ConfigureAwait(true);
        await using var running = host.ConfigureAwait(true);
        var omitted = QueryRequest(showResultsDialog: null);

        await host.AssertAdmittedAsync(client => Query(client, omitted, null)).ConfigureAwait(true);
        var error = await host.AssertDeniedAsync(client => Query(client, omitted, "profile=read-only; deny=interactive_ui"),
            QueryPointsToObjects, "caller-restriction-option-denied").ConfigureAwait(true);
        Assert.Equal(new Api.PolicyDenialDetails
        {
            Rule = Api.PolicyDenialRule.CallerFlag,
            CallerDeniedFlag = "interactive_ui",
            RequestOptionField = "show_results_dialog"
        }, error.PolicyDenial);
    }

    [Fact]
    public async Task EveryMalformedHeaderRejectsTheCallBeforeAnythingIsDispatched()
    {
        // The server would admit every one of these calls without the header.
        var host = await Host.StartAsync(OperationPolicyTests.CreatePolicy(profile: "full", settings: new()
        {
            ["Flags:interactive_ui"] = "allow"
        })).ConfigureAwait(true);
        await using var running = host.ConfigureAwait(true);
        var cases = CallerRestrictionHeaderTests.MalformedValues
            .Select(item => (item.Name, Headers: new Metadata { { HeaderName, item.Value } }))
            .Concat(
            [
                ("two equal entries", new Metadata { { HeaderName, "profile=read-only" }, { HeaderName, "profile=read-only" } }),
                ("two valid entries", new Metadata { { HeaderName, "profile=read-only" }, { HeaderName, "deny=interactive_ui" } }),
                ("an entry and a fragment", new Metadata { { HeaderName, "deny=interactive_ui" }, { HeaderName, "filesystem_write" } }),
                ("binary entry", new Metadata { { HeaderName + "-bin", Encoding.ASCII.GetBytes("profile=read-only") } }),
                ("text and binary entries", new Metadata
                {
                    { HeaderName, "profile=read-only" },
                    { HeaderName + "-bin", Encoding.ASCII.GetBytes("profile=read-only") }
                })
            ])
            .ToArray();
        var expected = Assert.Single(GrpcOperationOutcomeMapper.CreateValidationFailure(
            GetWorkingDirectory, "caller-restriction-invalid", replaySafety: Api.ReplaySafety.Safe).Trailers).ValueBytes;

        foreach (var (name, headers) in cases)
        {
            var exception = await Assert.ThrowsAsync<RpcException>(() =>
                new Api.FileOperations.FileOperationsClient(host.Channel)
                    .GetWorkingDirectoryAsync(new Api.GetWorkingDirectoryRequest(), headers).ResponseAsync)
                .ConfigureAwait(true);

            Assert.True(exception.StatusCode == StatusCode.InvalidArgument, name);
            Assert.Equal("The request is invalid.", exception.Status.Detail);
            Assert.True(expected.AsSpan().SequenceEqual(Trailer(exception)), name);
            var error = Api.OperationError.Parser.ParseFrom(Trailer(exception));
            Assert.Equal(Api.OperationFailureKind.Validation, error.Kind);
            Assert.Equal("caller-restriction-invalid", error.DiagnosticCode);
            Assert.Equal(Api.ExecutionDisposition.NotStarted, error.ExecutionDisposition);
            Assert.Equal(Api.ReplayGuidance.DoNotReplay, error.ReplayGuidance);
            Assert.Null(error.PolicyDenial);
            Assert.Null(error.RequestValidation);
        }

        Assert.Equal(0, host.Dispatcher.Submissions);
        Assert.Equal(0, host.Dispatcher.Mapped);
        Assert.True(cases.Length >= 45);

        // The header is read before policy: a call the server would deny is still a validation failure.
        var denied = await Assert.ThrowsAsync<RpcException>(() =>
            new Api.InstrumentOperations.InstrumentOperationsClient(host.Channel).StartInstrumentInterfaceAsync(
                new Api.StartInstrumentInterfaceRequest(), new Metadata { { HeaderName, "profile=bogus" } }).ResponseAsync)
            .ConfigureAwait(true);
        Assert.Equal(StatusCode.InvalidArgument, denied.StatusCode);
        Assert.Equal("caller-restriction-invalid", Api.OperationError.Parser.ParseFrom(Trailer(denied)).DiagnosticCode);
    }

    /// <summary>
    /// A call without the header: the decision, status, text, every trailer byte, and
    /// the audit event's properties are what the server produced before #443.
    /// </summary>
    [Fact]
    public async Task ACallWithoutTheHeaderIsDecidedReportedAndAuditedAsBefore()
    {
        var sink = new ObservabilityTests.CaptureSink();
        using var provider = new BriosaLogProvider(ObservabilityTests.Options(), new BriosaLogHealth(), sink);
        var host = await Host.StartAsync(OperationPolicyTests.CreatePolicy(), provider).ConfigureAwait(true);
        await using var running = host.ConfigureAwait(true);

        // A denial for an option alone, and an admission.
        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            Query(new Api.AnalysisOperations.AnalysisOperationsClient(host.Channel), QueryRequest(showResultsDialog: true), null))
            .ConfigureAwait(true);
        await host.AssertAdmittedAsync(client => Query(client, QueryRequest(showResultsDialog: false), null)).ConfigureAwait(true);

        // The bytes Server 0.10.1 wrote for this denial: field by field, in field order.
        var before = new Api.OperationError
        {
            OperationId = QueryPointsToObjects,
            Kind = Api.OperationFailureKind.PolicyDenied,
            DiagnosticCode = "operation-option-denied",
            WorkerGeneration = 0,
            ExecutionDisposition = Api.ExecutionDisposition.NotStarted,
            RecoveryGuidance = Api.RecoveryGuidance.None,
            ReplayGuidance = Api.ReplayGuidance.DoNotReplay,
            ReplaySafety = Api.ReplaySafety.Safe,
            PolicyDenial = new Api.PolicyDenialDetails
            {
                Rule = Api.PolicyDenialRule.Profile,
                AdmissionProfile = "standard",
                RequestOptionField = "show_results_dialog"
            }
        };
        Assert.Equal(StatusCode.PermissionDenied, exception.StatusCode);
        Assert.Equal(DeniedStatusText, exception.Status.Detail);
        Assert.Equal(before.ToByteArray(), Trailer(exception));
        Assert.Single(exception.Trailers, entry => entry.Key.StartsWith("briosa", StringComparison.Ordinal));

        await host.StopAsync().ConfigureAwait(true);
        var rejected = Assert.Single(sink.Events, logEvent => EventId(logEvent) == 2003);
        Assert.Equal(PolicyEventProperties, Names(rejected));
        Assert.Equal("option.show_results_dialog/profile.standard", Text(rejected, "PolicyRule"));
        Assert.Equal("operation-option-denied", Text(rejected, "DiagnosticCode"));
        var allowed = Assert.Single(sink.Events, logEvent => EventId(logEvent) == 2002);
        Assert.Equal(PolicyEventProperties, Names(allowed));
        Assert.DoesNotContain(sink.Events, logEvent => logEvent.Properties.Keys.Any(key =>
            key.StartsWith("Caller", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task ThePolicyEventRecordsTheRestrictionAndWhatItDecidedWithReviewedNamesOnly()
    {
        var sink = new ObservabilityTests.CaptureSink();
        using var provider = new BriosaLogProvider(ObservabilityTests.Options(), new BriosaLogHealth(), sink);
        var host = await Host.StartAsync(
            OperationPolicyTests.CreatePolicy(settings: new() { ["Flags:interactive_ui"] = "allow" }), provider)
            .ConfigureAwait(true);
        var header = "deny=interactive_ui,filesystem_write; profile=read-only";

        await Assert.ThrowsAsync<RpcException>(() => Query(new Api.AnalysisOperations.AnalysisOperationsClient(host.Channel),
            QueryRequest(showResultsDialog: true), header)).ConfigureAwait(true);
        await using var running = host.ConfigureAwait(true);
        await host.AssertAdmittedAsync(client => Query(client, QueryRequest(showResultsDialog: false), header))
            .ConfigureAwait(true);
        await Assert.ThrowsAsync<RpcException>(() => new Api.FileOperations.FileOperationsClient(host.Channel)
            .DeleteGeneralFileAsync(new Api.DeleteGeneralFileRequest(), new Metadata { { HeaderName, header } }).ResponseAsync)
            .ConfigureAwait(true);
        await host.StopAsync().ConfigureAwait(true);

        var events = sink.Events.Where(logEvent => EventId(logEvent) is 2002 or 2003).ToArray();
        Assert.Equal(3, events.Length);
        var expectedNames = PolicyEventProperties.Concat(["CallerProfile", "CallerDeniedFlags", "CallerRestrictionDecision"])
            .Order(StringComparer.Ordinal);
        Assert.All(events, logEvent =>
        {
            Assert.Equal(expectedNames, Names(logEvent));
            Assert.Equal("read-only", Text(logEvent, "CallerProfile"));
            // Vocabulary order, not header order.
            Assert.Equal("filesystem_write,interactive_ui", Text(logEvent, "CallerDeniedFlags"));
        });

        var callerDenied = Assert.Single(events, logEvent => Text(logEvent, "OperationId") == QueryPointsToObjects &&
            EventId(logEvent) == 2003);
        Assert.Equal("denied", Text(callerDenied, "CallerRestrictionDecision"));
        Assert.Equal("option.show_results_dialog/caller.flag.interactive_ui", Text(callerDenied, "PolicyRule"));
        Assert.Equal("caller-restriction-option-denied", Text(callerDenied, "DiagnosticCode"));
        var admitted = Assert.Single(events, logEvent => EventId(logEvent) == 2002);
        Assert.Equal("admitted", Text(admitted, "CallerRestrictionDecision"));
        Assert.Equal("profile.standard", Text(admitted, "PolicyRule"));
        var serverDenied = Assert.Single(events, logEvent => Text(logEvent, "OperationId") == "file_operations.delete_general_file");
        Assert.Equal("not_evaluated", Text(serverDenied, "CallerRestrictionDecision"));
        Assert.Equal("profile.standard", Text(serverDenied, "PolicyRule"));

        // The raw header text appears in no event.
        Assert.All(sink.Events, logEvent => Assert.DoesNotContain(header, Render(logEvent), StringComparison.Ordinal));
    }

    [Fact]
    public async Task NoHeaderTextReachesTheTrailersStatusLogsAuditOrTelemetry()
    {
        const string Sentinel = "qqcallersentinelqq";
        var sink = new ObservabilityTests.CaptureSink();
        using var provider = new BriosaLogProvider(ObservabilityTests.Options(), new BriosaLogHealth(), sink);
        using var telemetry = new BriosaTelemetry(new BriosaLogHealth());
        var activities = new ConcurrentQueue<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == BriosaTelemetry.InstrumentationName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activities.Enqueue
        };
        ActivitySource.AddActivityListener(listener);
        var host = await Host.StartAsync(OperationPolicyTests.CreatePolicy(profile: "full"), provider, telemetry)
            .ConfigureAwait(true);

        var failures = new List<RpcException>();
        foreach (var value in new[]
        {
            $"profile={Sentinel}",
            $"deny={Sentinel}",
            $"{Sentinel}=read-only",
            $"profile=read-only; deny=interactive_ui; {Sentinel}=x",
            $"profile=read-only {Sentinel}",
            $"deny=interactive_ui,{Sentinel.ToUpperInvariant()}"
        })
        {
            failures.Add(await Assert.ThrowsAsync<RpcException>(() => new Api.FileOperations.FileOperationsClient(host.Channel)
                .GetWorkingDirectoryAsync(new Api.GetWorkingDirectoryRequest(), new Metadata { { HeaderName, value } })
                .ResponseAsync).ConfigureAwait(true));
        }

        await host.StopAsync().ConfigureAwait(true);
        await using var running = host.ConfigureAwait(true);
        Assert.Equal(0, host.Dispatcher.Submissions);
        foreach (var failure in failures)
        {
            AssertNoSentinel(failure.Status.Detail);
            AssertNoSentinel(failure.Message);
            foreach (var entry in failure.Trailers)
            {
                AssertNoSentinel(entry.Key);
                AssertNoSentinel(entry.IsBinary ? Encoding.Latin1.GetString(entry.ValueBytes) : entry.Value);
            }
        }

        Assert.Contains(sink.Events, logEvent => Text(logEvent, "DiagnosticCode") == "caller-restriction-invalid");
        Assert.All(sink.Events, logEvent => AssertNoSentinel(Render(logEvent)));
        Assert.NotEmpty(activities);
        foreach (var activity in activities)
        {
            AssertNoSentinel(activity.DisplayName);
            foreach (var (key, value) in activity.TagObjects)
            {
                AssertNoSentinel($"{key}={value}");
            }
        }

        static void AssertNoSentinel(string? text) =>
            Assert.DoesNotContain(Sentinel, text ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    // The properties of PolicyAllowed and PolicyRejected before #443, plus the sink's own.
    private static readonly string[] PolicyEventProperties =
    [
        .. new[]
        {
            "CorrelationId", "OperationId", "Effect", "ExecutionScope", "RiskFlags", "PolicyRule", "DiagnosticCode",
            "SchemaVersion", "ServerInstanceId", "SpatialAnalyzerTarget", "SourceContext", "EventId"
        }.Order(StringComparer.Ordinal)
    ];

    // PolicyDecision is on PolicyRejected only, and a listener in a concurrent test can add trace identifiers.
    private static IEnumerable<string> Names(LogEvent logEvent) =>
        logEvent.Properties.Keys.Where(key => key is not ("PolicyDecision" or "TraceId" or "SpanId"))
            .Order(StringComparer.Ordinal);

    private static int EventId(LogEvent logEvent) =>
        logEvent.Properties.TryGetValue("EventId", out var value) && value is StructureValue structure &&
        structure.Properties.FirstOrDefault(property => property.Name == "Id")?.Value is ScalarValue { Value: int id }
            ? id
            : -1;

    private static string? Text(LogEvent logEvent, string name) =>
        logEvent.Properties.TryGetValue(name, out var value) && value is ScalarValue scalar
            ? scalar.Value?.ToString()
            : null;

    private static string Render(LogEvent logEvent)
    {
        var text = new StringBuilder(logEvent.RenderMessage(CultureInfo.InvariantCulture));
        text.Append(' ').Append(logEvent.MessageTemplate.Text);
        foreach (var (key, value) in logEvent.Properties)
        {
            text.Append(' ').Append(key).Append('=').Append(value);
        }

        return text.Append(' ').Append(logEvent.Exception).ToString();
    }

    private static byte[] Trailer(RpcException exception) =>
        Assert.Single(exception.Trailers, entry => entry.Key == GrpcOperationOutcomeMapper.ErrorTrailerName).ValueBytes;

    private static OperationPolicy ServerPolicy(
        string server,
        string profile = "standard",
        Dictionary<string, string?>? extra = null)
    {
        var settings = new Dictionary<string, string?>(extra ?? [], StringComparer.Ordinal);
        if (server == "interactive-flag-allow")
        {
            settings["Flags:interactive_ui"] = "allow";
        }
        else
        {
            settings["Overrides:analysis_operations:query_points_to_objects"] = "allow";
        }

        return OperationPolicyTests.CreatePolicy(profile: profile, settings: settings);
    }

    private static Api.QueryPointsToObjectsRequest QueryRequest(bool? showResultsDialog)
    {
        var request = OperationConditionalOptionTests.RequestPopulator.Create<Api.QueryPointsToObjectsRequest>();
        if (showResultsDialog is { } value)
        {
            request.ShowResultsDialog = value;
        }
        else
        {
            request.ClearShowResultsDialog();
        }

        return request;
    }

    private static Task<Api.QueryPointsToObjectsResult> Query(Api.AnalysisOperations.AnalysisOperationsClient client,
        Api.QueryPointsToObjectsRequest request, string? header) =>
        client.QueryPointsToObjectsAsync(request, Headers(header)).ResponseAsync;

    private static Task<Api.SetPointOfViewFromInstrumentUpdatesResult> PointOfView(Api.ViewControl.ViewControlClient client,
        Api.SetPointOfViewFromInstrumentUpdatesRequest request, string? header) =>
        client.SetPointOfViewFromInstrumentUpdatesAsync(request, Headers(header)).ResponseAsync;

    private static Metadata Headers(string? header) => header is null ? [] : new Metadata { { HeaderName, header } };

    /// <summary>
    /// A loopback gRPC server with the production registration, the policy enforcer,
    /// and a dispatcher that maps each admitted request and then reports a full queue.
    /// </summary>
    private sealed class Host : IAsyncDisposable
    {
        private readonly WebApplication _app;

        private Host(WebApplication app, MappingDispatcher dispatcher)
        {
            _app = app;
            Dispatcher = dispatcher;
            Channel = GrpcChannel.ForAddress(Assert.Single(app.Urls));
        }

        public GrpcChannel Channel { get; }

        public MappingDispatcher Dispatcher { get; }

        public static async Task<Host> StartAsync(
            OperationPolicy policy,
            ILoggerProvider? logs = null,
            BriosaTelemetry? telemetry = null)
        {
            var dispatcher = new MappingDispatcher();
            var builder = WebApplication.CreateSlimBuilder();
            builder.Logging.ClearProviders();
            if (logs is not null)
            {
                builder.Logging.SetMinimumLevel(LogLevel.Trace);
                // PolicyAllowed is a Debug event; the host's default configuration filters Debug.
                builder.Logging.AddFilter(typeof(OperationAuditLogger).FullName, LogLevel.Trace);
                builder.Logging.AddProvider(logs);
            }

            builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0,
                listener => listener.Protocols = HttpProtocols.Http2));
            builder.Services.AddBriosaGrpc();
            builder.Services.AddSingleton(services =>
            {
                var audit = new OperationAuditLogger(logs is null
                    ? NullLogger<OperationAuditLogger>.Instance
                    : services.GetRequiredService<ILogger<OperationAuditLogger>>());
                return new OperationExecutor(
                    new PolicyEnforcingWorkerCommandExecutor(dispatcher, dispatcher, policy, audit),
                    audit, TimeProvider.System, telemetry);
            });
            var app = builder.Build();
            app.MapGrpcService<AnalysisOperationsService>();
            app.MapGrpcService<ViewControlService>();
            app.MapGrpcService<FileOperationsService>();
            app.MapGrpcService<InstrumentOperationsService>();
            await app.StartAsync().ConfigureAwait(true);
            return new Host(app, dispatcher);
        }

        public Task StopAsync() => _app.StopAsync();

        public async Task AssertAdmittedAsync(Func<Api.AnalysisOperations.AnalysisOperationsClient, Task> call)
        {
            var before = Dispatcher.Mapped;
            var exception = await Assert.ThrowsAsync<RpcException>(() =>
                call(new Api.AnalysisOperations.AnalysisOperationsClient(Channel))).ConfigureAwait(true);

            // Admitted, mapped, and handed to the worker queue, which reports it full.
            Assert.Equal(StatusCode.ResourceExhausted, exception.StatusCode);
            Assert.Equal(before + 1, Dispatcher.Mapped);
        }

        public async Task<Api.OperationError> AssertDeniedAsync(
            Func<Api.AnalysisOperations.AnalysisOperationsClient, Task> call,
            string operationId,
            string diagnosticCode)
        {
            var before = Dispatcher.Submissions;
            var exception = await Assert.ThrowsAsync<RpcException>(() =>
                call(new Api.AnalysisOperations.AnalysisOperationsClient(Channel))).ConfigureAwait(true);

            Assert.Equal(before, Dispatcher.Submissions);
            Assert.Equal(StatusCode.PermissionDenied, exception.StatusCode);
            Assert.Equal(DeniedStatusText, exception.Status.Detail);
            var error = Api.OperationError.Parser.ParseFrom(Trailer(exception));
            Assert.Equal(operationId, error.OperationId);
            Assert.Equal(Api.OperationFailureKind.PolicyDenied, error.Kind);
            Assert.Equal(diagnosticCode, error.DiagnosticCode);
            Assert.Equal(Api.ExecutionDisposition.NotStarted, error.ExecutionDisposition);
            Assert.Equal(Api.RecoveryGuidance.None, error.RecoveryGuidance);
            Assert.Equal(Api.ReplayGuidance.DoNotReplay, error.ReplayGuidance);
            Assert.Equal(
                SpatialAnalyzerApi.Operations.Single(operation => operation.OperationId == operationId).ReplaySafety,
                error.ReplaySafety);
            Assert.Null(error.MpExecution);
            Assert.Null(error.RequestValidation);
            return error;
        }

        public async Task<RpcException> DenyAsync(Func<Api.ViewControl.ViewControlClient, Task> call)
        {
            var exception = await Assert.ThrowsAsync<RpcException>(() =>
                call(new Api.ViewControl.ViewControlClient(Channel))).ConfigureAwait(true);
            Assert.Equal(0, Dispatcher.Submissions);
            return exception;
        }

        public async ValueTask DisposeAsync()
        {
            Channel.Dispose();
            await _app.DisposeAsync().ConfigureAwait(true);
        }
    }

    /// <summary>Maps each admitted request, as the supervisor does, then reports a full queue.</summary>
    private sealed class MappingDispatcher : IWorkerCommandDispatcher, IWorkerStatusProvider
    {
        private int _submissions;
        private int _mapped;

        public int Submissions => Volatile.Read(ref _submissions);

        public int Mapped => Volatile.Read(ref _mapped);

        public WorkerLifecycleSnapshot Current { get; } = new(
            WorkerLifecycleState.Stopped, Generation: 0, ProcessId: null, RecoveryCount: 0,
            WorkerTerminationKind.None, "caller-restriction-test", Connection: null, DateTimeOffset.UnixEpoch);

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerCommandSubmission submission,
            Guid correlationId,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _submissions);
            _ = submission.CreateCommand();
            Interlocked.Increment(ref _mapped);
            return Task.FromResult(new WorkerExecutionOutcome(
                WorkerExecutionStatus.Overloaded, WorkerExecutionDisposition.NotStarted,
                Execution: null, Connection: null, "recorded", Generation: 0, correlationId));
        }
    }
}
