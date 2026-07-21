# Fake SDK and contract-test harness

The portable worker tests use a scripted adapter instead of installing, starting, or licensing SpatialAnalyzer. The harness exists to verify Briosa's own worker contracts: lifecycle ownership, STA affinity, serialization, result preservation, and recovery policy seams.

## Boundary under test

`ISpatialAnalyzerSdk` is an internal, synchronous worker-boundary contract. It uses Briosa-owned command and outcome types and exposes no COM types. `SerializedSdkExecutor` creates and disposes one adapter on a dedicated STA thread and sends all connection and command work through a single-consumer queue.

Cancellation can stop a caller from waiting, but it does not claim to cancel a synchronous SDK call that has already started. A production watchdog must recover from an unresponsive call by replacing the worker process.

## Scripted behaviors

The reusable `Briosa.Worker.Testing` assembly provides deterministic scripts for:

| Behavior | Contract exercised |
| --- | --- |
| Success | Connected execution and a successful MP result |
| MP failure | `ExecuteStep` may return true while the MP result reports failure |
| Connection failure | Connection availability remains distinct from command outcomes |
| Delay | A blocked command keeps later commands from entering the adapter |
| Hang | The watchdog reports a timeout and the supervisor seam replaces the worker |
| Crash | Abrupt worker loss is reported separately and followed by replacement |

The watchdog and supervisor types in this test-support assembly remain lightweight harness seams. The production process supervisor is exercised separately by `Briosa.Server.Tests`; issue #10 owns MP execution deadlines and watchdog policy.

## Reusing the contracts

`SdkContractAssertions` contains adapter-independent checks. A future licensed integration fixture can supply the real adapter factory and invoke the same applicable checks from a protected Windows environment. Scenarios that intentionally force MP failure, hangs, or crashes remain fake-only unless a safe real-SA procedure is explicitly approved.

## Non-emulation statement

The scripted fake is not an implementation, simulator, or behavioral model of SpatialAnalyzer. Its result codes, diagnostic codes, delays, failures, hangs, and crashes are invented test inputs. Passing these tests demonstrates Briosa behavior only at the contracts listed above.

Run the portable checks with:

```powershell
dotnet test tests/Briosa.Worker.Tests/Briosa.Worker.Tests.csproj -c Release
```

No SpatialAnalyzer installation, process, license, proprietary SDK executable, or protected runner is used.
