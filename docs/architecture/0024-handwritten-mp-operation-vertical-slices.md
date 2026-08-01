# ADR 0024: Handwritten MP operation vertical slices

- Status: Accepted
- Date: 2026-07-31
- Issue: [#132](https://github.com/spatialanalyzer/briosa/issues/132)
- Discussion: [spatialanalyzer Discussion #4](https://github.com/orgs/spatialanalyzer/discussions/4)
- Supersedes: [ADR 0006](0006-versioned-command-catalog.md) and [ADR 0009](0009-catalog-derived-operation-artifacts.md)
- Preserves: [ADR 0001](0001-spatialanalyzer-com-boundary.md), [ADR 0002](0002-worker-process-lifecycle.md), [ADR 0004](0004-mp-execution-pipeline.md), [ADR 0008](0008-mp-outcomes-and-grpc-errors.md), [ADR 0015](0015-command-policy-and-audit-events.md), [ADR 0017](0017-execution-channel-readiness.md), [ADR 0018](0018-uncertain-completion-and-replay.md), [ADR 0019](0019-global-state-workflow-isolation.md), and [ADR 0022](0022-runtime-identity-and-attestation.md)

## Context

Briosa began by building a deterministic command catalog and a custom generator intended to produce protobuf contracts, host bindings, worker mappings, capability metadata, documentation, conformance fixtures, and completeness reports. Before the end-item operation code had been established through repeated implementation and real use, that approach accumulated large generated and evidence surfaces while only a small number of MP commands had public implementations.

The intended users are developers already familiar with programming SpatialAnalyzer MPs. They need a strongly typed gRPC operation per MP command and mechanically recognizable MP names. A generic public command executor would make the service harder to discover and use and is not an acceptable substitute.

The project also expects thin clients in several languages. Their idioms and framework integrations will evolve independently. A Briosa-specific deterministic generator would require the project to encode and continuously maintain those language decisions. Generative-AI tools can help draft implementations from maintainer-provided evidence, but reviewed source and tests must remain the authority.

## Decision

Briosa implements MP commands as ordinary handwritten vertical slices. Each supported operation contains:

1. a mechanically MP-compatible protobuf RPC and field names;
2. strongly typed request and response messages;
3. handwritten host, worker-command, result-mapping, and SDK-adapter code;
4. capability and runtime-policy registration;
5. portable success, validation, failure, cancellation, and lifecycle tests appropriate to the operation;
6. an explicit exact-target real-SA validation result or an honest unvalidated/at-risk statement; and
7. user-facing documentation.

The `.proto` contracts are handwritten. Normal protobuf and gRPC tools still generate transport plumbing and clients. Briosa does not maintain a custom operation generator, a generic public `ExecuteCommand` RPC, or an inventory-completeness gate.

The source tree defines support:

- a public RPC exists only when its handwritten protobuf and implementation are committed;
- `SpatialAnalyzerApi.Operations` lists only implemented public operations;
- capability discovery is the runtime-policy-filtered view of that list; and
- tests exercise the actual handwritten mapping rather than comparing it with a parallel catalog.

An issue or pull request may contain one operation or a coherent batch of related operations. The review boundary is completeness and clarity, not an arbitrary command count: every operation in a batch must preserve its own exact-target evidence, strongly typed contract, mapping, registration, focused tests, and validation status. Shared service, workflow, smoke, and documentation coverage may be organized at the batch level when that avoids ceremony without obscuring an operation's behavior. No fixed batch maximum is established.

Formerly catalog-promoted operations remain unsupported until ordinary implementation work reintroduces them as reviewed vertical slices.

## Evidence boundary

The exact-target inventory, SDK-binding snapshot, semantic value evidence, installed documentation observations, View SDK Code observations, and pinned ObjectiveSA review remain useful inputs. They are reference evidence only. They do not:

- define the public API;
- approve an operation for implementation;
- establish runtime support;
- require complete disposition;
- generate committed operation code; or
- make a build fail because an evidence snapshot is stale.

Exact-target evidence wins over prior-release ObjectiveSA evidence. Raw vendor text, proprietary binaries, and ObjectiveSA source are not copied into Briosa. Removed catalogs, dispositions, generated conformance data, and derived documentation remain available in Git history.

## Preserved runtime architecture

This decision changes how operations are authored, not the runtime boundary. The public host still owns no COM state. A supervised worker process owns one SDK connection and one STA, serializes the entire MP sequence, fails closed on identity mismatch, distinguishes caller cancellation from COM completion, replaces hung workers, preserves uncertain outcomes, enforces single-tenant assumptions, applies exact operation policy, and emits value-free audit events.

Shared handwritten infrastructure is encouraged where it represents proven runtime behavior. `OperationExecutor`, worker control messages, SDK value codecs, outcome mapping, policy, audit, and supervision remain reusable. An operation should not duplicate those cross-cutting semantics.

## Adding operations

A follow-up command or coherent command batch is added without extending a Briosa-specific generator:

1. open a focused implementation issue identifying every MP command and its exact-target evidence;
2. hand-author or AI-draft each protobuf and C# vertical slice using MP-compatible names;
3. register every operation in `SpatialAnalyzerApi.Operations` and the appropriate gRPC service;
4. add focused contract and SDK-order tests per operation, reusing shared lifecycle and workflow coverage where behavior is already proven;
5. run ordinary restore, build, test, protocol, package, and smoke validation; and
6. review the code and evidence like any other product change.

Overloads may be represented by separate strongly typed RPCs when that is clearer for MP developers. Public COM types and generic untyped value bags remain prohibited.

## Consequences

- Product code can evolve from tested examples instead of speculative generator abstractions.
- A pull request shows the complete behavior of every included command in ordinary source while allowing related commands to share review and workflow context.
- Protocol names remain familiar to MP programmers.
- Inventory breadth no longer creates a false impression of supported API breadth.
- Repetition is accepted until several implemented operations demonstrate a stable abstraction worth extracting.
- Multi-language clients may use their ecosystems' normal generators and handwritten idiomatic layers without requiring a central language-template engine.
- Evidence refreshes and implementation delivery are independent work.

The tradeoff is more reviewed source per operation and the possibility of minor mechanical inconsistency. Focused tests, code review, standard protobuf compatibility checks, and later evidence-based refactoring are the chosen controls.
