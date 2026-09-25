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
