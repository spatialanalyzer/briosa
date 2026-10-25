# SA 2024 licensed probe session 2 — 2026-10-25

The maintainer authorized this second bounded probe session and confirmed each phase as it started. It ran `tools/Briosa.LicensedProbes` against one licensed SpatialAnalyzer `2024.1.0508.5` instance on 2026-10-25 (UTC), following the [licensed probe session runbook](../licensed-probe-session.md). It answers the follow-ups of the [2026-10-07 session](probe-277-2024.1.0508.5-2026-10-07.md):

| Issue | Probe |
| --- | --- |
| [#309](https://github.com/spatialanalyzer/briosa/issues/309) | probe 11 in isolation |
| [#311](https://github.com/spatialanalyzer/briosa/issues/311) | the probe 2 and probe 17 fit |
| [#312](https://github.com/spatialanalyzer/briosa/issues/312) | the probe 22 gesture |
| [#307](https://github.com/spatialanalyzer/briosa/issues/307) and [#310](https://github.com/spatialanalyzer/briosa/issues/310) | the R0 control, probe 24 |
| [#313](https://github.com/spatialanalyzer/briosa/issues/313) | Silent verification, probe 25 |

The harness and the package were built from main `21a621b` ([#341](https://github.com/spatialanalyzer/briosa/pull/341)); the package is `0.9.3-probe313`. Before the runs, the operator switched the SDK COM registration to the 2024.1.0508.5 install; the maintainer approved the UAC prompt. Each phase used exactly one SDK client and started from a clean SA instance with the pristine fixture job copied in. SA was closed without saving after every run.

- **Public-API phase.** The generated gRPC client ran against one packaged Briosa server. The server log shows the activated SDK runtime-verified and the connected SA operator-attested, both as exact matches for `2024.1.0508.5`.
- **Worker phase.** The harness hosted one unchanged `Briosa.Worker.exe` from the same package, with no server. The worker runtime-verified the activated SDK as `2024.1.0508.5`. The operator attested the connected SA release as `2024.1.0508.5` (reference `probe313-2024-sa-titlebar`).

These are observations of one bounded session on one machine, not vendor guarantees. The harness records hold only labels, step text, dispositions, MP codes, output retrieval, counts and flags about harness-created objects, and the operator's checkpoint answers. This summary adds the operator's observations under the same limits: no argument or returned values, fixture-manifest contents, file paths, or license data. Only this reviewed summary is committed; the raw records stay with the operator. Nothing here is transferred from the SA 2026 session, which has its own record (`targets/2026.1.0529.7/docs/testing/evidence/probe-session-2-2026.1.0529.7-2026-10-25.md`).

## Runs

| Run | Excluded | Result |
| --- | --- | --- |
| Public API, run 1 (02:28–02:38Z) | none | Stopped at `p11` with `outcome-unknown-do-not-replay`; 39 of 70 steps observed. |
| Public API, run 2 (02:40–02:45Z) | probe 11 | Complete: 68 of 68 steps observed, 2 excluded. |
| Worker (11:52–11:55Z) | none | Complete: 77 of 77 steps observed. |

- **Setup common to the public runs.**
  - The server used admission profile `device`, plus `allow` overrides for the runtime select, Ask for String and Set Interaction Mode.
  - It also used `--Briosa:Worker:ExecutionWatchdogTimeout=00:02:00`, which the maintainer approved.
  - The harness used `--step-timeout-seconds 150`.
- **Interaction mode.** Probe 25 used `--restore-sa-interaction-mode manual`, the maintainer's decision. Every guard and fixture step of every run was observed as planned.

### Exclusion

- **Probe 11, public run 2 (`p11`, `c11`).** Public run 1 stopped at `p11` with an unknown outcome; see [probe 11](#probe-11-the-substitution-stalls-in-isolation). Sending it again would have stalled for 600 s and blocked every later probe. Run 1 is probe 11's record.

## Results

MP code `2` is success. Steps are from public run 2 and the worker run, except probe 11.

| Probe | Question | Steps | Observed | Reading |
| --- | --- | --- | --- | --- |
| 1 | USMN nominals group: value, omit, blank | `p01-value`; `p01-shipped`, `p01-omit`, `p01-blank` | All four: MP code `2`, outputs 2 of 2. | Same as 2026-10-07 on this target. |
| 2 | Fit seed geometry, now fitting the coplanar group `GFIT` | `p02-value`, `c02-value`; `p02-shipped`, `p02-omit`, `p02-blank` and their counts | All four fits: MP code `2`, and exactly one plane with the fit's name each time. | With a coplanar group, the fit succeeds. A seed value, a skipped seed setter, and a blank seed are all accepted. Whether the seed changed the fit is not distinguished. |
| 3 | Chart template chart | `p03-value`; `p03-shipped`, `p03-omit`, `p03-blank` | All four: MP code `2`. | Same as 2026-10-07 on this target. |
| 4 | UI profile file | `p04-value`; `p04-*` | All four: MP code `2`. | Same as 2026-10-07 on this target. |
| 5, 6 | Bounding plane names | `p05-06-*`, `p05-*`, `p06-*` and counts | Value: both planes created. Omit: one plane with SA's default name. Blank: MP code `3` after a blocking "Blank 'High' Plane Not Allowed!" or "Blank 'Low' Plane Not Allowed!" dialog, with no plane created. | Same as 2026-10-07 on this target. |
| 11 | Mesh Orientation via `SetStringArg`, in isolation | `p11` (run 1) | SA raised "Encountered an improper argument." The MP never returned: `Indeterminate` after the server's 600 s watchdog. | The substitution itself raises the dialog and stalls. See [probe 11](#probe-11-the-substitution-stalls-in-isolation). |
| 12, 13 | Getters after Ensure Unique | `p12`, `p13` | MP code `2`, outputs 1 of 1, and the name differs from the input. | Same as 2026-10-07 on this target. |
| 17 | Label case: `Group to Fit` | `p17-recased`, `c17-recased` | MP code `2`, and one plane with the step's name. | The recased label was not rejected, and the fit created its plane. Whether SA applied the recased argument or reused an earlier value is not distinguished. |
| 18 | Label case: `Use number suffix?` | `p18-*` | Both: MP code `2`, outputs 1 of 1. | Same as 2026-10-07 on this target. |
| 19–21 | SDK versus documentation step text | `p19-*`, `p20-*`, `p21-*` | All MP code `2`, except `p20-doc-text` (curly apostrophe), which returned MP code `-1`. | Same as 2026-10-07 on this target. |
| 22 | Runtime-select vector group getter, with the documented gesture | `p22` | MP code `2` after 41 s. Output 0 of 1 (`OutputRetrievalFailed`, `sdk-output-retrieval-failed`). | The gesture completes the selection, but the getter returns nothing. See [probe 22](#probe-22-the-gesture-works-the-getter-does-not). |
| 23 | Vector group fractional percentages | `p23` | MP code `2`, outputs 17 of 17, both percentages fractional. | Same as 2026-10-07 on this target. |
| 24 | Radial delete with the `Cloud Names` list (R0, R0a) | `c24-*`, `p24-r0`, `p24-r0a` | Element typed `Cloud`: MP code `2`, 8 points before, 7 after. Element typed `Any`: MP code `2`, 8 before, 7 after. The sentinel held 8 before and 8 after. No dialogs. | The cloud reference list works on this target. See [probe 24](#probe-24-the-cloud-reference-list-works). |
| 25 | SA Interaction Mode Silent | `p25-*`, `c25-*` | See [probe 25](#probe-25-silent-suppresses-the-blocking-dialogs). | Silent suppresses all three blocking dialogs. Prompts still work. |

## Probe 11: the substitution stalls in isolation

- **Setup.** Probe 11 ran first after the fixtures. No other probe had run in this SA instance, and at the before-checkpoint the operator confirmed that no dialog was open.
- **The dialog.** Within about 35 s of dispatch, SA showed a modal "Encountered an improper argument." dialog: title "Spatial Analyzer", error icon, OK only. The operator dismissed it at about 50 s.
- **No result.** No MP result followed. Both before and after the dismissal, SA's CPU was idle while the `SpatialAnalyzerSDK` process used about one full core.
- **Watchdog.** The server's long-running watchdog fired at 600 s: `Indeterminate`, after 600061 ms, and `stop-sdk` reported `WATCHDOG_TERMINATED`.

The 2026-10-07 stall on this target was attributed to a dialog left by probes 8 to 10. This run shows the substitution itself raises the same dialog, and the MP never completes.

## Probe 22: the gesture works; the getter does not

The SA 2024 User Manual (chapter 6, "F2 Selection") gives the gesture for a selection prompt. During `p22` the operator:
1. pressed **F2**, which opened the "Select Vector Group" window (on this target it also has a "By Color" button);
2. expanded `B277` > Vector Groups and checked `VG1`;
3. clicked **OK**, then pressed **Enter**.

The prompt closed, and SA returned MP code `2`; on 2026-10-07 the operator's tree selection and Enter returned MP code `3`. The manual also says that selecting in the tree needs a double-click.

The 0.9.1 getter `GetCollectionVectorGroupNameRefListArg` then retrieved nothing (`sdk-output-retrieval-failed`). The same happened under Silent. On this target, the operation completes but cannot return its selection.

## Probe 24: the cloud reference list works

- **Request.** The radial delete sent the same `SetCollectionObjectNameRefListArg "Cloud Names"` value form as the deleted X Y Z range operation. It deleted the points within 1 unit of `D1`.
- **Result.** Each run deleted exactly one point from its own cloud: MP code `2`, 8 points before, 7 after. This held both with the element typed `Cloud` (R0) and typed `Any` (R0a).
- **Containment.** The sentinel cloud held 8 points before and after both deletes, and no dialog appeared.

So on this target the `Cloud Names` reference list is not what failed in probes 15 and 16 (2026-10-07). This supports the reading that their MP code `-1` came from the NOT_SUPPORTED bound setters. It likewise supports the same reading for probes 9 and 10 ([#310](https://github.com/spatialanalyzer/briosa/issues/310)).

## Probe 25: Silent suppresses the blocking dialogs

Each step probe 25 sent to set the mode used MP mode `Halt on Failure or Partial Success` and MP dialog mode `Allow Application Interaction`.
1. **Baseline.** `p25-baseline` set SA mode `Manual`.
2. **Before Silent.** Each trigger ran once.
3. **Silent.** `p25-silent` set `Silent`, and each trigger ran again.
4. **Restore.** `p25-restore` set `Manual` again.

All three mode steps returned MP code `2`.

| Trigger | Before Silent | Under Silent |
| --- | --- | --- |
| Wildcard selection that matches nothing (public `c25-zero-match-*`; worker `c25-blank-*-silent-named`) | "No objects matched specified criteria." (warning icon, OK only) blocked the MP until dismissed. MP code `3` after 14.0 s. | No dialog. MP code `3` in 2–5 ms. |
| Template chart that matches no chart (`p25-chart-missing-*`) | "Template Chart Not found" (warning icon, OK only) blocked the MP until dismissed. MP code `3` after 16.1 s. This is the first observation of this dialog on this target. | No dialog. MP code `3` in 5 ms. |
| Blank High plane name (worker, `p25-blank-high-*`) | "Blank 'High' Plane Not Allowed!" blocked the MP until dismissed. MP code `3` after 13.4 s. | No dialog. MP code `3` in 4 ms. |
| Blank Low plane name (worker, `p25-blank-low-*`) | "Blank 'Low' Plane Not Allowed!" blocked the MP until dismissed. MP code `3` after 12.6 s. | No dialog. MP code `3` in 4 ms. |
| Runtime select (`p22`; `p25-runtime-select-silent`) | Prompt shown. With the probe 22 gesture: MP code `2`, getter failed. | Prompt still shown. Same gesture, same result. |
| Ask for String (`p25-ask-*`) | "Input" prompt shown. The operator's entry came back: MP code `2`, `answer_is_operator_entry = true`. | Prompt still shown. Same result. |

- **Side effects.**
  - Neither missing-template chart was created, before or under Silent. After the run, `B277M` > Charts held only the template chart and the `p03-value` chart.
  - Under Silent, a blank plane name created neither a harness-named plane nor another default-named plane. The default-name counts stayed at 1.
- **Reading.** On this target, Silent turns each of the three state-triggered blocking dialogs into an immediate failure (MP code `3`) with no side effect. It does not suppress operator prompts, which still work.
- **The persisted value did not change.** The value named `InteractionMode` under `HKCU\Software\New River Kinematics\Spatial Analyzer\Settings` read `0` throughout, including after `p25-silent`. This record therefore cannot say whether, or when, SA writes that value, or which mode `0` means. Its key path names no SA version.

## SDK process ownership (#252)

- **After the watchdog.** After the `p11` watchdog termination in public run 1, the `SpatialAnalyzerSDK` process activated by that run's worker was still running more than 10 s after SA exited. The operator stopped it.
- **After a clean stop.** After the clean `stop-sdk` of public run 2, and after the worker run's graceful stop, no SDK process remained.

## Operator actions

- **Before the runs.** The SDK COM registration was switched from 2026.1.0529.7 to 2024.1.0508.5 (`SpatialAnalyzerSDK.exe /regserver`, UAC approved by the maintainer).
- **Every run.**
  - The operator closed every SA, Briosa, and SDK process, copied the pristine job over the working job, started one SA instance with it, and ran preflight.
  - Each run ended with postflight, where applicable, and SA closed without saving. The job file still matched the pristine copy every time.
- **Checkpoints and prompts.**
  - The operator answered every checkpoint after a screenshot.
  - Dialogs that blocked an MP were dismissed with OK during the step. That covers the four before-Silent triggers above, and the six expected dialogs of the worker run's probes 5 and 6.
  - At probes 22 and 25, the operator performed the F2 gesture and entered `B277-entered`.
- **Public run 1 recovery.** The operator ran `stop-sdk` and stopped the server, closed SA without saving, and stopped the leftover SDK process.
- **After the worker run.**
  - The operator restored the active UI profile to "< Factory Defaults >".
  - The operator switched the SDK COM registration back to 2026.1.0529.7. The first UAC prompt was cancelled; with the maintainer's OK, the switch was retried and approved. The three exported keys were verified byte-identical to the handoff backup.

## Follow-up

- [#309](https://github.com/spatialanalyzer/briosa/issues/309): probe 11 is answered on this target, with the same result as on 2026, observed separately. The disposition is the maintainer's decision.
- [#311](https://github.com/spatialanalyzer/briosa/issues/311): the fit request is fixed, and probes 2 and 17 are answered.
- [#312](https://github.com/spatialanalyzer/briosa/issues/312): the gesture is confirmed. The vector group runtime-select getter returns nothing after a completed selection; this is a new finding.
- [#313](https://github.com/spatialanalyzer/briosa/issues/313): Silent is verified on this target.
- [#307](https://github.com/spatialanalyzer/briosa/issues/307), [#310](https://github.com/spatialanalyzer/briosa/issues/310): R0 shows the cloud reference list works on this target.
- [#252](https://github.com/spatialanalyzer/briosa/issues/252): the SDK process again outlived a watchdog-terminated worker.
