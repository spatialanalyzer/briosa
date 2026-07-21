# Fake SDK and contract-test harness

The portable worker tests use a scripted adapter instead of installing, starting, or licensing SpatialAnalyzer. The harness exists to verify Briosa''s own worker contracts: lifecycle ownership, STA affinity, serialization, result-only argument retrieval, result preservation, and recovery policy seams.

## Boundary under test

`ISpatialAnalyzerSdk` is an internal, synchronous worker-boundary contract. It uses Briosa-owned command and outcome types and exposes no COM types. `SerializedSdkExecutor` creates and disposes one adapter on a dedicated STA thread and sends all connection and command work through a single-consumer queue.

`SdkConnectionManager` owns at most one active executor, models connection transitions, applies a bounded attempt policy, and returns `sdk-connection-not-ready` without entering the adapter unless its state is `Connected`. Concurrent connection callers share the same owner rather than creating additional SDK clients.

Cancellation can stop a caller from entering the owner or waiting through a retry delay, but it does not claim to cancel a synchronous SDK call that has already started. The production watchdog recovers from an unresponsive call by replacing the worker process.

## Scripted behaviors

The reusable `Briosa.Worker.Testing` assembly provides deterministic scripts for:

| Behavior | Contract exercised |
| --- | --- |
| Success | Connected execution, a successful MP result, and typed result-only arguments |
| MP failure | `ExecuteStep` may return true while the MP result reports failure |
| Connection failure | `ConnectEx` availability and status remain distinct from command outcomes |
| Delayed connection | Connecting state rejects work while concurrent callers share one adapter |
| Bounded reconnect | Attempt exhaustion faults deterministically; a new explicit cycle has the same bound |
| Delay | A blocked command keeps later commands from entering the adapter |
| Hang | The watchdog reports a timeout and the supervisor seam replaces the worker |
| Crash | Abrupt worker loss is reported separately and followed by replacement |

The watchdog and supervisor types in this test-support assembly remain lightweight harness seams. `Briosa.Server.Tests` exercises the production process queue, private execution transport, mixed output-value round trips, caller cancellation, watchdog, crash recovery, and MP-result preservation described in [ADR 0004](../architecture/0004-mp-execution-pipeline.md).

## Reusing the contracts

`SdkContractAssertions` contains adapter-independent checks. Production-adapter tests use an injectable synchronous call surface to verify the exact setter/execution/MP-result/getter order without COM activation. A future licensed integration fixture can supply the real adapter factory and invoke the same applicable checks from a protected Windows environment. Scenarios that intentionally force MP failure, hangs, or crashes remain fake-only unless a safe real-SA procedure is explicitly approved.

## Non-emulation statement

The scripted fake is not an implementation, simulator, or behavioral model of SpatialAnalyzer. Its result codes, diagnostic codes, delays, failures, hangs, and crashes are invented test inputs. Passing these tests demonstrates Briosa behavior only at the contracts listed above.

Run the portable checks with:

```powershell
dotnet test tests/Briosa.Worker.Tests/Briosa.Worker.Tests.csproj -c Release
```

No SpatialAnalyzer installation, process, license, proprietary SDK executable, or protected runner is used.
