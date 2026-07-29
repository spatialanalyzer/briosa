# ADR 0017: Verified execution-channel readiness

- Status: Accepted for the v0.2 command surface
- Date: 2026-07-28
- Issue: [#91](https://github.com/spatialanalyzer/briosa/issues/91)
- Amends: [ADR 0003](0003-sdk-connection-lifecycle.md), [ADR 0010](0010-health-version-and-capability-discovery.md)
- Amended by: [ADR 0022](0022-runtime-identity-and-attestation.md)

## Context

`ConnectEx` reports whether an SDK client attached to an already-running SpatialAnalyzer instance. It does not prove that the client owns a usable MP execution channel. Live SA 2026.1.0529.7 experiments showed multiple clients reporting successful connections while only the first eligible client could execute; another client blocked indefinitely in `ExecuteStep`.

The v0.1 state model maps a successful `ConnectEx` directly to `Connected`, admits MP work in that state, and makes public readiness depend on it. That model is useful attachment evidence but is too optimistic for a broader command surface. It can report ready immediately before the first command hangs.

Exact-target identity is a separate concern. COM activation can resolve to the SDK version currently registered on the machine, while the connected SpatialAnalyzer application has its own version. Neither may be inferred from the package's configured target.

## Decision

Briosa separates five readiness dimensions:

1. public-host liveness;
2. worker control-channel state;
3. SDK attachment state;
4. MP execution-channel verification; and
5. exact-target identity policy.

A successful `ConnectEx` transitions the SDK attachment to `ConnectedUnverified`. It does not admit catalog operations and cannot make `briosa.readiness` healthy.

Before admitting ordinary MP work, and only after [ADR 0022](0022-runtime-identity-and-attestation.md)'s exact-target identity gate passes, the worker performs one bounded, read-only ownership probe on the same SDK adapter and STA. The initial exact-target probe is Get Working Directory because it is already a reviewed v0.1 operation. The probe performs the normal `SetStep`, execution, MP-result, and output-retrieval sequence, but its returned path is discarded at the worker boundary and never logged or exposed through discovery.

The execution-verification states are:

- `Unverified`: no ownership probe has completed for this worker generation;
- `Verifying`: the one bounded probe is in progress;
- `ExecutionReady`: the probe completed with MP result code `2` and retrieved the expected output shape;
- `CompetingClientSuspected`: the probe timed out or lost its worker after execution began; and
- `OperatorRecoveryRequired`: ownership is ambiguous or the target has been quarantined pending explicit recovery.

Only `ExecutionReady` admits ordinary MP operations. Verification belongs to one worker generation and is lost whenever that worker exits.

A probe timeout, crash, or lost control response force-terminates the affected worker but does not automatically start another probe/reconnect cycle against the same target. The target enters `OperatorRecoveryRequired`. An operator must establish a clean SpatialAnalyzer/SDK state and explicitly clear the quarantine. This avoids consuming replacement generations against an endpoint whose first-instance or first-client ownership is already ambiguous.

Connection retries are status-aware. A completed `ConnectEx` failure is retried only when its exact status has a reviewed transient classification. Unknown, license-related, connection-in-use, and ownership-ambiguous statuses fail closed. Until that table is reviewed, the safe default is one attempt rather than the v0.1 unconditional three-attempt cycle. Activation failure and a hung `ConnectEx` remain worker-supervision concerns rather than evidence that reconnect is safe.

## Exact-target identity

Discovery represents these values independently:

- configured Briosa target;
- activated SDK engine/type-library version and verification state; and
- connected SpatialAnalyzer application version and verification state.

Configured target text is never copied into an observed-version field. A verified mismatch prevents readiness. When an authoritative runtime query is unavailable, deployment may use the per-claim operator-attestation procedure in [ADR 0022](0022-runtime-identity-and-attestation.md). Runtime evidence always takes precedence, and the attestation state remains distinguishable from runtime verification.

Public readiness requires a control-ready worker, `ExecutionReady`, an open command-admission policy, and an exact-target identity state allowed by the deployment policy. Liveness remains independent of every SDK and SpatialAnalyzer condition.

## Diagnostics and information boundary

Snapshots and logs may contain curated state, worker generation, attempt number, stable diagnostic code, probe timing, and version-verification state. They do not contain the configured hostname, probe output, working-directory path, raw SDK exception, license details, command arguments, or returned operation values.

## Testing

Portable fake-worker tests cover successful attachment and verification, attachment failure, probe rejection, MP failure, malformed output, timeout, crash, cancellation, quarantine, explicit recovery, worker-generation invalidation, and status-aware retry selection. Public health tests prove that `ConnectedUnverified` is not ready.

Protected validation records the SA 2026.1.0529.7 first-client/multiple-client behavior and confirms the chosen read-only probe without running untrusted code or logging its value.

## Consequences

- Readiness means Briosa has recently proved command execution on the current worker generation rather than merely called `ConnectEx` successfully.
- Startup performs one additional read-only MP operation and may remain unavailable until an operator repairs ambiguous port ownership.
- Worker replacement alone no longer clears execution-channel uncertainty.
- Runtime and attested version identity remain honest and separately observable.
