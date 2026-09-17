# SA 2024.1.0508.5 compatibility and validation

## Product status

This initial, unreleased product implements the 996 operations selected by the
completed [SA 2024 review](https://briosa.dev/mp-command-catalog/2024.1.0508.5/review-notes).
The compiled protobuf services, handwritten operation registry, and runtime
capability discovery define availability. Six read-only operations and basic
lifecycle behavior passed [local licensed checks](../testing/evidence/local-functional-2026-09-17.md).
The remaining operations have no 2024 runtime validation from this port.

The server, worker, Control Center, protocol, tests, interop metadata, and package
tools belong to this target. They have no project or source references to the
2026 product. The public protobuf package remains `briosa`; use the protocol
artifact for the exact SA target. A 2026 client contract is not interchangeable
with the 2024 contract where signatures differ.

## Review provenance

- Implementation baseline: `targets/2026.1.0529.7` at Briosa commit
  `7a04881063f7efb3dccd3fdc1cc0417929b52739`.
- Exact MP evidence: private `briosa-evidence` revision
  `07acae44ad6cbed11391b38391317c4974910af8`, under
  `2024.1.0508.5/mp-command-step-exports`. The review covers 1,283 group-qualified
  commands and 1,282 distinct labels. Private exports are not redistributed here.
- Completed disposition record: `briosa-docs` commit
  `4fd0ee3be7ae433c369b06ffca81d5eb730e1908`.
- The exact 2024 interop metadata is under
  [interop/SpatialAnalyzer/2024.1.0508.5](../../interop/SpatialAnalyzer/2024.1.0508.5).
  The manifest records the source hash, importer, and canonical API fingerprint.
  Its 151 COM methods match the reviewed 2026 interface surface; the assembly
  identity is `2024.1.508.5`. This is static interface evidence, not COM activation
  or runtime validation.

The port retains 993 existing implementations and adds `Run Crib Sheet`,
`Project Objects`, and `Stop Projection` as complete instrument operations.
The three additions remain at risk pending the named instrument fixtures.
Commands absent from the 2024 inventory are not exposed. In particular,
`Scan within perimeter`, `Edit Scan Perimeter Profile`, and `Scan CAD Faces`
remain SDK unavailable because their 2024 parameter-set-name binding is missing.
A generic string setter does not fill that evidence gap.

SA group hierarchy changes affect catalog navigation. Existing service and RPC
names remain stable for matching operations; they do not encode the SA menu tree.

## Exact-target API differences

The following are deliberate differences from the 2026 contract:

| Operation | 2024 behavior |
| --- | --- |
| Filter Clouds to Vector Groups - Resolve points | No proximity-points input. |
| Re-Compute Calculated Items | No filtered-cloud-data refresh input. |
| Get Cone Properties | No cut-length-from-apex output. |
| Set/Get Feature Check Reporting Options | No only-create-failed-vectors input/output. |
| Make Cylinder Fit Profile | No nominal-axis, nominal-orientation, align-with-nominal, reverse-axis, or first-to-last-point controls. |
| Do Relationship Fit | No randomized-start control. |
| Get General Relationship Statistics | `max_deviation` binds `Max Deviation`; no absolute-value guarantee is inferred. |
| Get Points to Objects Relationship Statistics | No average-deviation output. |
| Auto Filter Clouds to Nominal Geometry 2D/3D | No feature-specific-filter-settings input. |
| Set Geom Relationship Auto Vectors Nominal (AVN); Set Relationship Auto Vectors Fit (AVF) | No custom vector-group prefix controls. |
| Construct Objects From Surface Faces - Runtime Select | Seven explicit Boolean selectors: planes, cylinders, spheres, cones, lines, points, circles. No later object-type selector or point offset. |
| Construct Point Cloud from Existing Clouds | No voxel-to-cloud-RGB input. |
| Export ASCII Point Clouds | No cloud point labeling or scan direction vector options. |
| Get Instrument Targets and Mode/Profiles | The exact input label is `Instrument to set`, despite being a getter. |
| Make a System String | `User Name` is supported. Later license-user, Windows-user, and computer-name choices are absent. |
| Object/item domains | `Enhanced Cloud` is absent. Its wire numbers remain reserved; other choices keep their numbers. |
| Add New Instrument | Accepts the 185 exact 2024 literals, including `PMT Arm 4m 7 dof`. Seven later instrument choices and unknown names are rejected before SDK execution. |

The three vector-group exporter differences retain the reviewed three-argument
`SetColVectorGroupNameArg` mapping, which matches the exact interfaces in both
versions. The old collection/object setter representation does not remove the
four-argument `SetCollectionObjectNameArg2` method from the 2024 SDK; typed
collection/object inputs retain the reviewed adapter mapping.

## Defaults and result semantics

The retained contracts explicitly send Briosa's reviewed defaults for unchanged
inputs. These are application choices, not claims about SDK omission behavior.
The port does not carry the 2026 default-evidence files as 2024 observations.

- `DirectCadAccessRequest.surface_compatibility_mode` must be supplied, including
  an explicit `false` when intended.
- Both QDAS operations require the caller's `K0004: Date Time Stamp`; captured
  timestamps are not defaults.
- All seven surface-construction selectors require explicit presence.
- `Get Working Frame Properties` retains its operation-specific `Frame` fallback
  when a returned object-type literal is absent. The fallback has portable
  coverage. The full operation passed local 2024 smoke checks, which did not
  separately capture the raw type literal. Unknown nonempty literals still fail
  closed.

MP result retrieval and the raw result code remain separate from transport
completion. Only retrieved MP code `2` establishes success. Non-success results
are retained in the structured `briosa-operation-error-bin` trailer, including
projection outcomes that may have changed only part of the requested state.
Deadlines, cancellation, and worker replacement never trigger automatic replay.

## Validation and release gates

Portable checks exercise the complete registered protocol/worker/result mappings,
2024 signature differences, exact choices, identity mismatch rejection, fake SDK
sequences, process failures, policy, diagnostics, and package/client lifecycle.
See the [target guide](../../README.md) for commands and
[licensed instrument scenarios](../operations/legacy-instruments.md) for the
unexecuted hardware-dependent checks.

The [initial port validation record](../testing/initial-port-validation.md)
summarizes portable results. The subsequent
[local functional record](../testing/evidence/local-functional-2026-09-17.md)
covers exact SDK identity, mismatch rejection, six read-only commands, clean
restart, and shutdown against licensed SA 2024. It does not establish coverage
for other operations, instruments, or failure recovery under load.

Ordinary CI and release matrices explicitly include this target. The separate
`licensed-sa-2024.yml` workflow accepts manual dispatch from trusted `main` only,
uses the `licensed-sa-2024-1-0508-5` environment and exact-target runner label,
and consumes a hashed payload built on a hosted runner. Configuring this workflow
does not provision a licensed runner or constitute a passing runtime test.

Before a v1.0 promotion, broader licensed runtime validation, the protected CI
environment, and enterprise Artifactory integration verification remain
outstanding. First-party .NET, Python, and
JavaScript client products for 2024 follow the reviewed server/protocol contract;
this implementation does not claim those packages have shipped.
