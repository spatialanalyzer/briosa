# V1 command-surface planning

- Status: Evolving during v0.x development
- Exact target: SpatialAnalyzer `2026.1.0529.7`
- Governing RFC: [#145](https://github.com/spatialanalyzer/briosa/issues/145)
- Command-surface epic: [#42](https://github.com/spatialanalyzer/briosa/issues/42)

## Purpose

Briosa v1 means the complete, finite set of SpatialAnalyzer MP operations that
maintainers deliberately select for this exact target. V1 does not mean every
command in the retained inventory, every command once marked as a candidate, or
every method exposed by a prior-release wrapper.

The set may grow through coordinated v0.x releases. It becomes immutable only
when maintainers complete the freeze procedure below before the first v1 release
candidate.

This document governs planning. It does not define runtime support. The
handwritten protobuf contracts, operation implementations,
`SpatialAnalyzerApi.Operations`, and runtime policy remain the authorities for
compiled and admitted operations.

## Authority and evidence

GitHub issues and the
[Briosa Roadmap & Delivery Project](https://github.com/orgs/spatialanalyzer/projects/1)
are the planning source of truth:

- an accepted operation or coherent batch is a native direct sub-issue of #42;
- its body identifies the individual MP operations selected for v1 and records
  the review facts defined below;
- its pull requests deliver complete handwritten vertical slices; and
- its Project fields identify the intended v0.x delivery horizon.

The retained exact-target inventory, bindings, and semantic-value files are
non-authoritative evidence. Historical command dispositions and generated
artifacts are available in Git history as review aids only. Neither source can
select an operation, claim support, or satisfy an issue acceptance criterion by
itself.

Pinned ObjectiveSA is prior-release secondary evidence. A complete MP-step and
binding match can accelerate review, but exact-target evidence wins on conflict.
ObjectiveSA cannot add a field, binding, choice, default, or compatibility claim
that is absent from the exact target.

## Planning dispositions

Only commands that a maintainer has deliberately reviewed receive a disposition.
An inventory entry that has not been reviewed is simply unselected; it is not
implicitly deferred or intentionally excluded.

| Disposition | Meaning | GitHub placement |
| --- | --- | --- |
| Implemented baseline | The operation is already a complete handwritten vertical slice and is part of the evolving v1 set. | Its closed delivery issue is a direct sub-issue of #42. |
| Selected | The operation is committed to v1 but is not yet a complete supported vertical slice. | Its open operation or coherent-batch issue is a direct sub-issue of #42 and states `V1 disposition: Selected`. |
| Proposed | The operation is being evaluated and is not yet in v1. | A proposal or investigation may be a sub-issue of #145; it is not a direct operation sub-issue of #42. |
| Deferred | A reviewed operation is deliberately outside v1 but may be reconsidered for a later release. | The decision remains under #145 or another linked planning issue, with rationale and reconsideration trigger. |
| Intentional exclusion | A reviewed operation is outside Briosa's intended API because it conflicts with an accepted product, security, ownership, or architecture boundary. | The decision remains under #145 or another linked planning issue, with the governing reason. |
| SDK unavailable | Exact-target evidence shows that a required MP step or input/output binding cannot be expressed through the approved SDK surface. | The evidence and missing binding remain in a focused issue under #145 or the relevant external dependency. |

`At risk` is an orthogonal validation qualifier, not a reason to exclude a
command. A selected or implemented operation is at risk when complete licensed
validation depends on unavailable hardware, software, data, permissions, or
fixtures. Its issue must name the gap and contain an opt-in validation plan. It
must never claim that an unexecuted scenario passed.

An unresolved documentation/SDK discrepancy is not automatically `SDK
unavailable`. Keep it proposed or deferred pending evidence unless the exact
target proves that the required binding is absent.

## Selection principles

Maintainers select an operation or coherent batch only when the issue establishes
all of the following:

1. The operation enables a recognizable MP rewrite workflow or an accepted
   Briosa product need.
2. The exact MP step, arguments, directions, and target evidence are identified.
3. Every required SDK setter and getter is available or a focused evidence task
   is explicitly blocking selection.
4. Public request and result values can be represented with strongly typed,
   language-neutral protobuf concepts without exposing COM types.
5. The operation fits the worker, serialization, single-tenant, policy,
   uncertain-completion, and security boundaries, or an accepted architecture
   decision changes those boundaries first.
6. Its effect, execution scope, replay safety, risk flags, fixture needs,
   external dependencies, and validation plan are explicit.
7. The proposed batch is coherent by workflow, shared evidence, fixture, value
   family, or review context. Batch size alone is neither a reason to split nor a
   reason to combine operations.

Prefer candidate review in this order unless user value or a dependency justifies
a different order:

1. fixture-free read-only inspection and discovery workflows;
2. read-only workflows using small deterministic project fixtures;
3. common construction and mutation workflows with deterministic cleanup;
4. long-running, interactive, filesystem, or external-integration workflows; and
5. hardware- or license-dependent workflows that require explicit at-risk plans.

This ordering is prioritization guidance, not a selected-command list.

Missing local fixtures, third-party software, hardware, or licensed
infrastructure does not justify intentional exclusion when the exact operation
otherwise has a reviewed contract. Record an at-risk plan instead. When exact
ObjectiveSA parity is absent and selection remains uncertain, obtain a maintainer
decision rather than silently promoting or excluding the operation.

## Required selected-issue record

An operation or coherent-batch issue must contain the following before a
maintainer changes its v1 disposition to `Selected` and makes it a direct
sub-issue of #42.

### Scope identity

- `V1 disposition: Selected`;
- exact target `2026.1.0529.7`;
- each exact MP step in the batch;
- proposed service, RPC, and stable operation ID for each step; and
- intended v0.x delivery wave or an explicit unscheduled state.

### Evidence

For each operation, record:

- retained exact-target inventory key, when present;
- installed-documentation observation and fingerprint, when available;
- View SDK Code observation and fingerprint, when available;
- exact SDK setter/getter sequence and any unresolved discrepancy;
- relevant approved interop and semantic-value family; and
- ObjectiveSA parity, mismatch, absence, or non-applicability.

References point to curated evidence; issue bodies must not copy vendor prose or
proprietary generated source.

### Runtime and risk classification

Use the implemented runtime terminology:

- effect: `read_only` or `mutating`;
- execution scope: `self_contained`, `global_state_read`,
  `global_state_mutation`, or `exclusive_workflow`;
- replay safety: `safe`, `unsafe`, or `unknown`; and
- operation-specific risk flags with a short rationale.

An `exclusive_workflow` operation remains blocked until an accepted lease or
session design exists. Unknown effect, scope, or replay safety fails selection
closed unless the issue is explicitly an investigation rather than an operation
delivery issue.

### Fixtures and validation

Record:

- portable fake-worker scenarios and negative paths;
- deterministic project, object, geometry, filesystem, or external-system
  fixtures;
- cleanup and global-state restoration requirements;
- licensed exact-target success scenarios;
- unavailable infrastructure or data and the resulting at-risk qualifier; and
- non-Briosa dependencies with owning issues or external evidence references.

Each selected operation must still satisfy the complete vertical-slice definition
in the [operation and protocol model](../../../../docs/architecture/operation-and-protocol-model.md).

## Current implemented baseline

The following six operations are already complete handwritten vertical slices and
form the initial v1 baseline. Their implementation source and runtime registry,
not this table, remain the support authority.

| Operation ID | MP step | Delivery issue |
| --- | --- | --- |
| `file_operations.get_working_directory` | `Get Working Directory` | [#132](https://github.com/spatialanalyzer/briosa/issues/132) |
| `analysis_operations.get_i_th_collection_name` | `Get i-th Collection Name` | [#134](https://github.com/spatialanalyzer/briosa/issues/134) |
| `analysis_operations.get_number_of_collections` | `Get Number of Collections` | [#139](https://github.com/spatialanalyzer/briosa/issues/139) |
| `construction_operations.get_active_collection_name` | `Get Active Collection Name` | [#141](https://github.com/spatialanalyzer/briosa/issues/141) |
| `utility_operations.get_active_units` | `Get Active Units` | [#141](https://github.com/spatialanalyzer/briosa/issues/141) |
| `utility_operations.get_working_frame_properties` | `Get Working Frame Properties` | [#141](https://github.com/spatialanalyzer/briosa/issues/141) |

This baseline does not imply that v1 is complete at six operations. Additional
operations enter the evolving v1 set only through the selection procedure above.
No other inventory entry or historical candidate is selected by this document.

## V0.x scope changes

Before the freeze, maintainers may evolve the v1 set through ordinary review:

1. Create a proposed operation or coherent-batch issue with the required record.
2. Resolve evidence and architecture blockers without inventing missing target
   semantics.
3. Record the maintainer's selection decision in the issue.
4. For a selected issue, set `V1 disposition: Selected`, make it a direct native
   sub-issue of #42, assign Project fields, and place it in the intended v0.x
   milestone when known.
5. Deliver and review every operation as a complete vertical slice.
6. Keep server, protocol artifact, first-party clients, documentation, and
   validation claims synchronized through their owning issues.

Deferring, excluding, or declaring SDK unavailability requires a recorded reason
only for commands that were actually reviewed. Briosa does not create one issue
per retained inventory entry merely to prove that everything received a
disposition.

## V1 scope freeze

The validation and release epic [#47](https://github.com/spatialanalyzer/briosa/issues/47)
owns the freeze Task. The freeze begins only when maintainers intend to stop v0.x
surface growth and prepare the first v1 release candidate.

### Entry criteria

- #145's selection rules are accepted.
- Every selected operation is named in a native direct sub-issue of #42 with the
  required evidence and risk record.
- Every selected operation is implemented and registered as a complete vertical
  slice; no selected delivery issue remains open.
- Server, protocol, all three first-party clients, and public documentation agree
  on the selected surface.
- Required portable validation passes.
- Licensed validation status is explicit for every operation, including all
  accepted at-risk gaps.
- No unresolved release blocker can change a selected operation's public
  contract, execution safety, or support claim.

### Freeze record

The freeze Task records a reviewable snapshot containing:

- the accepted direct operation/batch issue URLs;
- each stable operation ID and exact MP step;
- server, protocol, client, and documentation release coordinates;
- portable and licensed validation evidence; and
- accepted at-risk qualifications.

This is release evidence in GitHub, not a checked-in command catalog, generated
manifest, source generator input, or build-time inventory-completeness gate. The
release review compares the snapshot with handwritten source, runtime discovery,
client surfaces, and documentation.

### Changes after freeze

Adding, removing, splitting, combining, or changing the public contract of a
selected operation requires a scope-exception issue linked to the freeze Task. It
must explain the reason, compatibility effect, client and documentation work,
validation impact, and replacement release-candidate plan.

A maintainer may reject the exception, approve it and repeat every affected
freeze check, or unfreeze the surface. Unfreezing is an explicit recorded
decision: v1 release-candidate claims are withdrawn, the project returns to v0.x
surface development, and a new freeze Task or clearly superseding freeze record
is required later.

## Prohibited mechanisms

This process must not introduce or restore:

- a generated or deterministic command catalog;
- a generated operation implementation or client facade;
- a machine-readable release-membership manifest;
- an inventory-wide disposition or implementation-completeness gate;
- a generic public command executor or untyped argument bag; or
- support claims derived from inventory counts, historical dispositions, or
  ObjectiveSA coverage.

Standard protobuf/gRPC generation remains part of the build. Small human-authored
tables inside a reviewed issue or release record are planning evidence, not
runtime or build inputs.
