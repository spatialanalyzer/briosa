# Runtime redesign migration

Status: in development; this is not a released compatibility claim. Server work
is tracked by [#220](https://github.com/spatialanalyzer/briosa/issues/220)
and [#221](https://github.com/spatialanalyzer/briosa/issues/221).

Both independent SA targets declare compatibility major 2, revision 0. A
major-1 client must reject a major-2 server during selection and handshake.
Regenerate consumers from the protocol artifact for the exact SA target. The
public protobuf package remains `briosa`; the SA release identifies the
product and package, not an RPC namespace.

## Public changes

| Surface | Change | Client action |
| --- | --- | --- |
| `GetRobotMachineParameterRequest.machine_id` | `CollectionInstrumentId` at field 3; field 1 reserved | Supply collection and instrument names. |
| `StartRobotMachineInterfaceRequest.machine_id` | `CollectionInstrumentId` at field 4; field 1 reserved | Supply collection and instrument names. |
| `StopRobotMachineInterfaceRequest.machine_id` | `CollectionInstrumentId` at field 2; field 1 reserved | Supply collection and instrument names. |
| Full execution queue | `ResourceExhausted` with typed `Overloaded`, `NotStarted`, and `MayReplay` evidence | Preserve the evidence; never retry automatically. |
| Public inbound message | Maximum 64 KiB before mapping | Handle size rejection even without typed trailers. |
| Discovery worker state | `Stopping` | Show shutdown without reporting readiness. |
| Mutating capabilities | Correct `Mutating` classification | Respect the server's operation effect. |

The robot MP parameter is still named “Machine ID”; its exact setter is
`SetColInstIdArg`, and its value is a collection/instrument identity.
Other robot operations using collection/machine identities retain that type.
Removed field numbers are reserved rather than repurposed.

Five callout operations now use `SetEditTextArg` for notes/text;
`Calculate TCP Fixture Uncertainties` retrieves notes with `GetEditTextArg`.
The public string-list shape is unchanged. See the
[argument-name migration](mp-argument-name-migration.md) for the separate
source/JSON identifier changes.

## Server implementation

The host and worker ship together. Their private protocol is version 23:
generated JSON metadata serializes explicit typed input, output, and lifecycle
alternatives with numeric discriminators. Unknown, missing, contradictory, or
mismatched private payloads fail at the channel boundary. Zero, false, empty
strings, and empty lists preserve their distinct presence. Private frames have
their own 64-KiB limit. An older worker is rejected by the version gate.
These private changes do not add public RPCs or COM-shaped protobuf fields.

The worker passes an owned command and its typed values to the SDK STA without
the former sparse input record or conversion copy. Exact SDK marshaling remains
inside the worker. Lifecycle failures have explicit categories; supervisors
and coordinators classify them from typed evidence and transition snapshots,
not diagnostic text. Incomplete process cleanup blocks a replacement
generation until cleanup succeeds. See the
[runtime boundary](../architecture/runtime-boundary-and-lifecycle.md) and
[execution outcomes](../architecture/execution-outcomes-and-recovery.md).

Handwritten operation classes and thin service overrides now own the supported
MP routes. Each operation owns its input validation, exact setter/getter
sequence, output mapping, capability registration, and portable tests. The
[typed-operation pattern](typed-operation-pattern.md) is the working example.
The supported surface comes from protobuf source, implementations,
`SpatialAnalyzerApi.Operations`, and runtime policy. Migration progress
belongs to the active issues and pull requests, not a count in this document.

The descriptor-driven interpreter and the two dynamic GD&T calls were removed
before the frozen protocol checkpoint. An unsupported exact-target SDK binding
does not become an available operation through late binding.

## Evidence and validation

For 2026 bindings, review the committed
[exact-target evidence guide](../../targets/2026.1.0529.7/docs/development/exact-target-evidence.md).
The independent 2024 callout, instrument, and robot review used private
`briosa-evidence` revision `07acae44ad6cbed11391b38391317c4974910af8`.
Private vendor exports are not republished here.

Portable tests cover worker control, serialization, overload, cancellation,
malformed replies, retired robot wire fields, typed mappings, and generated
HTTP/2 clients with a fake worker. These do not prove live robot, callout,
TCP-fixture, or SpatialAnalyzer throughput. Maintained synthetic measurements
run through each target's `eng/Test-RuntimePerformance.ps1 -IncludeGrpc`;
they record percentiles and allocations without imposing machine-dependent
latency thresholds.

The R17 portable checkpoint is source revision
`89b59aa0ddd05ea868d94c4e4a4f89ee0fbcbccb`, Server `0.9.0-dev.1`,
compatibility 2.0 for both targets. Both locked Release builds and portable
suites passed; see the [R17 record](r17-portable-validation.md) for test totals,
protocol package hashes, and the limits of synthetic performance measurements.
The six client `0.4.0` candidates pin those exact protocol artifacts. Packaged
host/worker and conformance artifacts and the installer were exercised with fake
SDKs in R21. These are local review artifacts, not published releases or live-SA
validation.

The [packaged compatibility matrix](../../compatibility/matrix.json) now records
the R24 checks against those frozen artifacts: twelve published major-1 client
packages rejected the major-2 candidate before MP work, and all six unpublished
major-2 client packages passed twelve fake-SDK conformance scenarios each.
The 18 earlier major-2 reports remain unchanged historical evidence. The
2026 tutorial was also run from an isolated test copy with the candidate
protocol and all three candidate client packages; its 47 portable fake-server
scenarios passed without changing its published dependency pins. The separate
[R25 licensed record](r25-licensed-validation.md) identifies the executed
package scenarios for both exact targets and the remaining fixture and
string-setter-RPC gaps.

On 2026-09-25, licensed 2026.1.0529.7 `OwnedApplication` and
`SdkLossRecovery` scenarios passed on the redesigned Debug host and worker.
A subsequent generated-client Variables run completed 22 successful MP calls
and one expected output-retrieval failure. See the
[Variables validation record](../../targets/2026.1.0529.7/docs/development/variables-runtime-validation.md)
and [lifecycle record](../../targets/2026.1.0529.7/docs/licensed-local-lifecycle-validation.md)
for scope and observations. These runs do not validate the 2024 target.

Subsequent live exact-target checks resolved the two affected MP steps with
`SetStringArg`: both the wildcard item-type choice and the two GD&T option
choices succeeded against matching 2026 and 2024 SDK/SA instances. The two
2026 RPCs also passed through a source-built server. See the
[2026 observation](../../targets/2026.1.0529.7/docs/testing/evidence/string-setters-2026-09-28.md)
and [2024 observation](../../targets/2024.1.0508.5/docs/testing/evidence/string-setters-2026-09-28.md)
for exact tested values and the 2024 RPC limitation.

## Coordinated product completion

The first-party [client behavioral contract](../architecture/client-library-behavioral-contract.md)
defines shared error and discovery behavior. Product tasks remain:
[.NET #35](https://github.com/spatialanalyzer/briosa-dotnet/issues/35),
[JavaScript #36](https://github.com/spatialanalyzer/briosa-js/issues/36),
[Python #35](https://github.com/spatialanalyzer/briosa-py/issues/35),
[Installer #22](https://github.com/spatialanalyzer/briosa-installer/issues/22),
[Documentation #80](https://github.com/spatialanalyzer/briosa-docs/issues/80),
and [Examples #9](https://github.com/spatialanalyzer/briosa-examples/issues/9).

The candidate host/worker packages, Control Center, side-by-side installer,
client major-version rejection and positive major-2 use, versioned site,
three-language examples, and compatibility matrix have local review results.
They are not published products. Before release, review the outstanding
licensed fixture and frozen-package string-setter RPC coverage identified in
R25, then coordinate publication and update tutorial dependency pins.
Preserve historical major-1 evidence and retire old-baseline migration
exceptions only after the new
baseline is established.
