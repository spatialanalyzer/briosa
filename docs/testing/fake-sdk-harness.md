# Fake SDK and contract-test harness

The portable worker tests use a scripted adapter instead of installing, starting, or licensing SpatialAnalyzer. The harness exists to verify Briosa's own worker contracts: lifecycle ownership, STA affinity, serialization, result-only argument retrieval, result preservation, and recovery policy seams.

## Boundary under test

`ISpatialAnalyzerSdk` is an internal, synchronous worker-boundary contract. It uses Briosa-owned command and outcome types and exposes no COM types. `SerializedSdkExecutor` creates and disposes one adapter on a dedicated STA thread and sends all connection and command work through a single-consumer queue.

`SdkConnectionManager` owns at most one active executor and models SDK attachment independently from execution verification. A successful `ConnectEx` remains `Unverified`; ordinary commands return `sdk-connection-not-ready` without entering the adapter until the dedicated Get Working Directory probe succeeds on the same STA. The probe result path is discarded before the worker replies. Unknown connection statuses and activation failures are not retried; only status codes in an explicit reviewed transient set can consume a larger attempt budget.

The production supervisor bounds the probe with its process watchdog. A probe hang, cancellation, crash, or lost response terminates the worker, records competing-client suspicion, and ends in operator-required recovery without automatically launching another generation. Portable process tests exercise explicit recovery after that quarantine.

Cancellation can stop a caller from entering the owner or waiting through a retry delay, but it does not claim to cancel a synchronous SDK call that has already started. The production watchdog recovers availability by replacing the worker process. It does not prove whether an in-flight command completed or make replay safe; [ADR 0018](../architecture/0018-uncertain-completion-and-replay.md) defines the required execution disposition and replay contract.

Process-level scenarios distinguish cancellation before enqueue (`NotStarted`) from cancellation after enqueue (`StartedOutcomeUnknown`). They also simulate a hang after execution starts, a completed command followed by worker exit before the response, and a response that is lost until the watchdog replaces the worker. Each ambiguous case retains the original worker generation and remains uncertain after the replacement reports ready.

## Scripted behaviors

The reusable `Briosa.Worker.Testing` assembly provides deterministic scripts for:

| Behavior | Contract exercised |
| --- | --- |
| Success | Attached-but-unverified, successful redacted verification, connected execution, a successful MP result, and typed result-only arguments |
| Probe rejection | `ExecuteStep` rejection fails closed before ordinary commands are admitted |
| Malformed probe output | MP success without the exact expected output shape requires operator recovery |
| MP-result retrieval failure | `GetMPStepResult` may return false, leaving no trustworthy numeric result |
| MP failure | `ExecuteStep` may return true while a retrieved MP result code other than `2` reports failure |
| Connection failure | `ConnectEx` availability and status remain distinct from command outcomes |
| Delayed connection | Connecting state rejects work while concurrent callers share one adapter |
| Status-aware reconnect | Unknown statuses fail closed after one attempt; only reviewed transient statuses consume the configured bound |
| Delay | A blocked command keeps later commands from entering the adapter |
| Hang | Ordinary execution uses replacement policy; a verification hang quarantines without reconnecting |
| Crash | Ordinary execution uses replacement policy; loss during verification requires explicit recovery |

The watchdog and supervisor types in this test-support assembly remain lightweight harness seams. `Briosa.Server.Tests` exercises the production process queue, private execution transport, mixed output-value round trips, caller cancellation, watchdog, crash recovery, and MP-result preservation described in [ADR 0004](../architecture/0004-mp-execution-pipeline.md).

## Reusing the contracts

`SdkContractAssertions` contains adapter-independent checks. Production-adapter tests use an injectable synchronous call surface to verify the exact setter/execution/MP-result/getter order without COM activation. The [generated-client smoke workflow](generated-client-smoke.md) exercises the packaged network boundary with portable fake-worker scenarios and provides an explicit real-SA success check. MP failure, getter failure, hangs, and crashes remain fake-only.

## Non-emulation statement

The scripted fake is not an implementation, simulator, or behavioral model of SpatialAnalyzer. It uses the documented MP result codes (`2` for success and `3` for failure), while its diagnostic codes, delays, failures, hangs, and crashes are controlled test inputs. Passing these tests demonstrates Briosa behavior only at the contracts listed above.

Run the portable checks with:

```powershell
dotnet test tests/Briosa.Worker.Tests/Briosa.Worker.Tests.csproj -c Release
```

No SpatialAnalyzer installation, process, license, proprietary SDK executable, or protected runner is used.
