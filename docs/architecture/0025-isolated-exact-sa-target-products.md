# ADR 0025: Isolated exact-SA-target products with a stable public namespace

- Status: Accepted
- Date: 2026-07-31
- Issue: [#136](https://github.com/spatialanalyzer/briosa/issues/136)
- Supersedes: [ADR 0005](0005-exact-sa-target-protocols.md) and the package/layout decision in [ADR 0021](0021-exact-target-protobuf-partitions-and-identifiers.md)
- Amends: [ADR 0011](0011-windows-package-identity.md) and [ADR 0020](0020-protocol-artifacts-and-client-conformance.md)
- Preserves: [ADR 0024](0024-handwritten-mp-operation-vertical-slices.md)

## Context

A machine may have several SpatialAnalyzer releases installed, but the SA SDK can drive only one eligible SpatialAnalyzer instance at a time. Briosa therefore does not need one running server to select among releases. The client launches the Briosa executable built for the exact SA instance it intends to use.

Briosa must support roughly two to three years of SA releases from one repository. Exact MP contracts and SDK behavior can differ even when command names and wire shapes appear equal. Sharing runtime projects across targets would let an apparently harmless change for one release affect every other product.

Encoding the SA release in protobuf packages and generated language namespaces would make the common single-SA application pay a permanent migration cost. Most applications should be able to move to a package for a newer SA release without rewriting source namespaces. The rare application loading more than one target client can use the target language's module, package, or assembly aliasing facilities.

## Decision

The repository contains one complete product subtree per supported exact SA release:

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

Each target owns its protobuf contract, host, worker, control protocol, tests, smoke tools, interop boundary, dependency pins, exact-target evidence, packaging, and operating documentation. A target project must not reference a project or include runtime source from another target. Repository-level governance, architecture records, common build policy, CI/release orchestration, and licensing may remain at the root; they are not target runtime logic.

One Briosa semantic version identifies a repository source release. CI and release workflows explicitly enumerate every supported target and produce independently verified, target-qualified server and protocol artifacts for each one. Adding a target means copying the closest reviewed product, reviewing it against the new exact release, and adding it to the orchestration matrices. It does not mean adding a runtime version branch to an existing server.

Every built server has one scalar exact SA target and owns one active SDK/SA connection. It has no runtime SA-version selector. Configured target, activated SDK identity, and connected SA identity remain separate claims. Both effective runtime identities must exactly equal the built target before the execution-channel probe or any ordinary MP command can run.

Every target uses:

- protobuf package `briosa`;
- generated C# namespace `Briosa`; and
- SA-release-neutral protobuf service, RPC, message, and field names.

The exact SA release appears in the target source path, server and protocol artifact names, manifests, discovery coordinates, client package names, and runtime compatibility gates. It does not appear in public protobuf identifiers. A stable identifier supports source migration; it does not claim that two target contracts or command semantics are interchangeable.

Client repositories publish one package per exact SA target, such as `Briosa.2026.1.0529.7`, with an independent semantic package version. The generated namespace remains `Briosa`. Simultaneous use of multiple target packages is an uncommon advanced case handled with language-specific aliasing or dependency isolation.

## Verification

- Every target solution restores, builds, and tests from its own target directory.
- CI runs the portable build, contract, fake-worker, smoke, and artifact suites once per enumerated target.
- Release assembly gathers target-qualified server and protocol artifacts from every matrix entry before publication.
- Structural checks reject cross-target project references and source includes.
- Package and protocol manifests expose one scalar exact target and the stable `briosa` protocol package.
- Licensed validation proves exact identity matching and mismatch rejection before ordinary MP execution.

## Consequences

Runtime and contract code will be duplicated between targets. A fix that applies to several SA releases must be reviewed and ported independently. CI and release work grow linearly with the supported-target count.

That duplication is intentional while Briosa is still discovering the correct product shape. It makes each executable auditable and limits regression scope. Shared build or generation machinery may be reconsidered only after repeated handwritten targets establish a stable need; it must not silently reintroduce shared runtime logic or SA-versioned public namespaces.
