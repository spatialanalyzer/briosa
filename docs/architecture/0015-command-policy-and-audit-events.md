# ADR 0015: Command policy and value-free audit events

## Status

Accepted for v0.1 on 2026-07-22.

## Context

The exact-target command catalog limits what a Briosa build can express, but an operator also needs a deployment-specific way to disable reviewed commands. MP methods can read or mutate files, geometry, measurements, devices, application state, or external systems. Inputs and results may contain proprietary data. A successful `ExecuteStep` return also does not establish the MP-level outcome, so audit events must distinguish transport, MP execution, and output retrieval.

The v0.1 endpoint is loopback-only and unauthenticated. Briosa therefore cannot claim a verified caller identity; it can report only an actor category derived from the supported connection posture.

## Decision

- The generated exact-target catalog is the immutable upper bound. Runtime configuration cannot enable an operation absent from that catalog.
- Runtime policy uses exact operation IDs in `Briosa:Security:Operations:Allow` and `Briosa:Security:Operations:Deny`. A missing allowlist enables nothing, and deny wins over allow.
- Empty, duplicate, scalar, or unknown configured IDs fail startup. Policy is fixed for the process lifetime so the logged fingerprint identifies the effective startup policy.
- The generated binding must supply the catalog operation ID and exact MP step. A missing operation is unsupported; a step mismatch is a binding error. Both are rejected before the worker and SDK.
- Operations with an `unknown` effect or risk flag are denied until reviewed. Catalog arguments require an explicit data classification such as `path`, `geometry`, `measurement`, `credential`, `proprietary`, or `unknown`.
- Discovery lists only operations enabled by the runtime policy.
- Policy denial returns gRPC `PERMISSION_DENIED` with the typed `POLICY_DENIED` failure kind and no-retry guidance. Unsupported or mismatched bindings return `UNIMPLEMENTED`.
- One correlation identifier follows a request through host audit events, the policy decision, worker-control messages, and the worker outcome.
- Stable audit events record actor category, endpoint, operation ID, effect, risk flags, worker generation, request and SDK timing, MP outcome, output-retrieval outcome, MP result code, gRPC status, and curated diagnostic code as applicable.
- Audit logger APIs do not accept request arguments or returned values. Paths, geometry, identifiers, measurements, credentials, license data, proprietary data, peer details, target hosts, process IDs, and raw exceptions are not audit fields. Enabling debug or trace logging does not relax this rule.
- Logs use the application's configured sink. Operators own access control, collection, and retention; retention should be no longer than operational or compliance requirements demand.

## Event contract

| Event ID | Meaning | Level |
| --- | --- | --- |
| 1201 | Worker lifecycle transition | Information or Warning |
| 2000 | Effective operation policy loaded | Information |
| 2001 | Operation request received | Information |
| 2002 | Operation allowed by policy | Information |
| 2003 | Operation rejected by policy | Warning |
| 2004 | Operation completed | Information |
| 2005 | Operation failed | Warning |

## Consequences

- A package may safely ship a narrow reviewed default while an operator can reduce it further.
- A policy rejection does not start, connect, enqueue work to, or call the SDK worker.
- Logs support correlation and outcome analysis without becoming a secondary store for SpatialAnalyzer data.
- This is command-level deployment policy, not caller authorization. Hostile local multi-user and remote deployments still require authenticated identity and authorization.
- Adding a catalog operation requires explicit effect, risk, and argument data-classification review plus a deliberate runtime allowlist change.

## Verification

Unit tests cover fail-closed configuration, deny precedence, unsupported and mismatched bindings, pre-worker rejection, discovery filtering, gRPC error mapping, correlation, outcome fields, and value redaction at verbose log levels. Package tests verify the reviewed default policy is present in the published configuration.
