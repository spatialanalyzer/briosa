# SA 2024 licensed probe session 3 — 2026-11-09

The maintainer authorized this third bounded probe session and gave the go-ahead for each run as it started. It ran `tools/Briosa.LicensedProbes` against one licensed SpatialAnalyzer `2024.1.0508.5` instance on 2026-11-09 (UTC), following the [licensed probe session runbook](../licensed-probe-session.md). Claude operated SA through computer use while the maintainer watched; "the operator" below means that. The session answers:

| Issue | Probe |
| --- | --- |
| [#313](https://github.com/spatialanalyzer/briosa/issues/313) | probes 26 and 27 (dialog reporting), probe 29 (Silent after Connect) |
| [#312](https://github.com/spatialanalyzer/briosa/issues/312), [#350](https://github.com/spatialanalyzer/briosa/issues/350) | probe 30, the runtime-select sample |
| [#311](https://github.com/spatialanalyzer/briosa/issues/311) | probe 31, the seed omitted on two more fits |
| [#351](https://github.com/spatialanalyzer/briosa/issues/351) | probe 32, typed partial success |
| [spatialanalyzer/briosa-dotnet#39](https://github.com/spatialanalyzer/briosa-dotnet/issues/39) | probe 33, USMN excluded groups omitted or empty |

The harness and the package were built from main `7d86a3fe` ([#362](https://github.com/spatialanalyzer/briosa/pull/362)). The package is `0.10.0-probe-s3` (`briosa-0.10.0-probe-s3-sa-2024.1.0508.5-win-x64.zip`, SHA-256 `DCC1479656C5557F3A5C3994C502F4B79C685108DF470EB49FA043AC84EB4A6F`); its smoke test passed. Each run used exactly one SDK client and started from a clean SA instance with the pristine fixture job copied in. SA was closed without saving after every run.

- **Public-API phase (runs A, B, C).** The generated gRPC client ran against one packaged Briosa server. The server log shows the activated SDK runtime-verified and the connected SA operator-attested, both as exact matches for `2024.1.0508.5`.
- **Worker phase (run D).** The harness hosted one unchanged `Briosa.Worker.exe` from the same package, with no server. The worker runtime-verified the activated SDK as `2024.1.0508.5`. The operator attested the connected SA release as `2024.1.0508.5` (reference `probe-s3-2024-sa-titlebar`).

These are observations of one bounded session on one machine, not vendor guarantees. The harness records hold only labels, step text, dispositions, MP codes, output retrieval, counts and flags about harness-created objects, and the operator's checkpoint answers. This summary adds the operator's and the server log's observations under the same limits: no argument or returned values, fixture-manifest contents, file paths, or license data. Only this reviewed summary is committed; the raw records stay with the operator. Nothing here is transferred from the SA 2026 session, which has its own record (`targets/2026.1.0529.7/docs/testing/evidence/probe-session-3-2026.1.0529.7-2026-11-09.md`).

## Runs

| Run | Selection | Result |
| --- | --- | --- |
| A (03:53–04:02Z) | probes 30, 31, 32, 26 | Complete: 55 of 55, ended in the planned zero-match stall. |
| B (04:06–04:10Z) | probe 27 | Complete: 39 of 39, ended in the planned template-chart stall. |
| C (04:22–04:23Z) | probe 29 | Complete: 40 of 40. |
| D (04:24–04:26Z) | probe 33 (worker phase) | Complete: 29 of 29. |

- **Server setup.** Admission profile `device`, with `allow` overrides for the eight sampled runtime selects (run A) and Set Interaction Mode; `--Briosa:Worker:ExecutionWatchdogTimeout=00:02:00`; `--Briosa:SpatialAnalyzer:InteractionMode=Unchanged`, or `Silent` for run C. The harness used `--step-timeout-seconds 150`.
- **Interaction mode.** Every run used `--restore-sa-interaction-mode manual`. Every guard and fixture step of every run was observed as planned.
- **Probe 28** is opt-in and was not run in this session.

## Results

MP code `2` is success.

| Probe | Question | Steps | Observed | Reading |
| --- | --- | --- | --- | --- |
| 26 | Does the server report the zero-match dialog? | `p26-baseline`, `p26-zero-match-stall` | Baseline MP code `2`. The stall step returned after 120048 ms: `grpc:Unavailable`, diagnostic `sa-modal-dialog-open`, failure kind `WorkerWatchdogTimeout`, disposition `StartedOutcomeUnknown`, guidance `OperatorInterventionRequired`. | Reported as designed. See [probes 26 and 27](#probes-26-and-27-the-server-reports-the-open-dialog). |
| 27 | Does the server report the template-chart dialog? | `p27-baseline`, `p27-chart-missing-stall` | Baseline MP code `2`. The stall step returned after 120127 ms with the same diagnostic, failure kind, disposition and guidance. No chart was created. | Reported as designed. |
| 29 | Does `InteractionMode=Silent` apply after Connect? | `c29-zero-match-setting`, `p29-chart-missing-setting`, `p29-restore` | Event 1202 at Connect with MP code `2`. Both triggers: MP code `3`, no dialog, no chart. Restore: MP code `2`. | Yes. See [probe 29](#probe-29-silent-applies-after-connect). |
| 30 | Runtime-select getters (eight of the remaining 22) | `p30-*` | Seven: MP code `2`, `selection_count = 1`, `selection_is_requested = true`. Vector name list: MP code `3`. | See [probe 30](#probe-30-seven-getters-return-the-selection). |
| 31 | Fits with the seed omitted (no setter) | `p31-*`, `c31-*` | Seeded control, seed-omitted plane, seed-omitted projected circle: each MP code `2`, and each count found exactly one object. | The seed can be omitted on both fits that #357 changed by analogy. |
| 32 | Typed partial success | `p32-partial`, `c32-partial` | MP code `4`, execution state `PartialSuccess`; the count found the plane. | Both hypotheses matched. |
| 33 | USMN excluded groups omitted or empty | `p33-shipped`, `p33-omit`, `p33-empty` | Control: MP code `2`, outputs 2 of 2. Omit: MP code `2`, outputs 2 of 2, errors equal to the control's. Empty: `ArgumentRejected`. | See [probe 33](#probe-33-skipping-the-setter-works-an-empty-list-is-refused). |

## Probes 26 and 27: the server reports the open dialog

Each stall step had a before-checkpoint (no dialog open) and was the last step of its run.
- **Zero match (run A, about 04:00Z).** SA showed "No objects matched specified criteria." (title "Spatial Analyzer", warning icon, OK only).
- **Template chart (run B, about 04:07Z).** SA showed "Template Chart Not found" (same title, icon and button).

The operator left each dialog open until the step returned at the 2-minute quick budget, then recorded it and dismissed it with OK. The server log agrees with the records: the worker went `Degraded` with a forced termination and diagnostic `sa-modal-dialog-open`, and the execution resolved as `started_outcome_unknown`. Its replay safety was `Safe` for the wildcard selection and `Unsafe` for the chart. After run B, `B277M` > Charts held only the template chart, and `B277` had no Charts folder.

## Probe 29: Silent applies after Connect

The server ran with `InteractionMode=Silent`. At Connect it logged event 1202 `InteractionModeApplied`: SA mode `Silent`, MP mode `HaltOnFailureOrPartialSuccess`, MP dialog mode `AllowApplicationInteraction`, MP code `2`.
- **Zero match.** MP code `3` in 3 ms of SDK time, with no dialog.
- **Template chart.** MP code `3` in 4 ms of SDK time, with no dialog and no chart created.
- **Restore.** `p29-restore` set `Manual`: MP code `2`. The `InteractionMode` registry value read `0` before and after the run, as in session 2.

## Probe 30: seven getters return the selection

For each step SA showed the harness prompt naming the fixture, and the operator pressed **F2**, chose the fixture, clicked **OK**, then pressed **Enter**.

| Step | Getter | Selection window and choice | Result |
| --- | --- | --- | --- |
| `p30-point-list` | `GetPointNameRefListArg` | "Target/Point Selection": checked `B277` > `G1` > `P1` | MP code `2`; the requested fixture |
| `p30-object-list` | `GetCollectionObjectNameRefListArg` | "Object Selection": checked `B277` > Planes > `PA` | MP code `2`; the requested fixture |
| `p30-relationship-list` | `GetCollectionObjectNameRefListArg` | "Select Relationships - Filtered Runtime Select": checked `B277` > Relationships > `R1` | MP code `2`; the requested fixture |
| `p30-vector-list` | `GetVectorNameRefListArg` | F2 refused (below) | MP code `3` |
| `p30-instrument-list` | `GetColInstIdRefListArg` | "Select Instruments" (a list): selected the first USMN fixture instrument | MP code `2`; the requested fixture |
| `p30-instrument` | `GetColInstIdArg` | "Select Instruments" (single select): the same instrument | MP code `2`; the requested fixture |
| `p30-point` | `GetPointNameArg` | "Target/Point Selection" (single select): `B277` > `G1` > `P1` | MP code `2`; the requested fixture |
| `p30-collection` | `GetCollectionNameArg` | "Select Collection": `B277` | MP code `2`; the requested fixture |

Each completed step took 17–30 s, mostly the gesture. The two instrument steps agree.

**The vector name list cannot be completed on this target.** Pressing F2 during the `p30-vector-list` prompt opened a modal "This selection mode does not allow a list." (title "Spatial Analyzer", warning icon, OK only), which the operator dismissed; the prompt stayed. The operator zoomed the graphics view onto the fixture, could not distinguish a vector of `VG1`, and pressed Enter with nothing selected. SA returned MP code `3` (`mp-command-failed`, outputs 0 of 1) after 32.5 s. Whether SA can pick a single vector of a group here stays open. The maintainer decided to retire `construction_operations.make_vector_name_ref_list_runtime_select` at contract major 3 ([#363](https://github.com/spatialanalyzer/briosa/pull/363)), which also removes this step from probe 30.

## Probe 33: skipping the setter works; an empty list is refused

The control is probe 1's shipped USMN request, which excludes `B277::G3`, a group USMN does not use. The harness kept the control's RMS and maximum errors in memory and recorded only whether each variant's errors matched them, within one part per million.
- **`p33-shipped`.** MP code `2`, outputs 2 of 2.
- **`p33-omit` (Groups to be Excluded setter skipped).** MP code `2`, outputs 2 of 2, `errors_equal_control = true`. All three hypotheses matched.
- **`p33-empty` (a list with no items, same SDK binding).** The SDK setter returned false, so `ExecuteStep` was never called: `ArgumentRejected`, `sdk-argument-rejected`, disposition `NotStarted`.

No dialog appeared. So on this target an empty `excluded_groups` can be served by skipping the setter, as the server already does for Quick Align's empty lists, but not by passing an empty list. Excluding `G3` does not change the solution, so the probe cannot tell a skipped setter from one SA kept from the control.

## SDK process ownership (#252)

- **After the watchdog.** After runs A and B, each ended by a worker watchdog, the `SpatialAnalyzerSDK` process activated by that run's worker was still running after SA exited, and postflight reported `RESIDUAL_SDK_PROCESS`. The operator stopped both with the maintainer's OK.
- **After a clean stop.** After runs C and D, no SDK process remained, and postflight passed.

## Operator actions

- **Before the session.**
  - The SDK COM registration was switched from `2026.1.0529.7` to `2024.1.0508.5` with `SpatialAnalyzerSDK.exe /regserver`, the maintainer approving the UAC prompt.
  - The installed Briosa 0.9.2 Control Center for SA 2026 stayed open and untouched, by the maintainer's decision. No server, worker or SDK process was running.
  - The `InteractionMode` registry value read `0`.
- **Every run.**
  - The operator copied the pristine job over the working job, started one SA instance with it (it opened maximized), and ran preflight.
  - Each run ended with `stop-sdk` and the server stopped (public runs), then SA closed without saving. The job file matched the pristine copy every time.
- **Checkpoints and prompts.** The operator answered every checkpoint after a screenshot, performed the probe 30 gestures, and left each planned stall's dialog open until its step returned.
- **After the session.** The registration was switched back to `2026.1.0529.7` with the maintainer's approval and verified byte-identical to the handoff backup.

## Follow-up

- [#313](https://github.com/spatialanalyzer/briosa/issues/313): on this target, the server reports both caller-reachable blocking dialogs as `sa-modal-dialog-open`, and the `InteractionMode=Silent` setting applies after Connect.
- [#312](https://github.com/spatialanalyzer/briosa/issues/312), [#350](https://github.com/spatialanalyzer/briosa/issues/350): seven of the eight sampled getters return the requested fixture with the F2 gesture. The vector name list cannot be completed, and is retired at major 3 in [#363](https://github.com/spatialanalyzer/briosa/pull/363).
- [#311](https://github.com/spatialanalyzer/briosa/issues/311): both fits changed by analogy accept the omitted seed.
- [#351](https://github.com/spatialanalyzer/briosa/issues/351): MP code `4` arrives as a typed partial success, and the plane is created.
- [spatialanalyzer/briosa-dotnet#39](https://github.com/spatialanalyzer/briosa-dotnet/issues/39): USMN runs correctly with the excluded-groups setter skipped, and the SDK refuses an empty list. The server change lands in [#363](https://github.com/spatialanalyzer/briosa/pull/363).
- [#252](https://github.com/spatialanalyzer/briosa/issues/252): the SDK process again outlived each watchdog-terminated worker.
