# Briosa architecture

These documents describe Briosa's current architecture. Briosa is unreleased, so
the active documentation deliberately presents the design as it exists now rather
than preserving every experimental approach in the working tree.

## Authority

When sources disagree, use this order:

1. repository invariants in [`AGENTS.md`](../../AGENTS.md);
2. the current architecture documents in this directory;
3. exact-target implementation, tests, and target-local documentation;
4. accepted GitHub issues and organization Discussions; and
5. Git history and closed experimental issues as historical evidence only.

Historical numbered ADRs were removed from the active tree after their current
decisions were consolidated here. They remain available in Git history, but they
are not current implementation guidance. In particular, the retired command
catalog, Briosa-specific operation generator, generated completeness system,
SA-versioned protobuf packages, and shared cross-target runtime design must not be
reintroduced merely because an old commit describes them.

## Current documents

- [Runtime boundary and lifecycle](runtime-boundary-and-lifecycle.md) explains the
  public host, supervised SDK worker, COM/STA ownership, connection identity, and
  readiness gates.
- [Execution outcomes and recovery](execution-outcomes-and-recovery.md) explains
  serialization, cancellation, watchdog recovery, MP results, replay safety, and
  workflow isolation.
- [Operation and protocol model](operation-and-protocol-model.md) explains the
  strongly typed handwritten MP surface, evidence boundary, stable identifiers,
  and standard protobuf generation.
- [First-party client behavioral contract](client-library-behavioral-contract.md)
  explains the language-neutral behavior and safety guarantees shared by the
  .NET, Python, and JavaScript/TypeScript clients while leaving idiomatic API
  expression to their repositories.
- [Exact-target product model](exact-target-product-model.md) explains isolated
  SA-release products, stable public namespaces, packaging, interop, and client
  version coordinates.
- [Security and observability](security-and-observability.md) explains the
  loopback deployment boundary, runtime command policy, value-free diagnostics,
  and protected licensed-runner trust model.
- [Validation and distribution](validation-and-distribution.md) explains portable,
  packaged, licensed, protocol, and release validation.

## Scope boundary

Repository-wide architecture belongs here. Exact-release observations, SDK
bindings, operation behavior, developer commands, and operator procedures belong
under `targets/<exact-sa-release>/docs`. The current target documentation starts at
[`targets/2026.1.0529.7/README.md`](../../targets/2026.1.0529.7/README.md).

The implementation is still evolving. A document may identify a decision as
**Provisional** when the repository has implemented a safe reversible minimum but
still lacks vendor evidence, protected infrastructure, or enough real operations
to establish a long-term abstraction. Unresolved decisions remain listed in
`AGENTS.md`; a current document must not silently convert one into policy.
