# Exact-target product model

- Status: Current
- Last reviewed: 2026-08-01

## One complete product per SA release

A machine may have several SpatialAnalyzer releases installed, but one running
Briosa server controls one eligible SDK/SpatialAnalyzer instance and is locked to
one exact SA release. A client selects and launches the server distribution built
for the SA instance it intends to use. Briosa has no runtime SA-version selector,
nearest-version fallback, or compatibility normalization.

The repository supports several exact releases through complete isolated products:

```text
targets/<exact-sa-release>/
  Briosa.slnx
  Directory.Packages.props
  buf.yaml
  proto/
  src/
  tests/
  tools/
  eng/
  interop/
  inventory/
  bindings/
  values/
  docs/
```

Each target owns its public contract, server, worker, private control protocol,
tests, smoke tools, interop boundary, dependency pins, evidence, packaging, and
operating documentation. Target projects must not reference projects or include
runtime source from another target.

Duplication is intentional while Briosa is discovering the product shape. A fix
that applies to several targets is ported and reviewed independently. Shared
repository governance, architecture, CI orchestration, and licensing may remain at
the root, but they are not shared target runtime logic.

Adding a target means copying the closest reviewed product, validating every
retained contract and SDK assumption against the new exact release, and adding the
new product to explicit CI, release, and protected-validation matrices. It does not
mean adding runtime version branches to an existing server.

## Version coordinates

These coordinates are independent:

- Briosa repository/server semantic version;
- exact SpatialAnalyzer target release;
- Windows runtime architecture;
- protocol artifact snapshot; and
- language-specific client package version.

One Briosa semantic version identifies a repository source release. That release
may produce one independently verified server and protocol artifact per supported
SA target. Artifact names and manifests include the exact target and runtime.

Client repositories publish one package per exact SA target, such as
`Briosa.2026.1.0529.7`, with an independent semantic package version. Generated
language namespaces remain release-neutral. Matching public names ease source
migration; they do not claim that contracts from different targets are wire- or
behavior-compatible.

## Windows server package

Each target produces a self-contained, non-trimmed, non-single-file Windows x64
archive named like:

```text
briosa-<briosa-semver>-sa-<exact-target>-win-x64.zip
```

The archive contains separate `Briosa.Server.exe` and `Briosa.Worker.exe`
processes, the approved managed interop assembly, safe operator documentation,
manifest and provenance data, internal file hashes, and the Apache-2.0 license. It
does not contain SpatialAnalyzer, the SDK executable, the original type-library
container, license material, or copied vendor documentation.

Package publishing merges independently produced outputs only when duplicate paths
have identical content. ZIP entries are sorted with deterministic timestamps, and
validation builds the archive twice and requires identical hashes.

## Protocol artifact

Each target may publish a runtime-neutral protocol archive beside its Windows
server package. It contains:

- canonical target `buf.yaml` and handwritten public `.proto` sources;
- a pure `google.protobuf.FileDescriptorSet`;
- a manifest with source revision, exact target, package identities, content
  fingerprints, and included-file hashes;
- internal and external checksums; and
- licensing and client-consumption guidance.

It contains no command catalog, release membership, generated operation source,
generated conformance manifest, or client-language template. Client repositories
use their ecosystems' standard protobuf/gRPC tools and maintain their idiomatic
layers as reviewed source.

## Interop boundary

Each target builds against managed COM metadata produced from a properly installed
and licensed exact-target SDK type library. The repository commits only the
approved managed interop assembly plus provenance and canonical public-API
manifests. Ordinary builds therefore do not consult the local COM registry or
require SpatialAnalyzer.

Interop generation records the target release, source type-library identity and
hash, importer identity and fixed arguments, assembly identity, and canonical API
fingerprint. Machine paths, usernames, license data, and timestamps are excluded.
Generated interop output is never hand-edited.

Raw assembly bytes may contain nondeterministic PE metadata. Briosa validates
canonical managed API equivalence and preserves an already equivalent committed
assembly rather than replacing it with a byte-different import.

The current exact target is documented at
[`targets/2026.1.0529.7`](../../targets/2026.1.0529.7/README.md).
