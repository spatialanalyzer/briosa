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
