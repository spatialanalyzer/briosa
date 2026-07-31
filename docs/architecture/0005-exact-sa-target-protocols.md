# ADR 0005: Core and exact-SA-targeted public protocols

- Status: Accepted for the v0.1 vertical slice
- Date: 2026-07-21
- Amended by: [ADR 0021](0021-exact-target-protobuf-partitions-and-identifiers.md) and [ADR 0024](0024-handwritten-mp-operation-vertical-slices.md)

## Context

SpatialAnalyzer MP commands are not a stable cross-release API. Between exact SA releases, a command may replace several scalar arguments with a structured argument, reshape its outputs, or retain the same argument count and SDK types while changing what those arguments mean. Structural comparison therefore cannot establish semantic compatibility. A request can be wire-compatible and still be operationally wrong.

Briosa needs to support several exact legacy SA releases without creating independent source forks, silently choosing a nearby catalog, or requiring reviewers to reconstruct inherited catalog overlays. The public gRPC identity must make an SA-target mismatch fail closed.

## Decision

Briosa has two public protocol layers.

- The stable core package starts at `briosa.core.v1alpha1`. It contains only release-independent infrastructure concepts such as reproducible version coordinates. Health, version, and capability services added in later issues will use this layer.
- Every MP operation belongs to an exact-SA-targeted package. The initial package is `briosa.sa.v2026_1_0529_7.v1alpha1`.
- The exact target is encoded in the gRPC package and service identity. A client for another target calls a different service path and cannot silently reach a structurally similar operation.
- Target-specific request, result, and SA value messages are authoritative only for their package. Identically shaped messages in two target packages do not assert equivalent semantics.

The source layout mirrors the package identity:

```text
proto/
  briosa/core/v1alpha1/
  briosa/sa/v2026_1_0529_7/v1alpha1/
    file_operations.proto
```

Generated C# namespaces use `Briosa.Core.V1Alpha1` and `Briosa.Sa.V2026_1_0529_7.V1Alpha1`. Version components retain every component and leading zero from the exact SA version, replacing punctuation with underscores only where required by protobuf and language identifiers.

## Independent version coordinates

A distribution records these independent identities:

| Coordinate | Purpose |
| --- | --- |
| Briosa semantic version | Versions the server implementation and behavior. |
| Core protocol package | Versions the release-independent gRPC contract. |
| Exact SA target | Selects the one vendor release supported by the build. |
| Target protocol package | Versions Briosa's public contract for that exact target. |
| Interop fingerprint | Identifies the approved generated interop metadata. |
| Source revision | Identifies the Briosa source used by the build. |

A published asset includes the Briosa version and exact target, for example `briosa-0.3.2-sa-2026.1.0529.7-win-x64.zip`. SA-targeted distributions do not acquire separate Briosa semantic-version histories.

One published server and worker distribution contains exactly one target protocol, its handwritten operation implementations, and one interop assembly. Target selection is a build-time composition decision. Startup discovery reports configured and effective runtime identities separately; a mismatch cannot become ready for MP execution.

## Version-faithful contracts

Each target protocol is defined independently. Target definitions do not inherit from, overlay, or apply deltas to another SA release. Tooling may compare protocols for reviewers, but comparison does not establish semantics.

Matching command names, argument names, ordinals, directions, SDK types, or wire shapes never proves semantic compatibility. A comparison may report that structures match, but it must not label the releases semantically compatible without a separate explicit decision.

For example, one target may expose `GetPointProperties` with three string fields while SA 2026.1.0529.7 exposes one `PointName`. Both RPCs may keep the natural operation name because their fully qualified service paths and request/result types belong to different target packages. Likewise, four doubles whose meanings changed between releases receive independently named target fields with no inferred relationship.

Cross-release normalization is outside the baseline architecture. If it is ever justified, it must be an optional, separately versioned facade above the version-faithful target services. Handwritten target operations remain unaware of that facade.

Deliberate duplication of SA messages across target packages is a safety property. Shared abstractions are limited to genuinely release-independent runtime mechanics and core protocol concepts. Runtime code contains no version thresholds, nearest-version fallback, or cross-release conversion.

## Initial value types and presence

The initial target package contains only the request and result messages required by `GetWorkingDirectory`. New value shapes are added only with the handwritten operation that uses them and exact-target evidence establishing their semantics. A COM `SAFEARRAY`, SDK setter/getter name, or private worker value union is never a public type.

Proto3 scalar fields use explicit presence when absence must remain distinguishable from a default value. Message presence represents semantic presence only. SDK output retrieval failure is a separate execution outcome defined by issue #13. A disabled tolerance remains a present `enabled + value` pair and is not encoded as an absent value.

## Compatibility and validation

Buf always validates formatting, lint rules, and schema compilation. Briosa is
currently unreleased, so the evolving `main` branch is not a published API
baseline and ordinary pull requests may make reviewed breaking schema
corrections.

Beginning with the first public release, compatibility validation must compare
against an explicit released ref and apply Buf's strict `FILE` breaking policy.
The release baseline must never be inferred from `main`.

`eng/Verify-Protocol.ps1 -AgainstRef <released-ref>` performs the explicit Buf
comparison. The released ref must be selected and reviewed by the release
workflow; ordinary builds do not infer it from an inventory or catalog.

- Core files are compared with the prior core baseline.
- A target file is compared only with the prior file in the same exact target and package line.
- Reviewed categories own stable service/file partitions inside that package; adding a category adds a file rather than moving existing symbols.
- Adding a different exact-target package creates an independent API rather than replacing an existing target.
- After a package line is publicly released, its field numbers, names, and meanings are immutable. Removed field numbers and names must be reserved.
- A semantic correction that changes a published meaning requires a new target package version even if the wire type is unchanged.
- Core schemas may not import target schemas. One target may not import another target. Target schemas may import the core package or files from their own exact target.
- Ordinary validation requires neither SpatialAnalyzer nor proprietary binaries.

The repository builds generated C# directly from the reviewed `.proto` sources. Generated sources remain build outputs and are not hand-edited or committed.

## Review requirements for later targets

A later target contribution must include its handwritten target package, operation evidence, and a reviewed comparison highlighting structural additions, removals, reordering, renaming, and type changes relative to a reviewer-selected release. Same-shape commands remain explicitly unverified until their meanings are reviewed.

## Performance consequences

Target choice and protobuf dispatch are resolved at build time. Handwritten handlers map strongly typed requests to immutable execution descriptions. Request processing performs no catalog lookup, reflection-based schema interpretation, release search, semantic normalization, or runtime version branching. The serialized SDK call remains the dominant operation.

## Consequences

- Clients intentionally select an exact-SA-target API while sharing the stable core discovery contract.
- Supporting legacy SA releases adds build-matrix entries and independently reviewed target packages, not source forks.
- Some target packages will contain intentionally duplicated message shapes.
- Schema tools detect structural breakage but cannot replace human semantic review.
- Long-term SA support duration and compatibility-family policy remain unresolved.
