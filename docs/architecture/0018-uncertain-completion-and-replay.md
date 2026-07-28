# ADR 0018: Uncertain MP completion and replay safety

- Status: Accepted for the v0.2 command surface
- Date: 2026-07-28
- Issue: [#92](https://github.com/spatialanalyzer/briosa/issues/92)
- Amends: [ADR 0004](0004-mp-execution-pipeline.md), [ADR 0008](0008-mp-outcomes-and-grpc-errors.md)

## Context

Client cancellation and gRPC deadlines stop a caller from waiting; they do not cancel an in-flight synchronous COM call. A worker watchdog, crash, or broken control response can also occur after SpatialAnalyzer performed a mutation but before Briosa received the result. Replacing the worker can restore availability without establishing whether replaying the command is safe.

The v0.1 error contract mixes recovery and retry guidance. In particular, watchdog and worker failures currently use `RETRY_AFTER_WORKER_REPLACEMENT`. Although the contract says this does not promise idempotency, a generated client can reasonably interpret that value as permission to replay the failed call. That is unsafe once mutating operations are exposed.

## Decision

Briosa reports execution disposition independently from transport status, worker recovery, MP result, and replay policy.

The public execution dispositions are:

- `NotStarted`: Briosa can prove that the operation did not enter SDK command execution;
- `StartedOutcomeUnknown`: execution may have started, but Briosa cannot prove its final SpatialAnalyzer effect; and
- `Completed`: Briosa obtained a trustworthy terminal MP result, even if the result reports failure or a later output getter failed.

Unspecified or missing disposition is never interpreted as `NotStarted`.

Every promoted catalog operation also has a reviewed replay-safety classification:

- `Safe`: replay is known to be idempotent for the exact operation contract and target;
- `Unsafe`: replay can duplicate or compound effects; or
- `Unknown`: evidence is insufficient, which is treated like `Unsafe` for automatic behavior.

Mutating operations default to `Unknown`. A read-only label alone is not enough to infer replay safety if the operation changes selection, caches, active state, measurement state, or external resources.

## Outcome rules

| Condition | Execution disposition | Automatic replay guidance |
| --- | --- | --- |
| Validation, unsupported operation, policy denial, queue rejection, or unavailable before enqueue | `NotStarted` | Retry only when the specific condition is recoverable |
| Cancellation or deadline proved before enqueue | `NotStarted` | Caller-controlled |
| Cancellation or deadline after enqueue | `StartedOutcomeUnknown` unless the worker proves it skipped the request | Do not automatically replay unless catalog classification is `Safe` |
| Setter rejection before `ExecuteStep` | `NotStarted` | Do not retry unchanged |
| `ExecuteStep` invoked but rejected, response lost, watchdog timeout, worker crash, or control failure | `StartedOutcomeUnknown` | Do not automatically replay unless catalog classification is `Safe` |
| `GetMPStepResult` could not retrieve a result | `StartedOutcomeUnknown` | Do not automatically replay |
| Retrieved MP success or failure result | `Completed` | Do not infer replay from success/failure alone |
| Output getter failed after MP success | `Completed` | Do not replay the command to recover a missing output |

The operation error contract exposes the exact enum fields:

- `execution_disposition`: `NOT_STARTED`, `STARTED_OUTCOME_UNKNOWN`, or `COMPLETED`;
- `recovery_guidance`: `NONE`, `WAIT_FOR_READINESS`, `WORKER_REPLACEMENT`, or `OPERATOR_INTERVENTION_REQUIRED`;
- `replay_guidance`: `DO_NOT_REPLAY`, `MAY_REPLAY`, or `RECONCILE_BEFORE_REPLAY`; and
- `replay_safety`: the exact-target catalog classification `SAFE`, `UNSAFE`, or `UNKNOWN`.

The former `retry_guidance` field and `RETRY_AFTER_WORKER_REPLACEMENT` value are removed and their field number is reserved before any mutating operation is promoted. Replacement may be recommended as an operator recovery action, but it is never automatic replay guidance. Only a proven `NotStarted` operation, or an exact-target operation classified `Safe`, may receive `MAY_REPLAY`.

A worker generation is included in uncertain-completion diagnostics so an operator can correlate recovery without exposing arguments or values. Briosa does not claim exactly-once execution across a synchronous COM boundary.

## Client behavior

Thin clients do not automatically replay `StartedOutcomeUnknown` calls by default. They surface a typed reconciliation-required outcome. Application-specific reconciliation may inspect SpatialAnalyzer state through separate approved read operations before a human or application chooses another command.

Generated documentation explains the execution and replay fields for every operation. Client-language convenience retries may exist only for `NotStarted` plus a recoverable readiness condition, or for operations explicitly classified `Safe`.

## Testing

Portable tests cover cancellation before and after enqueue, completion immediately before crash, a hang after execution begins, a lost response, `ExecuteStep` rejection, MP-result retrieval failure, output-getter failure, and recovery onto a new worker generation. Completeness checks require reviewed replay metadata before a mutating operation enters the catalog.

## Consequences

- Worker replaceability remains a reliability mechanism without becoming an unsafe delivery guarantee.
- Some failures require operator or application reconciliation rather than a transparent retry.
- Public outcome types become more explicit before the first stable protocol release.
- Briosa documents an honest at-most-once-attempt boundary rather than claiming exactly-once effects.
