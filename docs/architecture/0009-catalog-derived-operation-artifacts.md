# ADR 0009: Catalog-derived operation artifacts and completeness

- Status: Accepted for the v0.2 command surface
- Date: 2026-07-22
- Issue: [#16](https://github.com/spatialanalyzer/briosa/issues/16)
- Amended by: [ADR 0016](0016-command-argument-semantic-families.md) and [ADR 0021](0021-exact-target-protobuf-partitions-and-identifiers.md)

## Context

Briosa's exact-target command catalog is the reviewed source of truth for supported MP operations, but a public operation spans several surfaces: protobuf, private worker commands, typed response mapping, reference documentation, and tests. Maintaining those surfaces independently would allow direction, field identity, SDK setter/getter names, or omission behavior to drift.

SpatialAnalyzer command shapes are exact-release contracts. The generator must not infer compatibility, invent defaults, or reduce an exact SDK binding such as `SetStringArg2` to a broad string value kind.

## Decision

One deterministic generator emits the complete repetitive operation surface for each exact SA target:

1. one target protobuf service/file per reviewed category partition;
2. generated request validation/mapping, immutable worker-command construction, output contracts, and typed result mapping;
3. generated gRPC service methods and one aggregate endpoint-registration extension;
4. generated capability descriptors consumed by discovery and runtime policy;
5. Briosa-authored reference Markdown; and
6. a machine-readable coverage manifest; and
7. an executable portable-conformance binding registry plus an evidence-derived scenario manifest.

The generated binding owns catalog-derived mechanics: operation and MP-step identity, request presence and omission handling, reviewed defaults, ordered input setters, requested output getters, output contracts, and typed successful-result construction. It attaches the shared execution details defined by ADR 0008. Generated files contain no worker supervision, gRPC error policy, logging, security, or authorization decisions.

Scalar request fields use protobuf presence. Structured values require every component in the exact-target value shape before conversion. An omitted optional field either omits its SDK setter or uses a catalog default only when that default is explicitly marked `reviewed`. Generated SDK sample values are never runtime defaults.

The private worker command carries the exact SDK binding name in addition to its value kind. The worker executes only a binding explicitly supported for that kind. A new binding variant therefore fails closed until its precise interop call is implemented and tested.

`CatalogOperationExecutor` is the single hand-written transport seam. Generated service methods pass typed requests, catalog descriptors, and generated mapping delegates to it. The seam owns correlation, audit events, supervised dispatch, cancellation/deadline interpretation, typed gRPC failures, and result-mapping failure containment. Worker supervision, security policy, error mapping, and audit policy therefore remain reviewed code without a per-operation transport implementation.

`WorkerMpCommand` snapshots caller-owned input and output collections into immutable storage. A generated command cannot change after enqueue because a request mapper or caller later mutates its construction lists.

## Completeness

Each generated implementation method has an operation marker. The completeness test compares the exact operation set across catalog files, coverage manifests, protobuf descriptors, generated implementations, capability descriptors, and generated reference documentation. It also compares every coverage input/output semantic family with the reviewed catalog assignment.

The coverage manifest explicitly records protocol, request validation, request adapter, immutable command, result adapter, gRPC service, registration, capability, documentation, portable conformance, exact argument-family assignment coverage, and source release memberships. Each release member must exist in generated coverage and carry the same membership tag on its operation entry. Membership remains an additive delivery subset; it does not describe the complete installed SpatialAnalyzer command inventory or enable runtime policy.

The exact-target portable-conformance manifest fingerprints the catalog, every supported operation document, binding registry and review, and value-family evidence. It expands stable positive and negative identities for every supported operation, usable SDK method/family row, implemented value family, enum member, structured field shape, and exact multi-family command assignment. Generation fails on missing evidence, duplicate identities, or an unreviewed shared-method assignment. The committed manifest is a test inventory, not a second source of API truth; catalog and reviewed binding evidence remain authoritative.

CI regenerates every target in an empty temporary directory and compares both the expected path set and file bytes with the repository. Missing, extra, and stale files fail verification across every generated root, including obsolete category protocol files and registrations after a partition changes.

## Testing

Synthetic catalog tests exercise required inputs, optional omitted setters, reviewed defaults, input/output arguments, every currently modeled semantic type, typed output construction, and message-component validation. Portable runtime tests discover every generated operation binding and distinguish default-like present values, absent values, disconnected and unverified workers, setter/execute/MP/getter failures, policy denial, deadlines and cancellation before and after start, crash, hang, malformed responses, replay guidance, and fail-closed unknown returned type literals. Evidence-parity tests require every global manifest row to match the registry and value catalog exactly; the existing worker completeness suite executes those binding, value-shape, enum-literal, and command-assignment contracts against the production adapter seam. See the [portable conformance guide](../testing/portable-conformance.md).

All generation, completeness, and fake-worker tests remain portable and require neither SpatialAnalyzer nor a license.

## Consequences

- Adding a reviewed operation produces its transport adapter and registration without a hand-written per-operation service.
- Exact setter/getter names remain observable and cannot silently collapse to a broad SDK type.
- Generated mapping code is direct, strongly typed code with no runtime catalog parsing or reflection on the request path.
- Policy and exceptional behavior remain explicit review points outside replaceable generated files.
