# Operation and protocol model

- Status: Current
- Last reviewed: 2026-08-01

## Public API shape

Briosa exposes one mechanically recognizable, strongly typed gRPC operation per
supported SpatialAnalyzer MP command. Developers familiar with MPs should not need
to learn a second Briosa-specific command vocabulary. A generic public
`ExecuteCommand` RPC or untyped value bag is not an acceptable substitute.

Every exact SA target uses:

- protobuf package `briosa`;
- generated C# namespace `Briosa`; and
- SA-release-neutral service, RPC, message, and field names.

Exact SA releases identify products, artifacts, package names, discovery
coordinates, and runtime compatibility gates. They do not enter public protobuf
identifiers. This favors ordinary single-target application migration while rare
multi-target applications use language-specific package or assembly isolation.

## Handwritten vertical slices

Each supported MP operation is ordinary reviewed source containing:

1. a handwritten protobuf RPC with strongly typed request and result messages;
2. stable operation, MP-step, service, RPC, route, and field identities;
3. handwritten request validation, worker-command construction, result mapping,
   and exact SDK setter/getter sequence;
4. capability and runtime-policy registration;
5. focused portable tests for the operation's exact contract and bindings;
6. an honest exact-target validation status; and
7. user-facing documentation where it adds useful behavior, risk, evidence, or
   workflow context.

An issue or pull request may deliver one operation or a coherent batch. Every
operation remains individually traceable and complete, while related operations
may share service, workflow, smoke, and documentation coverage. There is no fixed
command-count limit.

Standard protobuf and gRPC tools generate transport plumbing and language clients.
Briosa does not maintain a custom operation generator, deterministic command
catalog, generated completeness manifest, inventory-driven implementation gate,
or client-language template engine. AI tools may draft source from maintainer
provided evidence, but committed code, tests, observations, and engineering review
are authoritative.

## Supported-surface authority

The source tree defines support:

- the handwritten `.proto` contract defines the compiled public RPC;
- the handwritten service and operation mapping define execution behavior;
- `SpatialAnalyzerApi.Operations` registers implemented capabilities; and
- `DiscoveryService/ListCapabilities` reports the runtime-policy-admitted subset.

Inventory membership, retained bindings, historical generated output, ObjectiveSA
coverage, and code found only in Git history do not make an operation supported.

## Exact-target evidence

Implementation review uses the smallest curated facts needed from:

- exact-target installed MP documentation observations;
- exact-target View SDK Code observations;
- approved interop metadata;
- retained exact-target inventory, binding, and semantic-value evidence;
- licensed live observations; and
- pinned ObjectiveSA prior-release wrappers as secondary evidence.

Exact-target evidence wins on conflict. ObjectiveSA cannot add an argument,
default, choice, compatibility claim, or SDK binding absent from the target. Raw
vendor documentation, proprietary binaries, and proprietary wrapper source are not
copied into Briosa.

Missing fixtures, software, hardware, or licenses create an at-risk validation gap;
they do not by themselves justify excluding an otherwise matched command. Gaps are
recorded explicitly and receive opt-in licensed scenarios when practical.

## Semantic value families

SDK method names do not uniquely establish MP argument semantics. An exact command
argument selects its semantic family from reviewed target evidence, and one SDK
method may serve several families.

For example, SA 2026.1.0529.7 collection-object-named SDK calls can carry either
the 26-choice object domain or the broader 42-choice item domain. Handwritten
operation code selects the correct family, uses a typed public and worker value,
and fails closed on unknown or out-of-family returned literals. A generic string,
integer, or object fallback is prohibited unless an exact operation documents a
narrow target-specific omission rule.

Detailed member sets and SDK observations belong to the exact target's
[binding reference](../../targets/2026.1.0529.7/docs/reference/sa/2026.1.0529.7/binding-family-completeness.md).

## Stable identifiers and compatibility

Public contracts are partitioned by stable MP/Briosa category. Adding a category
normally adds a service/file; existing published services are not moved merely to
rebalance file sizes.

Every operation explicitly chooses:

- stable operation ID, normally `<category_alias>.<operation_alias>`;
- exact MP step;
- service, RPC, request, and result names;
- fully qualified gRPC route;
- MP argument names and directions;
- exact SDK setter/getter order and method; and
- stable protobuf field numbers.

MP documentation ordinal, SDK order, and protobuf field number are separate
claims. Result field `1000` is reserved for `MpExecutionDetails execution`.
Removed published fields are reserved rather than reused after the first public
release. Buf formats, lints, and compiles each target contract; breaking checks use
an explicit released reference once one exists.

## Discovery and health

The standard `grpc.health.v1.Health` service exposes separate `briosa.liveness`
and `briosa.readiness` checks. `DiscoveryService/GetServerInfo` exposes safe build,
target, identity, and readiness coordinates. `ListCapabilities` returns only
handwritten operations allowed by runtime policy.

Discovery never returns an installed-SA command inventory, target hostname,
process identifiers, license information, evidence references, raw diagnostics,
arguments, or operation values.
