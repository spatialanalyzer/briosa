# ADR 0004: Serialized MP execution, deadlines, and worker recovery

- Status: Accepted for the v0.1 vertical slice
- Date: 2026-07-21
- Amended by: [ADR 0018](0018-uncertain-completion-and-replay.md), [ADR 0019](0019-global-state-workflow-isolation.md)

## Context

SpatialAnalyzer MP execution is a stateful sequence: select a step, set every input argument, execute the step, inspect the MP result, and retrieve requested result-only arguments after success. Experiments showed that concurrent clients can appear connected while one client blocks indefinitely during execution. A successful `ExecuteStep` Boolean also does not prove that the MP command succeeded. The official [Spatial Analyzer SDK guide](https://spatialanalyzer.com/ftp/SA/Install/Driver%20Downloads/SA%20SDK/Spatial%20Analyzer%20SDK.pdf) demonstrates calling `GetMPStepResult` and then the appropriate argument getters when the MP result is successful.

The public host must therefore serialize requests without owning COM state, distinguish a caller's deadline from an in-flight synchronous COM call, and recover from a call that never returns.

## Decision

Private worker protocol version 3 adds typed MP execution requests and responses.

- A request carries Briosa-owned operation and step identifiers, typed input values, and typed descriptors for requested result-only arguments. The initial private value family covers logical, whole-number, floating-point, text, point-name, vector, and tolerance-vector values. COM types do not cross the process boundary; issue #14 owns the complete catalog of specialized argument types.
- The server supervisor owns a bounded, single-consumer execution queue. This is the transport-neutral seam that future gRPC service methods call.
- A value-free execution snapshot reports queue capacity/depth, admission waiters, active and peak work, admitted/terminal counts, cancellation position, watchdogs, and worker failures. It is diagnostic state, not request telemetry, and never carries operation values.
- The worker maps each request to an `SdkCommand`. Its existing `SerializedSdkExecutor` performs the complete `SetStep`, input-setter, `ExecuteStep`, `GetMPStepResult`, and successful result-argument getter sequence on its one SDK-owning STA. No request can interleave another request's sequence.
- An argument setter that returns false produces the curated `sdk-argument-rejected` outcome and does not execute a partially configured step.
- The adapter calls `GetMPStepResult` only after `ExecuteStep` returns true. The getter Boolean means that the numeric result was retrieved; it is not the MP success flag. Result code `2` is success. Codes `-1`, `0`, `1`, `3`, `4`, `5`, and unknown values are preserved as non-success outcomes, and a false getter return is preserved separately as `sdk-mp-result-retrieval-failed` with no result code. Requested output getters run only after code `2`. Private worker protocol version 6 carries execute acceptance, MP-result retrieval, MP success, and the optional raw result code independently. A failed output getter produces `sdk-output-retrieval-failed` without silently substituting a default value.
- The server's production watchdog defaults to 30 seconds. The execution queue capacity defaults to 64. These are worker-safety limits and are independent of a gRPC deadline or caller cancellation token.
- A canceled caller stops waiting and receives `client-wait-cancelled`. Cancellation before enqueue is `NotStarted`; cancellation after enqueue is `StartedOutcomeUnknown` unless the worker proves that it skipped the request. An already queued request remains owned by the single consumer so its response is drained and the pipe stays synchronized. Cancellation does not claim to stop the COM call.
- Queue admission is generation-scoped. Shutdown closes admission and wakes capacity waiters. An exchange that has already entered the length-prefixed pipe continues under the execution watchdog rather than being interrupted by runtime-loop cancellation; remaining queued items are completed without entering the pipe. Every admitted item therefore reaches a terminal internal outcome before the worker is stopped or replaced.
- If the watchdog expires, the supervisor force-terminates the worker process tree, starts a replacement within the existing bounded restart policy, and reports `WatchdogTimeout` with `StartedOutcomeUnknown` for the affected request. Replacement restores availability but does not authorize replay.
- A worker crash or invalid/broken control response after the request may have entered the worker is reported as `WorkerFailure` with `StartedOutcomeUnknown` and uses the same replacement path. Worker unavailability proved before execution remains a separate `NotStarted` outcome.
- Heartbeats and executions share the supervisor's process gate, so a heartbeat cannot enter the request-response pipe while an execution is active. Shutdown cancels future heartbeat scheduling and waits for any bounded in-flight ping/pong exchange before attempting the stop/stopped exchange on that channel.

The v0.1 public command surface is not expanded by this decision. Generated gRPC operations will submit curated commands through this internal executor in later command-specific work.

## Diagnostics and data handling

Execution diagnostics contain operation-independent status codes, process generation, connection state, timing, and MP result codes. Raw arguments are not logged by default. The private pipe necessarily carries arguments to the local worker, but public protobuf messages remain free of COM implementation types.

## Testing

Portable process tests use the fake worker executable to verify:

- concurrent callers are served serially;
- full-queue callers remain outside admission, cancel as `NotStarted`, and never increase admitted depth beyond capacity;
- post-admission cancellation drains to a terminal internal outcome, and shutdown wakes capacity waiters;
- shutdown during an active execution drains its response before graceful stop, while queued work that has not entered the pipe terminates without execution;
- shutdown during an active heartbeat drains pong before reusing the channel for graceful stop;
- caller cancellation returns promptly while a later request succeeds on the same generation;
- a hung execution triggers forced replacement and the next call succeeds;
- a crashed execution is distinct from a watchdog timeout and is replaced;
- every documented non-success MP result code survives when `ExecuteStep` returns true and prevents output getter calls;
- MP-result retrieval failure remains distinct from an MP-reported failure and prevents output getter calls;
- scalar, point-name, vector, and tolerance-vector outputs round-trip across the process boundary;
- an SDK-faulted production worker returns unavailable without activating or controlling SpatialAnalyzer.
- repeated watchdog replacement, bounded lifecycle history, retained-memory evidence, and value-free audit correlation remain stable under portable sustained load.

Worker-unit tests verify STA affinity, non-interleaving, the exact production-adapter call order through MP result inspection and output getters, and output-getter failure preservation. Ordinary builds and tests require no SpatialAnalyzer process, installation, license, or proprietary runtime binary beyond the approved generated interop types already committed to the repository.

## Consequences

- One queue and one STA establish deterministic request ordering at both sides of the process boundary, including all requested output getters.
- Client responsiveness no longer implies unsafe cancellation of synchronous COM.
- A hung SDK call consumes a worker generation and restart budget rather than permanently blocking the public host.
- Requests canceled after enqueue may still execute. ADR 0018 requires clients to reconcile ambiguous `unsafe` or `unknown` operations before replay.
- Watchdog and queue defaults are currently process policy. Configuration and public deadline mapping can be added without changing the private result model.
