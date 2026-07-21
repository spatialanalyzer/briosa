# ADR 0003: Single-owner SDK connection lifecycle

- Status: Accepted for the v0.1 vertical slice
- Date: 2026-07-21

## Context

A SpatialAnalyzer SDK client connects to an already-running SpatialAnalyzer instance through `ConnectEx(host, statusCode)`. Experiments showed that multiple clients can report successful connections even though concurrent MP execution is unsafe and one client may block indefinitely. Briosa therefore needs one explicit connection owner per worker, a stable readiness outcome, and bounded retry behavior that does not create additional SDK clients concurrently.

## Decision

Each worker owns one `SdkConnectionManager` with an immutable target host.

- The target defaults to `localhost`. The server can pass an explicit host from `Briosa:SpatialAnalyzer:Host` (environment variable `Briosa__SpatialAnalyzer__Host`).
- Connection state is modeled as `Disconnected`, `Connecting`, `Connected`, `Faulted`, or `Stopping`.
- The manager owns at most one active `SerializedSdkExecutor`. That executor creates, calls, and releases one SDK adapter on its dedicated STA. Concurrent connection callers share the same owner and cannot create parallel active adapters.
- The production adapter activates the generated `SpatialAnalyzerSDKClass`, calls `ConnectEx`, captures its Boolean outcome and status code, and releases the COM object on the owning STA.
- One connection cycle makes at most three attempts with a one-second delay between attempts. Exhaustion transitions to `Faulted`; it does not start an unbounded background reconnect loop.
- A later explicit `ConnectAsync` call may begin a new bounded cycle from `Faulted`. The same attempt budget applies to every cycle.
- Caller cancellation can prevent entry or cancel a retry delay. Once `ConnectEx` has started, Briosa does not claim that cancellation stopped the synchronous COM call. The supervising process boundary remains the recovery mechanism for a hung call.
- Work is accepted only while the manager is `Connected`. Every other state returns the typed `Unavailable` outcome and stable diagnostic code `sdk-connection-not-ready`.
- Worker control protocol version 2 includes the connection snapshot in the process-ready envelope. A worker can therefore be control-ready while its SDK connection is faulted; these conditions are not conflated.

## Diagnostics and data handling

Connection snapshots contain only state, configured target host, `ConnectEx` status code, attempt counters, transition time, and a curated diagnostic code. Exception messages and SDK implementation details do not cross the worker boundary or enter default logs.

## Testing

Portable tests use the scripted SDK to cover success, status-code preservation, non-ready request rejection, delayed connection, concurrent callers, attempt exhaustion, explicit reconnect, activation failure, STA affinity, and single-adapter ownership. The production worker process smoke test disables SDK activation explicitly, so ordinary tests never start or attach to SpatialAnalyzer.

## Consequences

- The public server can report process and SDK readiness independently.
- A failed connection does not crash an otherwise healthy worker process.
- Automatic connection retries are finite and deterministic.
- MP request transport and process-level execution watchdog behavior are defined by [ADR 0004](0004-mp-execution-pipeline.md).
