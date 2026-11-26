# Server logging and telemetry

Briosa writes metadata-only operational logs to the console and rolling JSONL
files. The default location is
`%LOCALAPPDATA%\Briosa\logs\2024.1.0508.5\`. Each filename includes a random
server-instance ID, also present in its records. Hidden client-launched servers
write the same files without a visible console or administrator rights.

These are best-effort diagnostic records. They are not a durable audit ledger.
A crash, forced client-owned server termination, full queue, exhausted quota, or
failed filesystem can lose records.

## Startup configuration

Standard .NET configuration applies through appsettings, environment variables,
and command-line arguments. Settings and category filters are captured at startup.
Restart the server to change them. There is no logging RPC. The MP command
**Set Logging State** controls SpatialAnalyzer and is unrelated.

| Key | Default | Valid range |
| --- | --- | --- |
| `Logging:LogLevel:Default` | Information | Trace, Debug, Information, Warning, Error, Critical, None |
| `Logging:LogLevel:<category>` | Microsoft and Grpc: Warning | Same levels; longest matching category applies |
| `Briosa:Logging:ConsoleEnabled` | true | Boolean |
| `Briosa:Logging:File:Enabled` | true | Boolean |
| `Briosa:Logging:File:Directory` | Per-user target directory above | Absolute directory |
| `Briosa:Logging:File:MaxFileSizeMiB` | 20 | 1–1024 |
| `Briosa:Logging:File:RetainedFileCount` | 10 | 1–1000 |
| `Briosa:Logging:File:MaxAgeDays` | 7 | 1–365 |
| `Briosa:Logging:File:MaxTotalSizeMiB` | 200 | At least maximum file size; at most 10240 |
| `Briosa:Logging:QueueCapacity` | 4096 | 16–65536 |
| `Briosa:Logging:ShutdownTimeoutMilliseconds` | 2000 | 1–10000 |

For example, a direct launch can set
`--Logging:LogLevel:Briosa.Server=Debug --Briosa:Logging:ConsoleEnabled=false`.
Invalid settings fail startup with a value-free error. A filesystem failure
during operation is contained and does not replace RPC results.

## Retention and overload

The host uses source-generated ILogger events. Serilog performs file formatting,
rotation, and writes on its asynchronous consumer. The queue never waits for
space. Low-severity file records are dropped when 7/8 of capacity is occupied,
reserving headroom for Warning and higher events; those too can be dropped if the
queue fills. The console uses a separate bounded DropWrite queue.

File records have a 16 KiB maximum. Files roll daily or at the size threshold,
with headroom for one complete record. An exclusive directory lock coordinates
file writes and quota pruning across Briosa processes. Lock contention drops a
record; it never blocks an RPC. Each write checks age, count and total-byte bounds
and prunes eligible closed Briosa files, including earlier instances. Active files
owned by other instances and unrelated files are untouched. A writer can close
and reclaim its own file to continue within a one-file budget. If other active
files fill the budget, new records are dropped. Expired idle files are removed on
the next write, not by a resident cleanup process. Processes sharing a directory should use the same retention
settings; use separate directories for independent policies.

Writes flush to the OS cache before releasing the quota lock so another instance
can measure current lengths. There is no per-event durable disk flush. All of
this filesystem work occurs on the sink consumer. Orderly host shutdown drains
for the configured budget; a stalled writer can outlive that budget until process
exit.

File drops, queue depth, and provider failures are observable metrics. Changes
produce a metadata-only warning at most once every ten seconds through surviving
sinks. Console-provider queue drops are reported by its built-in console notice;
they are not included in the file-drop counter.

## Records and execution semantics

JSONL contains UTC Timestamp, Level, MessageTemplate (stable event name), and
Properties including SchemaVersion, SourceContext (category), EventId,
ServerInstanceId, SpatialAnalyzerTarget, and applicable event metadata.

| Event ID | Name | Default severity |
| --- | --- | --- |
| 1000 | ControlPlaneReady | Information |
| 1001 | EndpointListening (ElapsedMilliseconds since process start) | Information |
| 1201 | WorkerTransition | Information; Warning for blocked attached identity; Error for degraded worker |
| 1202 | InteractionModeApplied (SaInteractionMode, MpInteractionMode, MpDialogInteractionMode, MpResultCode) | Information; Warning when the mode was not applied |
| 1203 | WorkerStartPhase (Generation, Phase, ElapsedMilliseconds) | Information; Warning when the startup bound expired or the start failed |
| 1300 | ExecutionDispatched (includes DurationClass and ExecutionBudgetMilliseconds) | Information |
| 1301 | ExecutionResolved | Information; Warning for unsuccessful outcomes; Error for watchdog/worker failure |
| 1302 | LifecycleRejected (Rpc, GrpcStatus, DiagnosticCode, ElapsedMilliseconds) | Warning |
| 1303 / 1304 | LifecycleRpcStarted (Rpc) / LifecycleRpcCompleted (Rpc, GrpcStatus, ElapsedMilliseconds) | Information |
| 1305 | LifecycleRpcFaulted (Rpc, ElapsedMilliseconds) | Warning |
| 1400 | ApplicationTransition | Information; Warning for faulted application |
| 1401 | LogSinkDegraded | Warning |
| 1402 | LaunchPreconditionSlow (Phase, ElapsedMilliseconds) | Warning |
| 1600 | UnaryCallStarted (Rpc or OperationId) | Debug |
| 1601 / 1602 | UnaryCallCompleted (Rpc or OperationId, GrpcStatus, ElapsedMilliseconds) / UnaryCallFaulted (Rpc or OperationId, ElapsedMilliseconds) | Debug |
| 1603 | UnaryCallResponseEnded (Rpc or OperationId, GrpcStatus, ElapsedMilliseconds) | Debug |
| 2000 | PolicyLoaded | Information |
| 2001 / 2002 | RequestStarted / PolicyAllowed | Debug |
| 2003 | PolicyRejected | Warning |
| 2004 / 2005 | RpcCompleted / RpcFailed | Information / Warning |

RPC completion and SDK completion are different observations. A cancelled RPC can record unknown
execution and later have an ExecutionResolved event with a retrieved MP result.
A request cancelled while still queued is abandoned instead. Its RpcFailed and
ExecutionResolved events both report `not_started` with diagnostic
`queued-request-abandoned`, and no ExecutionDispatched event follows. A lifecycle
caller that stops waiting is recorded as LifecycleRejected with gRPC status
`Cancelled`, or `DeadlineExceeded` when the call's deadline had passed; later
WorkerTransition events record the accepted exchange's terminal state.
Correlate by CorrelationId, ServerInstanceId and Generation. Later evidence does
not change the original response or authorize replay. Only a retrieved MP result
code 2 means MP success, and code 4 a partial success (MP outcome
`partial_success`), except for the operations whose code 4 is reported as an MP
failure (#351). Output retrieval remains separate.

The lifecycle records form a start-up timeline, so a slow or stalled start can
be placed from the log alone:

- EndpointListening records when the public endpoint started listening, in
  milliseconds since the server process was created. The time from there to
  the first lifecycle RPC is the client's own connect delay.
- WorkerStartPhase records each worker start sub-phase with the milliseconds
  since the start began: `process-created`, `control-pipe-connected`, and
  `ready-received` (a valid Ready message), or `startup-bound-expired` or
  `start-failed` at Warning.
  A `worker-startup-timeout` therefore shows whether the bound went to process
  creation, runtime start-up and the control pipe, or the wait for Ready.
- Every SDK and application lifecycle RPC that changes state (Start, Connect,
  Reconnect, Stop, Recover, Launch, and Close) records its arrival
  (LifecycleRpcStarted) and exactly one exit: LifecycleRpcCompleted,
  LifecycleRejected, or, for an exception without a typed lifecycle detail,
  LifecycleRpcFaulted. The state queries are not recorded at Information, so
  polling does not fill the log; the Debug call records below cover them.
- LaunchPreconditionSlow warns when a launch precondition step takes more than
  2 seconds. The steps run on the request thread before the launch is
  accepted: `process-scan` (the eligible-process scan) and
  `installation-check` (the job file, installation identity, and executable
  checks). A launch whose arrival is recorded but whose ApplicationTransition
  to `spatial-analyzer-starting` never follows stalled in these steps.

Every unary call, including discovery, state reads, health checks and MP
operations, passes one gRPC interceptor that records it at Debug (#450):
UnaryCallStarted when it arrives, UnaryCallCompleted with its gRPC status when
its handler returns a response or a status, or UnaryCallFaulted when the
handler ends without one, and UnaryCallResponseEnded with the status once the
server has finished the response. A call whose handler returned but whose
response never ended stalled in the server's transport, not in its handler. A
call is named by `Rpc`, its RPC name, or by `OperationId` for an MP operation,
never by its request, status message or metadata. The records are off at the
default Information level, where the interceptor only checks the level and
passes the call through unchanged. Turn them on for this one category with
`--Logging:LogLevel:Briosa.Server.Services.UnaryCallAuditInterceptor=Debug`, or
the environment variable
`Logging__LogLevel__Briosa.Server.Services.UnaryCallAuditInterceptor=Debug`. They
add three records of about 500 bytes to each call, which matters for a client
that polls state; the client conformance runner turns them on for its
scenarios.

These timing records are best effort. A clock or logger failure loses the record
and nothing else: it cannot skip a worker's cleanup, replace an RPC's response,
or hide the exception that ended a call. As in the worker supervisor, an
`OutOfMemoryException` is not contained. `ElapsedMilliseconds` is `-1` when the
time could not be measured. EndpointListening, WorkerStartPhase,
LifecycleRpcStarted, LifecycleRpcCompleted, LifecycleRpcFaulted,
LaunchPreconditionSlow and the unary call records carry only fixed names,
operation IDs, enum values and numbers; LifecycleRejected also carries the
lifecycle error's diagnostic code.

The metadata boundary discards raw messages, exceptions and ambient scopes from
framework logs, preserving their category, ID, severity and a FrameworkEvent
name. Reviewed Briosa events admit only known metadata properties. Debug and
Trace never unlock arguments, return values, geometry, paths, evidence references,
credentials, process IDs or vendor exception text. Trace/span IDs are optional.

## Metrics and optional tracing

The `Briosa.Server` Meter publishes:

- `briosa.rpc.completed` and `briosa.rpc.duration`;
- `briosa.execution.resolved`;
- `briosa.admission.duration`, `briosa.queue.duration`,
  `briosa.worker.exchange.duration`, and `briosa.sdk.duration` (seconds);
- `briosa.queue.depth`, `briosa.admission.waiters`, and `briosa.execution.active`;
- `briosa.worker.watchdogs`, `briosa.worker.failures`, and
  `briosa.worker.recovery_attempts` (explicit recovery attempts since startup; a gauge because startup resets it);
- `briosa.ready` and `briosa.identity.match` (separate SDK/SA claims);
- `briosa.log.queue.depth`, `briosa.log.dropped`, and `briosa.log.failures`.

Admission waits for capacity. Queue duration runs from admission until the
supervisor acquires the execution gate. Worker exchange duration includes private
IPC and SDK execution; SDK duration comes from the worker's existing measurement.
Unavailable durations are omitted from SDK histograms, not recorded as zero.
Callbacks read cached state and never call the SDK. Metric dimensions use reviewed
operation IDs and fixed outcomes; unrecognized operation IDs map to unsupported.

The matching ActivitySource supplies host-side RPC, admission, queue, worker
exchange, and execution-resolution spans. Queued work captures its originating
trace context. No worker-side exporter or extra COM calls are introduced.

Export is disabled by default. To opt in to an operator-managed OTLP/gRPC
collector, configure:

```json
{
  "Briosa": {
    "Telemetry": {
      "Enabled": true,
      "Endpoint": "http://localhost:4317",
      "SampleRatio": 0.1
    }
  }
}
```

Only Briosa's manual traces and metrics are exported; file logs remain local.
The trace queue holds 2048 spans, batches up to 256 every five seconds, and has a
two-second export timeout. Metrics export every 30 seconds with the same timeout
and at most 4096 attribute combinations per stream. The sampler retains a
trace-ID-based fraction. Duration buckets cover 100 microseconds through 120
seconds. Sampling never suppresses operational error logs.
Resource metadata contains service identity and exact target without environment,
host, or process detectors. Endpoint credentials/query strings are rejected, and
ambient OTLP headers are cleared. A local collector can own authenticated remote
forwarding. No new public listener is exposed.

See [performance evidence](../testing/runtime-performance-and-soak.md) and the
[shared client contract](../../../../docs/architecture/client-library-behavioral-contract.md).
