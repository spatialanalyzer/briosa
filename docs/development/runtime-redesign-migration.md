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

## Removing CLR Reflection from Remaining Mappings

The retained interpreter now creates messages through generated protobuf parsers
and writes repeated values through the collection interface. It no longer uses
`Activator.CreateInstance`, enumerates `Add` methods, or invokes a reflected method
for each returned element. This intermediate cleanup still interprets protobuf
descriptors and allocates boxed values; concrete operation migration remains required.

All catalog and typed-slice mapping tests passed (36 for 2024, 35 for 2026).
A local 2026 Release microbenchmark measured validated mapping of 4,096 doubles
at approximately 150 microseconds before and 62–65 microseconds after, with
allocation falling from about 330 KB to 198 KB. The handwritten typed mapping
remained substantially cheaper. These are mapping-only measurements; they do
not establish gRPC throughput or SpatialAnalyzer execution performance.

## Runtime Path Measurements and Retention Work

Local Release runs for both targets now exercise generated HTTP/2 clients,
production policy/admission/mapping, the process supervisor, and a separate
synthetic worker over named pipes. Scenarios cover scalar values, nested options,
32/1,024/4,096-value lists, concurrent calls, overload, and an idle heartbeat.
Measurements include latency percentiles and allocation; client and host share
the measured process, while the fake worker reports separately.

The 256-call delayed burst reached exactly 64 queued requests. Excess requests
returned typed Overloaded/NotStarted evidence; admitted requests all reached a
terminal outcome. Both targets retained readiness and reported zero worker
failures, watchdog timeouts, logging drops, and logging failures. These runs use
synthetic SDK responses and do not establish licensed SpatialAnalyzer throughput.

File retention now performs its post-write scan only when a file opens or rotates.
Every write still reserves its bounded record under the cross-process lock and
checks the current directory budget. Concurrent-instance, age, rotation, and
single-file quota tests pass for both targets. The repeated full-path run remained
healthy; mixed latency variation does not establish a general logging speedup.

## Production File Organization

The remaining files containing multiple top-level types have been split into
matching files. This separates process interfaces, identity evidence, policy
decisions, logging health, lifecycle exceptions, desktop messages, and operation
contracts. Private implementation types remain with their sole owner where useful;
generated protobuf files remain generated.

For each target, 65 declarations from 22 files were checked for syntax equivalence
after extraction. Unused imports were removed. All 1,074 portable tests passed
with the new layout, including the desktop and lifecycle suites.

## Typed Variables Domain

All 36 Variables RPCs now use handwritten typed command and result mappings in
`Operations/Variables`, with shared identity, transform, vector, and font mappers
in `Operations/Values`. The remaining 32 Variables interpreter registrations have
been deleted. Capability registration and the public service route directly to
the new implementations; no Variables call uses protobuf descriptor interpretation.

The public schema, defaults, execution classification, and private protocol 23
are unchanged. Required lists still reject empty input. Empty fields inside list
members retain their prior meaning. Object-type omission uses Any; optional item
types distinguish absence from explicitly supplied Unspecified. Wildcard deletion
uses the exact SDK double-hyphen spelling on both targets. Structured values own their
inputs, and lists map directly to typed immutable values without intermediate
boxed arrays.

A generated HTTP/2 client exercises every Variables RPC through the private value
codec, including all scalar and structured families, duplicates, empty returned
lists, minimum/maximum output ordering, font defaults, and invalid requests that
never reach the worker. The full server suites passed: 314 tests for 2024 and 291
for 2026. Worker and protocol binaries are unchanged from their preceding passing
suites. This completes the Variables domain, not the remaining domain migrations.

## Local Licensed Validation on 2026-09-25

