# Runtime redesign migration

Status: in development; not a released compatibility claim.
Server implementation task: [#220](https://github.com/spatialanalyzer/briosa/issues/220).
Typed runtime follow-on: [#221](https://github.com/spatialanalyzer/briosa/issues/221).

Both independent SA targets declare compatibility major 2, revision 0. Packages
for the two SA releases remain independent; protobuf package names remain
`briosa`. Regenerate consumers from the exact-target protocol artifact. A client
for major 1 must not select a major-2 server merely because its package is newer.

## Public changes

| Surface | Change | Required migration |
| --- | --- | --- |
| `GetRobotMachineParameterRequest.machine_id` | `CollectionInstrumentId` at field 3; field 1 reserved | Supply `collection_name` and `instrument_id`. |
| `StartRobotMachineInterfaceRequest.machine_id` | `CollectionInstrumentId` at field 4; field 1 reserved | Supply `collection_name` and `instrument_id`. |
| `StopRobotMachineInterfaceRequest.machine_id` | `CollectionInstrumentId` at field 2; field 1 reserved | Supply `collection_name` and `instrument_id`. |
| Full execution queue | `ResourceExhausted`, typed `Overloaded`, `NotStarted`, `MayReplay` | Preserve all error evidence; never automatically retry. |
| Public inbound message size | Maximum 64 KiB before mapping | Handle transport size rejection even without typed trailers. |
| Discovery worker state | `Stopping` | Display shutdown without reporting readiness. |
| Mutating capabilities | Correctly report `Mutating` | Preserve the server's operation effect classification. |

The robot parameter keeps its MP name, “Machine ID”. Its exact SDK binding is
`SetColInstIdArg`, and its semantic type is a collection/instrument identity.
Existing field numbers are reserved rather than silently reinterpreted. Other
robot operations that actually use collection/machine identities keep that type.

Five callout operations now send notes/text as edit text using `SetEditTextArg`.
`Calculate TCP Fixture Uncertainties` retrieves result notes with `GetEditTextArg`.
Their public string-list representation is unchanged.

## Evidence and validation boundaries

The 2026 binding review uses the exact-target inventory and View SDK Code evidence
retained under that target. The independent 2024 review uses private
`briosa-evidence` revision `07acae44ad6cbed11391b38391317c4974910af8`, specifically
the callout, instrument, and robot command-step exports. The private vendor
exports are not republished here.

Portable regression tests exercise actual catalog mapping, the worker channel,
overload and cancellation, malformed worker replies, and retired robot wire
fields. These tests do not claim live validation of robot hardware, callouts, or
TCP-fixture workflows. The licensed validation record must state which scenarios
were actually run for each SA release.

## Coordinated product work

Implementation tasks: [.NET #35](https://github.com/spatialanalyzer/briosa-dotnet/issues/35),
[JavaScript #36](https://github.com/spatialanalyzer/briosa-js/issues/36),
[Python #35](https://github.com/spatialanalyzer/briosa-py/issues/35),
[Installer #22](https://github.com/spatialanalyzer/briosa-installer/issues/22),
[Documentation #80](https://github.com/spatialanalyzer/briosa-docs/issues/80), and
[Examples #9](https://github.com/spatialanalyzer/briosa-examples/issues/9).

The initial client migration has local portable evidence for both targets:
340 unit tests, 72 packaged fake-SDK scenarios, and six expected rejections of
the retained Server 0.6.1 build. These are development packages; no new package
has been published. Installer validation, documentation snapshots, examples,
and the remaining internal runtime redesign are still in progress.

- .NET, JavaScript/TypeScript, and Python: import the exact protocol artifacts for
  both targets, regenerate transports, adopt major 2 in selection and handshake
  checks, test overload decoding and corrected robot input types, and retain
  conservative handling of missing/unknown error evidence.
- Installer: validate complete matching host/worker packages, manifest contract
  coordinates, side-by-side installation, and major-aware client selection.
  A private host/worker change does not require teaching the installer MP types.
- Documentation site: publish versioned API/client references and this migration;
  preserve references for released major-1 products.
- Examples: update pins and exercise all three languages against each target;
  direct gRPC examples must also use the new protocol.
- Control Center: project `Stopping` correctly and preserve compatible diagnostic
  fields while the host, worker, and desktop are packaged together.
- Compatibility matrix: record measured positive major-2 combinations and
  negative major-1/major-2 selection checks. Do not overwrite historical evidence.

These downstream changes and release validations are required before publishing
the redesign. This document is a migration specification, not evidence that those
products have already been updated or released.

## Private Worker Protocol 17 (In Development)

The host and worker must be upgraded together. Protocol 17 uses numeric JSON
value discriminators and compile-time generated serialization metadata, omitting
unused null properties. An older worker is rejected by the private protocol
version gate. This private transport change does not alter public RPC names or
protobuf packages. Zero, false, empty strings, and empty lists remain present.
The independent 64-KiB private payload limit remains enforced before writing a
frame. Generated serialization does not remove numeric JSON conversion costs.

Worker contract types are grouped under Connection, Execution, and Values, with
one top-level type per file. Explicit value/outcome alternatives and the broader
typed-operation migration remain in progress under #221.

## Typed Mapping Progress

Four variable operations now use handwritten, concrete request/result mappings:
Get/Set Double Variable and Get/Set Named Double List Variable. Their interpreter
registrations were removed. Names, SDK bindings, defaults, execution/replay
metadata, and required-list behavior remain unchanged in each target. Output
shape validation now compares ordered names and kinds in one pass, followed by
positional result access. The remaining operation migration is still in progress.

The initial portable suite passed 953 tests across both products. Additional
HTTP/2 generated-client coverage exercises the four public RPCs and a 4,096-value
list; focused outcome/admission tests and worker tests also pass. SDK specialized
enum conversion uses explicit casts and defined-value validation instead of
`Enum.ToObject`.

A local Release microbenchmark compares the retained interpreter with these
actual production mappings, including output shape/retrieval validation. On the
2026 build, mapping a 4,096-double input fell from about 55.8 to 2.0 microseconds
and 166 KB to 34 KB allocated; validated result mapping fell from about 180.4 to
2.1 microseconds and 329 KB to 33 KB. These are in-process mapping measurements,
not gRPC latency, SDK performance, or end-to-end throughput. The harness and raw
samples remain in the local review artifacts; broader runtime measurements and
licensed validation remain outstanding.

## Explicit Execution Outcomes (Private Protocol 18)

Private protocol 18 replaces independently assignable MP execution booleans with
four alternatives: arguments rejected before execution, ExecuteStep rejected,
MP result unavailable, and retrieved MP result. A retrieved result carries its
raw code; success is derived solely from code 2. Outputs can be attached only to
a successful MP result. Audit and public error projection identify argument
rejection from its outcome type rather than diagnostic wording.

The SDK conversion boundary currently uses a validating adapter while its own
models are migrated. That adapter rejects contradictory evidence. Sparse input
and output value models, the remaining typed operations, and authoritative
lifecycle ownership are still outstanding; this outcome migration does not mark
the full redesign complete.

Validation of the outcome migration passed all 977 portable tests across both
products, including generated-client HTTP/2 calls and malformed private frames.

## Lifecycle Ownership and Scheduling

The supervisor snapshot now owns the state revision, application association,
queue admission state, and common execution-readiness predicate. Health,
discovery, and SDK lifecycle responses project those facts without mutating state
on reads. A generation-bound queue owns reservations, cancellation, and its
consumer; a mapper that outlives shutdown cannot submit into a replacement.
Unexpected consumer failures retire the generation, resolve the active command
with its available execution evidence, and resolve queued commands as not started.
No command is replayed.

Heartbeats acquire the exchange gate only when idle. They skip active or reserved
work and use recent successful exchanges as liveness evidence. This does not
replace the exact-generation MP readiness proof or watchdog. Deterministic tests
cover queue-generation races, consumer failures, consistent projections, and a
heartbeat tick during active and queued work. The broader process/channel owner
separation and typed connection-failure classification remain in progress.

## Shared SDK Values and Outcomes

The SDK adapter now constructs the same explicit MP outcomes sent to the host.
The duplicate `SdkExecutionResult`, `SdkMpResult`, output argument/value, and basic
value types were removed. Ten collection families now own immutable storage;
SDK mapping reuses those owned values instead of copying each list and element.
Object/item parsing uses fixed, exact-target lookup tables, preserving the 2024
reserved Enhanced Cloud slots and rejecting unspecified/unknown values.

Remaining SDK-specific input options are isolated in `SdkCommandMapper` while the
sparse input model is migrated. SDK connection, execution, and value contracts
are grouped in those directories with one top-level type per file. The shared
model stays inside each independent target. No public protobuf or private wire
schema change is introduced by this consolidation.

## Async Worker Control and Output Delivery (Private Protocol 19)

The pipe control loop now awaits sequential exchanges without owning a second
STA. The serialized SDK executor remains the sole owner of COM initialization,
access, and disposal. If a successful MP result cannot be encoded before any
response bytes are written, the worker sends a bounded `outputs-unavailable`
outcome. It preserves raw MP code 2 and completion, returns a typed public
DataLoss/output-retrieval failure, and never replays the command. Actual pipe I/O
failures retain their existing ambiguous-completion treatment.

All 1,007 Debug portable tests passed across the two products. New tests run the
production control loop over real named pipes with an injected fake SDK. They
exercise oversized and non-finite results, a subsequent heartbeat and operation,
clean stop, and same-thread STA disposal. Licensed SpatialAnalyzer validation
and broader throughput measurements remain outstanding.

## Build Identity and Discovery Metadata

Runtime version, source revision, and the root informational response now use
build-produced constants finalized by the .NET SDK. Package overrides are
preserved; no assembly-attribute reflection occurs on discovery requests.
Capability metadata is materialized once per discovery service and cloned when
returned, so a consumer cannot mutate future responses. Focused validation
covers ordinary builds, release package overrides, and response ownership.

## Typed Output Values (Private Protocol 20)

The sparse output record is replaced by `WorkerRetrievedOutput`, containing one
explicit typed value, and `WorkerUnavailableOutput`, containing no value. The
retrieved constructor verifies the MP kind and payload type. Scalar zero/false,
empty text, and empty lists remain valid values. SDK getter evidence is converted
once at the boundary; contradictory retrieval evidence is rejected.

Generated private serialization uses explicit retrieval and value discriminators
and rejects unknown members. Malformed, missing, mismatched, or unknown payloads
fail at the channel boundary. Local SDK getter diagnostics remain off the wire.
This changes neither public protobuf messages nor the language-client API.

The sparse input record and SDK-specific input options still require migration.
Most operation mappings still use the old interpreter; the value change does not
complete that separate work or establish a new end-to-end performance claim.

All 1,025 Debug portable tests passed after this output migration: 532 for the
2024 product and 493 for 2026. This includes all output-family round trips,
malformed wire cases, active operation mappings, process supervision, generated
client smoke tests, and production worker control with the fake SDK.

## Typed Nested Fit Constraints

`GetRelationshipFitConstraintsScalarType` and
`SetRelationshipFitConstraintsScalarType` now call handwritten typed mappings.
Their old interpreter registrations are removed. Collection-object identity
validation and scalar fit-constraint conversion live in separate value mappers.
Missing high/low constraints and missing leaves retain independent disabled/zero
defaults; explicit false and zero outputs preserve public protobuf presence.
The 2024 reserved object-type slot remains rejected.

Generated-client HTTP/2 tests exercise the real services, round-trip the private
command/result serialization with a fake executor, verify all five ordered
outputs and exact binding names, and reject invalid requests before submission.
These tests do not attach to SpatialAnalyzer or measure full worker-process
latency. Public messages and the supported operation set are unchanged.

## Typed Input Values (Private Protocol 21)

Each input argument now contains one typed value, with constructor checks for
missing or mismatched payloads. The former record's nullable per-kind properties
are removed. Input and output families share the same explicit value hierarchy;
private generated serialization rejects unknown payloads and extra members.
The remaining SDK-specific input conversion is temporary and still needs removal.

Encoding every supported catalog fixture exposed existing mapping defects:
filter-proximity settings and fit degrees of freedom lacked option values;
cloud and vector-group names lacked text values; and some object-list outputs
carried a scalar-only omitted-type fallback. Those mappings are corrected.
List outputs still require their documented embedded type information.

All 1,050 Debug portable tests passed across both products: 545 for 2024 and
505 for 2026. Coverage includes serialized catalog commands, option field
positions, malformed input payloads, generated clients, and fake worker lifecycle
behavior. Public RPC messages are unchanged by this private protocol migration.

## Shared SDK Inputs and Typed Choices (Private Protocol 22)

The worker now passes its immutable command and values directly to the SDK STA.
`SdkCommand`, the sparse `SdkInputArgument`, duplicate option records, and the
conversion layer are removed. SDK marshalling and exact literal conversion stay
inside the worker. Each target owns its own shared contracts and enum definitions.

Specialized choices now carry their concrete enum type through generated private
serialization. A value from a different choice family is rejected at construction;
unknown and reserved enum values are rejected before transport. The legacy
interpreter's ordinal conversion is isolated in `WorkerChoiceFactory` until those
operations are replaced by typed mappings. The 2024 enum differences remain intact.

Validation passed 1,050 portable tests across both products, followed by both full
worker suites (216 and 199 tests) with expanded enum round-trip coverage and a new
STA command-identity test. The adapter receives the original owned command and
list value without a conversion copy. This is an allocation removal established
by code and ownership tests, not a measured end-to-end speedup.

## Typed Lifecycle Failures (Private Protocol 23)

SDK activation, connection, process-exit, and liveness failures now carry an
explicit category from the worker to the supervisor. Recovery decisions and
public incident classification use that evidence rather than matching diagnostic
strings. Startup failures retain an incident, and watchdog retirement records its
cause directly. Readiness verification likewise uses its result rather than the
spelling of a diagnostic code. Diagnostics remain descriptive, safe text.

The private channel rejects unknown failure categories and failures attached to
an apparently connected worker. Readiness requires no active connection failure.
Public protobuf definitions and client packages are unchanged by this step.

All portable suites pass: 557 tests for 2024 and 517 for 2026, including changed
fake-worker diagnostics, misleading incident text, failure round trips, and
contradictory evidence. Worker tests were rerun after correcting async analyzer
findings in the new tests. No licensed SpatialAnalyzer validation is claimed.
