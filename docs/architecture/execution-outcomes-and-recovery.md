# Execution outcomes and recovery

- Status: Current
- Last reviewed: 2026-08-01

## Serialized MP execution

One server supervisor owns a bounded, single-consumer execution queue for its one
worker generation. One worker-owned STA serializes each complete MP sequence:

1. select the exact MP step;
2. set every supplied input argument in reviewed order;
3. call `ExecuteStep`;
4. retrieve the MP result with `GetMPStepResult`; and
5. retrieve requested output arguments only after MP success.

No request may interleave another request's sequence. Queue serialization protects
the SDK call protocol; it does not create application transactions or isolate
SpatialAnalyzer global state across several RPCs.

An SDK argument setter returning false stops the sequence before `ExecuteStep`.
`ExecuteStep` returning true means only that the call was accepted. Briosa then
calls `GetMPStepResult`; its Boolean means that the numeric result was retrieved,
and result code `2` is the success state. All other codes are retained as MP
failure. Output getters run only after retrieved code `2`, and a failed getter is
preserved separately rather than replaced with a default value.

## Public result and error model

Every strongly typed result contains `MpExecutionDetails execution = 1000`.
Successful scalar fields use protobuf presence when absence must remain distinct
from an empty string, zero, or false. Output retrieval details identify the public
field and distinguish `Retrieved`, `NotAttempted`, and `Failed` without containing
the field value.

Non-OK RPCs use a canonical gRPC status and exactly one typed
`OperationError` trailer. That error keeps these dimensions separate:

- failure kind and curated diagnostic code;
- execution disposition;
- worker recovery guidance;
- replay guidance and the operation's reviewed replay safety;
- worker generation; and
- MP and output-retrieval details when available.

The error contains no raw arguments or returned values. Status text is generic and
value-free.

## Execution disposition

Briosa reports whether execution definitely did not start, may have started with
an unknown outcome, or completed:

- `NotStarted` means Briosa can prove the request did not enter SDK command
  execution.
- `StartedOutcomeUnknown` means the request may have entered execution, but Briosa
  cannot prove the final SpatialAnalyzer effect.
- `Completed` means Briosa obtained a trustworthy terminal MP result, even if that
  result was failure or a later output getter failed.

Missing or unspecified disposition is never interpreted as `NotStarted`.

| Condition | Disposition | Typical gRPC status |
| --- | --- | --- |
| Validation, unsupported operation, policy denial, or unavailable before enqueue | `NotStarted` | Request-specific or `Unavailable` |
| Setter rejected before `ExecuteStep` | `NotStarted` | `FailedPrecondition` |
| Cancellation or deadline after enqueue | `StartedOutcomeUnknown` unless the worker proves it skipped execution | `Cancelled` or `DeadlineExceeded` |
| `ExecuteStep` invoked but response lost, watchdog elapsed, or worker failed | `StartedOutcomeUnknown` | `Unavailable` |
| MP result could not be retrieved | `StartedOutcomeUnknown` | `Internal` |
| Retrieved MP failure | `Completed` | `FailedPrecondition` |
| Output getter failed after MP success | `Completed` | `DataLoss` |

## Cancellation, watchdogs, and replacement

Caller cancellation and gRPC deadlines stop that caller from waiting. They do not
cancel a synchronous COM call. Once a request enters the supervisor queue, the
queue retains ownership and drains any later worker response so the private pipe
cannot become desynchronized.

The independent execution watchdog protects worker availability. When it expires,
the supervisor terminates the worker process tree and may start a replacement
within the restart budget. The affected operation remains
`StartedOutcomeUnknown`. A watchdog timeout is not reported as the caller's
deadline, and worker replacement does not establish the interrupted command's
result.

## Replay safety

Every handwritten operation has an explicit exact-target replay-safety
classification:

- `Safe`: reviewed evidence establishes that replay is safe for the exact contract;
- `Unsafe`: replay can duplicate or compound effects; or
- `Unknown`: evidence is insufficient and automatic behavior treats it as unsafe.

A read-only label alone does not establish replay safety if a command changes
selection, caches, active state, measurement state, or an external resource.
Mutating operations start as `Unknown` unless exact evidence supports a stronger
claim.

Thin clients do not automatically replay `StartedOutcomeUnknown` operations by
default. Recovery of worker availability and permission to replay are independent
decisions. An output-getter failure after MP success never justifies replay merely
to recover the missing output.

## Global-state and workflow isolation

The initial deployment is single-tenant per worker/SpatialAnalyzer target: one
mutually trusting application or coordinated application group. Briosa does not
isolate unrelated local callers or provide independent SpatialAnalyzer sessions.

Each operation also has an execution scope:

- `SelfContained` completes within one serialized MP sequence;
- `GlobalStateRead` depends on named application-global state;
- `GlobalStateMutation` changes global state within one RPC; or
- `ExclusiveWorkflow` requires ownership across several RPCs or an interactive or
  device session.

Unknown scope fails closed. `ExclusiveWorkflow` operations remain unsupported
until a separate lease/session design defines authenticated ownership, lifetime,
renewal, fairness, disconnect behavior, revocation, worker-generation binding,
audit behavior, and denial semantics. Callers sharing a target must currently
coordinate application-global state outside Briosa.

See the target-local [workflow isolation guide](../../targets/2026.1.0529.7/docs/operations/workflow-isolation.md)
for operator-facing implications.
