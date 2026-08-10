# First-party client behavioral contract

- Status: Current v1 design contract; client implementations may still be pre-conformance
- Last reviewed: 2026-08-10

## Purpose and authority

This document defines the language-neutral behavior shared by the first-party
Briosa clients for .NET, Python, and JavaScript/TypeScript. It is the central
contract for behavior that must remain equivalent across those libraries without
forcing them to expose identical syntax or type systems.

The public protobuf contract, handwritten server implementation, runtime
capability registration, and exact target remain authoritative for MP command
semantics. The target-qualified protocol artifact is the transport input consumed
by each client repository. This document defines how first-party idiomatic clients
present and preserve those semantics; it does not add commands, defaults, fields,
capabilities, or compatibility claims.

Each client repository owns its language-specific public API contract. The
reviewed language decisions are currently recorded in the
[.NET contract pull request](https://github.com/spatialanalyzer/briosa-dotnet/pull/4),
[Python review](https://github.com/orgs/spatialanalyzer/discussions/6#discussioncomment-17926452),
and
[JavaScript/TypeScript
review](https://github.com/orgs/spatialanalyzer/discussions/6#discussioncomment-17926469).
Their client-repository contracts must link back here when published.

Those contracts may choose idiomatic names, types, and resource-management
features, but they must not weaken or contradict the shared behavior below.
[Discussion #6](https://github.com/orgs/spatialanalyzer/discussions/6) records the
cross-language review that established this boundary.

## Contract layers

Briosa client design has three distinct layers:

1. The server and protocol own MP operations, inputs, outputs, presence, fixed
   defaults, typed failures, compatibility coordinates, capabilities, execution
   disposition, and replay safety.
2. This document owns the common behavior and safety guarantees of first-party
   idiomatic clients.
3. Each client repository owns the language expression of those guarantees.

When a language client finds an ambiguous shared semantic, it must resolve that
ambiguity in `spatialanalyzer/briosa` instead of inventing a local rule. A
language-specific convenience cannot become shared policy merely because one
client implemented it first.

## Command identity and discovery

Every supported MP command has one canonical operation in each first-party
client. A developer who knows the exact MP command name must be able to find that
operation without knowing its MP Editor category.

Client operation names are mechanically derived from the exact MP command text:

- preserve every word in its original order;
- preserve MP abbreviations rather than expanding or replacing them;
- remove punctuation and apply the target language's documented identifier
  casing deterministically;
- do not introduce discretionary synonyms or grammatical cleanup; and
- review and test genuine reserved-word or normalized-name collisions.

The exact MP name remains in public documentation. Source and documentation may
use MP categories for organization, but categories are not required in the
ordinary call path. Async suffix conventions belong to the language contract.

## Idiomatic facade and transport boundary

Each first-party package exposes a handwritten idiomatic facade. Generated
protobuf messages, enums, service clients, call objects, transport metadata, and
raw channel types are private implementation details. They are not re-exported
and do not appear in supported public parameters, results, errors, discovery
models, or documentation.

An idiomatic command accepts MP inputs through handwritten language values rather
than a generated request envelope. The language contract decides whether those
inputs are direct parameters or one handwritten command-input object. Transport
controls remain separate from MP inputs.

V1 exposes one canonical public operation per MP command. It does not maintain
parallel singleton and collection forms, sync and async forms, callback forms,
aliases, generated and handwritten forms, or another convenience-overload matrix.
Consumers who want raw gRPC generate their own bindings from the published exact
target protocol artifact.

## MP-native values, presence, and results

Use a built-in language value only when it represents the MP value exactly and
losslessly. Otherwise, expose a handwritten public domain value or enum that
preserves the SA-native concept. Generated wire sentinels remain private unless a
sentinel has reviewed domain meaning.

Public output cardinality follows the top-level MP outputs:

- no output completes asynchronously without a value;
- one output returns that value directly; and
- multiple outputs return one named handwritten result value.

Ordinary success results do not use a generic command envelope and do not mix in
gRPC status, trailers, execution disposition, or other transport metadata.
Multiple-output properties preserve MP output identity and order.

Public optionality follows the semantic success contract rather than protobuf
defaults, language truthiness, or zero values. A required-on-success value remains
required. A missing required wire value is a protocol-contract failure, while a
present empty string, zero, false, empty collection, or other default-like value
is preserved when valid.

Collection inputs are finite, deterministic language iterables or sequences. A
client consumes each input no more than once and fully materializes and validates
it before starting the RPC. Collection outputs are fresh detached language
collections that preserve order and duplicates. Present-but-empty and absent are
never collapsed into one state.

The language contracts own concrete domain-model, enum, named-result, optional,
integer, iterable, and collection types.

## Reviewed fixed defaults

When the shared command contract declares a reviewed fixed MP default, every
first-party client makes that default visible through its ordinary language API,
computes the effective value, and sends it explicitly.

A client must not use null, omission, a language default value, a hidden
preliminary MP call, or current SpatialAnalyzer state as an undocumented
substitute for a real fixed default. Fixed structured defaults use named immutable
domain values. Context-dependent behavior is not presented as a fixed default.

The command contract owns the canonical value. Each language owns its idiomatic
signature or input-property expression. Changing a published default is a
behavioral API change.

## Asynchronous calls and caller controls

All first-party MP operations are asynchronous and use the language's native
async primitive. The clients provide no synchronous wrappers around remote MP
work.

Cancellation and caller deadlines use language-native controls and remain
separate from MP inputs. In the absence of an explicitly configured client
command timeout or caller deadline, the client does not impose an additional
short MP-command deadline. Server watchdog and recovery policy remain independent.
Startup uses separate timeout and cancellation policy because establishing a
runtime generation is not an MP command.

Cancellation or deadline expiry stops that caller from waiting. It does not prove
that SpatialAnalyzer did not execute the command, that execution was cancelled,
or that effects were rolled back.

## Client validation boundary

Clients validate the integrity of their handwritten public values, collection
materialization, reviewed domain invariants, and safe construction of the complete
protocol request. Validation is strict and does not silently coerce, trim,
normalize, or reinterpret caller data.

The server remains authoritative for runtime MP semantics and executability,
including current SpatialAnalyzer state, object existence and runtime type,
licensing, geometry, cross-argument constraints, and command-specific conditions.
First-party clients do not maintain competing replicas of those rules.

The language contract owns its normal local validation exception types and the
exact async failure timing.

## Typed failures and transport isolation

Each first-party client presents a handwritten error boundary. Applications do
not need to catch gRPC-library exceptions, inspect generated error messages, or
parse status text.

Clients distinguish at least:

- a valid typed Briosa operation failure;
- a transport failure without a valid typed operation detail;
- lifecycle, startup, and compatibility failures before MP submission;
- caller cancellation; and
- local argument or domain validation failures.

Clients decode only the typed `briosa-operation-error-bin` trailer for operation
policy. The canonical gRPC status and typed operation detail remain separate.
Status text is never parsed to reconstruct policy. Public error values preserve
the server's execution disposition, recovery guidance, replay guidance, and
replay-safety classification as separate dimensions.

Raw generated and transport error types are not part of the supported shared
contract. Whether a language retains an underlying diagnostic privately or
through its conventional cause mechanism is language-specific and must not require
consumer transport knowledge.

## Completion ambiguity, recovery, and replay

A first-party client never automatically replays an MP command once the server may
have observed it. Timeout, cancellation, disconnection, worker loss, and a lost
response can leave completion unknown. Restoring transport or worker availability
does not make the original command safe to repeat.

Execution disposition, recovery guidance, replay guidance, and replay safety must
not be collapsed into a `retryable` or `canRetry` Boolean. Applications remain
responsible for reconciliation and any later caller-initiated replay.

Low-level transport recovery is permitted only when the client can prove that the
server did not observe the command. A client must not configure a transparent
gRPC retry policy capable of resubmitting an accepted MP command.

## Construction, generations, and concurrency

Creating, registering, or importing a client is side-effect-free with respect to
external systems. Construction may validate and retain immutable handwritten
configuration and create local coordination state, but it does not:

- launch SpatialAnalyzer or Briosa;
- create or connect to an SDK engine;
- perform an RPC or readiness probe;
- open a transport channel eagerly; or
- make the first MP command trigger hidden startup.

Startup is explicit. It establishes one provisional runtime generation and
publishes that generation atomically only after the selected lifecycle mode has:

1. launched or located its runtime resources;
2. established transport liveness and MP readiness;
3. verified the exact target and locked protocol identity;
4. retrieved the admitted capability set; and
5. completed any required ownership checks.

A command cannot enter a partial or unverified generation. A command submitted
without a ready generation fails through the handwritten lifecycle boundary.
Every replacement generation repeats verification, even when it uses the same
endpoint.

The public client or handle is long-lived, uses effectively immutable
configuration, and may survive multiple start/stop generations. It is safe for
overlapping calls within the concurrency domain promised by its language contract.
That safety does not promise SpatialAnalyzer parallelism, implicit ordering, or
cross-language transaction isolation. Callers that depend on order await commands
sequentially.

Concurrent lifecycle calls cannot create competing generations, admit commands to
partial state, or publish a stopped generation. The detailed state machine and
supported runtime-ownership modes are defined by
[issue #147](https://github.com/spatialanalyzer/briosa/issues/147) and then
expressed idiomatically by each client.

## Stop, disposal, and resource ownership

Every first-party client provides explicit asynchronous cleanup. Stopping an
active generation closes command admission, unpublishes the generation, performs
bounded cleanup of resources the client owns, and releases local transport
resources. It makes no claim that in-flight MP effects were rolled back.

A reusable stop operation leaves the long-lived client dormant for a later
explicit start. A language may also provide final close, asynchronous disposal,
or context-management conveniences when its resource conventions warrant them.
Those conveniences must delegate to the same ownership rules and must not create a
second MP command surface.

A client may stop only a Briosa server generation that it launched and owns. It
must not terminate an externally owned Briosa server. Ordinary cleanup never
forcefully terminates SpatialAnalyzer.

SpatialAnalyzer, the SA SDK engine, the Briosa server, and the language client are
distinct lifecycle entities. A live SDK COM object never crosses a process or
language boundary. An application using the SDK directly must disconnect and
release that ownership before a Briosa worker creates its own connection. V1 does
not support competing direct-SDK and Briosa execution ownership.

Issue #147 owns the exact client-owned and externally managed runtime modes,
artifact discovery, endpoint selection, startup and partial-startup cleanup,
SpatialAnalyzer startup assumptions, and detailed ownership state machine.

## Compatibility and capabilities

First-party packages are target-specific and lock one reviewed target-qualified
protocol artifact. Startup verifies the server identity coordinates actually
published by discovery against the exact target and locked artifact. A client does
not invent or require a runtime fingerprint that the server does not publish.

The client captures the admitted capability set for each generation. Runtime
policy may expose a supported subset, so startup does not infer that every RPC
compiled into the package is admitted. Capability and compatibility information
is represented with handwritten public values rather than generated discovery
messages.

Package semantic version, Briosa server semantic version, exact SpatialAnalyzer
target, protocol artifact identity, and runtime architecture remain separate
coordinates as described by the
[exact-target product model](exact-target-product-model.md).

## Shared conformance testing

All first-party clients use one versioned, target-specific, language-neutral test
host owned by `spatialanalyzer/briosa`. The host implements the real public gRPC
contract over deterministic fake-worker scenarios for:

- lifecycle and readiness;
- exact-target and protocol compatibility;
- capability subsets;
- presence and default-like values;
- typed failures and malformed details;
- deadlines and cancellation;
- disconnection, worker crash, and watchdog replacement;
- completion ambiguity, replay guidance, and recovery; and
- shutdown and partial-startup cleanup.

Each client repository owns only a thin idiomatic fixture plus local unit tests for
its public mappings and validation. It does not independently recreate fake MP
semantics. The shared host requires no SpatialAnalyzer installation, license, SDK
binary, vendor documentation, or proprietary data and is not a SpatialAnalyzer
emulator.

The host, scenario contract, and artifact are implemented under
[issue #148](https://github.com/spatialanalyzer/briosa/issues/148). Test-only
controls must remain structurally isolated from the production server.

## Language-owned API choices

Subject to the shared guarantees above, each client contract owns:

- class methods versus module-level command functions;
- direct parameters versus a handwritten command-input object;
- identifier casing and async suffix conventions;
- concrete immutable domain, enum, named-result, integer, optional, and
  collection types;
- local validation exception types and async failure timing;
- cancellation and one-off deadline primitives;
- dependency-injection, factory, context-manager, and async-disposal
  conveniences;
- final-close behavior in addition to reusable stop; and
- source layout and public documentation organization.

Language-specific choices must remain deterministic, MP-recognizable, and covered
by public API tests. They cannot expose generated transport types or weaken shared
failure, lifecycle, ownership, compatibility, or replay guarantees.

## V1 non-goals

- Identical syntax or object models across .NET, Python, and JavaScript.
- A generated idiomatic facade, domain model, or documentation surface.
- Public generated protobuf or gRPC APIs inside the idiomatic packages.
- Synchronous wrappers, callback alternatives, aliases, or an MP overload matrix.
- Hidden startup on import, construction, registration, or first command.
- Hidden preliminary MP calls or contextual defaults.
- Automatic replay after execution may have started.
- Client-side replication of SpatialAnalyzer runtime semantics.
- Direct COM integration or live COM-object transfer through a core client.
- Independently maintained fake MP implementations in each client repository.
- Treating the shared test host as a SpatialAnalyzer emulator.

## Update process

Shared client behavior changes begin with an issue or architecture Discussion in
`spatialanalyzer/briosa`. The accepted change updates this document and any
authoritative server or protocol contract in the same coherent review sequence.

Each affected client then updates its language-specific contract, implementation,
tests, and documentation through its own issue and pull request. A client contract
links to this document and records only its idiomatic expression or an explicitly
reviewed language limitation. It does not copy this document and create a second
shared authority.

If a future language cannot express a shared guarantee without distorting its
normal API, maintainers review that tension centrally before granting an exception.
No one client repository may silently redefine cross-language behavior.