The approved local `OwnedApplication` and `SdkLossRecovery` scenarios passed on
SpatialAnalyzer 2026.1.0529.7 using the redesigned Debug host and worker (private
protocol 23). They exercised inert host startup, owned application launch, exact
identity gating, execution readiness, SDK restart/reconnection, deliberate loss
of the test-owned SDK, explicit recovery, and normal application shutdown. No
SpatialAnalyzer, SDK, host, or worker process remained after either scenario.

The activated SDK's file hash matched the retained interop provenance. The local
32-bit COM registration and application file version independently identified
2026.1.0529.7. These observations do not establish live 2024 compatibility.

A subsequent generated-client Variables run completed 22 successful MP calls and
one expected output-retrieval failure after clearing a double list. The worker
remained ready and cleanup completed. This found and corrected an inherited
2026 wildcard-deletion binding that used the documentation title instead of the
View SDK Code step spelling. See the [exact-target Variables observations](../../targets/2026.1.0529.7/docs/development/variables-runtime-validation.md)
for the tested operations, failed pre-fix run, and SDK getter evidence.

A separate read-only `IDispatch.GetIDsOfNames` probe on the installed 2026 SDK
resolved `SetDoubleArg` but returned `DISP_E_UNKNOWNNAME` (`0x80020006`) for both
`SetMPGDTOptionsDistanceBetweenModeArg` and
`SetMPGDTOptionsCheckValidatorTypeArg`. It did not connect to SpatialAnalyzer or
invoke either setter. The disposition of `Set GD&T Options` remains a maintainer
decision; no supported substitute binding is established by this probe.

## Heartbeat and Process Ownership

Heartbeat scheduling and cancellation now belong to a small monitor; readiness
decisions remain in the generation controller. Concurrent monitor disposal waits
for the same stop, and unexpected probe failures retire the generation instead
of silently leaving it ready without supervision.

The process owner now bounds forced termination, exit confirmation, and disposal.
The named-pipe factory transfers ownership immediately after launch, so a child
that never connects is still covered by the controller's cleanup. A failed kill
is no longer swallowed before an unbounded wait. Disposal only releases handles
after confirmed exit and cannot trigger a hidden second termination attempt.

Incomplete cleanup retains the process and pending cleanup task, publishes a
faulted state, and blocks a successor generation. A later explicit lifecycle
request can finish cleanup; it does not replay the previous command. These
changes preserve the public protocol and private protocol 23. Portable regression
coverage includes ignored cancellation, failed termination, missing exit evidence,
stalled disposal, recovery exclusion, and children that never connect.

## Read-only Context Routes

Get Working Directory, Get i-th Collection Name, Get Number of Collections,
Get Active Units, and Get Working Frame Properties now route through their
handwritten typed implementations. Their duplicate interpreter registrations and
the working-directory service's test-only execution method have been removed.
Existing outcome tests call the actual gRPC override, and a generated HTTP/2 client
checks these operations alongside Get Active Collection Name through the private
value codec.

Result mapping uses the output order already validated by the shared executor,
without repeated name searches. The collection-object mapper is shared with the
Variables domain and continues rejecting unknown types. The frame-specific omitted
type fallback remains local to Get Working Frame Properties. Omitted collection
index still means zero, preserving the live public route's behavior; the unused
implementation's conflicting presence requirement has been removed. No protobuf
or private wire change is required. All 46 focused mapping, catalog, value, and
service tests passed on each target.

## Typed Lifecycle Failure Classification

Lifecycle snapshots now distinguish startup, connection, readiness, and stop
timeouts from failed operations, cancellation, identity rejection, and incomplete
cleanup. The SDK lifecycle coordinator derives the gRPC failure kind from this
typed evidence and uses the same captured snapshot for the returned public state.
It no longer parses diagnostic text to recognize a timeout or identity rejection.
An activation-failure regression includes the word `timeout` in its diagnostic
and still returns the activation-failure status. Actual timeout scenarios retain
their DeadlineExceeded behavior and recovery evidence.

## Maintained Performance and Package Validation

