# ADR 0002: Isolated worker process lifecycle

- Status: Accepted for the v0.1 vertical slice
- Date: 2026-07-21

## Context

SpatialAnalyzer automation is synchronous COM/OLE Automation. A blocked call cannot be made safe merely by cancelling a gRPC request, and experiments showed that blocked SDK clients can retain connections. The public gRPC host therefore needs a replaceable process boundary around all SDK and COM state.

## Decision

The public host supervises one child worker generation at a time.

- The host and worker use a private, randomly named Windows named pipe. The pipe carries versioned, length-prefixed JSON control envelopes with correlation identifiers.
- Protocol versions 1 and 2 established worker ready, heartbeat ping/pong, graceful stop, stopped acknowledgement, and connection readiness. Protocol version 3 adds the serialized execution messages defined by [ADR 0004](0004-mp-execution-pipeline.md).
- The host reports `Starting`, `Ready`, `Degraded`, and `Stopped` snapshots with generation, process identity, restart count, termination kind, timestamp, and a safe diagnostic code.
- A heartbeat timeout, broken control channel, or process exit degrades the current generation. The host terminates the entire worker process tree when graceful cleanup is no longer trustworthy, then starts a fresh generation without restarting the public host.
- Restarts are limited to a configured count inside a rolling time window. Exhausting that budget leaves the supervisor degraded and suppresses further automatic launches.
- Normal host shutdown requests a graceful worker stop and waits for acknowledgement and process exit. Timeout or transport failure escalates to process-tree termination.
- The worker receives the host process identifier and exits if its parent disappears, reducing orphan risk after an abnormal host exit.
- The worker creates its SDK lifetime on a dedicated STA and releases it on that same STA during graceful shutdown. A killed generation makes no claim that COM cleanup ran; process isolation is the cleanup boundary.

`Ready` in this ADR means that the child process and control channel are ready. The separate SpatialAnalyzer connection snapshot is defined by [ADR 0003](0003-sdk-connection-lifecycle.md).

## Security and data handling

The control pipe is local to the machine, has an unguessable per-generation name, and is never exposed as a public endpoint. Control messages contain lifecycle metadata only; geometry, credentials, paths, licenses, and raw MP arguments are excluded.

## Testing

Ordinary CI launches a separate fake worker executable that speaks the real lifecycle protocol. It can block its STA, exit abruptly, or ignore graceful shutdown so tests can verify replacement and forced cleanup without SpatialAnalyzer or proprietary binaries. The fake is not a SpatialAnalyzer emulator.

## Consequences

- The public server never loads the SpatialAnalyzer interop assembly.
- A hung SDK call is recovered by terminating the child process, not by claiming in-process cancellation.
- The internal protocol is versioned independently from the public gRPC schema.
- Production MP request transport and deadline policy are defined by [ADR 0004](0004-mp-execution-pipeline.md).
