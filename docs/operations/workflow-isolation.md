# Workflow isolation

Briosa's current target-isolation mode is `single_tenant`. One worker and its SpatialAnalyzer target belong to one mutually trusting application or coordinated application group. Briosa does not provide independent sessions between unrelated callers, even when the endpoint is bound to loopback.

The worker queue serializes each complete MP sequence. It does not reserve application-global state between separate RPCs. Callers in the same tenant must therefore coordinate changes to working frames, active collections and files, selections, view state, instrument state, and similar SpatialAnalyzer context. Unrelated tenants require separate Briosa worker/SpatialAnalyzer targets.

`DiscoveryService/GetServerInfo` reports the target-isolation mode. `ListCapabilities` reports each enabled operation's reviewed execution scope:

| Execution scope | Contract in single-tenant mode |
| --- | --- |
| `self_contained` | All state needed for correctness is contained in one serialized MP sequence. |
| `global_state_read` | Reads documented target-global state. The tenant coordinates concurrent mutations. |
| `global_state_mutation` | Changes documented target-global state and completes within one RPC. The tenant coordinates dependent calls. |
| `exclusive_workflow` | Requires ownership across RPCs or an interactive/device session. Denied because leases are not implemented. |
| `unknown` or unspecified | Denied because the isolation review is incomplete. |

An operation can appear in capability discovery only when its handwritten descriptor has a reviewed scope, the current isolation mode supports it, and runtime allow/deny policy enables it. Adding an exclusive workflow to an allowlist cannot bypass the isolation denial.

## Command-family inventory

The following inventory is an implementation guide, not a blanket classification. Exact command semantics can move an operation into a stricter scope, so every handwritten operation still requires an operation-specific isolation review.

| Family | Typical global state or lifetime | Promotion expectation |
| --- | --- | --- |
| Working directory, units, language, backup, logging, wildcard, and other application settings | Process-wide settings | `global_state_read` or `global_state_mutation`; document the named setting and coordination requirement. |
| Active collection/file, working frames, selections, relationships, object visibility, and view state | Active document and application context | Usually `global_state_read` or `global_state_mutation`; require deterministic cleanup or restoration where applicable. |
| Collection objects, geometry, dimensions, analysis, variables, vectors, and reports | Named objects in a shared collection | Classify per command. Explicit-value calculations may be `self_contained`; reads or changes to named objects are global-state operations. |
| File import/export and generated reports | Collection state plus filesystem effects | Usually `global_state_mutation`; filesystem policy and replay review remain separate required gates. |
| Live measurement, scanning, targeting, guiding, projection, trapping, robot motion, and calibration | Device or interactive session spanning commands | `exclusive_workflow` unless an exact command demonstrably completes and cleans up within one RPC. |
| Event monitoring, relationship watching, UI interaction, and other start/continue/stop sequences | Long-lived application session | `exclusive_workflow`; ordinary RPCs remain blocked. |

Reference evidence may suggest a likely scope, but the operation pull request is where execution scope becomes a reviewed, tested, machine-enforced descriptor.

## Worker replacement and future leases

The current release issues no workflow token and holds no cross-RPC ownership state. A worker can therefore never resume an owned workflow after replacement. A future lease protocol must bind every token to both the target and worker generation; exit, quarantine, connection loss, or replacement must revoke it. That future design must also define bounded lifetime, renewal, disconnect cleanup, fairness, operator revocation, authentication, audit events, and denial for callers without the lease before `lease_isolated` can be reported.

Audit events record the target-isolation mode, reviewed execution scope, policy decision, correlation ID, and worker generation where execution reaches the worker. They do not record arguments, results, workflow tokens, geometry, paths, device data, or proprietary values.

See [ADR 0019](../architecture/0019-global-state-workflow-isolation.md) for the decision and future lease boundary.
