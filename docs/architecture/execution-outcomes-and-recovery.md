# Execution outcomes and recovery

- Status: Current
- Last reviewed: 2026-10-04

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

Admission checks operation policy and reserves a queue slot before mapping the
protobuf request. The production queue holds at most 64 reservations (including
requests being mapped), in addition to the one active execution. There are no
callers waiting outside that bound for a queue slot. A full queue returns
`ResourceExhausted`, failure kind `Overloaded`, diagnostic `worker-admission-full`,
and `NotStarted`. `MayReplay` permits the caller to submit a new request later;
it never enables automatic retries in first-party clients.

The public gRPC receive limit and the private worker payload limit are each
64 KiB. The public limit applies before operation mapping. JSON envelope overhead
can still make a smaller protobuf request exceed the worker limit; encoding
rejection returns typed `InvalidArgument`/`Validation` with `NotStarted` and does
not retire a healthy worker. These limits bound encoded request sizes, not an
exact amount of managed memory. A transport-level size rejection can occur before
the service runs and therefore need not include an `OperationError` trailer.

An SDK argument setter returning false stops the sequence before `ExecuteStep`.
`ExecuteStep` returning true means only that the call was accepted. Briosa then
calls `GetMPStepResult`; its Boolean means that the numeric result was retrieved,
and result code `2` is the success state. All other codes are retained as MP
failure. Output getters run only after retrieved code `2`, and a failed getter is
preserved separately rather than replaced with a default value.

### SDK call faults

An exception from one SDK call is reported with the dedicated failure kind
`SdkCallFaulted` (`OPERATION_FAILURE_KIND_SDK_CALL_FAULTED`). The execution
disposition carries the phase that Briosa can prove. The worker sends a dedicated
private SDK-fault result with that phase and a value-free diagnostic code, never
the exception text:

| Faulting call | Diagnostic code | gRPC status | Disposition | Replay guidance | MP details |
| --- | --- | --- | --- | --- | --- |
| `SetStep` or an input setter | `sdk-call-faulted-before-execute` | `Internal` | `NotStarted` | `DoNotReplay` | none |
| `ExecuteStep` | `sdk-execute-step-faulted` | `Internal` | `StartedOutcomeUnknown` | `ReconcileBeforeReplay` | `ResultUnavailable`, outputs `NotAttempted` |
| `GetMPStepResult` | `sdk-mp-result-retrieval-faulted` | `Internal` | `StartedOutcomeUnknown` | `ReconcileBeforeReplay` | `ResultUnavailable`, outputs `NotAttempted` |
| An output getter after code `2` | `sdk-output-getter-faulted` | `DataLoss` | `Completed` | `DoNotReplay` | `Succeeded`, code `2`, each output `Retrieved` or `Failed` |

Recovery guidance is `None` in every phase. A fault before `ExecuteStep` is not
reported as `SdkArgumentRejected`, because no argument was rejected. It stays
`DoNotReplay` even though nothing executed: the SDK just threw, so automatic
retry into a possibly unhealthy SDK is not safe. Clients refresh SDK state and
retry deliberately. A fault in `ExecuteStep` or `GetMPStepResult` is
`ReconcileBeforeReplay` regardless of the operation's replay safety. A faulted
getter makes only its own output unavailable; the remaining getters still run,
and retrieved outputs are reported as `Retrieved`. Each failed output carries
the `sdk-output-getter-faulted` code.

`SdkCallFaulted` replaces the interim 0.9.2 mappings, which reported these faults
as `Internal`, `MpResultRetrievalFailure`, or `OutputRetrievalFailure`. The
existing `MpExecutionState` values are unchanged.

These per-call faults leave the worker STA healthy, so the worker generation stays
in service and the private pipe stays usable. Loss of the SDK process is still
detected by the heartbeat liveness probe. The adapter does not convert faults that
make its own state untrustworthy: a detached COM object, out-of-memory, stack
exhaustion, disposal, a native SEH exception, or an STA thread interrupt. Those
faults, and any fault from the serialized executor itself, stop the worker. It
writes only the exception type and HRESULT to standard error and exits without a
response. The supervisor then retires the generation and reports
`StartedOutcomeUnknown` with worker replacement.

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
| Admission capacity exhausted before mapping | `NotStarted` | `ResourceExhausted` |
| Setter rejected before `ExecuteStep` | `NotStarted` | `FailedPrecondition` |
| SDK call faulted before `ExecuteStep` (`SdkCallFaulted`) | `NotStarted` | `Internal` |
| Cancellation or deadline after enqueue | `StartedOutcomeUnknown`; the request stays queued and may still be dispatched | `Cancelled` or `DeadlineExceeded` |
| `ExecuteStep` invoked but response lost, watchdog elapsed, or worker failed | `StartedOutcomeUnknown` | `Unavailable` |
| MP result could not be retrieved | `StartedOutcomeUnknown` | `Internal` |
| `ExecuteStep` or `GetMPStepResult` faulted (`SdkCallFaulted`) | `StartedOutcomeUnknown` | `Internal` |
| Retrieved MP failure | `Completed` | `FailedPrecondition` |
| Output getter failed after MP success | `Completed` | `DataLoss` |
| Output getter faulted after MP success (`SdkCallFaulted`) | `Completed` | `DataLoss` |

## Cancellation, watchdogs, and replacement

Caller cancellation and gRPC deadlines stop that caller from waiting. They do not
cancel a synchronous COM call or withdraw an admitted request. Once a request
enters the supervisor queue, the queue retains ownership: a request whose caller
stopped waiting stays queued and may still be dispatched to the worker and
executed later, after the caller has received `StartedOutcomeUnknown`. The queue
also drains any later worker response so the private pipe cannot become
desynchronized.

The independent execution watchdog protects worker availability. Its budget is
chosen on the server from the request's effective duration class (`quick`,
`long_running`, or `interactive`): the operation's reviewed class, escalated to
`interactive` when an admitted request turns on operator UI, and never shorter
than the reviewed class's budget. An operation without a reviewed class is not
executed.
The readiness probe and worker startup have separate bounds. When the watchdog
expires, the supervisor terminates the worker process tree and requires an explicit
generation-checked SDK recovery request. There is no automatic restart budget.
The affected operation remains
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
