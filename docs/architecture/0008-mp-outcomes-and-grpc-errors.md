# ADR 0008: MP outcomes, output retrieval, and gRPC errors

- Status: Accepted for the v0.1 vertical slice
- Date: 2026-07-22
- Amended by: [ADR 0018](0018-uncertain-completion-and-replay.md)

## Context

An MP request can fail at several independent boundaries. The gRPC request may be invalid or unsupported, SpatialAnalyzer or its worker may be unavailable, a caller may stop waiting, the worker watchdog may replace an unresponsive process, `ExecuteStep` may reject execution, `GetMPStepResult` may fail to retrieve a result, the retrieved numeric result may report MP failure, or a requested result-only argument getter may fail after MP success.

These cases have different retry and data-validity implications. In particular, `ExecuteStep` returning true does not establish MP success, and MP success does not establish that every requested output was retrieved. A failed output getter cannot be represented as an absent/default value without losing information.

The first vertical slice used operation-local status mapping while this shared contract remained deliberately unsettled. Clients need one language-neutral representation that can be reused by later exact-SA operations without exposing private worker or COM types.

## Decision

The stable core package defines release-independent outcome types in `operation_outcomes.proto`.

- `MpExecutionDetails` records the overall MP state, the numeric MP result code when available, and retrieval details for every requested output.
- `OutputRetrievalDetails` identifies a public result field and distinguishes `RETRIEVED`, `NOT_ATTEMPTED`, and `FAILED`. It never contains the field's value.
- Exact-SA result messages retain their strongly typed operation fields and add `MpExecutionDetails execution = 1000`. The high, fixed field number leaves ordinary operation fields separate. Existing result field numbers are never reused or renumbered.
- A successful typed value is present only when its retrieval state is `RETRIEVED`. A successfully retrieved empty string, zero, or false value therefore remains distinguishable from absence or failure.
- MP rejection, MP-result retrieval failure, or a retrieved non-success result marks every requested output `NOT_ATTEMPTED`, matching the executor rule that getters run only after result code `2`.
- `RESULT_UNAVAILABLE` with `MP_RESULT_RETRIEVAL_FAILURE` represents a false `GetMPStepResult` Boolean and carries no numeric result code. `FAILED` with `MP_FAILURE` means the getter succeeded but returned a code other than `2`; the raw code is retained.

Non-OK calls use canonical gRPC status codes and include exactly one Briosa-specific metadata entry: a serialized `OperationError` in `briosa-operation-error-bin`. The typed detail is the language-neutral error contract. Diagnostic and MP-result codes are carried inside that message rather than duplicated in scalar trailers.

`OperationError` contains only:

- stable Briosa operation identity;
- failure kind and curated diagnostic code;
- execution disposition (`NotStarted`, `StartedOutcomeUnknown`, or `Completed`);
- worker recovery guidance, kept independent from replay;
- replay guidance and the operation descriptor's reviewed replay-safety classification;
- worker generation;
- MP and output-retrieval details when execution reached that boundary.

It contains no raw command arguments or returned output values. gRPC status text is similarly generic and value-free.

## Status, execution, recovery, and replay matrix

