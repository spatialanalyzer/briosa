# Operation retirement

Status: on `main` for contract major 3, which is not released yet. The
compatibility contract stays at major 2 until the major-3 freeze pull request
bumps it. Decided by D2 point 5 on
[#243](https://github.com/spatialanalyzer/briosa/issues/243#issuecomment-6107759671)
and on [#308](https://github.com/spatialanalyzer/briosa/issues/308#issuecomment-6109883364)
and [#310](https://github.com/spatialanalyzer/briosa/issues/310#issuecomment-6109883530),
for `DeleteCloudPointsByXYZRange` on
[#307](https://github.com/spatialanalyzer/briosa/issues/307#issuecomment-6113702460),
and, after licensed session 2, on
[#309](https://github.com/spatialanalyzer/briosa/issues/309#issuecomment-6404290445)
and [#350](https://github.com/spatialanalyzer/briosa/issues/350#issuecomment-6404290753).

An operation is retired, meaning deleted as a complete vertical slice, when
licensed evidence on the exact target shows that it cannot work through the SDK
and no untried binding exists. Retiring is a breaking change, so it happens only
in a contract major. Re-adding an operation later is an additive change.

## Retired in contract major 3

Each operation failed on both exact targets in a licensed probe session, and
the binding search before deleting found no other setter or getter in the
committed interop surface or the exact-target evidence of either target.

- The first six used a substitution for an argument the SDK exporter marks
  `NOT_SUPPORTED`. The 2026-10-07 session showed that the substitution does not
  work.
- The last two were decided after licensed session 2 (2026-10-25). The Mesh
  Orientation substitution stalls the worker, and the vector group
  runtime-select getter returns nothing.

| Operation | MP step | Probe | Observed on both targets | Why no other binding exists |
| --- | --- | --- | --- | --- |
| `construction_operations.make_callout_view_ref_list_wildcard_selection` | Make a Callout View Ref List - WildCard Selection | 7 | MP code 2, but the list getter returned nothing (`DataLoss`). | The SDK marks the result `NOT_SUPPORTED`; the interop has no callout-view list getter. |
| `construction_operations.add_collection_instruments_to_ref_list_wildcard_selection` | Add Collection Instruments to a Ref List - WildCard Selection | 14 | MP code 2, but reading the input list back returned nothing (`DataLoss`). | The MP documents no return argument, so there is nothing else to read. |
| `construction_operations.set_callout_view_properties` | Set Callout View Properties | 8 | MP code -1. | The SDK marks the callout view list `NOT_SUPPORTED`; the interop has no callout-view list setter, and the collection-object list setter was rejected. |
| `construction_operations.construct_circles_lines_from_surfaces` | Construct Circles (Lines) From Surfaces | 9 | MP code -1 for both `Circle` and `Line`. | The SDK marks the Circle Line Mode Type setter `NOT_SUPPORTED`; the interop has none. |
| `construction_operations.mirror_objects` | Mirror Object(s) | 10 | MP code -1. | The SDK marks the MP Plane Type setter `NOT_SUPPORTED`; the interop has none. |
| `cloud_and_mesh_operations.delete_cloud_points_by_xyz_range` | Delete Cloud Points by X Y Z Range | 15, 16 | MP code -1 with all six bounds and with only the X bounds; no point was deleted. | The six bounds are Optional Double, which the SDK marks `NOT_SUPPORTED`; no interop setter carries an optional double, and no other MP uses the type. Without its bounds the step is not a range delete. |
| `construction_operations.construct_polygonized_surface_from_point_clouds` | Construct Polygonized Surface from Point Clouds | 11 | Run first, with no dialog open: "Encountered an improper argument." and no MP result; the server's 600 s long-running watchdog ended the worker (`Indeterminate`). | The SDK names `SetMeshOrientationTypeArg`, which the committed interop does not have, and no other MP uses the Mesh Orientation Type. |
| `construction_operations.make_collection_vector_group_name_ref_list_runtime_select` | Make a Collection Vector Group Name Ref List - Runtime Select | 22 | With the F2 gesture SA documents, MP code 2, but the getter returned nothing (`OutputRetrievalFailed`); the same under Silent. | `GetCollectionVectorGroupNameRefListArg` is the SDK's own getter for the documented result. The interop has no other vector-group list getter, and no other MP returns the type. |

The evidence is each target's probe records: for the first six, the 2026-10-07
session on
[2026](../../targets/2026.1.0529.7/docs/testing/evidence/probe-277-2026.1.0529.7-2026-10-07.md)
and [2024](../../targets/2024.1.0508.5/docs/testing/evidence/probe-277-2024.1.0508.5-2026-10-07.md);
for the last two, session 2 on
[2026](../../targets/2026.1.0529.7/docs/testing/evidence/probe-session-2-2026.1.0529.7-2026-10-25.md)
and [2024](../../targets/2024.1.0508.5/docs/testing/evidence/probe-session-2-2024.1.0508.5-2026-10-25.md).
The records stay unchanged as history. The licensed probe harness no longer
sends probes 7 to 11, 14 to 16, and 22, and their numbers are not reused.

The other 22 runtime-select operations per target stay. None of them has been
observed on a licensed run; the decision on #350 has the next licensed session
test a few of their getters before anything else is decided for the family.

### What callers do

- Regenerate from the major-3 protocol artifact. The RPCs, their request and
  result messages, and the `CircleLineMode`, `MirrorFramePlane` and
  `MeshOrientationType` enums are gone, so code that used them stops compiling
  instead of failing at run time.
- Remove client facades and methods for the eight operations.
- To select callout views, `MakeCollectionItemNameRefListWildcardSelection` with
  the callout-view item type uses a different MP step. That item type has not
  been observed on a licensed run.
- A reference list is a client-side value in Briosa, so a caller that needs an
  instrument list can build it directly.
- No replacement exists for setting callout view properties on a list, for
  circle and line extraction from surfaces, for mirroring objects, for
  deleting cloud points by an X Y Z box, for polygonizing point clouds, or for
  selecting vector groups at run time on these exact targets.
  `DeleteCloudPointsByRadialDistanceFromPoints` deletes by distance from points;
  it has different semantics and no licensed validation yet (it is the control
  step of the #307 re-probe).

## How the protocol check accepts exactly these removals

`eng/Verify-Protocol.ps1 -AgainstRef <base>` runs Buf's `FILE` breaking rules
against the base revision, both in pull requests and against the previous
release. Each target's `docs/development/retired-operations.json` is a reviewed
ledger. For each retired operation it lists the service, the RPC, its request
and result messages, and any enums only that request uses, with the issue, the
probes, and the target's probe record that is its evidence.

Before the `FILE` comparison, `eng/RetiredOperations.psm1` removes the listed
elements from an ephemeral copy of the baseline descriptor. Every other change
keeps the normal rules. The step fails closed when:

- the current protocol still defines a listed RPC, message, or enum;
- the baseline RPC has a different request or result, or streams;
- the baseline defines a listed message or enum without its RPC, or the RPC
  without one of them; or
- a retained baseline element still uses a removed type.

A baseline that no longer contains the listed operations, such as any base after
this change merges, gets the ordinary strict check. The reviewed MP
argument-name and robot ID migrations apply the same step to their pinned
baselines.

`eng/Test-RetiredOperationPolicy.ps1` runs in CI for each target. It builds a
baseline that still defines the listed operations and shows that their removal
passes. It also shows that these still fail: an unlisted RPC removal, an
unlisted operation removal, a field change, a reintroduced retired RPC, a
changed RPC shape, a retained reference to a retired type, and a ledger that
does not match the baseline. The portable `RetiredOperationTests` require each
listed operation to be absent from the registry, classification, discovery,
service overrides, and generated protocol. They also require the ledger's
contract major to be the current major or the next one, and each entry's
evidence record to exist in the target.

Remove ledger entries once no checked baseline still contains them, at the
latest when the next contract major is prepared.
