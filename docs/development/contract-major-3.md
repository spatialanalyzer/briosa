# Contract major 3

Status: not released yet. Both exact-SA targets declare behavioral contract
major 3, revision 0. The planned release is Server 0.10.0, with the first-party
clients at 0.5.0.

Wave 2b ships as one planned behavioral release
([#244](https://github.com/spatialanalyzer/briosa/issues/244)). #244 asks for
each change to be classified against the accepted contract, 2.0, before the
version is assigned. This page is that record. It also holds the frozen protocol
checkpoint that the clients build against, and what must happen before the
release.

## Classification

Several changes break callers of contract 2.0, so the release needs a new
contract major. Before 1.0, a new contract major moves the server's minor
version. Major 2 shipped as Server 0.9.0.

| Change | Pull request | Class | Where it is described |
| --- | --- | --- | --- |
| Named admission profiles and a narrower default; `Allow` and `Deny` removed | [#297](https://github.com/spatialanalyzer/briosa/pull/297) | Behavioral and operator breaking | [Admission profile migration](admission-profile-migration.md) |
| Ten request defaults flipped; request option guard | [#297](https://github.com/spatialanalyzer/briosa/pull/297) | Behavioral breaking | [Admission profile migration](admission-profile-migration.md) |
| `ReplaySafety.Unknown` denied; runtime-select replay marked unsafe | [#297](https://github.com/spatialanalyzer/briosa/pull/297) | Behavioral | [Admission profile migration](admission-profile-migration.md) |
| Duration-class execution budgets; separate readiness and startup bounds | [#298](https://github.com/spatialanalyzer/briosa/pull/298) | Behavioral, for operators | [Execution outcomes](../architecture/execution-outcomes-and-recovery.md) |
| `SDK_CALL_FAULTED` failure kind; interim mappings removed | [#301](https://github.com/spatialanalyzer/briosa/pull/301) | Additive enum value, behavioral breaking | [Client behavioral contract](../architecture/client-library-behavioral-contract.md) |
| Admission profile and per-operation classification in discovery | [#314](https://github.com/spatialanalyzer/briosa/pull/314) | Additive | [Admission profile migration](admission-profile-migration.md) |
| Cancelled queued work abandoned; accepted lifecycle exchanges detached | [#315](https://github.com/spatialanalyzer/briosa/pull/315) | Behavioral breaking | [Runtime boundary](../architecture/runtime-boundary-and-lifecycle.md) |
| D2 argument rules: omission, requiredness, and defaults | [#318](https://github.com/spatialanalyzer/briosa/pull/318), [#324](https://github.com/spatialanalyzer/briosa/pull/324), [#325](https://github.com/spatialanalyzer/briosa/pull/325) | Request-validation breaking | [Argument defaults migration](argument-defaults-migration.md) |
| SA process observation cost bounded; generation kept across a missed observation | [#319](https://github.com/spatialanalyzer/briosa/pull/319) | Compatible | [Runtime boundary](../architecture/runtime-boundary-and-lifecycle.md) |
| Six operations removed | [#320](https://github.com/spatialanalyzer/briosa/pull/320) | Protocol breaking, through the retirement ledger | [Operation retirement](operation-retirement.md) |
| SA interaction mode setting; `sa-modal-dialog-open` diagnostic | [#355](https://github.com/spatialanalyzer/briosa/pull/355) | Operator additive, behavioral | [Execution outcomes](../architecture/execution-outcomes-and-recovery.md) |
| Accepted launches detached; host shutdown sized from the lifecycle bounds | [#356](https://github.com/spatialanalyzer/briosa/pull/356) | Behavioral | [Runtime boundary](../architecture/runtime-boundary-and-lifecycle.md) |
| Two operations removed; fit seed optional | [#357](https://github.com/spatialanalyzer/briosa/pull/357) | Protocol breaking, through the retirement ledger; relaxing | [Operation retirement](operation-retirement.md), [argument defaults migration](argument-defaults-migration.md) |
| MP partial success (code 4) reported as a typed success | [#360](https://github.com/spatialanalyzer/briosa/pull/360) | Additive enum value, behavioral breaking | [Execution outcomes](../architecture/execution-outcomes-and-recovery.md#partial-success-mp-code-4) |
| Caller-cancellation kinds for lifecycle failures | [#363](https://github.com/spatialanalyzer/briosa/pull/363) | Additive enum values, behavioral | [Runtime boundary](../architecture/runtime-boundary-and-lifecycle.md) |
| Two cross-target wire conflicts kept target-specific ([#275](https://github.com/spatialanalyzer/briosa/issues/275)) | [#363](https://github.com/spatialanalyzer/briosa/pull/363) | No wire change; documented limitation for raw clients | Each target's `docs/operations/protocol-artifacts.md` |
| Major-1 retained clients retired from the live gate ([#250](https://github.com/spatialanalyzer/briosa/issues/250)) | [#363](https://github.com/spatialanalyzer/briosa/pull/363) | Release-gate policy | [Compatibility evidence](../../compatibility/README.md) |
| USMN accepts an empty `excluded_groups` list and skips its setter (licensed session 3, probe 33) | [#363](https://github.com/spatialanalyzer/briosa/pull/363) | Relaxing | [Argument defaults migration](argument-defaults-migration.md) |
| Vector-name-list runtime select removed; no operator could complete it in licensed session 3, so it is unverified | [#363](https://github.com/spatialanalyzer/briosa/pull/363) | Protocol breaking, through the retirement ledger | [Operation retirement](operation-retirement.md) |

## What major 3 leaves out

Major 3 is the Wave 2b release only. These stay with later waves or their
own decisions:

- capacity and request limits (F2, Wave 2c, [#245](https://github.com/spatialanalyzer/briosa/issues/245));
- local authentication ([#251](https://github.com/spatialanalyzer/briosa/issues/251));
- SDK ownership ([#252](https://github.com/spatialanalyzer/briosa/issues/252));
- cross-target structure ([#253](https://github.com/spatialanalyzer/briosa/issues/253));
- broader requiredness and default metadata, and deeper refactoring.

The presence-insensitive optional arguments in the F7 appendix of the
[argument defaults table](../../eng/classification/argument-defaults.md) stay
unchanged until licensed probes settle them.

Each later change is classified against the 3.x contract first, and only then
versioned. Raising an internal limit or adding an API can be compatible. A new
rejection of a request that 3.x accepts is breaking.

## Protocol checkpoint

The clients build against a frozen checkpoint of the freeze change, as
[R17](r17-portable-validation.md) did for major 2. Each client pins the
checkpoint's source commit in its `protocol.lock.json`.

The checkpoint was cut after licensed probe session 3 (2026-11-09), whose two
changes it includes: the retired vector-name-list runtime select and the USMN
excluded-groups mapping. Each target's protocol ZIP was built three times as
version `0.10.0-dev.1` from the source commit below, and the builds are
byte-identical. Each passed the reproducibility, descriptor, manifest, and
checksum checks of `eng/Test-ProtocolArtifact.ps1`, and its provenance records
contract major 3, revision 0, and that source commit.

| Target | Protocol ZIP SHA-256 | Descriptor SHA-256 |
| --- | --- | --- |
| 2026.1.0529.7 | `0bf2b7f216a8d1040bd416b4566cf6c53a13f398ec81657dc91b674e0f45cae3` | `aa938e86fc62627d58994953398da3b7dc6a202f5f3c90e778bbddccda0261f8` |
| 2024.1.0508.5 | `5c18656b8148e50b428351a9f67b34d5fba5f43b25eb6ba7f0ad920390d12396` | `15f5e8370436412ed571a13f1a12249eaebcd46f8f63c6db87ee7445b4b0b359` |

Checkpoint version: `0.10.0-dev.1`. Source commit:
`2960705eb3152646513dca2b5d2043cd7cfffb75`.

Any later change in the freeze pull request to the protocol or to the
conformance package needs a new checkpoint, and all three clients must import
it again.

## Before the release

Server 0.10.0 is promoted only after these steps:

1. The six client products publish 0.5.0, and a follow-up change pins the
   published packages, as #230 did for major 2.
2. briosa-examples and the documentation site are updated and validated
   against the candidates.
3. The signed draft release passes an acceptance pass with the actual installer:
   - install, verify, repair, and remove each 0.10.0 package;
   - each 0.10.0 package installed beside 0.9.2, with discovery reporting
     each installation's major and each client selecting a compatible server;
   - Control Center navigation;
   - upgrade from 0.9.2, then an explicit rollback that selects the 0.9.2
     pair.

   The acceptance pass launches no SpatialAnalyzer, worker, or SDK.

Examples and documentation go live once the release artifacts exist.

## Client requirements

The six first-party products are briosa-dotnet, briosa-js, and briosa-py, one
package per exact-SA target each. They adopt major 3 together as 0.5.0. The
shared requirements are in the
[client behavioral contract](../architecture/client-library-behavioral-contract.md)
and the migration pages above.

One operation-specific rule changes only the clients:
`set_robot_machine_parameter` takes `parameter_name` as a required argument
with no default, like `parameter_value`. The client sends an explicit value
unchanged and doesn't check it for emptiness. The server passes the name to
SpatialAnalyzer as given.

The behavioral contract also sets four rules for major 3. Each client checks
[every MP success](../architecture/client-library-behavioral-contract.md#partial-success),
requires [every input the server requires](../architecture/client-library-behavioral-contract.md#reviewed-fixed-defaults),
returns [one denied-operations listing](../architecture/client-library-behavioral-contract.md#compatibility-and-capabilities),
and uses [one transport failure code format](../architecture/client-library-behavioral-contract.md#typed-failures-and-transport-isolation).

Each client change also proves every changed facade default, omission, and
requiredness rule with focused tests, because the shared conformance scenarios
don't exercise those mappings.
