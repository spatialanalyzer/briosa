# Fake SDK and worker harness

Ordinary builds and tests must not require SpatialAnalyzer, a license, a desktop session, or SDK activation. Briosa keeps the SDK behind private interfaces and uses deterministic fakes at three levels.

## Direct SDK adapter tests

Production-adapter tests use an injectable synchronous call surface to verify exact ordering and failure behavior without COM activation. The `GetWorkingDirectory` test proves:

1. `SetStep("Get Working Directory")`;
2. `ExecuteStep`;
3. `GetMPStepResult`;
4. success only when result code `2` is retrieved; and
5. `GetStringArg("Directory", ...)` only after MP success.

Reusable codec tests cover scalar, list, identity/reference, and structured value marshaling retained by the worker. Testing an internal codec does not make any MP command public.

## In-process server fakes

Server tests inject `IWorkerCommandExecutor` to exercise the handwritten `GetWorkingDirectoryOperation` command and result mapping through `OperationExecutor`. They cover success, MP failure, output retrieval failure, caller deadline, cancellation, result-mapping failure, typed error details, policy, capability discovery, and audit redaction.

The outcome-mapper matrix validates present default-like values, failed retrieval, malformed results, uncertain completion, recovery guidance, and replay guidance independently from SpatialAnalyzer.

## Process-level fake workers

`Briosa.Worker.TestHost` and `Briosa.SmokeWorker` cross the real private named-pipe control boundary. Scenarios include:

- normal execution;
- disconnected or faulted SDK state;
- retry and reconnect;
- delayed execution;
- hung execution and watchdog replacement;
- worker crash;
- malformed responses;
- MP failure; and
- output getter failure.

These tests prove host survival, bounded shutdown, generation changes, queue serialization, cancellation before and after admission, uncertain completion, readiness, identity gating, and cleanup.

Cancellation can stop a caller from entering the queue or waiting for a response. It does not claim to cancel a synchronous SDK call already in progress. Worker replacement restores availability but does not prove whether the command completed or make replay safe.

## Packaged client boundary

The [standard generated-client smoke workflow](client-smoke.md) starts the packaged server with a separate fake worker and crosses the actual loopback HTTP/2 endpoint. The licensed workflow uses the same public client for the one authorized real-SA success path.

Mutating failure injection, competing-client experiments, hangs, and crashes remain fake-only unless a separate licensed test is explicitly authorized.
