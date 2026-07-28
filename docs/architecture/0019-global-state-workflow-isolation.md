# ADR 0019: SpatialAnalyzer global-state workflow isolation

- Status: Accepted for the v0.2 command surface
- Date: 2026-07-28
- Issue: [#93](https://github.com/spatialanalyzer/briosa/issues/93)
- Amends: [ADR 0004](0004-mp-execution-pipeline.md), [ADR 0006](0006-versioned-command-catalog.md), [ADR 0015](0015-command-policy-and-audit-events.md)

## Context

The worker queue and STA make one MP sequence atomic. They do not isolate a workflow composed of several gRPC calls. SpatialAnalyzer has application-global state such as working frames, active collections and files, interaction modes, measurement/device sessions, selections, and other context that one caller can change between another caller's operations.

ObjectiveSA historically assumed one in-process owner. Briosa is a service and can receive calls from multiple local processes even while its public endpoint remains loopback-only. Describing serialization as client or workflow isolation would therefore be misleading.

## Decision

The initial v0.2 deployment contract is single-tenant per worker/SpatialAnalyzer target. A tenant is one mutually trusting application or coordinated application group under one operator. Briosa does not yet provide independent sessions or isolation between unrelated callers.

The [workflow-isolation operator guide](../operations/workflow-isolation.md) inventories the command families that commonly read, mutate, or retain application-global state. The family inventory guides review; the exact promoted operation remains the unit of catalog classification.

Single-tenant deployment is not sufficient to promote an arbitrary multi-call stateful workflow. Catalog operations are reviewed with an execution-scope classification:

- `SelfContained`: the operation's contract is complete within one serialized MP sequence;
- `GlobalStateRead`: the result depends on named SpatialAnalyzer global state but does not intentionally change it;
- `GlobalStateMutation`: the operation changes global state and completes within one RPC; or
- `ExclusiveWorkflow`: correctness requires ownership across multiple RPCs or an interactive/device session.

Unknown execution scope fails closed. A `GlobalStateRead` or `GlobalStateMutation` operation may be promoted only when its dependency/effect is documented and compatible with the single-tenant deployment contract. `ExclusiveWorkflow` operations remain blocked until Briosa implements an explicit lease protocol and command policy for them.

The public contract and operator documentation state that:

- the single queue guarantees command-sequence serialization, not per-client state isolation;
- callers sharing a target must coordinate application-global state outside Briosa;
- unrelated tenants must use separate Briosa worker/SpatialAnalyzer targets; and
- loopback binding is not an isolation mechanism between processes running as the same user or machine.

## Future lease boundary

An exclusive-workflow lease is intentionally not invented in this ADR. A future design must define token authentication, acquisition, bounded lifetime, renewal, disconnect behavior, fairness, operator revocation, audit events, and denial behavior for callers without the lease.

Any future lease is bound to the target and worker generation. Worker exit, replacement, connection quarantine, or loss of execution readiness invalidates it. A replacement worker never silently continues the previous workflow.

## Policy and observability

Runtime policy denies an operation whose execution scope requires isolation the server does not provide. Capability discovery does not advertise a policy-denied exclusive workflow.

Audit events may include operation identity, execution scope, tenant/lease decision, worker generation, timing, and outcome. They do not include raw tokens, arguments, geometry, paths, device data, or returned values.

## Testing

Portable tests prove that concurrent ordinary operations remain serialized, unknown/exclusive scopes fail closed, and worker replacement invalidates any future ownership state. If leases are introduced, tests also cover competing callers, expiry, disconnect, revocation, and stale-generation tokens.

## Consequences

- Briosa makes its initial single-tenant assumption explicit instead of implying client isolation.
- Stateful commands require additional catalog review and may remain blocked even when an SDK binding exists.
- Shared or remotely authenticated multi-tenant deployment requires a later lease/session decision.
- Self-contained read-only operations can continue to scale without waiting for a general workflow protocol.
