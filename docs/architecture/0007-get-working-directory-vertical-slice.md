# ADR 0007: Get Working Directory reference vertical slice

- Status: Accepted; authoring strategy revised by [ADR 0024](0024-handwritten-mp-operation-vertical-slices.md)
- Date: 2026-07-22
- Revised: 2026-07-31

## Context

`Get Working Directory` is Briosa's first production-shaped public MP operation for SpatialAnalyzer 2026.1.0529.7. It has no inputs and one result-only string argument. The installed MP command reference and View SDK Code agree that the MP step is `Get Working Directory` and the SDK getter argument is `Directory`.

The command is intentionally small, but it exercises the same boundaries required by larger commands: a strongly typed public RPC, runtime policy, the supervised worker process, the serialized SDK-owning STA, MP-result inspection, result-only argument retrieval, typed outcomes, and redacted diagnostics.

## Decision

`GetWorkingDirectory` is maintained as ordinary reviewed source:

- `file_operations.proto` declares the exact-target `FileOperations/GetWorkingDirectory` RPC, an empty request, and a result with optional `directory` plus shared execution details.
- `GetWorkingDirectoryOperation` defines the exact operation ID, MP step, `GetStringArg("Directory", ...)` output binding, worker command, result mapping, replay safety, scope, and capability descriptor.
- `FileOperationsService` submits the operation through the shared handwritten `OperationExecutor`.
- `SpatialAnalyzerApi.Operations` registers the implemented capability and provides the policy/discovery source.
- The worker executes `SetStep`, `ExecuteStep`, `GetMPStepResult`, and—only after a retrieved success code of `2`—`GetStringArg("Directory", ...)` on its single SDK-owning STA.

A successful getter produces a present `directory`. An MP failure suppresses output retrieval. A failed getter produces a gRPC failure and never creates a successful response containing an invented empty path. Logs and error status never contain the returned directory.

Standard protobuf/gRPC generation produces transport plumbing and the smoke-test client. No Briosa-specific catalog or operation generator participates.

## Transport mapping

ADR 0008 owns the complete typed outcome contract. The important mappings for this operation are:

| Internal outcome | gRPC status |
| --- | --- |
| Completed with retrieved output | `OK` |
| `ExecuteStep` rejected or MP result failed | `FailedPrecondition` |
| MP result could not be retrieved | `Internal` |
| Output getter failed | `DataLoss` |
| Worker unavailable, crashed, or watchdog expired | `Unavailable` |
| Caller stopped waiting | `Cancelled` or `DeadlineExceeded` |

A worker watchdog expiration is not reported as the caller's deadline. Worker replacement does not prove whether an interrupted COM operation completed.

## Testing and validation

Portable tests verify the exact worker command, success mapping, MP failure, getter failure, policy, cancellation, deadlines, watchdog replacement, discovery, reflection, audit redaction, and SDK call order. These tests do not activate SpatialAnalyzer.

The opt-in licensed workflow uses the standard generated client against a separately installed and licensed SA 2026.1.0529.7 instance. It validates the real public RPC and does not print or retain the returned directory.

## Consequences

- The reference operation is readable end to end without reconstructing a generated pipeline.
- Future operations can copy the proven structure and then refactor only after repetition demonstrates a stable abstraction.
- Getter failure cannot be mistaken for a successful empty path.
- The worker, readiness, outcome, policy, and redaction architecture remains unchanged.
