# ADR 0020: Protocol artifacts and cross-client conformance

- Status: Accepted for the v0.2 command surface
- Date: 2026-07-28
- Issue: [#94](https://github.com/spatialanalyzer/briosa/issues/94)
- Amends: [ADR 0005](0005-exact-sa-target-protocols.md), [ADR 0010](0010-health-version-and-capability-discovery.md), [ADR 0012](0012-generated-client-verification.md)

## Context

`spatialanalyzer/briosa` owns the public protobuf contracts, exact-target catalog, and shared operation semantics. The .NET, JavaScript/TypeScript, and Python client repositories must consume the same reviewed snapshot without copying policy or hand-maintaining parallel command surfaces like the legacy ObjectiveSA wrapper.

A Briosa server version, catalog revision, exact SpatialAnalyzer target, and client package version are independent coordinates. Treating any one of them as a substitute for the others would imply compatibility that has not been established. Source-only protobuf consumption is also insufficient when a client needs a reproducible descriptor input, a verifiable generation identity, and shared failure/replay expectations.

## Decision

Every Briosa release publishes one runtime-neutral protocol artifact beside the Windows server artifact. It contains:

- the canonical `buf.yaml` and public `.proto` source tree;
- a pure `google.protobuf.FileDescriptorSet` built from that tree;
- the generated exact-target catalog coverage manifest;
- versioned, value-safe live-scenario and typed-error conformance fixtures;
- an artifact manifest with Briosa version, source revision, exact SA target, protocol packages, catalog identity, content fingerprints, and every included file hash; and
- Apache-2.0 licensing and a client-consumption guide.

The artifact is named `briosa-protocol-<briosa-version>-sa-<exact-target>-catalog-<revision>.zip`. It is byte-reproducible for the same source, version, and toolchain and has an adjacent SHA-256 file and provenance manifest.

Clients generate repetitive transport code from the artifact's sources or descriptor set. A client repository may add idiomatic adapters, packaging, cancellation/deadline integration, and language-specific presence handling, but it does not copy operation policy, catalog facts, error mapping, or compatibility rules. It must record at least the protocol ZIP SHA-256, protocol-schema fingerprint, core and target package names, exact SA target, catalog ID/revision, and fixture-set IDs used by that release.

Client package versions remain independent. A new client release can consume an unchanged Briosa protocol artifact, and a new Briosa release can reuse an unchanged protocol/catalog snapshot. Neither case claims compatibility with another SpatialAnalyzer release. At runtime, a client compares exact discovery coordinates and capability identity rather than inferring support from semantic-version ranges.

## Conformance contract

The live fixture set drives the packaged fake-worker scenarios. It covers identity and capability discovery, readiness, success, policy denial, unavailable SA, MP failure, output-retrieval failure, deadline, cancellation, watchdog replacement, and an unsupported exact-target method.

The typed-error fixture set is transport-language-neutral and includes both executable and synthetic safety cases. It distinguishes execution disposition, recovery guidance, replay guidance, and replay safety, including unsafe and unknown ambiguous-completion cases that must require reconciliation. A client must decode `briosa-operation-error-bin` as `OperationError`; it must not parse status text or automatically replay merely because the worker becomes ready.

Fixtures contain enum names, stable operation IDs, status codes, field presence, and value-free diagnostics only. They contain no paths, geometry, object identifiers, credentials, license data, device data, raw arguments, or returned values.

Each client repository runs a minimal generated Get Working Directory client against the same live fixture IDs and validates its error adapter against the typed-error cases. The client-specific bootstrap issues own packaging and idiomatic API choices; Briosa remains the fixture and semantic source of truth.

## Drift and publication

CI builds the protocol artifact twice and requires identical ZIP hashes. It rebuilds the descriptor set from the bundled source, validates internal and external checksums, verifies fixture identity and required cases, and rejects source/manifest drift. The release workflow publishes protocol and Windows artifacts together from one tag and source revision.

A client drift check fails when its recorded artifact checksum or coordinates differ from the downloaded manifest. Updating generated files requires an explicit artifact update; a client cannot silently regenerate from a moving branch or independently edited `.proto` copy.

## Consequences

- All supported languages consume one reviewed, auditable protocol input.
- Descriptor-driven and source-driven generators receive equivalent content.
- Shared behavior is test data rather than duplicated prose or language-specific policy.
- Client repositories can release independently while retaining exact generation provenance.
- Publishing to a schema registry or adding other platforms can be layered on later without changing the artifact identity contract.
