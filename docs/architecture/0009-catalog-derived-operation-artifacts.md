# ADR 0009: Catalog-derived operation artifacts and completeness

- Status: Accepted for the v0.1 vertical slice
- Date: 2026-07-22
- Issue: [#16](https://github.com/spatialanalyzer/briosa/issues/16)

## Context

Briosa's exact-target command catalog is the reviewed source of truth for supported MP operations, but a public operation spans several surfaces: protobuf, private worker commands, typed response mapping, reference documentation, and tests. Maintaining those surfaces independently would allow direction, field identity, SDK setter/getter names, or omission behavior to drift.

SpatialAnalyzer command shapes are exact-release contracts. The generator must not infer compatibility, invent defaults, or reduce an exact SDK binding such as `SetStringArg2` to a broad string value kind.

## Decision

One deterministic generator emits four checked-in artifacts for each exact SA target:

1. the target `operations.proto`;
2. generated server operation bindings;
3. Briosa-authored reference Markdown; and
4. a machine-readable coverage manifest.

The generated binding owns catalog-derived mechanics: operation and MP-step identity, request presence and omission handling, reviewed defaults, ordered input setters, requested output getters, output contracts, and typed successful-result construction. It attaches the shared execution details defined by ADR 0008. Generated files contain no worker supervision, gRPC error policy, logging, security, or authorization decisions.

Scalar request fields use protobuf presence. Structured values require every component in the exact-target value shape before conversion. An omitted optional field either omits its SDK setter or uses a catalog default only when that default is explicitly marked `reviewed`. Generated SDK sample values are never runtime defaults.

The private worker command carries the exact SDK binding name in addition to its value kind. The worker executes only a binding explicitly supported for that kind. A new binding variant therefore fails closed until its precise interop call is implemented and tested.

Hand-written gRPC methods are stable extension points. They create a generated command, submit it through the supervised worker, pass the outcome through the shared hand-written policy mapper, and give the successful execution to the generated typed result mapper.

## Completeness

Each hand-written implementation method has an operation marker, and at least one portable test has the corresponding test marker. A completeness test combines those reflected markers with every operation in the generated coverage manifests. It fails when an operation is generated but not implemented or tested, or when a marker names an operation outside the reviewed catalog.

The coverage manifest also records input and output mappings and whether protocol, command adapter, result adapter, and documentation were generated. It describes generation coverage, not support for the complete installed SpatialAnalyzer command inventory.

CI regenerates every target in an empty temporary directory and compares both the expected path set and file bytes with the repository. Missing, extra, and stale files fail verification across all four generated roots.

## Testing

Synthetic catalog tests exercise required inputs, optional omitted setters, reviewed defaults, input/output arguments, every currently modeled semantic type, typed output construction, and message-component validation. The committed vertical slice test verifies that `GetWorkingDirectory` uses the generated command, output contract, and response adapter while preserving the hand-written service method.

All generation, completeness, and fake-worker tests remain portable and require neither SpatialAnalyzer nor a license.

## Consequences

- Adding a reviewed operation without its hand-written service and portable test intentionally breaks completeness.
- Exact setter/getter names remain observable and cannot silently collapse to a broad SDK type.
- Generated mapping code is direct, strongly typed code with no runtime catalog parsing or reflection on the request path.
- Policy and exceptional behavior remain explicit review points outside replaceable generated files.
