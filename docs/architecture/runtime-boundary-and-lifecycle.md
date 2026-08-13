# Runtime boundary and lifecycle

- Status: Current
- Last reviewed: 2026-08-12

## Process and COM ownership

SpatialAnalyzer exposes its SDK through the out-of-process
`SpatialAnalyzerSDK.exe` OLE Automation/DCOM server. Briosa targets .NET 10 on
Windows x64, but the public gRPC host never activates or owns the SDK COM object.

Starting `Briosa.Server` brings up only the loopback gRPC control plane. It does
not start a worker, activate the SDK, launch SpatialAnalyzer, or call
`ConnectEx`. The public `SpatialAnalyzerSdkLifecycle` and
`SpatialAnalyzerLifecycle` services perform those transitions explicitly.

`Briosa.Server` supervises at most one `Briosa.Worker` generation at a time. The worker
owns exactly one SDK client, creates it on one dedicated STA thread, executes every
SDK call on that STA, and releases it there during graceful shutdown. The server
communicates with the worker through a private randomly named Windows named pipe
using versioned, length-prefixed, correlated control messages. COM types never
cross that pipe or enter public protobuf contracts.

The process boundary is the recovery boundary. If a synchronous SDK call hangs,
the server cannot safely cancel the COM call or reuse the worker. It terminates
that worker process tree, closes MP admission, records the incident and any
known execution disposition, and waits for an explicit generation-guarded
recovery request. Recovery creates a disconnected replacement and never replays
the interrupted command. Forced termination makes no claim that COM cleanup ran.

The worker exits if its parent server disappears. Normal server shutdown closes
admission, drains bounded in-flight control exchanges, requests a graceful worker
stop, and escalates to process-tree termination when acknowledgement or process
exit does not complete in time. Briosa never terminates a pre-existing
SpatialAnalyzer process it did not start.

## SDK attachment and ownership

SpatialAnalyzer must already be running before the worker calls
`ConnectEx(host, statusCode)`. The current public lifecycle contract always uses
`localhost`; caller-selected hosts and remote administration are deferred. The
public gRPC listener remains loopback-only.

`StartSpatialAnalyzerSdk` creates one disconnected worker/SDK generation and is
valid before or after SpatialAnalyzer starts. Only `ConnectToSpatialAnalyzer`
and `ReconnectToSpatialAnalyzer` call `ConnectEx`. One worker owns at most one
active SDK adapter. Concurrent lifecycle transitions are serialized and cannot
create competing adapters. Until exact `ConnectEx`
status codes receive reviewed transient classifications, a completed connection
failure is not retried speculatively. Reconnect reuses a healthy SDK generation;
recovery replaces a faulted generation without connecting it.

The worker identifies the exact `SpatialAnalyzerSDK.exe` process created during
COM activation and includes its liveness in the normal heartbeat. An unexpected
SDK engine exit faults the current generation, closes admission, and remains
observable through lifecycle state until explicit recovery. The server does not
adopt or terminate SDK processes it cannot associate uniquely with its worker.

A successful `ConnectEx` proves attachment only. Live testing showed that several
SDK clients may report successful connections while only the first eligible client
can execute an MP command and a later client may block indefinitely. Briosa
therefore keeps attachment and execution readiness separate.

## Exact-target identity

Every worker preserves three independent claims:

1. the exact SA release configured by the built target;
2. the version of the activated SDK engine/type library; and
3. the version of the connected SpatialAnalyzer application.

Configured target text is never copied into a runtime-observation field. Each
runtime claim records an optional version, evidence source, and match state.
Runtime evidence takes precedence. When an authoritative runtime observation is
unavailable, an operator may attest that claim with an explicit version and a
non-sensitive, change-controlled evidence reference. An attestation cannot mask a
runtime mismatch.

Both effective runtime claims must exactly match the built target before Briosa
probes or admits MP execution. Missing, partial, malformed, unstable, or mismatched
identity evidence fails closed. Evidence references are configuration inputs only;
they are not sent to the worker, returned by discovery, or written to default
logs.

Authoritative SDK and connected-application version probes remain provisional
pending reviewed vendor guidance. Operator attestation is an explicit reversible
minimum, not an inferred compatibility guarantee.

## Execution-channel verification

After exact identity matches, the worker performs one bounded read-only MP probe
using the same SDK adapter and STA that will execute ordinary work. The probe runs
the complete MP sequence and discards its returned value at the worker boundary.
It is never logged or exposed through discovery.

Execution verification belongs to one worker generation. Only a successful probe
creates `ExecutionReady`; worker replacement loses that evidence. A probe timeout,
worker loss, or ambiguous ownership quarantines the target and requires explicit
operator recovery rather than repeatedly launching clients against an uncertain
SDK owner.

Public readiness requires all of the following:

- a live public host and control-ready worker;
- an attached SDK client;
- exact-match activated-SDK and connected-SA identities;
- `ExecutionReady` for the current worker generation; and
- open command admission under runtime policy.

Public liveness remains independent of worker and SpatialAnalyzer state.

## SpatialAnalyzer application ownership

The application lifecycle is independent from the server and SDK lifecycles.
`LaunchSpatialAnalyzer` resolves the exact-target executable through trusted
server configuration and accepts only the reviewed job, quick-start instrument,
and minimized launch inputs. The server retains the launched process ID and
creation time as an opaque application generation; public state never exposes
the operating-system identity.

An exact-target application observed by executable path but not launched by the
current server is `External`. Multiple eligible applications are `Ambiguous`.
Neither state grants close authority. `CloseOwnedSpatialAnalyzer` requires an
exact current generation, server-launched ownership, and a stopped SDK. It asks
the retained process to close normally and does not escalate to machine-wide or
uncertain process termination. Server shutdown stops its SDK generation but
does not close SpatialAnalyzer.

## Observability boundary

Lifecycle and readiness diagnostics may include curated states, worker generation,
restart counts, timings, MP status codes, evidence source, match state, and stable
diagnostic codes. They exclude target hostnames, process identifiers, executable
paths, raw exceptions, license details, identity references, MP arguments, and
returned SpatialAnalyzer values.

Exact SA 2026.1.0529.7 observations and operating procedures are recorded in the
[target documentation](../../targets/2026.1.0529.7/docs/operations/health-and-discovery.md).