Each target's `eng/Test-RuntimePerformance.ps1 -IncludeGrpc` now exercises generated
HTTP/2 clients through production typed mappings, policy/admission, the supervisor,
and the private named-pipe codec. The separate worker supplies synthetic results
without activating COM. Scalar, nested, and 32/1,024/4,096-double list cases record
p50/p95/p99, throughput, and allocations, with logging enabled and disabled.
Client, host, and assertion allocations share the parent process measurement;
worker allocation is separate. These measurements do not establish SA throughput.

Both target runs passed the existing dispatch check and both generated-client
modes. Each generated-client run measured ten cases after warmup, then admitted 65
of a 256-request delayed burst and rejected 191 with typed NotStarted overload
evidence. Peak queue depth was 64, admitted and terminal counts matched, and there
were no worker failures, watchdog timeouts, queued log records, or log drops at
shutdown. An idle heartbeat succeeded after load. The test gates these structural
invariants rather than machine-dependent latency thresholds.

Complete `0.9.0-dev.3` Windows package checks passed for both targets at production
source `82b6ce51cb199a4e04ef3c142af4b27a8c34d1e4`: two clean builds produced identical
archives, all checksums/provenance and offline diagnostics matched, unsafe binding
was rejected, and packaged Control Center startup, detach/reopen, restart, and
graceful shutdown passed with a fake worker. These are unpublished local products.

The installer validated both preceding `0.9.0-dev.2` products from the same source
through a disposable signed catalog and private store, including coexistence,
receipts, verification, and exact-package repair. Its 178 core/CLI tests and WPF
smoke passed. No installer engine change was needed; the validation script now
accepts an explicit expected compatibility major. The documentation site has an
unreleased migration page with its full build, search, and API/history/SEO checks
passing. Published example pins remain major 1 until compatible releases exist;
their migration must update all client pins, the verified protocol, and fixtures
together.

## Event Operations

All five existing Event Operations RPCs now use handwritten typed mappings in
their domain directory, with one implementation per file. Their interpreter
records have been removed and capability registration still describes the same
surface and read/mutation/replay classifications. A shared file-reference mapper
validates required paths without introducing filesystem access during mapping.

The migration preserves required nonempty reference lists, default index zero,
default decimal precision six with explicit zero retained, and false overwrite
defaults. Scalar event names keep the collection-object family; event lists and
the indexed getter retain the broader collection-item family used by the reviewed
SDK bindings. No new target or command semantics are inferred from the migration.
Generated-client checks cover all five routes, private value round trips, exact
argument names/bindings, required-value rejection before execution, zero output
presence, and getter failure with Completed/DoNotReplay evidence. No live Event
Operations validation or new protocol change is claimed.
The complete Release server suites passed after this migration: 310 current-target
tests and 333 legacy-target tests, with no failures or skips.

## Lifecycle Results Retain Their Own State

Start, connect, recovery, and stop now return explicit internal success/failure
results with the immutable snapshot captured while the controller still owns the
transition. The coordinator uses that result for both classification and public
state, rather than awaiting a Boolean and independently reading a potentially
newer snapshot. Application-generation association also returns the snapshot it
observed or published under the controller's gate.

This exposed and corrected an inherited stop-classification bug: a worker already
retired during failed startup could be fully cleaned up, but a subsequent stop
was reported as failed solely because the retained termination history said
Forced. Stop now reports its own cleanup outcome. Earlier incidents remain
visible; genuine stop failures, timeouts, and incomplete cleanup remain failures.
Queued-stop regressions preserve the completed start's readiness snapshot and a
failed start's timeout evidence after cleanup has finished. The public coordinator
also verifies successful stop after activation failure while retaining the earlier
StartFailed incident. No protobuf or private protocol change is needed.

The hosted service's unused startup-result logging methods were removed; the
controller continues owning transition logs and the host still starts inert.
Both complete Release server suites passed: 313 current-target tests and 336
legacy-target tests, with no failures or skips.
