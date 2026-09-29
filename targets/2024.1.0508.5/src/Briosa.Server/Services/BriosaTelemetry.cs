using System.Diagnostics;
using System.Diagnostics.Metrics;
using Briosa.Server.Operations;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using IdentityMatch = Briosa.Server.Workers.RuntimeIdentityMatchState;

namespace Briosa.Server.Services;

internal sealed class BriosaTelemetry : IDisposable
{
    public const string InstrumentationName = "Briosa.Server";
    private static readonly Dictionary<string, global::Briosa.ReplaySafety> Operations =
        SpatialAnalyzerApi.Operations.ToDictionary(operation => operation.OperationId,
            operation => operation.ReplaySafety, StringComparer.Ordinal);
    internal static readonly ActivitySource Activities = new(InstrumentationName);
    private readonly Meter _meter = new(InstrumentationName);
    private readonly Counter<long> _requests;
    private readonly Counter<long> _executions;
    private readonly Histogram<double> _requestDuration;
    private readonly Histogram<double> _admissionDuration;
    private readonly Histogram<double> _queueDuration;
    private readonly Histogram<double> _exchangeDuration;
    private readonly Histogram<double> _sdkDuration;
    private WorkerProcessSupervisor? _supervisor;

    public BriosaTelemetry(BriosaLogHealth health)
    {
        _requests = _meter.CreateCounter<long>("briosa.rpc.completed");
        _executions = _meter.CreateCounter<long>("briosa.execution.resolved");
        _requestDuration = _meter.CreateHistogram<double>("briosa.rpc.duration", "s");
        _admissionDuration = _meter.CreateHistogram<double>("briosa.admission.duration", "s");
        _queueDuration = _meter.CreateHistogram<double>("briosa.queue.duration", "s");
        _exchangeDuration = _meter.CreateHistogram<double>("briosa.worker.exchange.duration", "s");
        _sdkDuration = _meter.CreateHistogram<double>("briosa.sdk.duration", "s");
        _meter.CreateObservableGauge("briosa.queue.depth", () => _supervisor?.ExecutionSnapshot.QueuedRequests ?? 0);
        _meter.CreateObservableGauge("briosa.admission.waiters", () => _supervisor?.ExecutionSnapshot.WaitingForAdmission ?? 0);
        _meter.CreateObservableGauge("briosa.execution.active", () => _supervisor?.ExecutionSnapshot.ActiveExecutions ?? 0);
        _meter.CreateObservableCounter("briosa.worker.watchdogs", () => _supervisor?.ExecutionSnapshot.WatchdogTimeouts ?? 0);
        _meter.CreateObservableCounter("briosa.worker.failures", () => _supervisor?.ExecutionSnapshot.WorkerFailures ?? 0);
        // Explicit recovery attempts since Start; no automatic restart budget exists.
        _meter.CreateObservableGauge("briosa.worker.recovery_attempts", () => _supervisor?.Current.RecoveryCount ?? 0);
        _meter.CreateObservableGauge("briosa.ready", Ready);
        _meter.CreateObservableGauge("briosa.identity.match", IdentityMatches);
        _meter.CreateObservableGauge("briosa.log.queue.depth", () => health.Queued);
        _meter.CreateObservableCounter("briosa.log.dropped", () => health.Dropped);
        _meter.CreateObservableCounter("briosa.log.failures", () => health.Failures);
    }

    public void Attach(WorkerProcessSupervisor supervisor) => _supervisor = supervisor;
    public static string OperationId(string operationId) => Operations.ContainsKey(operationId) ? operationId : "unsupported";
    public static global::Briosa.ReplaySafety ReplaySafety(string operationId) =>
        Operations.TryGetValue(operationId, out var safety) ? safety : global::Briosa.ReplaySafety.Unknown;

    public static Activity? Start(string name, string operationId, ActivityContext? parent = null)
    {
        var activity = parent.HasValue
            ? Activities.StartActivity(name, ActivityKind.Internal, parent.Value)
            : Activities.StartActivity(name);
        activity?.SetTag("briosa.operation", OperationId(operationId));
        return activity;
    }

    public void RpcCompleted(string operation, string status, double milliseconds)
    {
        var tags = new TagList { { "briosa.operation", OperationId(operation) }, { "rpc.status", status } };
        _requests.Add(1, tags);
        _requestDuration.Record(milliseconds / 1000, tags);
    }
    public void Admission(string operation, double milliseconds) =>
        _admissionDuration.Record(milliseconds / 1000, new KeyValuePair<string, object?>("briosa.operation", OperationId(operation)));
    public void Queue(string operation, double milliseconds) =>
        _queueDuration.Record(milliseconds / 1000, new KeyValuePair<string, object?>("briosa.operation", OperationId(operation)));
    public void Exchange(string operation, double milliseconds) =>
        _exchangeDuration.Record(milliseconds / 1000, new KeyValuePair<string, object?>("briosa.operation", OperationId(operation)));
    public void Resolved(string operation, WorkerExecutionOutcome outcome)
    {
        var summary = OperationAuditSummary.Create(outcome);
        var tags = new TagList
        {
            { "briosa.operation", OperationId(operation) },
            { "briosa.disposition", summary.ExecutionDisposition },
            { "briosa.mp.outcome", summary.MpOutcome }
        };
        _executions.Add(1, tags);
        if (outcome.Execution is { } execution)
            _sdkDuration.Record(execution.DurationMilliseconds / 1000.0, new KeyValuePair<string, object?>("briosa.operation", OperationId(operation)));
    }
    private int Ready()
    {
        var state = _supervisor?.Current;
        return state is
        {
            State: WorkerLifecycleState.Ready, RuntimeIdentity.AllowsExecution: true,
            Connection.State: WorkerConnectionState.Connected,
            Connection.ExecutionReadinessState: WorkerExecutionReadinessState.ExecutionReady
        } ? 1 : 0;
    }
    private IEnumerable<Measurement<int>> IdentityMatches()
    {
        var identity = _supervisor?.Current.RuntimeIdentity;
        return
        [
            new(identity?.ActivatedSdk.MatchState == IdentityMatch.ExactMatch ? 1 : 0,
                new KeyValuePair<string, object?>("claim", "activated_sdk")),
            new(identity?.ConnectedSpatialAnalyzer.MatchState == IdentityMatch.ExactMatch ? 1 : 0,
                new KeyValuePair<string, object?>("claim", "connected_sa"))
        ];
    }
    public void Dispose() => _meter.Dispose();
}
