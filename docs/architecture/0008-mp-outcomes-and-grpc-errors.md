# ADR 0008: MP outcomes, output retrieval, and gRPC errors

- Status: Accepted for the v0.1 vertical slice
- Date: 2026-07-22

## Context

An MP request can fail at several independent boundaries. The gRPC request may be invalid or unsupported, SpatialAnalyzer or its worker may be unavailable, a caller may stop waiting, the worker watchdog may replace an unresponsive process, `ExecuteStep` may reject execution, `GetMPStepResult` may report MP failure, or a requested result-only argument getter may fail after MP success.

These cases have different retry and data-validity implications. In particular, `ExecuteStep` returning true does not establish MP success, and MP success does not establish that every requested output was retrieved. A failed output getter cannot be represented as an absent/default value without losing information.

The first vertical slice used operation-local status mapping while this shared contract remained deliberately unsettled. Generated clients now need one language-neutral representation that can be reused by later exact-SA operations without exposing private worker or COM types.

## Decision

The stable core package defines release-independent outcome types in `operation_outcomes.proto`.

- `MpExecutionDetails` records the overall MP state, the numeric MP result code when available, and retrieval details for every requested output.
- `OutputRetrievalDetails` identifies a public result field and distinguishes `RETRIEVED`, `NOT_ATTEMPTED`, and `FAILED`. It never contains the field's value.
- Exact-SA result messages retain their strongly typed operation fields and add `MpExecutionDetails execution = 1000`. The high, fixed field number leaves ordinary catalog-derived field numbering unchanged. Existing result field numbers are never reused or renumbered.
- A successful typed value is present only when its retrieval state is `RETRIEVED`. A successfully retrieved empty string, zero, or false value therefore remains distinguishable from absence or failure.
- MP rejection or failure marks every requested output `NOT_ATTEMPTED`, matching the executor rule that getters run only after successful MP-result inspection.

Non-OK calls use canonical gRPC status codes and include a serialized `OperationError` in the binary metadata entry `briosa-operation-error-bin`. The typed detail is the language-neutral source of truth. The existing `briosa-diagnostic-code` and numeric `briosa-mp-result-code` metadata entries remain as compact operational conveniences.

`OperationError` contains only:

- stable Briosa operation identity;
- failure kind and curated diagnostic code;
- retry guidance;
- worker generation;
- MP and output-retrieval details when execution reached that boundary.

It contains no raw command arguments or returned output values. gRPC status text is similarly generic and value-free.

## Status and retry matrix

| Condition | gRPC status | Failure kind | Retry guidance |
| --- | --- | --- | --- |
| Invalid request or catalog validation | `InvalidArgument` | `VALIDATION` | `DO_NOT_RETRY` unchanged |
| Unsupported operation or exact SA target | `Unimplemented` | `UNSUPPORTED` | `DO_NOT_RETRY` unchanged |
| SA connection not ready | `Unavailable` | `SPATIAL_ANALYZER_UNAVAILABLE` | `RETRY_AFTER_READINESS` |
| Worker not ready | `Unavailable` | `WORKER_UNAVAILABLE` | `RETRY_AFTER_READINESS` |
| Caller cancellation | `Cancelled` | `CALLER_CANCELLED` | `CALLER_CONTROLLED` |
| Caller deadline elapsed | `DeadlineExceeded` | `CALLER_DEADLINE_EXCEEDED` | `CALLER_CONTROLLED` |
| Independent worker watchdog elapsed | `Unavailable` | `WORKER_WATCHDOG_TIMEOUT` | `RETRY_AFTER_WORKER_REPLACEMENT` |
| Worker crash or control failure | `Unavailable` | `WORKER_FAILURE` | `RETRY_AFTER_WORKER_REPLACEMENT` |
| `ExecuteStep` rejected | `FailedPrecondition` | `EXECUTE_STEP_REJECTED` | `DO_NOT_RETRY` unchanged |
| MP result reported failure | `FailedPrecondition` | `MP_FAILURE` | `DO_NOT_RETRY` unchanged |
| Requested output getter failed | `DataLoss` | `OUTPUT_RETRIEVAL_FAILURE` | `DO_NOT_RETRY` automatically |
| Invalid worker or result shape | `Internal` | `INTERNAL` | `DO_NOT_RETRY` automatically |

Retry guidance is intentionally conservative. It does not claim that a mutating MP operation is idempotent. Command-risk and idempotency policy remain separate catalog and operational concerns.

## Deadlines and worker watchdogs

The caller cancellation token controls only that caller's wait. If the token is canceled after a request enters the supervisor queue, the queue continues to own the request and drains its worker response so the private pipe cannot become desynchronized.

The gRPC service distinguishes an elapsed `ServerCallContext.Deadline` from other caller cancellation. It reports the former as `DeadlineExceeded` and the latter as `Cancelled`.

The worker watchdog is independent. Its timeout force-terminates and replaces the unresponsive worker within the bounded restart policy, and the affected call receives `Unavailable` with `WORKER_WATCHDOG_TIMEOUT`. A watchdog expiry is never reported as the caller's deadline.

## Implementation boundary

`GrpcOperationOutcomeMapper` is the reviewed hand-written policy point. It validates the private worker result shape, creates explicit successful execution details, and maps failures to gRPC status plus typed metadata. Generated catalog artifacts provide public field names and private MP argument names but do not choose error policy.

Issue #16 will generalize operation adapter and result mapping generation. Issue #18 will build the packaged external-client and cross-process failure suite on this stable contract. Health, readiness, and capability services remain owned by issue #12.

## Testing

Portable tests verify every matrix row without SpatialAnalyzer. They also verify that:

- a retrieved empty typed value remains present and marked `RETRIEVED`;
- MP failure retains its numeric result and marks getters `NOT_ATTEMPTED`;
- getter failure is `DataLoss` with `FAILED` retrieval and no returned value in metadata;
- caller deadline and worker watchdog produce different statuses and failure kinds;
- malformed output shapes fail as `Internal`;
- catalog regeneration adds shared execution details deterministically without renumbering operation fields.

Buf formatting, linting, schema compilation, and FILE-level comparison against `origin/main` remain ordinary CI checks. Catalog-artifact verification regenerates checked-in operation contracts and fails on drift.

## Consequences

- Thin clients can make typed decisions without parsing status text.
- Operation values remain exact-target and strongly typed while outcome mechanics remain stable core concepts.
- Successful default-like values cannot be confused with retrieval failure.
- Public failure metadata is safe for default diagnostics because it excludes raw values.
- Adding the execution field is wire-compatible with the existing vertical-slice result because `directory = 1` is preserved.
