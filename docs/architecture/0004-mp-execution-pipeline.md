# ADR 0004: Serialized MP execution, deadlines, and worker recovery

- Status: Accepted for the v0.1 vertical slice
- Date: 2026-07-21

## Context

SpatialAnalyzer MP execution is a stateful sequence: select a step, set every named argument, execute the step, and retrieve the MP result. Experiments showed that concurrent clients can appear connected while one client blocks indefinitely during execution. A successful `ExecuteStep` Boolean also does not prove that the MP command succeeded.

The public host must therefore serialize requests without owning COM state, distinguish a caller's deadline from an in-flight synchronous COM call, and recover from a call that never returns.

## Decision

Private worker protocol version 3 adds typed MP execution requests and responses.

- A request carries Briosa-owned operation and step identifiers plus typed logical, whole-number, floating-point, and text arguments. COM types do not cross the process boundary.
- The server supervisor owns a bounded, single-consumer execution queue. This is the transport-neutral seam that future gRPC service methods call.
- The worker maps each request to an `SdkCommand`. Its existing `SerializedSdkExecutor` performs the complete `SetStep`, argument-setter, `ExecuteStep`, and `GetMPStepResult` sequence on its one SDK-owning STA. No request can interleave another request's sequence.
- An argument setter that returns false produces the curated `sdk-argument-rejected` outcome and does not execute a partially configured step.
- After `ExecuteStep`, the adapter always calls `GetMPStepResult`. The response preserves both Boolean outcomes, the MP result code, SDK-sequence duration, and a curated diagnostic code.
- The server's production watchdog defaults to 30 seconds. The execution queue capacity defaults to 64. These are worker-safety limits and are independent of a gRPC deadline or caller cancellation token.
- A canceled caller stops waiting and receives `client-wait-cancelled`. An already queued request remains owned by the single consumer so its response is drained and the pipe stays synchronized. Cancellation does not claim to stop the COM call.
- If the watchdog expires, the supervisor force-terminates the worker process tree, starts a replacement within the existing bounded restart policy, and reports `WatchdogTimeout` for the affected request.
- A worker crash or invalid/broken control response is reported as `WorkerFailure` and uses the same replacement path. Worker unavailability remains a separate typed outcome.
- Heartbeats and executions share the supervisor's process gate, so a heartbeat cannot enter the request-response pipe while an execution is active.

The v0.1 public command surface is not expanded by this decision. Generated gRPC operations will submit curated commands through this internal executor in later command-specific work.

## Diagnostics and data handling

Execution diagnostics contain operation-independent status codes, process generation, connection state, timing, and MP result codes. Raw arguments are not logged by default. The private pipe necessarily carries arguments to the local worker, but public protobuf messages remain free of COM implementation types.

## Testing

Portable process tests use the fake worker executable to verify:

- concurrent callers are served serially;
- caller cancellation returns promptly while a later request succeeds on the same generation;
- a hung execution triggers forced replacement and the next call succeeds;
- a crashed execution is distinct from a watchdog timeout and is replaced;
- MP failure survives when `ExecuteStep` returns true; and
- an SDK-faulted production worker returns unavailable without activating or controlling SpatialAnalyzer.

Worker-unit tests continue to verify STA affinity and non-interleaving at the SDK adapter boundary. Ordinary builds and tests require no SpatialAnalyzer process, installation, license, or proprietary runtime binary beyond the approved generated interop types already committed to the repository.

## Consequences

- One queue and one STA establish deterministic request ordering at both sides of the process boundary.
- Client responsiveness no longer implies unsafe cancellation of synchronous COM.
- A hung SDK call consumes a worker generation and restart budget rather than permanently blocking the public host.
- Requests canceled after enqueue may still execute. Future command-risk policy may decide whether a not-yet-started abandoned request can be safely skipped.
- Watchdog and queue defaults are currently process policy. Configuration and public deadline mapping can be added without changing the private result model.
