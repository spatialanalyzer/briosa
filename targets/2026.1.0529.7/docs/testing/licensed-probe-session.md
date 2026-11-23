# Bounded licensed probe session (#277)

`tools/Briosa.LicensedProbes` prepares the bounded licensed session from
[#277](https://github.com/spatialanalyzer/briosa/issues/277) for SpatialAnalyzer
`2026.1.0529.7`. It settles specific binding questions that portable tests cannot
answer. It is not broad validation of the operation surface.

The maintainer approved the probe list on 2026-10-03, and the second session's
changes (probes 24 and 25, and the re-runs of probes 2, 11, 17, and 22) on
2026-10-19. Probes 26 to 33 prepare the third session, which does not run probe 28:
the `sa-modal-dialog-open`
report and the `InteractionMode` setting
([#313](https://github.com/spatialanalyzer/briosa/issues/313)), a sample of the
runtime-select getters ([#350](https://github.com/spatialanalyzer/briosa/issues/350)),
the omitted fit seed ([#311](https://github.com/spatialanalyzer/briosa/issues/311)),
the typed partial success
([#351](https://github.com/spatialanalyzer/briosa/issues/351)), and USMN's excluded
groups omitted or empty
([briosa-dotnet#39](https://github.com/spatialanalyzer/briosa-dotnet/issues/39)). Probes 34 to 37 and the
`--ownership` and `--startup-timing` commands prepare the fourth session
([#402](https://github.com/spatialanalyzer/briosa/issues/402)); see
[Licensed session 4](#licensed-session-4-402). **Attaching to a running
SpatialAnalyzer still needs a separate maintainer go-ahead at session time**
(AGENTS.md validation level 5). Until then, use the dry run only. The harness is
never packaged, never part of a licensed-runner payload, and never run by
ordinary builds or tests. CI only compiles it and runs its portable tests.

## Design

The session has two phases. Each phase uses exactly one SDK client and starts
from a clean SA instance. The phases never overlap.

| Phase | Client | Runs |
| --- | --- | --- |
| `public-api` | The generated gRPC client against one packaged Briosa server. The server's single worker is the only SDK client. | Every probe the public API can express. The harness sends only shipped sequences. |
| `worker` | The harness hosts one unchanged `Briosa.Worker.exe` from the same package over the existing private control protocol. No Briosa server runs. | Only what the public API intentionally cannot express: a skipped setter, a blank value or empty list the API rejects, a recased label, alternate step text. |

The public path follows the existing SmokeClient licensed-scenario pattern: the
target-specific confirmation flag, a loopback-only address, an exact target
identity check, required MP readiness, typed errors read from the trailer, and
no retry. It does not connect or stop the SDK. The operator does that with the
maintained lifecycle client.

The worker path is a minimal host, like the server supervisor. The worker owns
the only SDK client on its single STA. It reports the runtime-verified activated
SDK version, which must equal the target. It attaches with one `ConnectEx`
attempt and proves execution readiness with the bounded `Get Working Directory`
probe before any step. The connected SA release cannot be verified at runtime,
so the operator attests it with a non-sensitive reference. The worker reports
diagnostic codes only, never raw exception text.

Every worker command starts from the target's own shipped command builder for
the same request that the public phase uses. At most one explicit variation is
then applied: omit one setter, blank one value with the same SDK binding, send
one list with no items, recase one label, or replace the step text. The dry run
prints each sequence exactly as it will be sent, using labels only and never
values.

| # | Question | Path | Steps |
| --- | --- | --- | --- |
| 1 | USMN nominals group: omit, blank, or value | value: public. Omit and blank: worker (the API requires a value). | `p01-value`, `p01-shipped`, `p01-omit`, `p01-blank` |
| 2 | Fit seed geometry | value: public. Omit and blank: worker. | `p02-*` and the `c02-*` result-plane counts |
| 3 | Chart template chart | value: public. Omit and blank: worker. | `p03-*` |
| 4 | UI profile file | value: public. Omit and blank: worker. | `p04-*` |
| 5, 6 | Bounding plane High and Low names (SDK defaults `HighPlane` and `LowPlane`) | value: public. Omit and blank: worker. | `p05-06-*`, `p05-*`, `p06-*`, plus default-name and harness-name counts |
| 7 to 10 | Retired: the callout view list getter and setter, Circle Line Mode, and mirror frame plane substitutions | none | none (see below) |
| 11 | Retired: the Mesh Orientation substitution | none | none (see below) |
| 12, 13 | Getters without evidence | public | `p12`, `p13` |
| 14 | Retired: the instrument reference list getter after a wildcard add | none | none (see below) |
| 15, 16 | Retired: the restored `DeleteCloudPointsByXYZRange` bounds | none | none (see below) |
| 17, 18 | Label case sensitivity | worker (the API cannot recase a label) | `p17-recased` (control `p02-shipped`), `p18-shipped`, `p18-recased` |
| 19 to 21 | SDK step text (#246) | worker (both texts on both targets) | `p19-*`, `p20-*`, `p21-*` |
| 22 | Retired: the vector group runtime-select getter | none | none (see below) |
| 23 | Vector group fractional percentages (0.9.1) | public | `p23` |
| 24 | R0 and R0a: does the radial delete accept the `Cloud Names` reference list that the deleted X Y Z range operation sent ([#307](https://github.com/spatialanalyzer/briosa/issues/307), [#310](https://github.com/spatialanalyzer/briosa/issues/310))? | public; destructive on disposable clouds only | `c24-*`, `p24-r0`, `p24-r0a` |
| 25 | Does SA Interaction Mode Silent suppress the observed blocking dialogs, and do prompts still work under it ([#313](https://github.com/spatialanalyzer/briosa/issues/313))? | public: zero-match wildcard, missing template chart, Ask for String. Worker: blank High and Low plane names. | `p25-*`, `c25-*`; see [Probe 25](#probe-25-interaction-mode) |
| 26, 27 | With the setting `Unchanged` and SA in Manual, does the server report `sa-modal-dialog-open` when a dialog blocks an MP until its watchdog fires (#313)? 26: zero-match wildcard. 27: template chart that matches no chart. | public; opt-in; each ends its run in a planned stall | `p26-*`, `p27-*`; see [Probes 26 to 29](#probes-26-to-29-modal-dialogs-and-the-interactionmode-setting) |
| 28 | Does the server's detector see the blank High plane name dialog (#313)? The public API rejects a blank name, so the server cannot send it. | worker; opt-in; ends its run in a planned stall; not part of the third session | `p28-*` |
| 29 | Does the server setting `InteractionMode=Silent` apply Silent after Connect, so that the public triggers fail at once (#313)? | public; opt-in; runs alone on a server started with the setting | `c29-*`, `p29-*` |
| 30 | Do the runtime-select getters return the operator's selection ([#350](https://github.com/spatialanalyzer/briosa/issues/350), [#312](https://github.com/spatialanalyzer/briosa/issues/312))? A sample: one operation per list getter kind and three single-value getters. | public; the operator selects | `p30-*`; see [Probe 30](#probe-30-runtime-select-sample) |
| 31 | Do Fit Geometry to Points and Fit Geometry to Point Group Projected to Plane accept the omitted seed ([#311](https://github.com/spatialanalyzer/briosa/issues/311))? | public (the API omits the setter when the seed is absent) | `p31-*`, `c31-*` |
| 32 | Is MP code 4 reported end to end as a typed partial success ([#351](https://github.com/spatialanalyzer/briosa/issues/351))? | public | `p32-partial`, `c32-partial` |
| 33 | Does SA run USMN correctly with `Groups to be Excluded` omitted or empty ([briosa-dotnet#39](https://github.com/spatialanalyzer/briosa-dotnet/issues/39))? The public API never sends SA an empty list. | worker | `p33-*`; see [Probe 33](#probe-33-usmn-excluded-groups) |
| 34 | How large are representative list results where the response budget applies ([#245](https://github.com/spatialanalyzer/briosa/issues/245), [#399](https://github.com/spatialanalyzer/briosa/issues/399))? | public and worker; opt-in | `p34-*`, `c34-*`; see [Probes 34 to 37](#probes-34-to-37) |
| 35 | Do the five corrected callout text setters work ([#220](https://github.com/spatialanalyzer/briosa/issues/220))? | public; opt-in; needs `callout_view` | `p35-*`, `c35-*` |
| 36 | Does the corrected TCP uncertainty notes getter work (#220)? | public; opt-in; needs `tcp_fixture` | `p36-notes` |
| 37 | Does the corrected robot `Machine ID` reach Get Robot/Machine Parameter (#220)? | public; opt-in; needs `robot_machine` | `p37-parameter` |

**Probe 2 fixture ([#311](https://github.com/spatialanalyzer/briosa/issues/311)).** The fits
in probes 2 and 17 fit the coplanar group `GFIT` (the four corners of `G1`). The
2026-10-07 session fitted `G1`, whose two points at z = ±20 exceed the default
fit profile's tolerance. Both targets' MP documentation lists that case as
`PARTIAL SUCCESS` ("the fit was successful, but the tolerance was exceeded"),
which is the MP code `4` that session recorded. A blank `Fit Profile Name`, which
the shipped mapping sends when the field is absent, selects the default profile.

**Runtime-select gesture ([#312](https://github.com/spatialanalyzer/briosa/issues/312)).**
SA's help (`Selection.htm`, `F2Selection.htm`) says that a click in the main tree
does not add to a prompt's selection. Licensed session 2 completed the retired
probe 22's selection with **F2**, a check in the selection tree, **OK**, then
**Enter**. Probe 30's prompts ask for the same gesture.

**Partial success trigger (#351).** Probe 32 fits `G1`, as the 2026-10-07 session
did, so that the tolerance is exceeded. That fit returned MP code `4`
(`PARTIAL SUCCESS`) and created its plane, the only code-4 trigger seen on both
targets. The shipped server now reports it as a success with execution state
`MP_EXECUTION_STATE_PARTIAL_SUCCESS`.

Step texts for #19 to #21 are the SDK View Code forms, which are authoritative
per #246: `Compute Group to Group Orientation (Rx,Ry,Rz)`,
`Angle Between Two Planes' normals` (straight apostrophe), and
`Show / Hide Points`. They are paired with the SA 2026 installed-documentation
forms (`(Rx, Ry, Rz)`, a curly apostrophe, and `Show/Hide Points`). Each record
states which text this target ships.

The SDK-ownership probes from #252 (SDK process identity, normal release, worker
crash, watchdog kill, parent death, a competing client) are not probe steps. The
fourth session runs them as separate `--ownership` scenarios, each with its own
authorization; see [Licensed session 4](#licensed-session-4-402).

**Retired probes.** Probes 7 to 10 and 14 to 16 answered their questions in the
2026-10-07 session (see [Recorded sessions](#recorded-sessions)): on both exact targets the
substituted getters returned nothing and the substituted setters were rejected
with MP code `-1`. Their operations were removed for contract major 3
([#308](https://github.com/spatialanalyzer/briosa/issues/308),
[#310](https://github.com/spatialanalyzer/briosa/issues/310),
[#307](https://github.com/spatialanalyzer/briosa/issues/307)), so the plan no
longer sends them and `--exclude-probe` refuses their numbers. The numbers are
not reused, so the 2026-10-07 records keep their meaning. Retiring them also
removes the "Encountered an improper argument." dialog that preceded `p11` in
that session.

Probes 11 and 22 answered their questions in licensed session 2 (2026-10-25).
Run first and in isolation, the Mesh Orientation substitution raised that same
dialog and the MP never completed until the server's watchdog ended the worker.
The runtime-select gesture completed probe 22's selection, but the vector group
getter returned nothing. Their operations were removed for contract major 3
([#309](https://github.com/spatialanalyzer/briosa/issues/309),
[#350](https://github.com/spatialanalyzer/briosa/issues/350)), and the plan
retires both numbers the same way. It also drops the `GRID` and `CPOLY`
fixtures that only probe 11 used, and the runtime select that probe 25 sent
under Silent.

## Safety controls

- **Explicit confirmation.** A licensed run requires `--confirm-licensed-sa2026`.
  Any other target's confirmation is refused. Unknown, repeated, or
  contradictory options are refused before any client starts.
- **Dry run.** `--dry-run` builds and validates the plan, prints every planned
  call, and creates no channel, process, or connection. It refuses connection
  options.
- **One client.** The public phase requires exactly one SA process, one Briosa
  worker, and at most one SDK process. The worker phase requires one SA process
  and no Briosa server, Briosa worker, or `SpatialAnalyzerSDK` process. The
  harness counts processes by name only.
- **Harness-owned fixtures.** Setup counts collections, creates `B277`, and
  requires the count to rise by exactly one. Manual fixtures may never use `B277`.
- **Destructive scope.** Only probe 24's two steps destroy anything: `p24-r0`
  and `p24-r0a` each delete the points within 1 unit of `D1` from their own
  disposable 8-point cloud (`CDELR`, `CDELRA`). The only accepted destructive
  request is `DeleteCloudPointsByRadialDistanceFromPoints`. Plan validation
  proves that it names only one disposable cloud in `B277`, points only in
  `B277`, and that an earlier setup step creates the cloud. The session refuses
  the step unless that creation succeeded in the same session. Each delete
  requires a before-count of exactly 8, and the sentinel cloud `CDELS`, which no
  step targets, must still hold 8 points after both deletes, or the session
  stops as a containment failure. The worker phase never runs destructive
  operations.
- **Operator checkpoints.** A licensed run pauses at each checkpoint the dry
  run lists, and sends nothing until the operator answers it (see
  [Operator checkpoints](#operator-checkpoints)). An unanswered checkpoint stops
  the session (`operator-checkpoint-not-confirmed`). After a step, the step's own
  result is kept.
- **Interaction mode.** SA keeps its interaction mode across restarts and has no
  getter. Probe 25 therefore changes it only between its own baseline and restore
  steps, the plan refuses to end with Silent in effect, and a licensed run that
  includes probe 25 requires the operator's `--restore-sa-interaction-mode`. See
  [Probe 25](#probe-25-interaction-mode). The same holds for probes 26 to 29.
- **Opt-in probes and planned stalls.** Probes 26 to 29 run only when `--probe`
  names them (see [Selecting probes](#selecting-probes)). A stall step (probes 26
  to 28) accepts only the stall it is planned to end in. The plan holds at most one
  stall step, and it is always the last step, so nothing is ever sent after a
  stall. Probe 29 needs a server setting that no other probe uses, so it runs
  alone.
- **Stop on the first unexpected disposition.** Guards, setup steps, and worker
  controls accept only success. Wildcard checks also accept an MP failure,
  because an empty match may be reported that way. Probes and variants accept
  any determinate SDK outcome, because that outcome is the answer:

  | Outcome | Execution disposition | Accepted by probes and variants |
  | --- | --- | --- |
  | `Succeeded`: MP code `2`, or code `4` (`PARTIAL SUCCESS`), and every output retrieved | Completed | Yes |
  | `MpFailed`: `GetMPStepResult` returned true with another MP code, or code `4` from an operation on the server's exception list | Completed | Yes |
  | `OutputRetrievalFailed`: an MP code, but an output was not retrieved | Completed | Yes |
  | `ArgumentRejected`: a setter returned false; `ExecuteStep` was not called | Not started | Yes |
  | `ExecuteStepRejected`: setters returned true; `ExecuteStep` returned false | Started, outcome unknown | Never |
  | `MpResultUnavailable`: `ExecuteStep` returned true; `GetMPStepResult` returned false | Started, outcome unknown | Never |
  | `Indeterminate`: timeout, cancellation, lost channel, or untyped failure | Started, outcome unknown | Never |

  An SDK call that throws is classified by its proven phase, as the shipped
  server reports `SdkCallFaulted`: a fault before `ExecuteStep` is `NotStarted`
  and is not accepted; a fault in `ExecuteStep` or `GetMPStepResult` is
  `Indeterminate`; a getter fault after MP code `2` or `4` is `OutputRetrievalFailed`.
  The record keeps the value-free `sdk-*-faulted` diagnostic code.

  Code `4` is classified as the shipped server reports it
  ([#351](https://github.com/spatialanalyzer/briosa/issues/351)): a partial
  success is `Succeeded`, and the record keeps MP code `4`. The four operations
  on the server's `MpPartialSuccessExceptions` list report code `4` as
  `MpFailed`.

  The dispositions match the shipped server. Anything not accepted stops the
  session and leaves the remaining steps not run. This includes a not-started
  refusal, an unmet requirement, and every outcome whose completion is unknown.
  The record keeps the exact kind, for example `ExecuteStepRejected`, with the
  stop reason `outcome-unknown-do-not-replay`.
- **No replay.** Each step is sent at most once. A timeout, cancellation, or lost
  channel is recorded as an unknown outcome. After any outcome whose completion
  is unknown, including `ExecuteStep` returning false or `GetMPStepResult`
  returning false, no further step is sent and nothing is retried. In the worker
  phase the harness also terminates its worker, as the server watchdog does.
- **Operator exclusions only remove probes.** `--exclude-probe` removes one
  probe's probe, variant, and check steps. It never removes a guard or fixture
  setup step, and the remaining plan must pass the same validation. See
  [Excluding probes](#excluding-probes).
- **Missing manual fixtures (#402).** A probe 34 to 37 step whose manual
  fixture the manifest omits is recorded as `fixture-missing`. It is never sent and
  never a pass, and the dry run, the JSON record (`fixtures_missing`) and the
  markdown record each name the missing keys. A guard or setup step never needs a
  manual fixture.
- **Ownership probes terminate only Briosa processes.** `--ownership worker-crash`
  and `parent-death` each end exactly one Briosa worker or server, after checking
  its name and start time; nothing in the harness ends SpatialAnalyzer or an SDK
  process.
- **No sensitive data.** Records hold labels, MP step text, dispositions, MP
  codes, output retrieval, counts of harness-created objects, and value-free
  flags (probes 30 and 33). They never hold argument or returned values,
  manifest contents, paths, or license data.

## Fixtures

### Created by the harness

Every phase creates these objects in the new collection `B277` before any probe
runs.

| Fixture | Purpose |
| --- | --- |
| `G1` with points P1 to P6: four corners of a 100-unit square at z = 0, plus two points at z = ±20 | Fits, bounding planes, ensure-unique, show/hide |
| `G2`: `G1` rotated 1° about Z | Group-to-group orientation (#19) |
| `PA` (normal +Z) and `PB` (45° to `PA`) | Seed geometry, reference plane, angle (#20) |
| `G3`: measured P1 to P3, one within ±1 and two 5 units out | Relationship `R1` and vector group `VG1` (one third in tolerance) |
| `GFIT`: the four coplanar corners of `G1` | Fits (#2, #17) |
| `GDEL` (eight points D1 to D9 without D6, as in the 2026-10-07 session) and clouds `CDELR`, `CDELRA`, and the sentinel `CDELS`, each built from `GDEL` | Radial delete controls (#24) |

The worker phase creates only `G1`, `G2`, `GFIT`, `PA`, `PB`, `G3`, `R1`, and `VG1`.

### Prepared manually

These cannot be created reliably through the public API. Prepare them once per
target in a disposable job that contains a collection named, for example,
`B277M`, and never a collection named `B277`. Save the job and reopen it at the
start of each phase. **Never save the job after a phase.**

1. **USMN instruments (#1, #33).** Two instruments in `B277M`, each with
   observations to the same four or more targets. Simulated observations are
   acceptable. Also a nominal point group, for example `Nominals`, that has the
   same target names. A manual USMN on this data must solve.
2. **Template chart (#3).** One existing chart. Its name goes in
   `template_chart_name`.
3. **User-interface profile file (#4).** Export a UI profile to a local file.
   Record its full path and profile name. This probe changes the active UI
   profile, so restore your own profile after the session.

Fixture manifest (the harness reads it; its values never appear in any output):

```json
{
  "schema_version": 1,
  "spatial_analyzer_target": "2026.1.0529.7",
  "usmn_instruments": [
    { "collection": "B277M", "instrument_id": 0 },
    { "collection": "B277M", "instrument_id": 1 }
  ],
  "usmn_nominals_group": { "collection": "B277M", "name": "Nominals" },
  "template_chart_name": "B277M Template",
  "ui_profile_name": "Default",
  "ui_profile_file": "C:\\briosa-277\\profile.xml"
}
```

The manifest no longer accepts `circle_line_surface`, which served a retired
probe. Remove it from a manifest written for the 2026-10-07 session, or the harness
refuses it as an unknown property. `callout_view` served the retired probes 7 to 10
and now serves probe 35, with the same shape; see
[Session 4 fixtures](#session-4-fixtures). The other session 4 keys are optional
too, so a manifest without them stays valid.

## Probe 25: interaction mode

Probe 25 runs last in each phase. It verifies, before the
[#313](https://github.com/spatialanalyzer/briosa/issues/313) `InteractionMode`
setting is built, what SA Interaction Mode Silent does to the blocking dialogs
the 2026-10-07 session observed.

| Step | Public phase | Worker phase |
| --- | --- | --- |
| Baseline | `p25-baseline` sets the restore mode | `p25-baseline` sets the restore mode |
| Before Silent | zero-match wildcard (`c25-zero-match-baseline`), template chart that matches no chart (`p25-chart-missing-baseline`), Ask for String (`p25-ask-baseline`) | blank High and Low plane names (`p25-blank-*-baseline`) |
| Silent | `p25-silent` | `p25-silent` |
| Under Silent | the same three triggers | the same two blanks, with harness-name and default-name counts |
| Restore | `p25-restore` | `p25-restore` |

- **Modes.** `Set Interaction Mode` sets three values at once. Every probe 25
  step sends MP mode `Halt on Failure or Partial Success`, which SA documents as
  the value it returns to when a script exits, and MP dialog mode
  `Allow Application Interaction` (maintainer decision, 2026-10-19), so the
  operator can use SA's UI during prompts. Only the SA mode changes: the
  baseline and restore steps send `--restore-sa-interaction-mode`, the Silent
  step sends `Silent`. The baseline step makes the before-Silent steps a control
  in the same SA instance.
- **The restore mode comes from the operator.** SA persists the SA mode and the
  MP dialog mode across restarts and has no getter. On the probe machine a value
  named `InteractionMode` exists under
  `HKCU\Software\New River Kinematics\Spatial Analyzer\Settings`. Read it before
  the session, at the `p25-baseline`, `p25-silent`, and `p25-restore`
  checkpoints, and include its digit in the checkpoint answer as `registry-<digit>`,
  for example `none registry-0`. Whether and when SA writes it, and
  which mode each value means, are observations for the evidence record, not
  facts the harness relies on.
- **Dialogs that Silent does not suppress block the MP** until the operator
  clicks OK, as in the 2026-10-07 session. Watch the screen during every probe
  25 step and dismiss such a dialog promptly. In the public phase, start the
  server with a 2-minute quick budget and the harness with a longer step timeout
  (see [Phase 1](#phase-1-public-api)), so a dismissal is not overtaken by the
  watchdog.
- **Prompts.** For `p25-ask-baseline` and `p25-ask-silent`, replace the answer
  with `B277-entered` and click OK. The record keeps only whether the answer was
  the initial answer or that entry. Under Silent, SA may not prompt at all;
  record what it does.
- **If a session stops inside probe 25** after `p25-silent`, SA stays in Silent.
  Recover as in [When a session stops](#when-a-session-stops), then run the
  same phase with `--interaction-mode-restore-only --restore-sa-interaction-mode <mode>`
  on a clean SA. That plan sends only `p25-restore`.
- `--exclude-probe 25` removes the whole block; the run then needs no restore mode.

## Probes 26 to 29: modal dialogs and the InteractionMode setting

[#313](https://github.com/spatialanalyzer/briosa/issues/313) added the server
setting `Briosa:SpatialAnalyzer:InteractionMode` and the `sa-modal-dialog-open`
report. When an exchange stalls past its bound while SpatialAnalyzer shows a
modal dialog, the server reports that diagnostic instead of the generic timeout
code (see the
[Windows package guide](../operations/windows-package.md#spatialanalyzer-interaction-mode)).
These probes check both against the dialogs that licensed sessions 1 and 2
observed. All four are opt-in, and the third session runs 26, 27, and 29.

| Probe | Run | Steps | Expected |
| --- | --- | --- | --- |
| 26 | public, server setting `Unchanged` | `p26-baseline` sets the restore mode, which must be Manual. Then `p26-zero-match-stall`. | "No objects matched specified criteria." stays open until the 2-minute quick budget ends the MP. The server then reports `sa-modal-dialog-open`, failure kind `WORKER_WATCHDOG_TIMEOUT`, disposition `STARTED_OUTCOME_UNKNOWN`, and recovery guidance `OPERATOR_INTERVENTION_REQUIRED`. |
| 27 | public, server setting `Unchanged` | `p27-baseline`, `p27-chart-missing-stall` | The same, with "Template Chart Not found". |
| 28 | worker | `p28-baseline`, `p28-blank-high-stall` | "Blank 'High' Plane Not Allowed!" stays open until the step's 60 s deadline. The harness then runs the server's own detector, which reports `sa-modal-dialog-open`. |
| 29 | public, server setting `Silent` | `c29-zero-match-setting`, `p29-chart-missing-setting`, `p29-restore` | `external-connect` reports `ready_for_mp`, which the server allows only after Set Interaction Mode returned MP code `2`. The server log records event 1202 with `interaction-mode-applied`. Both triggers return MP code `3` within milliseconds, with no dialog. `p29-restore` sets the restore mode. |

- **Planned stalls (26 to 28).** Each stall step ends its run on purpose. When its
  outcome is the planned stall, the step is recorded as observed, the run
  completes with exit code `0`, and the record says `ended_in_planned_stall`. Its
  completion is still unknown: nothing is sent after it, and the run needs the
  [stall recovery](#after-a-planned-stall). Any other outcome is unexpected and
  stops the run as usual, for example MP code `3` because the dialog was
  dismissed, or the generic `worker-execution-watchdog-timeout` code.
- **Do not dismiss a stall step's dialog** until the harness prints the step's
  result. The console says `PLANNED STALL` when the step is dispatched. The
  checkpoint before the step confirms that no dialog is open. At the checkpoint
  after it, dismiss the dialog with OK and answer `dialog-dismissed`; the dialog's
  text is never recorded.
- **Every other dialog** in the same run, for example a zero-match dialog from a
  count check, must still be dismissed promptly, before the quick budget ends.
- **Why probe 28 runs in the worker phase, and not in the third session.** The
  public API rejects a blank plane name before dispatch, so a server can never send
  it. The worker phase has no server. When a worker step's deadline elapses, the
  harness checks for a dialog with the server's own detector
  (`SpatialAnalyzerModalDialogProbe`) before it ends its worker, as the server's
  watchdog does. Probe 28 therefore checks the shipped detection rule against the
  third dialog, but not the server's reporting path. No caller can reach that
  dialog, and licensed session 2 already showed that Silent suppresses both
  blank-plane dialogs, so the third session leaves probe 28 out. It stays opt-in, and
  the evidence records it as unrun.
- **Probe 29's server setting.** Start the server with the setting that the dry
  run prints (`--Briosa:SpatialAnalyzer:InteractionMode=Silent`). For every other
  public plan, the dry run prints `Unchanged`. The setting persists the mode in SA.
  `p29-restore` returns SA to the restore mode, and the server sets Silent again
  only at its next Connect. If the run stops before `p29-restore`, restart the
  server **without** the setting before you run `--interaction-mode-restore-only`
  (see [When a session stops](#when-a-session-stops)).

## Probe 30: runtime-select sample

[#350](https://github.com/spatialanalyzer/briosa/issues/350) removed the vector
group runtime select, because its getter returned nothing after a completed
selection. Probe 30 samples the other runtime-select operations: one per list
getter kind, and three single-value getters. Its vector name list step
(`p30-vector-list`) was retired with its operation after licensed session 3
([#363](https://github.com/spatialanalyzer/briosa/pull/363)): on both targets F2
opened "This selection mode does not allow a list." and no operator could complete
the selection. The step id is not reused.

| Step | Getter | Value kind | Operator selects |
| --- | --- | --- | --- |
| `p30-point-list` | `GetPointNameRefListArg` | point name list | `B277::G1::P1` |
| `p30-object-list` | `GetCollectionObjectNameRefListArg` | collection object name list (Object Type Plane) | `B277::PA` |
| `p30-relationship-list` | `GetCollectionObjectNameRefListArg` | collection item name list | `B277::R1` |
| `p30-instrument-list` | `GetColInstIdRefListArg` | collection instrument list | the first USMN fixture instrument |
| `p30-instrument` | `GetColInstIdArg` | collection instrument | the first USMN fixture instrument |
| `p30-point` | `GetPointNameArg` | point name | `B277::G1::P1` |
| `p30-collection` | `GetCollectionNameArg` | collection name | `B277` |

SA's prompt names the fixture and the gesture. Press **F2**, check the fixture in
the selection tree, click **OK**, then press **Enter**. The record keeps the number
of items returned and whether they are exactly the requested fixture
(`selection_count`, `selection_is_requested`), never a name. A getter that returns
nothing is recorded as `OutputRetrievalFailed`, as the removed operation was.

The first USMN fixture instrument is the first entry of `usmn_instruments` in the
fixture manifest. Both instrument steps select it, and `selection_is_requested` is
true only for that instrument.

Reading the results:
- `p30-object-list` and `p30-point` use getters that earlier sessions read after
  other MP steps: the wildcard counts, and probe 12. A failure there points at the
  runtime select rather than at the getter.
- The two instrument steps are controls for each other. If both fail, investigate
  their shared selection path before either getter.
- A retirement decision stays scoped to the RPC and exact target observed. A failed
  step does not retire the other operations of the family, or the same operation on
  the other target.

The sample leaves out:
- the report and picture lists, which use the relationship list's getter and value
  kind and need fixtures this job does not have;
- the object-name single getter, which probe 13 read after Ensure Unique;
- the cloud-point select, which needs a pick in the graphics view;
- the eight surface-face constructors and the surface-face list, which need CAD
  surfaces. The constructors have no getter.

## Probe 33: USMN excluded groups

Every client offers USMN's `excluded_groups` as optional
([briosa-dotnet#39](https://github.com/spatialanalyzer/briosa-dotnet/issues/39)),
but the mapping that session 3 tested required at least one group. It refused an
empty list with `InvalidArgument` before SA saw the request, so only the worker
phase can ask SA whether USMN needs the list. Since
[#363](https://github.com/spatialanalyzer/briosa/pull/363), the server skips the
setter for an empty list instead, so the public API still never sends one. Probe 33
runs USMN three times on probe 1's fixtures:

| Step | `Groups to be Excluded` | Expected if SA runs USMN correctly without the list |
| --- | --- | --- |
| `p33-shipped` | `G3`, as the shipped request sends it (the control) | MP code `2` and both outputs |
| `p33-omit` | setter skipped | MP code `2`, both outputs, and the control's errors |
| `p33-empty` | a list with no items | the same |

- `G3` is a harness group that USMN does not use, so excluding it and excluding
  nothing should give the same solution.
- The harness keeps the control's RMS and maximum errors in memory. Each variant
  records only `errors_equal_control`: `true` when both its errors equal the
  control's within one part per million (an absolute 1e-9 for a zero error). No
  error value is recorded.
- The variants accept any determinate outcome, so an MP failure or a refused
  setter is an answer, and the run continues. An outcome whose completion is
  unknown stops the run as usual, and `p33-empty` is then recorded as not run.
- USMN runs with `Show USMN Dialog` set to `No`. If a dialog opens anyway, dismiss
  it within the step timeout and answer `dialog-dismissed` at the checkpoint after
  the step.
- Excluding `G3` does not change the solution, so the probe cannot tell a skipped
  setter from one that SA kept from the control. The shipped server already relies
  on skipped setters elsewhere, for example for Quick Align's empty lists.

**What the result decides.** Read each target's record on its own; a result never
carries over to the other target.
- If SA runs either variant correctly (all three of its hypotheses match), the
  server accepts an empty `excluded_groups` before the protocol checkpoint
  ([#363](https://github.com/spatialanalyzer/briosa/pull/363)). It skips the setter,
  as it does for Quick Align's empty lists, or sends the empty list, whichever the
  probe shows SA accepts. The clients keep the list optional.
- If SA runs neither variant correctly, `excluded_groups` becomes required in the
  0.5.0 clients.

**Result (licensed session 3, 2026-11-09).** Both targets ran `p33-omit`
correctly, and the SDK setter refused the empty list in `p33-empty`
(`ArgumentRejected`), so that step never ran. #363 therefore skips the setter for
an empty list and never sends one. The clients keep the list optional.

**Not covered.** The server also refuses five other lists when they are empty.
The third session does not probe them:
- Locate Instrument (Group to Surface Quick Fit) `other_objects_to_transform`:
  neither fixture job has a surface to fit to, and adding one would need manual
  work with licensed SA in both jobs. The list becomes required in the 0.5.0
  clients, as the server requires it now.
- Feature Inspection Auto Filter's `point_names`, `group_names`, `cloud_names`,
  and `feature_check_name_list`: a probe would need CAD with feature checks. They
  become required in the 0.5.0 clients.

## Operator checkpoints

A licensed run prints one progress line when it dispatches each step and one
when the step returns, with the step identifier, the outcome kind, the MP code,
and the elapsed time. At each checkpoint it writes a request file into the
`checkpoints` folder of `--output-directory`, prints its name, and waits up to
`--operator-timeout-seconds` for the matching `.response.txt` file. Write one
line to it:

- `none` when no dialog was open;
- `dialog-seen` when a dialog appeared and was already gone, or `dialog-dismissed`
  when you dismissed it with OK, including during the step; or
- `license-prompt-abort` when SpatialAnalyzer shows a license prompt. Do not
  dismiss it and do not copy anything from it. The run then sends nothing more.

You may add up to two detail words after the first: `lines-shown` or
`lines-not-shown` (probe 35's callout lines) and `registry-<digit>` (probe 25's
`InteractionMode` registry value). Nothing else is accepted, so no dialog text,
machine or account name, process id, license text or path can reach a record, and
none may be written anywhere else in the evidence either. A refused answer is set
aside as `.refused-<n>.txt` and the harness keeps waiting. Checkpoints:

| Step | When | Why |
| --- | --- | --- |
| `p24-r0`, `p24-r0a` | after | #307: record any dialog from the delete before the next step |
| every probe 25 step | after | #313: record whether each trigger opened a dialog |
| the stall steps of probes 26 to 28 | before and after | #313: no dialog before the step; after it, the dialog that stalled it, still open |
| every other step of probes 26 to 33 | after | record any dialog, including the selection prompt of probe 30 and a zero-match dialog from a count |

## Dry run

Run the dry run from this target directory with the pinned SDK. It needs no SA,
license, or SDK.

```powershell
dotnet restore Briosa.slnx --locked-mode
dotnet build tools/Briosa.LicensedProbes -c Release --no-restore
$probe = 'tools/Briosa.LicensedProbes/bin/Release/net10.0-windows/Briosa.LicensedProbes.exe'
& $probe --phase public-api --dry-run --fixtures C:\briosa-277\fixtures.json
& $probe --phase worker --dry-run --output-directory artifacts\probe-277\plan-worker
```

Without `--fixtures`, the dry run uses a built-in placeholder manifest. With
`--output-directory`, it also writes JSON and markdown plan records marked as
dry runs. Those records are not evidence.

## Excluding probes

`--exclude-probe <n>` removes probe `n` from the selected phase. `n` is one probe
number from the approved list (the `#` column above), for example `24`. Repeat
the option to exclude several probes. It is accepted in dry runs and licensed
runs of both phases. Use it to collect the remaining probes when one probe
cannot run, for example after it stopped a session. Rerun the whole phase on a
clean SA as described in [When a session stops](#when-a-session-stops).

```powershell
& $probe --phase public-api --dry-run --fixtures C:\briosa-277\fixtures.json --exclude-probe 24
```

The harness refuses, before any client starts and with exit code `2`:

- a value that is not one canonical probe number (`024`, `+24`, and `5-6` are
  refused), a number outside the approved list, a retired probe (7 to 11,
  14 to 16, 22), or the same number twice;
- a probe that has no steps in the selected phase, for example `17` in the
  public phase or `24` in the worker phase;
- a probe that shares a step with another probe unless both are excluded:
  `5` and `6` share `p05-06-value` (public) and `p05-06-shipped` (worker);
- an exclusion that would remove a prerequisite of a remaining step. The plan
  declares these explicitly, and the dry run prints them as
  `prerequisites (run earlier)`. `p17-recased` needs its control
  `p02-shipped`, so the worker phase refuses `--exclude-probe 2` unless
  `--exclude-probe 17` is also given;
- an exclusion that leaves no probe in the phase.

Excluding a probe removes only its probe, variant, and check steps, for example
`p02-value` and `c02-value` for probe 2 in the public phase. Guard and fixture setup steps always run, including fixtures
that only the excluded probe uses, so the remaining probes see the same SA
state as in the full plan.

The dry-run output, the JSON record, and the markdown record each state the
exclusion, for example
`Excluded by operator: #24 — reason not recorded by harness`. Excluded steps
keep their place in the records with the classification `excluded` (markdown:
"Excluded by operator; not sent."), which is distinct from `not-run`, a planned
step that a stop or refusal prevented. A licensed run completes, with exit code
`0`, when every step that was not excluded is observed.

The harness does not record why a probe was excluded. **Every exclusion must be
justified in the committed evidence**: name the earlier record that stopped (or
the other reason), the step, and its stop reason, and state that the excluded
probe remains unanswered by this record.

`--exclude-probe` refuses the opt-in probes 26 to 29, which the default plan
never contains.

## Selecting probes

`--probe <n>` runs only probe `n` of the selected phase. Repeat it to run several
probes. Every guard and fixture setup step still runs. It is the only way to run
the opt-in probes 26 to 29, and the third session uses it so that it does not
repeat the probes that earlier sessions answered.

```powershell
& $probe --phase public-api --dry-run --probe 30 --probe 31 --probe 32 --probe 26 --restore-sa-interaction-mode manual
& $probe --phase worker --dry-run --probe 33
```

The harness refuses, before any client starts and with exit code `2`:

- `--probe` together with `--exclude-probe` or `--interaction-mode-restore-only`;
- a value that is not one canonical probe number, a retired probe, or the same
  number twice;
- a probe that has no steps in the selected phase;
- a selection that splits a step serving several probes (`5` without `6`), or that
  leaves out a prerequisite of a selected step: in the worker phase, `--probe 17`
  needs `--probe 2`;
- two stall probes, for example `--probe 26 --probe 27`;
- probe 29 with any other probe;
- a stall probe with a restore mode other than `manual`. The stall ends the run,
  so no later step could restore another mode.

The dry run, the JSON record (`selection`), and the markdown record each state
the selection, for example
`Selected by operator (--probe): #26, #30; no other probe is part of this plan`.
Probes that were not selected do not appear in the records at all.

## Licensed session (only after the session-time go-ahead)

Use the package and harness built from the same reviewed commit. Pass
`--harness-revision <sha>` so the record names that commit. Each attestation
reference is one of the harness's predefined evidence aliases, which say which kind
of evidence was read and never what it said: `sa-title-bar`, `sa-about-dialog`,
`sa-installed-programs`, `sdk-file-version` or `installer-record`. The harness
refuses any other reference; use the same aliases for the server's attestation
arguments.

### Phase 1: public API

1. Close every SpatialAnalyzer, Briosa, and `SpatialAnalyzerSDK` process. Start
   exactly one SA `2026.1.0529.7` instance normally, not hidden, and open the
   fixture job.
2. Run `./eng/Test-LicensedRunnerState.ps1 -Phase Preflight`.
3. Extract the package. Start `Briosa.Server.exe` with
   `--Briosa:Desktop:Mode=Disabled`, the loopback port, and the four
   `--Briosa:SpatialAnalyzer:Identity:...:OperatorAttestation:...` values,
   exactly as `eng/Test-LicensedSpatialAnalyzer.ps1` does. Add every
   `--Briosa:` argument that the public-phase dry run prints for the same plan
   (the same `--probe` or `--exclude-probe` options): the `Server admission`
   arguments and the `InteractionMode` setting. The
   packaged `standard` profile does not admit the device and operator-guided
   probes, so the harness needs `--Briosa:Security:Operations:Profile=device`
   plus a per-operation `allow` override for each operator-guided selector and
   for the probe 25 operations. The harness refuses a plan that needs an
   exclusive workflow, which no setting can admit. Also add
   `--Briosa:Worker:ExecutionWatchdogTimeout=00:02:00`: probe 25 opens dialogs
   that block a quick MP until the operator dismisses them, and the default
   30 s quick budget would end the run as an unknown outcome first. Record the
   setting in the evidence.
4. Run `Briosa.LifecycleClient --address http://127.0.0.1:50051 --scenario external-connect --timeout-seconds 90`.
   It must report `ready_for_mp`.
5. Run the public phase. Its client step timeout must exceed the 2-minute quick
   budget:

   ```powershell
   & $probe --phase public-api --confirm-licensed-sa2026 `
     --fixtures C:\briosa-277\fixtures.json `
     --output-directory artifacts\probe-277\public-api `
     --address http://127.0.0.1:50051 --harness-revision <sha> `
     --step-timeout-seconds 150 --restore-sa-interaction-mode <manual|automatic>
   ```

   Answer each [operator checkpoint](#operator-checkpoints) as it is printed.
   During each probe 30 step, SA prompts for a selection: press **F2**, check the
   fixture that the prompt names in the selection tree, click **OK**, then press
   **Enter**, within `--operator-timeout-seconds` (default 300).
6. Run `Briosa.LifecycleClient ... --scenario stop-sdk`, stop the server, and run
   `Test-LicensedRunnerState.ps1 -Phase Postflight`.

### Phase 2: worker

1. Close SA completely, without saving. Start one clean SA `2026.1.0529.7`
   instance and reopen the fixture job. Do not start a Briosa server.
2. Run `Test-LicensedRunnerState.ps1 -Phase Preflight`.
3. Run the worker phase with the worker from the same extracted package:

   ```powershell
   & $probe --phase worker --confirm-licensed-sa2026 `
     --fixtures C:\briosa-277\fixtures.json `
     --output-directory artifacts\probe-277\worker `
     --worker-path <extracted package>\Briosa.Worker.exe `
     --connected-sa-attested-version 2026.1.0529.7 `
     --connected-sa-attestation-reference <evidence alias> `
     --harness-revision <sha> --restore-sa-interaction-mode <manual|automatic>
   ```

   Answer each [operator checkpoint](#operator-checkpoints) as it is printed. The
   worker's step timeout (`--step-timeout-seconds`, default 120 s) is its only
   watchdog, so dismiss a blocking dialog well within it.

4. Run `Test-LicensedRunnerState.ps1 -Phase Postflight`. Close SA without saving.

Exit code `0` means every step that was not excluded was observed as planned. `1` means the session
stopped or was refused, and the record says where and why. `2` means a usage or
manifest error, and nothing started.

### Third session: runs

The third session runs probes 26, 27, and 29 to 33, in three public runs and one
worker run per target. Each run needs the maintainer's go-ahead, and each starts
from a clean SA with the fixture job: a public run as step 1 of phase 1 describes,
and run D as step 1 of phase 2 describes. Every run uses
`--restore-sa-interaction-mode manual`. The public runs also use the 2-minute quick
budget and `--step-timeout-seconds 150`. Run D has no server and no quick budget:
the worker's default step timeout of 120 s is its only watchdog.

Build the packages from the reviewed merge commit with the label `0.10.0-probe-s3`.
The label also appears as the server version in the records. Record the merge SHA
and the SHA-256 of both targets' packages in the evidence. The probe build still
declares compatibility 2.0 on both targets; the major-3 freeze raises it to 3.0
separately.

| Run | Phase | Selection | Server setting from the dry run | Ends with |
| --- | --- | --- | --- | --- |
| A | public | `--probe 30 --probe 31 --probe 32 --probe 26` | `Unchanged` | the planned stall `p26-zero-match-stall` |
| B | public | `--probe 27` | `Unchanged` | the planned stall `p27-chart-missing-stall` |
| C | public | `--probe 29` | `Silent` | `p29-restore` |
| D | worker | `--probe 33` | none (no server) | `p33-empty` |

In run C, check the server log for event 1202 with `interaction-mode-applied`
before you run the harness. If run A stops before its stall, run probe 26 again
alone with `--probe 26`. The evidence records probe 28 as unrun. Read run D's
record as [Probe 33](#probe-33-usmn-excluded-groups) describes before the server or
the clients change `excluded_groups`.

Before closing [#313](https://github.com/spatialanalyzer/briosa/issues/313), read
these facts from each target's records. The harness exit code alone does not
establish them:

- **Runs A and B.** The stall step's `execution_disposition` is exactly
  `StartedOutcomeUnknown`. The match for a planned stall compares the transport, the
  diagnostic, the failure kind, and the recovery guidance, but not the disposition.
- **Run C.** `c29-zero-match-setting` and `p29-chart-missing-setting` both have MP
  code `3`, and the operator's answer after each one is `none`, so no dialog opened.
  These steps accept other determinate outcomes too, and the MP code is a hypothesis
  that does not stop the run.

### When a session stops

An unknown outcome (`outcome-unknown-do-not-replay`), including `ExecuteStep`
or `GetMPStepResult` returning false, can leave SA or the SDK in an uncertain
state. Do not rerun the phase against the same SA instance. Follow
the licensed-runner recovery steps: close residual Briosa and SDK processes,
close every SA instance, and start a clean one. Other stop reasons, such as a
failed fixture step, unmet requirement, or failed control, also end the phase.
After fixing the cause, rerun the whole phase from step 1 on a clean SA. Steps
are never resumed or replayed individually. To collect the remaining probes
without the probe that stopped the session, rerun the whole phase from step 1
on a clean SA with `--exclude-probe` (see [Excluding probes](#excluding-probes)).
The rerun repeats every guard, fixture, and remaining probe step; it never
resumes the stopped session.

If the stop came after `p25-silent` and before `p25-restore` completed, SA is
still in Silent, and it stays so across restarts. After recovery, on a clean SA,
run the same phase with `--interaction-mode-restore-only` and the same
`--restore-sa-interaction-mode` before anything else. It sends only
`p25-restore`, and needs no fixture job.

The same applies when run C (probe 29) stops before `p29-restore` completed,
because its server set Silent at Connect. Start the server for the recovery from
the restore-only dry run, which prints `InteractionMode=Unchanged`. A server that
still has the Silent setting would set Silent again at its Connect.

### After a planned stall

A run that ends in its planned stall (probes 26 to 28) completes, but the stalled
step's completion is unknown, so recover before any further SDK use:

1. At the checkpoint after the stall step, dismiss the dialog with OK and answer
   `dialog-dismissed`. Never record the dialog's text.
2. Public runs: run `Briosa.LifecycleClient ... --scenario stop-sdk`, then stop the
   server. The worker run has already ended its worker.
3. Close SA without saving (File > Exit > No).
4. Census the processes. A `SpatialAnalyzerSDK` process that a stalled worker
   activated can outlive SA ([#252](https://github.com/spatialanalyzer/briosa/issues/252)).
   Stop it only with the maintainer's OK, and record that in the session notes.
5. Start the next run from a clean SA.

## Licensed session 4 (#402)

[#402](https://github.com/spatialanalyzer/briosa/issues/402) supplies the
measurements that Server 0.10.1 and its follow-ups wait for: result sizes for
[#245](https://github.com/spatialanalyzer/briosa/issues/245) and
[#399](https://github.com/spatialanalyzer/briosa/issues/399), cold starts for
[#371](https://github.com/spatialanalyzer/briosa/issues/371), and the dedicated
SDK-ownership probes of [#252](https://github.com/spatialanalyzer/briosa/issues/252).
It also exercises the bindings that
[#220](https://github.com/spatialanalyzer/briosa/issues/220) and
[#221](https://github.com/spatialanalyzer/briosa/issues/221) corrected but never ran
live.

**Status: prepared, not run.** Nothing in this section may run against a licensed
SpatialAnalyzer until the maintainer authorizes the session, and again at each
authorization point below (AGENTS.md validation level 5). Every rule marked
**Proposed** is a recommendation until the maintainer agrees to it on #402; change
it to **Agreed** with the date before the session starts. No timed step may wait
on a question or on the operator's judgement: if a situation is not covered by an
agreed rule, stop the run with Ctrl+C as the abandon rule does (or with the license
signal, for a license prompt), record it, and ask between runs.

This target is observed on its own. Nothing recorded on SA `2024.1.0508.5` applies here,
and nothing recorded here applies there.

### What each scope item uses

| # | Scope item | Harness command | Records | Posted on |
| --- | --- | --- | --- | --- |
| 1 | Result sizes | `--phase public-api --probe 34` and `--phase worker --probe 34` | element counts; private frame bytes (worker phase) and protobuf message bytes (public phase); whether the worker rejected a result's encoding (its size is then unknown) | #245 (and #399) |
| 2 | Cold starts | `--ownership release` on a new server, then `--startup-timing` over that server's log | the server's own start-phase and lifecycle RPC times (#412), labelled stock or changed configuration | #371 |
| 3 | SDK ownership | `--ownership release`, `worker-crash`, `parent-death`, `watchdog-kill`, `competing-client`; `--ownership observe` after SA closes | labelled processes, their parents, attribution, start times, and last-seen and first-absent times; lifecycle states | #252 |
| 4 | Corrected bindings | `--phase public-api --probe 35 --probe 36 --probe 37` | setter and getter dispositions, MP codes, output retrieval | #402 (#220 and #221 are closed) |
| 5 | Tutorials (optional) | `briosa-examples` on its current pins | each program's exit code and whether its output was as expected | #402 |

### Probes 34 to 37

All four are opt-in: they run only when `--probe` names them, and the default plan
never contains them. A step that needs a manual fixture the manifest omits is
recorded as `fixture-missing`: it is never sent, and it is never a pass.

| # | Question | Phase | Steps | Manual fixture |
| --- | --- | --- | --- | --- |
| 34 | How large are representative list results, measured where the response budget applies? | public and worker | `p34-bspline`; for each of 100, 500, 2,000, 10,000 and 20,000 points: `p34-points-N`, `c34-count-N`, `p34-list-N`; `p34-objects-harness`, `p34-objects-job`; `c34-count-job-group`, `p34-list-job-group`; `c34-count-job-cloud` | `result_size_point_group` (the job's large group), `result_size_cloud` (the job's cloud); the ladder needs none |
| 35 | Do the five callout text and notes setters, now `SetEditTextArg`, work through the generated client? | public | `c35-callouts-before`, `p35-text`, `p35-point`, `p35-point-comparison`, `p35-relationship`, `p35-vector`, `c35-callouts-after` | `callout_view` |
| 36 | Does the TCP uncertainty notes getter, now `GetEditTextArg`, return the notes? | public | `p36-notes` | `tcp_fixture` |
| 37 | Does the robot `Machine ID`, now `SetColInstIdArg`, reach Get Robot/Machine Parameter? | public | `p37-parameter` | `robot_machine` |

- **Probe 34's ladder.** The harness fits the B-spline `BSZ` through `GFIT`'s four
  corners and constructs each group `SZ<N>` with Construct Points N-Spaced on
  Curves, so the ladder needs no manual work. The worker response budget is 1 MiB
  ([#411](https://github.com/spatialanalyzer/briosa/pull/411)). At about 70 bytes
  of private frame per point name, a test pins against this build's encoders, 100
  to 10,000 points fit it and 20,000 do not; 2,000 is the first rung beyond the
  former 64 KiB frame limit. The protobuf message is roughly a third of the frame.
  So the ladder brackets the worker budget, but it does not establish a crossing of
  the published clients' 4 MiB receive limit: no rung's message can reach it. The
  rungs run in ascending order, so a stop loses only the larger ones.
- **What is measured.** In the worker phase the harness is the worker's host, and it
  counts the bytes of each frame it reads: that payload is the quantity the worker
  response budget bounds. In the public phase it measures the protobuf message the
  generated client received. When the worker cannot encode a result, it sends its
  bounded replacement: the existing completed `DataLoss` / `OutputRetrievalFailure`
  outcome, or an SDK call fault with that output withheld, with
  `worker-output-encoding-rejected`. The product gives that code both to a result
  over the budget and to a value it cannot encode at all (for example a non-finite
  number), so the record says `encoding_rejected` with `original_size: unknown`,
  never an overflow, and keeps the replacement frame's size apart. The count step
  before each list says how many elements the rejected result held.
- **Clouds.** No operation returns cloud points as a list, so the job's cloud is
  counted only (`c34-count-job-cloud`). The ladder's bytes per element give the size a
  list of that many names would need, as an estimate labelled as such.
- **Probe 35.** A refused setter is `ArgumentRejected`. Any recorded MP code means
  every setter, including the text one, returned true. The vector callout names
  vector `P1` of `VG1`. If that is not a vector name in `VG1`, the MP fails after the
  setters, which still answers the question. At the checkpoint after each callout,
  answer with the dialog word and `lines-shown` or `lines-not-shown` for whether the
  callout shows the harness's two lines.
- **Probe 36.** All nine outputs retrieved (hypothesis `9 of 9`) means the notes
  getter worked. A refused getter after MP code `2` is `OutputRetrievalFailed`. The
  record keeps only the number of note lines.
- **Probe 37 and the other two robot routes.** Start and Stop Robot/Machine
  Interface carry the same `Machine ID` correction, but they are exclusive
  workflows, which no server setting admits. No generated client can send them, so
  the session records both as **not run: refused by admission policy (exclusive
  workflow)**. See decision D10.

### SDK-ownership probes

`Briosa.LicensedProbes --ownership <scenario>` runs one scripted probe against one
packaged server. Each starts from an inert server and a complete process census:
exactly one SA, one Briosa server with no SDK started, and no worker or
`SpatialAnalyzerSDK` process. Every lifecycle call has a deadline
(`--step-timeout-seconds`, default 120 s), and a scenario refuses to start when that
deadline is not longer than the server's published start, connect or stop bound
(`step-timeout-below-published-bound`). Every MP step is sent once.

**Binding.** A run acts only on what it binds, once:

- **The server**: the one server in the census, which must be in the operator's
  Windows session and must own every listener the endpoint's address can reach
  (its own address or its family's wildcard, read from the TCP table), opened
  through a handle whose name and start time match the census.
- **The connection**: nothing is sent before the server is bound. Every connection
  the harness opens to the endpoint is then checked in the TCP connection table
  before its first byte: its other end must be owned by the bound server. A
  connection that cannot be proven, including a replacement opened later in the run,
  or a table that cannot be read, is closed unused, and the call is recorded as
  `peer-unverified` (never sent). The run then stops with
  `endpoint-peer-not-the-bound-server`. The record counts verified and refused
  connections.
- **The SDK generation**: only the one this run's own **successful** start created.
  A generation carried in a failed start's error (for example `sdk-already-active`,
  which reports the server's current generation) is recorded as observed and never
  acted on.
- **The worker**: after the start, and only on a **complete** census, the one new
  worker whose parent is the bound server, in the same session, opened the same
  way. An incomplete census binds nothing (`census-incomplete`).

The handles are held for the whole run, so a check or a termination can never
reach a process that reused an id. Each handle can query and wait only; only
`worker-crash` holds the right to terminate the worker, and only `parent-death` the
right to terminate the server. When the bound process has gone, the run records
`bound-process-exited` and stops without acting; it never selects a replacement.
Cleanup stops only the bound generation, and only while the bound server runs; when
the server reports another generation, the run records `generation-changed` and
stops nothing. An SDK process is never bound. The record gives it the attribution
`appeared-during-start` (first seen while this run's start was in flight), which is
timing, not proof of ownership, and records its parent's role as the snapshot
showed it.

**Observation.** The harness reads a snapshot of the process list (names, parents,
start times, sessions) every 500 ms during observation windows and at every step.
Each snapshot is **complete**, **incomplete** (the list ended early, or a tracked
process's start time could not be read) or **failed**. Only a complete snapshot can
show a process absent; the others are counted as gaps. The record never claims an
exit time: a process exited after its `last_seen_ms` and no later than its
`first_absent_ms`. A process that a complete snapshot missed and a later one listed
again is counted under `reappearances`. Each process is labelled the first time it is
seen (`sdk-1`, `worker-1`) with its role, its parent's role (`server`, `harness`,
`svchost` for a Windows service host, `other`, or `exited`), its attribution and its
start time. Process names are matched without case, as Windows matches them. The
record keeps no process id, path or argument. A window ends early once a complete
snapshot shows that the processes it watches are gone, or after `--observe-seconds`
(proposed: 120 s).

| Scenario | What it does | Observation |
| --- | --- | --- |
| `release` | start, connect, stop the bound generation | **SDK process identity**: which SDK process appears with the worker, and its parent. **Normal release**: whether the worker and that SDK process exit after a clean stop |
| `worker-crash` | start, connect, then terminate the bound worker through its handle, that process only | whether the server reports `Faulted` within 30 s; whether the SDK process survives the worker; then the stop of the bound generation |
| `parent-death` | start, connect; then, only while the server still reports the bound generation, ready, and the bound worker still runs (`recheck-before-termination`), terminate the bound server through its handle, that process only. Otherwise nothing is terminated and the bound generation is stopped | whether the worker notices its parent's death and exits, and whether it releases the SDK process |
| `watchdog-kill` | start, connect, set SA Interaction Mode Manual; the operator checkpoint; then, just before dispatch, a descendant census of the bound worker. Only when that census is complete, the bound worker still runs, and every descendant is approved (`conhost` only), a wildcard selection that matches nothing in any collection, which stalls on SA's dialog until **the product's watchdog terminates the worker and its whole process tree**. Otherwise no stall is sent (`worker-descendants-not-approved`) and the bound generation is stopped | the SDK process with the dialog still open, then after the operator dismisses it at the checkpoint, then after the stop |
| `competing-client` | start, connect; a read-only MP through the server, which must succeed; then a second, unchanged worker from the bound server's own folder, which must report this exact target's activated SDK, attaches with one `ConnectEx`, which must succeed; a read-only MP through the server while it is attached, which must succeed; the second worker stops, and its exit must be confirmed by its stop and by a complete census; a read-only MP | whether the second client gets its own SDK process, whether its `ConnectEx` succeeds, and whether the first client still works. The first failed gate ends the script; only the stops follow (`second-worker-not-admitted`, `second-worker-exit-unconfirmed`, or the MP's own stop). **The second client never runs an MP.** |
| `observe` | nothing but snapshots, until a complete one shows no SDK, worker or server process | what is left after SA closes |

**What can be terminated.** The harness itself terminates only the bound worker
(`worker-crash`) or the bound server (`parent-death`), through its handle, and never
a process tree; a test fails if any harness source names a whole-tree termination.
It also stops, or terminates, its own competing second worker. It never terminates
SA or an SDK process. The **product's own watchdog** is different: when it ends a
worker, the server terminates that worker's whole process tree. So `watchdog-kill`
sends its stall only after a descendant census of the bound worker, taken after the
operator's checkpoint and just before dispatch, is complete and lists only approved
descendants (`conhost`); it records the labels of tracked descendants and counts of
all of them and of those not approved, never another name. That census is a
pre-dispatch observation: it does not record what the watchdog later ended. Every
scenario also records one after connect. Whether the SDK process is ever a
descendant of the worker is not established on this target. On this target, the SDK process that a watchdog-terminated worker had activated was still running after SA exited in session 1 (fixture attempt 1), session 2 (public run 1) and session 3 (runs A, A2 and B), and no session recorded its parent. See
decision D18.

**Residual process.** At the end of a run's final window: any `SpatialAnalyzerSDK`
or `Briosa.Worker` process still alive, and after `parent-death` or in `observe` any
`Briosa.Server` process. The record lists each under `residual`. When the run ended
without a complete snapshot, `residual_known` is false and postflight cannot pass on
that record. See [Session 4 postflight](#session-4-postflight).

### Cold starts

The server logs each worker start's phases (`WorkerStartPhase`, event 1203:
`process-created`, `control-pipe-connected`, `ready-received`, or
`startup-bound-expired` and `start-failed`) and each lifecycle RPC's arrival and
exit (1302 to 1305), merged in [#412](https://github.com/spatialanalyzer/briosa/pull/412).
`ready-received` includes the SDK's COM activation. The harness adds no timing of its
own. `--startup-timing` reads one server's log folder, offline:

- It reads only records of this exact target, refuses a folder that holds records
  of another target or more than one server instance, checks every JSON value's
  kind, and counts every line it cannot use instead of failing on it.
- It labels the first `StartSpatialAnalyzerSdk` with the measured condition only
  when the log proves it was the server's first start: the server's
  `EndpointListening` record before it, no lifecycle record earlier, and worker
  generation 1. Otherwise the label is `first-attempt-unproven`.
- The log names no request, so it follows every lifecycle request as outstanding
  from its arrival until the first later exit of the same RPC. An attempt that
  overlapped any other request, including another start, is
  `overlapping-requests`: neither it nor the request it overlapped reports an RPC
  time (`withheld-overlapping`) or phases. An attempt whose phases name more than one
  worker generation is `several-generations` and has no phases. Later unambiguous
  attempts are `repeat` (warm). A line whose known field has the wrong JSON kind is
  counted as unreadable, never read as an event without that field.
- It needs `--startup-timeout-seconds`, the bound the server ran with. A bound other
  than the product's 10 s default is labelled **changed configuration**, not a
  stock start, in the JSON, the markdown and the console line.

**Proposed** definitions of "cold":

| Condition | How the operator establishes it |
| --- | --- |
| `fresh-install` | Extract the probe package into a new folder that no process has run from. The machine has been up for at least 10 minutes. SA has been started normally with the fixture job and left idle for 2 minutes. Then start the server from the new folder and run `--ownership release`. Its `StartSpatialAnalyzerSdk` is the first that package has ever run. |
| `after-reboot` | The same package, which has already started the SDK before the restart. Restart Windows, log on, and wait 5 minutes. Start SA normally with the fixture job and leave it idle for 2 minutes. No Briosa or SDK process has run since the restart. Then start the server and run `--ownership release`. |
| `warm` | The second `--ownership release` on the **same** server, right after the first, reported as attempt 2 (`repeat`) of the same log. If the first release left a residual SDK or worker process, the second refuses its precondition, and the warm measurement is recorded as not run. |

Each measured server is started with its own empty log folder
(`--Briosa:Logging:File:Directory=<run folder>\server-logs`). **Proposed** (decision
D4): the cold-start servers also get `--Briosa:Worker:StartupTimeout=00:01:00`, so a
slow start is measured to its end instead of being cut off at the 10 s default. That
is a changed configuration, and every record of those starts says so; the report
states the 10 s default beside each `ready-received` time.

### Tutorials (optional, SA 2026 only)

The only licensed run of
[briosa-examples](https://github.com/spatialanalyzer/briosa-examples) on record used
Server 0.7.0 and the 0.2.0 clients. If time allows (A8), run its four
point-inspection tutorials on that repository's current pins: the 0.5.0 .NET,
TypeScript and Python clients and the Server 0.10.0 protocol. Nothing in that
repository changes.

1. The language clients start the *installed* Briosa server for SA 2026, not the
   probe package, so the installed Briosa must be Server 0.10.0 (decision D14).
2. Check the [clean state](#before-the-session-once-per-target): the previous run's
   postflight passed, and no Briosa or SDK process runs. A process that breaks it is
   a residual, never closed to make the state clean. Start SA 2026 with a new empty
   job, never the fixture job.
3. Follow that repository's `docs/setup.md`: set the two connected-SA attestation
   variables in the terminal, run the .NET, TypeScript and Python programs one at a
   time, then run the direct gRPC program against a server that Control Center
   started and connected.
4. Record, per program, its exit code and whether it printed the coordinates and the
   distance the tutorial expects. Record "as expected" or "not as expected", never
   the printed values.
5. Postflight as for any run (no generic `stop-sdk`; stop the server Control Center
   started through Control Center), and close SA without saving.

**Fallback (proposed).** A program that fails is recorded with its exit code, its
error type and its diagnostic code. The remaining programs still run. Never edit a
tutorial during the session.

### Session 4 fixtures

The probe 1 to 4 fixtures are unchanged. Session 4 adds optional manifest keys;
leave out any fixture that does not exist, and its steps are recorded as not run.
Prepare them in the same disposable fixture job, never in `B277`.

| Key | Fixture | Used by |
| --- | --- | --- |
| `result_size_point_group` | `{ "collection", "name" }` of a representative large point group, proposed at least 20,000 points (for example from an imported ASCII point file) | `c34-count-job-group`, `p34-list-job-group` |
| `result_size_cloud` | `{ "collection", "name" }` of a representative scan cloud, proposed at least 1,000,000 points | `c34-count-job-cloud` |
| `callout_view` | `{ "collection", "name" }` of an existing callout view. The key served the retired probes 7 to 10 with the same shape; it now serves probe 35. | probe 35 |
| `tcp_fixture` | `{ "fixture": { "collection", "name" }, "measurements": [ { "collection", "group", "target" }, ... ], "tcp_in_working": [16 numbers, row by row] }`; `tcp_in_working` defaults to the identity | probe 36 |
| `robot_machine` | `{ "collection", "instrument_id", "parameter_name" }` of a robot in the job and one of its parameter names | probe 37 |

The values never appear in any output. The dry run with `--fixtures` lists each
missing key as `FIXTURE MISSING`.

### Before the session (once per target)

1. Build the package and the harness from the reviewed merge commit, as for the
   third session, with the label `0.10.1-probe-s4`. Record the merge SHA and the
   package's SHA-256 in the evidence. Pass `--harness-revision <sha>` to every
   harness command.
2. Write the fixture manifest, then check it with the dry runs. Each must exit `0`
   and print no `FIXTURE MISSING` line that you did not expect:

   ```powershell
   & $probe --phase public-api --dry-run --fixtures <manifest> --probe 34 --probe 35 --probe 36 --probe 37
   & $probe --phase worker --dry-run --fixtures <manifest> --probe 34
   & $probe --ownership watchdog-kill --dry-run --restore-sa-interaction-mode manual
   & $probe --ownership competing-client --dry-run
   ```

3. Confirm on #402 that every **Proposed** rule and decision in this section is
   agreed. Agree the go-ahead for each run separately.
4. Read the `InteractionMode` registry value, as for probe 25, and record it in the
   session notes. If the operator's mode is not Manual, decision D19 applies to O3.

**Clean state (checked, never made).** Every run below starts only when the
operator has checked all of these, and none is produced by closing or terminating a
process:

- the previous run's postflight passed, or every residual it left is recorded as
  unresolved and the maintainer agreed, between runs, to go on;
- `Test-LicensedRunnerState.ps1 -Phase Preflight` passes;
- exactly one SA of this target runs, started normally, with the fixture job copied
  over from the pristine copy, and no SA job was ever saved;
- no Briosa or `SpatialAnalyzerSDK` process runs that the operator did not start
  for this run.

A process that breaks the clean state is a residual (D7): the run does not start,
and the process is recorded, not closed to make the state clean.

### Run order and authorization points

**Proposed:** SA 2026.1.0529.7 first, then SA 2024.1.0508.5 (decision D1). Each row is one
authorization point (A1 to A9): the maintainer's go-ahead for that row, given
between runs. `competing-client` (A9) is the last run on each target: after it come
only the clean SA restart and postflight (decision D6).

| Run | What | Server started with | Expected | Abandon after |
| --- | --- | --- | --- | --- |
| A1 C1 | `fresh-install` cold start: new package folder; `--ownership release`, then `--ownership release` again on the same server (warm), each with `--step-timeout-seconds 300` (above the published bounds at the raised startup bound); stop the server; `--startup-timing --condition fresh-install --startup-timeout-seconds 60` | the standard arguments, its own log folder, `--Briosa:Worker:StartupTimeout=00:01:00` | 5 to 10 min | 20 min |
| A2 R1 | `--phase public-api --probe 34 --probe 35 --probe 36 --probe 37` | the public-phase dry run's admission and `InteractionMode` arguments; `--Briosa:Worker:ExecutionWatchdogTimeout=00:02:00`; harness `--step-timeout-seconds 150` | 15 to 25 min | 45 min |
| A3 R2 | `--phase worker --probe 34` | no server | 5 to 10 min | 30 min |
| A4 O1 | `--ownership worker-crash` | standard arguments | 3 to 6 min | 15 min |
| A5 O2 | `--ownership parent-death` | standard arguments | 2 to 5 min | 15 min |
| A6 O3 | `--ownership watchdog-kill --restore-sa-interaction-mode manual`, only under decisions D18 and D19 | the dry run's admission arguments; the default 30 s quick budget | 4 to 8 min | 20 min |
| A7 C2 | `after-reboot` cold start: restart Windows (decision D2); as C1 with the C1 package and `--condition after-reboot` | as C1 | 15 to 20 min including the restart | 40 min |
| A8 T | Optional: the four `briosa-examples` tutorials on their current pins, as in [Tutorials](#tutorials-optional-sa-2026-only) | the installed Server 0.10.0; Control Center for direct gRPC | 10 min | 20 min |
| A9 O4 | `--ownership competing-client --worker-path <package>\Briosa.Worker.exe`, optional, **the last run on this target**, followed only by a clean SA restart and postflight (decision D6) | standard arguments | 3 to 6 min | 15 min |

"Standard arguments" are those of [Phase 1](#phase-1-public-api), step 3:
`--Briosa:Desktop:Mode=Disabled`, the loopback port, and the four identity
attestation values, whose references are predefined evidence aliases (see
[Licensed session](#licensed-session-only-after-the-session-time-go-ahead)). R1 and
R2 follow Phase 1 and Phase 2 with the session 4 selection, with two differences:
each starts from the clean state above instead of Phase 1's step 1, and each ends
with [Session 4 postflight](#session-4-postflight) instead of Phase 1's step 6, so no
generic `stop-sdk` is run. For C1, C2 and O1 to O4, start the server but **do not**
run Phase 1's step 4 (`external-connect`): each `--ownership` scenario starts and connects the SDK
itself, and refuses a server whose SDK is already started. Give every run's server
its own log folder. After every run, follow
[Session 4 postflight](#session-4-postflight) before the next run's preflight.

**Abandon rule (proposed).** When a run's "Abandon after" time passes, stop the
harness with Ctrl+C. The harness then sends nothing more on that run's token,
records which step was interrupted and whether it had been sent, stops the bound
SDK generation on a cleanup budget of its own, and writes its records. Then follow
[Session 4 postflight](#session-4-postflight), which never closes a residual or an
unproven server to recover, record the run as abandoned, and go on to the next run's
authorization point. Never extend a time box during the run.

### Operator answers in session 4

Checkpoint answers are a closed vocabulary; nothing else is accepted, so no dialog
text, name, path, process id or license text can reach a record (see
[Operator checkpoints](#operator-checkpoints)). Never copy text from a dialog into a
checkpoint, the session notes or the evidence.

**License prompt.** If SpatialAnalyzer shows one, do not dismiss it and do not copy
anything from it. At a checkpoint, answer `license-prompt-abort`. At any other time,
including during probe 34, which has no checkpoints, create the signal file
`checkpoints\license-prompt-abort.signal` in the run's output folder (for example
`New-Item -ItemType File <output>\checkpoints\license-prompt-abort.signal`); the
harness creates the folder at start and prints the name. Only the file's existence
is read. Either way the run sends nothing more: no step, no cleanup, no graceful stop
of a worker, and it terminates nothing; a step already in flight ends on its own
deadline. That is unlike Ctrl+C, which cancels and then stops the bound generation.
F11 applies.

### Fallback rules (proposed)

Each rule is decided before the session, so no timed step waits on a question.

| # | Situation | Rule |
| --- | --- | --- |
| F1 | A probe run (R1, R2) stops | Recover on a clean SA. Rerun once, at the next authorization point. After `outcome-unknown-do-not-replay`, the rerun's `--probe` names only probes none of whose steps had been sent: the probe that held the unknown step, and every other probe that had started, is recorded as not finished and is not rerun in this session (D17). After any other stop, the rerun names the probes that did not finish; probe 34 then restarts from its first rung, because a rerun repeats every step of a selected probe. A second stop ends that run as recorded. |
| F2 | A probe 34 to 37 **probe** step ends with a determinate failure (an MP failure, a refused setter, a failed getter) | Nothing to do: that outcome is the answer, and the run continues. A **count check** (`c34-*`, `c35-*`) accepts only success or an MP failure, so a refused setter or a failed getter there stops the run as an unexpected disposition, which is recorded; F1 then applies. |
| F3 | A dialog opens during a probe 34 to 37 step | Dismiss it at once with OK, so that no quick budget expires, and answer `dialog-dismissed` at the step's checkpoint (probes 35 to 37), or note the step id in the session notes (probe 34). Never record the dialog's text. A license prompt is not dismissed: create the license signal at once (probe 34 has no checkpoint) or answer `license-prompt-abort` at a checkpoint; see [Operator answers in session 4](#operator-answers-in-session-4). |
| F4 | A manual fixture is missing | Do not create it during the session. The harness records the steps as `fixture-missing`. |
| F5 | An ownership scenario's start or connect fails, or it records `process-not-bound`, `bound-process-not-running`, `generation-changed-not-stopped`, `endpoint-peer-not-the-bound-server` or `worker-descendants-not-approved` | Nothing more is acted on in that run; it is recorded, and postflight leaves an unproven server and generation alone. Rerun the scenario once from the clean state at the next authorization point; a second failure is recorded as such. O4 is never rerun (F7). |
| F6 | A run's record lists a residual process, or `residual_known` is false | Follow [Session 4 postflight](#session-4-postflight). |
| F7 | In `competing-client` a gate fails: the baseline MP, the second worker's folder, its activated-SDK identity, its `ConnectEx`, the MP while it is attached, or the confirmation of its exit | The harness sends nothing more through the first client, stops the second worker (or terminates it, that process only) and stops the bound generation. Never rerun O4. |
| F8 | In `watchdog-kill` the stall ends another way (for example MP code `3` at once, or the generic watchdog code) | Recorded as unexpected. The script still watches and stops the bound generation. No rerun. |
| F9 | A cold start ends in `startup-bound-expired` at the raised bound | Recorded as a typed failure with its phases. Do not retry the cold measurement. The warm repeat still runs on the same server, as defined; if that server can no longer start the SDK, or the first start left a residual, warm is recorded as not run. |
| F10 | An operator checkpoint is not answered within 300 s | The harness stops, as it does today. No rerun of that step. |
| F11 | SA crashes or hangs, or shows a license prompt | Stop this target's session at that point and record what ran. Nothing is copied from a license prompt and it is not dismissed by the operator. The other target starts only at its own authorization point, after the maintainer has seen the record. |
| F12 | The reboot is declined, or the machine cannot be restarted | Record C2 as not run. |

### Session 4 postflight

After each run. Never run a generic `Briosa.LifecycleClient ... --scenario stop-sdk`
in session 4: it stops whatever generation the server reports, which the run may
not own.

1. **Ownership runs (C1, C2, O1 to O4).** The harness has already stopped the bound
   generation wherever it could prove the server, the connection and the generation
   were its own. If the run's stop reason is `ownership-precondition-not-met`,
   `process-not-bound`, `bound-process-not-running`,
   `generation-changed-not-stopped`, `endpoint-peer-not-the-bound-server` or
   `license-prompt-operator-abort`, or its record shows no successful stop of the
   bound generation, leave that server and its SDK generation exactly as they are and
   record both as unresolved; the maintainer decides between runs. Otherwise stop
   the server you started for this run with Ctrl+C in its own console window (after
   `parent-death` it is already gone).
   **Public run (R1).** Unless it ended with `license-prompt-operator-abort`, stop the
   server you started for it with Ctrl+C in its own console window; the server's own
   shutdown stops its worker. After a license prompt, leave the server alone and
   record it as unresolved.
   **Worker run (R2).** The harness hosted the worker and stopped it, except after a
   license prompt, when it left the worker as it was: record it as unresolved.
2. Run `Test-LicensedRunnerState.ps1 -Phase Postflight` while SA is still open, and
   keep its JSON line.
3. Close SA without saving (File > Exit > No).
4. Run `--ownership observe --observe-seconds 300` (proposed; decision D8) with a
   new output folder. It needs no server and no SA. It stops as soon as a complete
   snapshot shows no SDK, worker or server process, and otherwise lists what is left
   as residual. A process alive at its start with the same role and start time as a
   labelled process in the run's record is that process; any other is unattributed.
5. **A residual is unresolved** until it exits on its own (decision D7). Nothing is
   pre-authorized: stopping one is the operator's decision at the time, made only
   for a process the records attribute to the run (role and start time), and the
   session notes then record its label, the time and the decision. A residual the
   records do not attribute is left alone and recorded as unresolved.
6. The next run starts only from the [clean state](#before-the-session-once-per-target),
   which the operator checks and never produces by closing a Briosa or SDK process.

Postflight passes when the `observe` record ends with a complete snapshot and says
`residual: none`. An observe record whose final snapshot is incomplete or failed
cannot pass postflight; run `observe` again. Every unresolved residual, and every
process the operator stopped, is listed in the evidence.

### Recording and posting

Each command writes a JSON record and a markdown draft into its own output folder:
`probe-277-<phase>-2026.1.0529.7.*` (schema 5), `ownership-<scenario>-2026.1.0529.7.*`
(schema 1), and `startup-timing-<condition>-2026.1.0529.7.*` (schema 2). None holds a
returned value, a path, a process id, license data, or free text: checkpoint
answers are a closed vocabulary, and any diagnostic code, version, revision or field
name from the server, the worker or a log that is not on the reviewed list or in
its strict shape is recorded as `withheld`. The raw records stay with the operator.
Commit one reviewed summary, filled in from
[the session 4 evidence template](evidence/probe-session-4-template.md), as
`evidence/probe-session-4-2026.1.0529.7-<date>.md`.

Post afterwards, per target, each observation stated as this target's own:

- **#245:** probe 34's table (elements, received frame bytes, protobuf bytes,
  encoding rejected or not) for both phases, the bytes per element per list kind,
  the job's representative counts, and which rungs the worker rejected.
- **#252:** one row per ownership scenario: the bindings, the SDK process's parent
  and attribution, the worker's descendants, whether the SDK process outlived the
  worker, the server or SA, its last-seen and first-absent bounds, and every
  unresolved residual or process the operator stopped.
- **#371:** the `fresh-install`, `after-reboot` and `warm` start times with their
  phases and labels, against the 10 s default and the published
  `start_sdk_milliseconds`, each marked as a changed configuration where the bound
  was raised.
- **#402:** probes 35 to 37 per target, Start and Stop Robot/Machine Interface as
  not run (exclusive workflow), and the tutorials result or "not run".

### Decisions for the maintainer (proposed until agreed)

| # | Decision | Recommendation |
| --- | --- | --- |
| D1 | Order of targets | SA 2026 first, then SA 2024; each target's session is complete before the other starts. |
| D2 | A restart of Windows per target for `after-reboot` | Yes, once per target, just before C2 (A7), which comes after the ownership runs O1 to O3 and before the tutorials and O4 (two restarts). If declined, C2 is not run. |
| D3 | What "fresh install" means | A new extraction of the probe package into an unused folder. Neither SA nor the Briosa Installer is reinstalled. |
| D4 | The worker startup bound for the cold-start servers | `--Briosa:Worker:StartupTimeout=00:01:00`, so the whole start is measured, recorded everywhere as a changed configuration, not a stock start. Alternative: keep the stock 10 s and learn only whether it holds. |
| D5 | Time boxes | As in the run table; the abandon rule applies. |
| D6 | Whether to run `competing-client` | Optional. If run, it is the last run on each target (A9): after it come only a clean SpatialAnalyzer restart and postflight, and nothing else runs on that SA or that target. One `ConnectEx`, no MP through the second client, and every gate required, the first failed gate ending the script. If declined, it is recorded as not run. |
| D7 | Stopping a residual SDK or Briosa process | Not pre-authorized. A residual is recorded as unresolved; stopping one is the operator's decision at the time, only for a process the records attribute to the run, recorded with its label. |
| D8 | Observation windows | `--observe-seconds 120` inside scenarios and 300 s after SA closes. In session 3 on this target, two residual SDK processes were already gone when next checked, about 18 and 26 minutes after SA exited; those are check times, not measured exit times, and the operator stopped the third. One longer window (1,800 s) after `watchdog-kill` only if time allows. |
| D9 | Which manual fixtures to prepare | A large point group (20,000 or more points), a scan cloud (1,000,000 or more points) and a callout view on each target. A TCP fixture and a robot only if the maintainer can prepare them; otherwise probes 36 and 37 are not run. |
| D10 | Start and Stop Robot/Machine Interface | Record them as not run (refused by admission: exclusive workflow). No worker-phase bypass in this session. |
| D11 | The ladder | 100, 500, 2,000, 10,000 and 20,000 points (32,600 points created in `B277`). |
| D12 | Watchdog budgets | 2 min for R1, as in session 3; the default 30 s for O3. |
| D13 | The response budget under test | Settled: the 1 MiB worker response budget from #411 is on `main` and is what the session measures. |
| D14 | The tutorials | Optional, SA 2026 only. The current pins need Server 0.10.0 installed for SA 2026 with the Briosa Installer, because the 0.5.0 clients start the installed server; the machine had 0.9.2 in session 3. Run them only if 0.10.0 is already installed or the maintainer agrees to install it; otherwise record them as not run. |
| D15 | The probe build | `0.10.1-probe-s4` from this pull request's merge commit. |
| D16 | Who operates | As in session 3: Claude through computer use while the maintainer watches, never asking a question or making a judgement while a step's clock runs; every timed action is one of the agreed rules. This is not itself permission to operate a desktop: each run still needs its own go-ahead, as AGENTS.md requires. |
| D17 | Reruns | At most one rerun per run, under F1 or F5, and none of O4. A rerun never stands in for re-establishing a cold condition (a cold run that has to be repeated needs its condition established again, or it is recorded as not run), and never replays an operation whose completion was unknown: after such a stop it selects only probes that had not started at all (F1). |
| D18 | `watchdog-kill` observes the product watchdog, which terminates the worker's whole process tree | Run O3 only under the harness's gates, or not at all. The stall is sent only when the descendant census taken after the operator's checkpoint, just before dispatch, is complete, the bound worker still runs, and every process descended from it is approved (`conhost` only); otherwise the run records `worker-descendants-not-approved` or `bound-process-not-running`, sends no stall and stops the bound generation. That census is a pre-dispatch observation, not a record of what the watchdog ended. On this target, the SDK process that a watchdog-terminated worker had activated was still running after SA exited in session 1 (fixture attempt 1), session 2 (public run 1) and session 3 (runs A, A2 and B), and no session recorded its parent. Recommendation: run O3 under these gates. Alternative: record O3 as not run. |
| D19 | The operator's SA Interaction Mode is not Manual before O3 | O3 sets Manual, SA keeps it across restarts, and nothing in the scenario restores another mode. Run O3 only when the operator's mode, read before the session (step 4), is already Manual, which is what `--restore-sa-interaction-mode manual` attests; otherwise record O3 as not run. |

## Recording

Each phase writes `probe-277-<phase>-2026.1.0529.7.json` (structured, schema 5;
schema 2 added the `exclusions` object and the `excluded` step classification;
schema 3 added `interaction_mode` and, per step, the planned checkpoints, the
operator's checkpoint answers, the client deadline, and the elapsed time;
schema 4 adds `selection`, `server_interaction_mode_setting`, and
`ended_in_planned_stall`, per step `expected_stall`, `server_interaction_mode`, and
the `empty-list-argument` variant kind, and per outcome `recovery_guidance` and
`execution_state`; schema 5 adds `fixtures_missing` and `response_limits`, per step
`required_fixture`, `measures_response_size` and the `fixture-missing`
classification, and per outcome `response_size`)
and a markdown draft in the style of
[`string-setters-2026-09-28.md`](evidence/string-setters-2026-09-28.md). Review
the draft, add follow-up tasks, and commit it under `docs/testing/evidence/`
as observations, not vendor guarantees. A record made with `--exclude-probe`
must carry the justification for each exclusion before it is committed. Never
transfer one target's observations to the other target.

## Recorded sessions

| Date | Record |
| --- | --- |
| 2026-10-07 | [Licensed probe session on SA `2026.1.0529.7`](evidence/probe-277-2026.1.0529.7-2026-10-07.md). Both phases completed, with probes excluded through `--exclude-probe`. The record lists the open questions and follow-up issues. |
| 2026-10-25 | [Licensed probe session 2 on SA `2026.1.0529.7`](evidence/probe-session-2-2026.1.0529.7-2026-10-25.md). Both phases completed; probe 11 stalled public run 1 and was excluded from run 2. The record answers probes 2, 11, 17, 22, 24, and 25 and lists the follow-ups. |
