# Command-surface selection

This document records the accepted selection boundary for exact target
`2026.1.0529.7`. [#145](https://github.com/spatialanalyzer/briosa/issues/145)
governs selection; [#42](https://github.com/spatialanalyzer/briosa/issues/42)
owns implementation; [#152](https://github.com/spatialanalyzer/briosa/issues/152)
and [briosa-docs#13](https://github.com/spatialanalyzer/briosa-docs/issues/13)
own complete public catalog dispositions. GitHub issues and the
[Roadmap & Delivery Project](https://github.com/orgs/spatialanalyzer/projects/1)
are the planning source of truth.

The evolving planned v1 set contains only operations deliberately selected by
a maintainer. Each becomes supported when delivered as a complete handwritten
vertical slice. Inventory membership, historical candidates, ObjectiveSA
coverage, and a public documentation record do not select an operation.
Handwritten protobuf and operation source, `SpatialAnalyzerApi.Operations`,
and runtime policy define what the product actually supports.

## Selection and evidence

An accepted operation or coherent batch has a native direct sub-issue of #42.
Selection requires a recognizable MP workflow or accepted Briosa product need,
a language-neutral typed protobuf shape, and compatibility with the worker,
serialization, single-tenant, security, and recovery boundaries.
That issue identifies each exact MP step, target, proposed service/RPC and
stable operation ID, and the intended v0.x delivery horizon. For each
operation, record the exact-target inventory and installed documentation
observations, View SDK Code observation, interop surface, setter/getter order,
argument names and directions, semantic family, and any discrepancy. ObjectiveSA
may corroborate a complete match but cannot add a binding, choice, default,
or compatibility claim absent from the target. Link curated evidence; do not
copy vendor prose or generated SDK code.

The issue also records effect (`read_only` or `mutating`), execution scope
(`self_contained`, `global_state_read`, `global_state_mutation`, or
`exclusive_workflow`), replay safety (`safe`, `unsafe`, or `unknown`),
risk flags, fixture requirements, portable negative paths, licensed
validation scenarios, and cleanup of application-global state. An exclusive
workflow remains blocked until a lease/session design is accepted. Unresolved
effect, scope, or replay safety blocks selection. A reviewed batch can contain
multiple commands; batch size alone does not determine issue or PR boundaries.

Missing fixtures, third-party software, hardware, permissions, or licenses
make an otherwise matched operation **at risk**. Record the gap and an opt-in
licensed scenario; do not call it an intentional exclusion or a passing test.
If ObjectiveSA parity is absent and the disposition remains uncertain, ask the
maintainer. The [operation and protocol model](../../../../docs/architecture/operation-and-protocol-model.md)
defines the required supported vertical slice.

## Dispositions and public catalog

Only reviewed operations receive an implementation disposition: selected,
deferred, intentionally excluded, or SDK unavailable. An unreviewed inventory
entry is unselected; its public status is `Under review`. `SDK unavailable`
requires exact-target evidence that an essential MP step or binding cannot be
expressed through the approved SDK surface. A documentation/SDK discrepancy
alone is insufficient.

The documentation site covers every retained command identity, including
unsupported commands, without becoming a runtime authority. Its statuses are
`Supported`, `Selected for v1`, `Under review`, `Deferred beyond v1`,
`Intentionally excluded`, and `SDK unavailable`. Each reviewed record has
a project-authored rationale, validation qualification, and an alternative,
workaround, or explicit none-known statement where applicable. `At risk`
qualifies validation; it is not a primary status. Documentation-only structured
content and coverage checks are allowed in the documentation repository;
server, clients, packages, and Briosa builds must not consume them.

## V1 freeze

[Validation and release #47](https://github.com/spatialanalyzer/briosa/issues/47)
owns the freeze Task. Before the first v1 release candidate, every selected
operation must be complete and registered, server/protocol/clients/docs must
agree, all retained commands must have final public catalog statuses, and
portable and licensed validation status—including at-risk gaps—must be
explicit. No open blocker may change a selected public contract or support
claim.

The freeze Task records operation IDs and exact MP steps, accepted issue URLs,
server/protocol/client/documentation coordinates, catalog revision and
coverage, validation evidence, and at-risk qualifications. It is a review
snapshot in GitHub, not an input to runtime or source generation. Changing the
frozen surface requires a linked scope-exception issue, compatibility and
validation impact review, and renewed affected checks; an explicit unfreeze
withdraws the release-candidate claim.

Do not restore a generated implementation catalog, generated operation or
client facade, implementation-completeness gate, generic public command
executor, or support claims inferred from inventory counts. Standard
protobuf/gRPC generation remains allowed.
