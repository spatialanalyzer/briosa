# Fake SDK and worker harness

Ordinary builds and tests must not require SpatialAnalyzer, a license, a desktop session, or SDK activation. Briosa keeps the SDK behind private interfaces and uses deterministic fakes at three levels.

## Direct SDK adapter tests

Production-adapter tests use an injectable synchronous call surface to verify exact ordering and failure behavior without COM activation. The implemented-operation tests prove:

1. the exact `SetStep` call occurs first;
2. input setters run in MP argument order;
3. `ExecuteStep` precedes `GetMPStepResult`;
4. success requires retrieved result code `2`; and
5. exact output getters run only after MP success.

Reusable codec tests cover scalar, list, identity/reference, and structured value marshaling retained by the worker. Testing an internal codec does not make any MP command public.

## In-process server fakes

Focused operation tests exercise each handwritten command and result mapping directly, including required input presence and valid zero values. Shared `OperationExecutor` and outcome-mapper tests cover MP failure, output retrieval failure, caller deadline, cancellation, result-mapping failure, typed error details, policy, capability discovery, and audit redaction without repeating that infrastructure for every MP command.

The outcome-mapper matrix validates present default-like values, failed retrieval, malformed results, uncertain completion, recovery guidance, and replay guidance independently from SpatialAnalyzer.

## Process-level fake workers

`Briosa.Worker.TestHost` and `Briosa.SmokeWorker` cross the real private named-pipe control boundary. Scenarios include:

- normal execution;
- disconnected or faulted SDK state;
- retry and reconnect;
- delayed execution;
- hung execution, admission closure, and explicit watchdog recovery;
- worker crash;
- malformed responses;
- MP failure; and
- output getter failure.

These tests prove host survival, bounded shutdown, generation changes, queue serialization, cancellation before and after admission, uncertain completion, readiness, identity gating, and cleanup.

Cancellation can stop a caller from entering the queue or waiting for a response. It does not claim to cancel a synchronous SDK call already in progress. A watchdog or worker loss faults the generation and leaves later calls unadmitted until explicit recovery. Replacement restores availability but does not prove whether the command completed or make replay safe.

## Packaged client boundary

The [standard generated-client smoke workflow](client-smoke.md) starts the
packaged server inert, drives the public lifecycle RPCs with fake application
and worker processes, and crosses the actual loopback HTTP/2 endpoint. The
licensed workflow uses the same public lifecycle and operation contracts for
authorized real-SA paths.

Mutating failure injection, competing-client experiments, hangs, and crashes remain fake-only unless a separate licensed test is explicitly authorized.