| Condition | gRPC status / failure kind | Execution disposition | Recovery guidance | Replay guidance |
| --- | --- | --- | --- | --- |
| Invalid request, unsupported operation, or policy denial | canonical request status / matching kind | `NOT_STARTED` | `NONE` | `DO_NOT_REPLAY` unchanged |
| SA or worker unavailable before enqueue or SDK execution | `Unavailable` / availability kind | `NOT_STARTED` | `WAIT_FOR_READINESS` | `MAY_REPLAY` after readiness |
| Caller cancellation or deadline before enqueue | caller status / caller kind | `NOT_STARTED` | `NONE` | `MAY_REPLAY` |
| Caller cancellation or deadline after enqueue | caller status / caller kind | `STARTED_OUTCOME_UNKNOWN` | `NONE` | `MAY_REPLAY` only for operation `SAFE`; otherwise reconcile |
| SDK argument setter rejected before `ExecuteStep` | `FailedPrecondition` / `SDK_ARGUMENT_REJECTED` | `NOT_STARTED` | `NONE` | `DO_NOT_REPLAY` unchanged |
| `ExecuteStep` rejected | `FailedPrecondition` / `EXECUTE_STEP_REJECTED` | `STARTED_OUTCOME_UNKNOWN` | `NONE` | `MAY_REPLAY` only for operation `SAFE`; otherwise reconcile |
| Independent worker watchdog elapsed | `Unavailable` / `WORKER_WATCHDOG_TIMEOUT` | `STARTED_OUTCOME_UNKNOWN` | `WORKER_REPLACEMENT` | `MAY_REPLAY` only for operation `SAFE`; otherwise reconcile |
| Worker crash or control response loss | `Unavailable` / `WORKER_FAILURE` | `STARTED_OUTCOME_UNKNOWN` | `WORKER_REPLACEMENT` | `MAY_REPLAY` only for operation `SAFE`; otherwise reconcile |
| `GetMPStepResult` failed to retrieve a result | `Internal` / `MP_RESULT_RETRIEVAL_FAILURE` | `STARTED_OUTCOME_UNKNOWN` | `NONE` | `RECONCILE_BEFORE_REPLAY` |
| Retrieved MP result code was not `2` | `FailedPrecondition` / `MP_FAILURE` | `COMPLETED` | `NONE` | `DO_NOT_REPLAY` unchanged |
| Requested output getter failed | `DataLoss` / `OUTPUT_RETRIEVAL_FAILURE` | `COMPLETED` | `NONE` | `DO_NOT_REPLAY` to recover output |
| Invalid result shape after a terminal MP result | `Internal` / `INTERNAL` | `COMPLETED` | `NONE` | `DO_NOT_REPLAY` |

Missing or unspecified execution disposition is never treated as `NOT_STARTED`. Operation replay safety is separately reviewed as `SAFE`, `UNSAFE`, or `UNKNOWN`; `UNKNOWN` is handled like `UNSAFE` for automatic behavior.

## Deadlines and worker watchdogs

The caller cancellation token controls only that caller's wait. If the token is canceled after a request enters the supervisor queue, the queue continues to own the request and drains its worker response so the private pipe cannot become desynchronized.

The gRPC service distinguishes an elapsed `ServerCallContext.Deadline` from other caller cancellation. It reports the former as `DeadlineExceeded` and the latter as `Cancelled`.

The worker watchdog is independent. Its timeout force-terminates and replaces the unresponsive worker within the bounded restart policy, and the affected call receives `Unavailable` with `WORKER_WATCHDOG_TIMEOUT`. A watchdog expiry is never reported as the caller's deadline.

## Implementation boundary

`GrpcOperationOutcomeMapper` is the reviewed handwritten policy point. It validates the private worker result shape, creates explicit successful execution details, and maps failures to gRPC status plus typed metadata. Each handwritten operation provides its public field mapping and private MP argument names but does not choose error policy.

Issue #16 will generalize operation adapter and result mapping generation. Issue #18 will build the packaged external-client and cross-process failure suite on this stable contract. Health, readiness, and capability services remain owned by issue #12.

## Testing

Portable tests verify every matrix row without SpatialAnalyzer. They also verify that:

- a retrieved empty typed value remains present and marked `RETRIEVED`;
- MP-result retrieval failure carries no numeric result and marks getters `NOT_ATTEMPTED`;
- MP failure retains its numeric result and marks getters `NOT_ATTEMPTED`;
- getter failure is `DataLoss` with `FAILED` retrieval and no returned value in metadata;
- caller deadline and worker watchdog produce different statuses and failure kinds;
- pre-enqueue cancellation is `NOT_STARTED`, while post-enqueue cancellation is uncertain;
- worker replacement guidance never becomes replay guidance for `UNSAFE` or `UNKNOWN` operations;
- malformed output shapes fail as `Internal`;
- operation tests verify shared execution details without renumbering operation fields.
- non-OK calls carry only the typed `OperationError` trailer;

Buf formatting, linting, and schema compilation remain ordinary CI checks. Once Briosa has a public release, FILE-level compatibility is checked against that explicit release baseline rather than the evolving `main` branch.

## Consequences

- Thin clients can make typed decisions without parsing status text.
- Operation values remain exact-target and strongly typed while outcome mechanics remain stable core concepts.
- Successful default-like values cannot be confused with retrieval failure.
- Public failure metadata is safe for default diagnostics because it excludes raw values.
- The high execution field number keeps shared execution metadata separate from ordinary operation result fields.
