# ADR 0020: Protocol artifacts and cross-client generation

- Status: Accepted; conformance-manifest strategy superseded by [ADR 0024](0024-handwritten-mp-operation-vertical-slices.md)
- Date: 2026-07-28
- Revised: 2026-07-31
- Issue: [#94](https://github.com/spatialanalyzer/briosa/issues/94)
- Amends: [ADR 0005](0005-exact-sa-target-protocols.md) and [ADR 0012](0012-generated-client-verification.md)

## Context

`spatialanalyzer/briosa` owns the public protobuf contracts and shared transport semantics. The .NET, JavaScript/TypeScript, Python, and other client repositories need a reproducible input without copying moving `.proto` files or depending on a Briosa-specific language generator.

A Briosa server version, exact SpatialAnalyzer target, target protocol package, and client package version are independent coordinates. Source-only consumption is also insufficient when a client needs a verifiable descriptor input and generation provenance.

## Decision

Every Briosa release may publish one runtime-neutral protocol artifact beside the Windows server artifact. It contains:

- canonical `buf.yaml` and handwritten public `.proto` sources;
- a pure `google.protobuf.FileDescriptorSet` built from those sources;
- a manifest with Briosa version, source revision, exact SA target, protocol packages, content fingerprints, and every included file hash;
- internal and external SHA-256 checksums; and
- Apache-2.0 licensing and a client-consumption guide.

The artifact is named `briosa-protocol-<briosa-version>-sa-<exact-target>.zip`. It contains no command catalog, release membership, generated operation source, generated conformance manifest, or client-language template.

Clients use standard protobuf/gRPC tools against the sources or descriptor set. A client repository may add idiomatic adapters, packaging, cancellation/deadline integration, and language-specific presence handling as reviewed source. It must not infer cross-SA compatibility from a matching wire shape.

Client package versions remain independent. A new client release can consume an unchanged Briosa protocol artifact, and a new Briosa release can reuse an unchanged protocol snapshot.

## Verification

CI builds the protocol artifact twice and requires identical ZIP hashes. It rebuilds the descriptor set from the bundled source, validates internal and external checksums, and rejects source/manifest drift.

Portable server behavior is tested in the repository's ordinary test projects and focused packaged-client scenarios. Client repositories test their language-specific error and convenience layers directly. A central generated conformance manifest is not required.

## Consequences

- All languages can consume one auditable protocol input with normal ecosystem tools.
- Descriptor-driven and source-driven generators receive equivalent content.
- Client repositories can evolve idiomatically without a central template engine.
- Operation behavior remains authoritative in handwritten source and focused tests.
- Publishing to a schema registry or adding platforms can be layered on later without changing the operation-authoring strategy.
