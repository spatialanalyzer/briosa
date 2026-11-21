# Contract major 3

Status: released. Both exact-SA targets (`2026.1.0529.7` and `2024.1.0508.5`)
declare behavioral contract major 3, revision 0.
[Server 0.10.0](https://github.com/spatialanalyzer/briosa/releases/tag/v0.10.0)
was published on 2026-11-21 at 03:03 UTC (tag `v0.10.0` at
`ee16784d9fdaf67ce7d7bb0b68609a98fe403e1c`), with the first-party clients at
0.5.0 ([.NET](https://github.com/spatialanalyzer/briosa-dotnet/releases/tag/v0.5.0),
[JavaScript/TypeScript](https://github.com/spatialanalyzer/briosa-js/releases/tag/v0.5.0),
and [Python](https://github.com/spatialanalyzer/briosa-py/releases/tag/v0.5.0)).
Both signed server packages are published in the
[shared catalog](../../eng/publishing/catalog.json)
([briosa#390](https://github.com/spatialanalyzer/briosa/pull/390)) on
`https://briosa.dev/downloads`, and the release documentation and migration
guides are live on [briosa.dev](https://briosa.dev/)
([briosa-docs#134](https://github.com/spatialanalyzer/briosa-docs/pull/134)).

Wave 2b shipped as one planned behavioral release
([#244](https://github.com/spatialanalyzer/briosa/issues/244)). That decision
asked for each change to be classified against the accepted contract, 2.0,
before the version was assigned. This page is that record. It also holds the
frozen protocol checkpoint that the 0.5.0 clients built against, and the
release and validation record for Server 0.10.0.

## Classification

Several changes break callers of contract 2.0, so the release needed a new
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

The 0.5.0 clients built against a frozen checkpoint of the freeze change, as
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

The released [Server 0.10.0](https://github.com/spatialanalyzer/briosa/releases/tag/v0.10.0)
protocol is identical to this frozen checkpoint. In the published release
protocol archives from `ee16784d9fdaf67ce7d7bb0b68609a98fe403e1c`, each
archive's `descriptor/briosa.protoset` hashes to the checkpoint descriptor
SHA-256 above (`aa938e86fc62627d58994953398da3b7dc6a202f5f3c90e778bbddccda0261f8`
for `2026.1.0529.7` and
`15f5e8370436412ed571a13f1a12249eaebcd46f8f63c6db87ee7445b4b0b359` for
`2024.1.0508.5`), and each provenance file records contract major 3, revision 0,
and that release revision. The release protocol ZIP digests differ from the
checkpoint's (`8dfb579434997ee4d6b07500810575d16b32499ccff57b4b4c1f278afa2c16f8`
for `2026.1.0529.7` and
`cfec0dfad6f6d388ca0d924ff7cf5985b36c2e85c1abe608c791d184a6e8c372` for
`2024.1.0508.5`), as expected, because each archive manifest carries the release
version (`0.10.0`) and source revision.

## Release record

Server 0.10.0 was promoted after completing the release sequence below:

1. **0.5.0 clients published and pinned (done, 2026-11-13 and 2026-11-20).**
   All six 0.5.0 client packages (.NET, JavaScript/TypeScript, and Python, one
   per exact target) were published to NuGet, npm, and PyPI on 2026-11-13, with
   their GitHub `v0.5.0` releases published on 2026-11-20:
   [briosa-dotnet v0.5.0](https://github.com/spatialanalyzer/briosa-dotnet/releases/tag/v0.5.0)
   (`bc722444`),
   [briosa-js v0.5.0](https://github.com/spatialanalyzer/briosa-js/releases/tag/v0.5.0)
   (`b94fce3f`), and
   [briosa-py v0.5.0](https://github.com/spatialanalyzer/briosa-py/releases/tag/v0.5.0)
   (`cae6e535`).
   [briosa#381](https://github.com/spatialanalyzer/briosa/pull/381) (merged
   2026-11-20 as `ee16784d9fdaf67ce7d7bb0b68609a98fe403e1c`) pinned the six
   published registry packages in
   [`compatibility/retained-clients.json`](../../compatibility/retained-clients.json)
   for the Server 0.10.0 release gate.
2. **Examples and documentation updated and validated (done; finalized
   2026-11-21).** Both
   [briosa-examples#16](https://github.com/spatialanalyzer/briosa-examples/pull/16)
   and [briosa-docs#134](https://github.com/spatialanalyzer/briosa-docs/pull/134)
   were updated and validated as drafts against the frozen checkpoint and
   candidates before Server 0.10.0 was promoted, then finalized and merged
   against the published release assets (below).
3. **Release workflow build and signed-draft acceptance pass (done, 2026-11-20
   and 2026-11-21).**
   - **Release workflow (2026-11-20):**
     [Release run 44744696173](https://github.com/spatialanalyzer/briosa/actions/runs/44744696173)
     (attempt 2; attempt 1 failed in the `Build SA 2024.1.0508.5 products` job)
     built both targets from `ee16784d9fdaf67ce7d7bb0b68609a98fe403e1c`,
     verified reproducibility by building each archive twice, signed all 14
     first-party binaries per product with timestamped Windows signatures, and
     created the draft `v0.10.0` release. It ran both targets' server test
     suites, the protocol comparison against `v0.9.2` (breaking only through
     the nine reviewed removals), packaged client scenarios with a fake SDK,
     and the retained-client gate (six published 0.5.0 packages passed 15
     scenarios each; six published 0.4.0 packages rejected the server before
     launch; 0.2.0 and 0.3.0 remain retired per
     [briosa#250](https://github.com/spatialanalyzer/briosa/issues/250)). The
     run executed against no licensed SpatialAnalyzer.
   - **Acceptance pass on the signed draft packages (2026-11-20 and
     2026-11-21):** Before publication, the signed draft 0.10.0 packages were
     checked with the signed
     [Installer 0.4.1](https://github.com/spatialanalyzer/briosa-installer/releases/tag/v0.4.1)
     (`d5c50d13`) and the published 0.5.0 and 0.4.0 clients from a signed local
     catalog on a private store. The pass launched no SpatialAnalyzer, worker,
     or SDK:
     - *Install, verify, repair, and remove:* With the CLI, both targets'
       0.10.0 packages were installed beside 0.9.2, verified, repaired after a
       single-file modification (`verify` exited 6 and `repair` restored
       payload digests equal to install time; `2026.1.0529.7` on 2026-11-20 and
       `2024.1.0508.5` in a single-package CLI run on 2026-11-21 at 02:22 UTC),
       and removed, leaving `HKCU`/`HKLM` `Software\Briosa` byte-identical to
       the baseline snapshot. With the Installer GUI app, `0.10.0` for
       `2026.1.0529.7` was installed, verified, repaired, verified, and
       removed; `2024.1.0508.5` was exercised with the CLI only, not the GUI
       app.
     - *Side-by-side discovery and client selection:* With 0.9.2 and 0.10.0
       installed side by side for both targets via the CLI, each 0.9.2 receipt
       and manifest reported major 2, each 0.10.0 receipt and manifest reported
       major 3, and the 0.9.2 payloads stayed byte-identical. In all three
       languages, the 0.5.0 clients selected 0.10.0 for their own target and
       the 0.4.0 clients selected 0.9.2 for their own target, each rejecting
       the other major and the other target (`server-installation-incompatible`)
       both by explicit installation ID and in a store-only scan. Client probes
       were not re-run against a GUI-created install.
     - *Control Center:* Control Center opened for 0.10.0 on both targets and
       for 0.9.2 on `2026.1.0529.7` without starting a server; Control Center
       was not opened for 0.9.2 on `2024.1.0508.5`.
     - *Side-by-side addition and removal (planned upgrade/rollback):* Because
       contract majors 2 and 3 install side by side rather than upgrading in
       place, no in-place upgrade or separate installer rollback operation was
       run. Adding 0.10.0 beside 0.9.2 left 0.9.2 unchanged, and removing
       0.10.0 left 0.9.2 byte-identical and still selected by the 0.4.0 clients
       while the 0.5.0 clients found no compatible server.
   - **Publication (2026-11-21 at 03:03 UTC):**
     [Server 0.10.0](https://github.com/spatialanalyzer/briosa/releases/tag/v0.10.0)
     was published for both targets, creating tag `v0.10.0` at
     `ee16784d9fdaf67ce7d7bb0b68609a98fe403e1c`.

Post-publication steps completed on 2026-11-21, and remaining open follow-ups:

- **Shared catalog and R2 publication (done, 2026-11-21):**
  [briosa#390](https://github.com/spatialanalyzer/briosa/pull/390) (merged as
  `810f60d5`) added both signed 0.10.0 packages to
  [`eng/publishing/catalog.json`](../../eng/publishing/catalog.json), and
  [publish-r2 run 44919687133](https://github.com/spatialanalyzer/briosa/actions/runs/44919687133)
  published them to R2. An independent read-only public check against
  `https://briosa.dev/downloads` confirmed that the public catalog equals
  `main`, its signature is valid, and both 0.10.0 packages equal the GitHub
  release assets
  ([closing comment on briosa#389](https://github.com/spatialanalyzer/briosa/issues/389#issuecomment-7065335981)).
- **Examples (done, 2026-11-21):**
  [briosa-examples#16](https://github.com/spatialanalyzer/briosa-examples/pull/16)
  (merged as `f5d9397b`) pinned the published 0.10.0 protocol archive and
  passed 51 fake-server scenarios (no SpatialAnalyzer).
- **Documentation (done, 2026-11-21):**
  [briosa-docs#134](https://github.com/spatialanalyzer/briosa-docs/pull/134)
  (merged as `20714d2`) updated `briosa.dev` for Server 0.10.0 and the 0.5.0
  clients, and deployed via
  [run 44924192930](https://github.com/spatialanalyzer/briosa-docs/actions/runs/44924192930).
- **Compatibility matrix and README (done, 2026-11-21):**
  [briosa#392](https://github.com/spatialanalyzer/briosa/pull/392) (merged as
  `4d846d2`) recorded the 12 released-package pairs from
  [run 44744696173](https://github.com/spatialanalyzer/briosa/actions/runs/44744696173)
  attempt 2 in [`compatibility/matrix.json`](../../compatibility/matrix.json)
  (six 0.5.0 clients passed 15 fake-SDK scenarios each; six 0.4.0 clients
  rejected the server before launch) and updated [`README.md`](../../README.md).
- **Installer candidate check (open):** The acceptance pass used signed
  [Installer 0.4.1](https://github.com/spatialanalyzer/briosa-installer/releases/tag/v0.4.1).
  Adding the 0.10.0 packages to the installer's own candidate check and moving
  its catalog-tool pin remain open in
  [briosa-installer#43](https://github.com/spatialanalyzer/briosa-installer/issues/43).
- **Governance (open):** Recording the decided retained-client window for the
  release gate remains open in
  [governance#5](https://github.com/spatialanalyzer/governance/issues/5).

### Licensed evidence and coverage limits

The release workflow ran against no licensed SpatialAnalyzer, and the
acceptance pass launched no SpatialAnalyzer, worker, or SDK. Three licensed
probe sessions on development builds across both targets
([briosa#306](https://github.com/spatialanalyzer/briosa/pull/306),
[briosa#349](https://github.com/spatialanalyzer/briosa/pull/349), and
[briosa#370](https://github.com/spatialanalyzer/briosa/pull/370)) provide the
licensed evidence for the nine operation removals, the inputs now left out when
absent, and the `Silent` interaction mode; those sessions also observed one
typed partial-success fit result on each target. The admission and lifecycle
changes are covered by portable tests only.

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
