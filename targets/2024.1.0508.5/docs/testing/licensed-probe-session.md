# Bounded licensed probe session (#277)

`tools/Briosa.LicensedProbes` prepares the bounded licensed session from
[#277](https://github.com/spatialanalyzer/briosa/issues/277) for SpatialAnalyzer
`2024.1.0508.5`. It settles specific binding questions that portable tests cannot
answer. It is not broad validation of the operation surface.

The maintainer approved the probe list on 2026-10-03, and the second session's
changes (probes 24 and 25, and the re-runs of probes 2, 11, 17, and 22) on
2026-10-19. **Attaching to a running
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
| `worker` | The harness hosts one unchanged `Briosa.Worker.exe` from the same package over the existing private control protocol. No Briosa server runs. | Only what the public API intentionally cannot express: a skipped setter, a blank value the API rejects, a recased label, alternate step text. |

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
then applied: omit one setter, blank one value with the same SDK binding,
recase one label, or replace the step text. The dry run prints each sequence
exactly as it will be sent, using labels only and never values.

| # | Question | Path | Steps |
| --- | --- | --- | --- |
| 1 | USMN nominals group: omit, blank, or value | value: public. Omit and blank: worker (the API requires a value). | `p01-value`, `p01-shipped`, `p01-omit`, `p01-blank` |
| 2 | Fit seed geometry | value: public. Omit and blank: worker. | `p02-*` and the `c02-*` result-plane counts |
| 3 | Chart template chart | value: public. Omit and blank: worker. | `p03-*` |
| 4 | UI profile file | value: public. Omit and blank: worker. | `p04-*` |
| 5, 6 | Bounding plane High and Low names (SDK defaults `HighPlane` and `LowPlane`) | value: public. Omit and blank: worker. | `p05-06-*`, `p05-*`, `p06-*`, plus default-name and harness-name counts |
| 7 to 10 | Retired: the callout view list getter and setter, Circle Line Mode, and mirror frame plane substitutions | none | none (see below) |
| 11 | Mesh Orientation via `SetStringArg`, in isolation ([#309](https://github.com/spatialanalyzer/briosa/issues/309)) | public; the first probe after the fixtures, with operator checkpoints before and after | `p11`, `c11` |
| 12, 13 | Getters without evidence | public | `p12`, `p13` |
| 14 | Retired: the instrument reference list getter after a wildcard add | none | none (see below) |
| 15, 16 | Retired: the restored `DeleteCloudPointsByXYZRange` bounds | none | none (see below) |
| 17, 18 | Label case sensitivity | worker (the API cannot recase a label) | `p17-recased` (control `p02-shipped`), `p18-shipped`, `p18-recased` |
| 19 to 21 | SDK step text (#246) | worker (both texts on both targets) | `p19-*`, `p20-*`, `p21-*` |
| 22 | Vector group runtime-select getter (0.9.1), with the gesture SA's help documents ([#312](https://github.com/spatialanalyzer/briosa/issues/312)) | public; the operator completes the selection | `p22` |
| 23 | Vector group fractional percentages (0.9.1) | public | `p23` |
| 24 | R0 and R0a: does the radial delete accept the `Cloud Names` reference list that the deleted X Y Z range operation sent ([#307](https://github.com/spatialanalyzer/briosa/issues/307), [#310](https://github.com/spatialanalyzer/briosa/issues/310))? | public; destructive on disposable clouds only | `c24-*`, `p24-r0`, `p24-r0a` |
| 25 | Does SA Interaction Mode Silent suppress the observed blocking dialogs, and do prompts still work under it ([#313](https://github.com/spatialanalyzer/briosa/issues/313))? | public: zero-match wildcard, missing template chart, runtime select, Ask for String. Worker: blank High and Low plane names. | `p25-*`, `c25-*`; see [Probe 25](#probe-25-interaction-mode) |

**Probe 2 fixture ([#311](https://github.com/spatialanalyzer/briosa/issues/311)).** The fits
in probes 2 and 17 fit the coplanar group `GFIT` (the four corners of `G1`). The
2026-10-07 session fitted `G1`, whose two points at z = ±20 exceed the default
fit profile's tolerance. Both targets' MP documentation lists that case as
`PARTIAL SUCCESS` ("the fit was successful, but the tolerance was exceeded"),
which is the MP code `4` that session recorded. A blank `Fit Profile Name`, which
the shipped mapping sends when the field is absent, selects the default profile.

**Probe 22 gesture ([#312](https://github.com/spatialanalyzer/briosa/issues/312)).**
The SA 2024 User Manual (chapter 6, "F2 Selection") gives this gesture for a
selection prompt, and says that a selection in the tree needs a double-click.
During `p22`, press **F2**, select `B277::VG1` in the selection tree, click
**OK**, then press **Enter**.

Step texts for #19 to #21 are the SDK View Code forms, which are authoritative
per #246: `Compute Group to Group Orientation (Rx,Ry,Rz)`,
`Angle Between Two Planes' normals` (straight apostrophe), and
`Show / Hide Points`. They are paired with the SA 2026 installed-documentation
forms (`(Rx, Ry, Rz)`, a curly apostrophe, and `Show/Hide Points`). Each record
states which text this target ships.

The optional SDK-ownership probes from #252 (worker crash, watchdog kill, parent
death) are not included. They need their own authorization.

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

## Safety controls

- **Explicit confirmation.** A licensed run requires `--confirm-licensed-sa2024`.
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
  [Probe 25](#probe-25-interaction-mode).
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
- **No sensitive data.** Records hold labels, MP step text, dispositions, MP
  codes, output retrieval, and counts of harness-created objects. They never
  hold argument or returned values, manifest contents, paths, or license data.

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
| `GRID` (5 × 5 planar) and cloud `CPOLY` | Polygonize (#11) |
| `GDEL` (eight points D1 to D9 without D6, as in the 2026-10-07 session) and clouds `CDELR`, `CDELRA`, and the sentinel `CDELS`, each built from `GDEL` | Radial delete controls (#24) |

The worker phase creates only `G1`, `G2`, `GFIT`, `PA`, `PB`, `G3`, `R1`, and `VG1`.

### Prepared manually

These cannot be created reliably through the public API. Prepare them once per
target in a disposable job that contains a collection named, for example,
`B277M`, and never a collection named `B277`. Save the job and reopen it at the
start of each phase. **Never save the job after a phase.**

1. **USMN instruments (#1).** Two instruments in `B277M`, each with
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
  "spatial_analyzer_target": "2024.1.0508.5",
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

The manifest no longer accepts `circle_line_surface` or `callout_view`; they
served the retired probes. Remove both keys from a manifest written for the
2026-10-07 session, or the harness refuses it as an unknown property.

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
| Under Silent | the same three triggers, plus the runtime select with the probe 22 gesture | the same two blanks, with harness-name and default-name counts |
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
  checkpoints, and include it in the checkpoint answer, for example
  `none; InteractionMode registry value 0`. Whether and when SA writes it, and
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
  the initial answer or that entry. For `p25-runtime-select-silent`, use the
  probe 22 gesture. Under Silent, SA may not prompt at all; record what it does.
- **If a session stops inside probe 25** after `p25-silent`, SA stays in Silent.
  Recover as in [When a session stops](#when-a-session-stops), then run the
  same phase with `--interaction-mode-restore-only --restore-sa-interaction-mode <mode>`
  on a clean SA. That plan sends only `p25-restore`.
- `--exclude-probe 25` removes the whole block; the run then needs no restore mode.

## Operator checkpoints

A licensed run prints one progress line when it dispatches each step and one
when the step returns, with the step identifier, the outcome kind, the MP code,
and the elapsed time. At each checkpoint it writes a request file into the
`checkpoints` folder of `--output-directory`, prints its name, and waits up to
`--operator-timeout-seconds` for the matching `.response.txt` file. Write one
line to it:

- `none`, optionally followed by a note after `;`, when no dialog was open; or
- `dialog: <exact title and text>`, including a dialog you dismissed while the
  step ran, and what you did, for example
  `dialog: Template Chart Not found (dismissed with OK during the step)`.

The answer must be printable ASCII, at most 300 characters, with no backslash,
backtick, or pipe, so no path can be recorded by mistake. A refused answer is set
aside as `.refused-<n>.txt` and the harness keeps waiting. Checkpoints:

| Step | When | Why |
| --- | --- | --- |
| `p11` | before and after | #309: no dialog may be open when the polygonize is sent |
| `p24-r0`, `p24-r0a` | after | #307: record any dialog from the delete before the next step |
| `p22` | after | #312: record the gesture and any dialog |
| every probe 25 step | after | #313: record whether each trigger opened a dialog |

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
number from the approved list (the `#` column above), for example `11`. Repeat
the option to exclude several probes. It is accepted in dry runs and licensed
runs of both phases. Use it to collect the remaining probes when one probe
cannot run, for example after it stopped a session. Rerun the whole phase on a
clean SA as described in [When a session stops](#when-a-session-stops).

```powershell
& $probe --phase public-api --dry-run --fixtures C:\briosa-277\fixtures.json --exclude-probe 11
```

The harness refuses, before any client starts and with exit code `2`:

- a value that is not one canonical probe number (`011`, `+11`, and `5-6` are
  refused), a number outside the approved list, a retired probe (7 to 10,
  14 to 16), or the same number twice;
- a probe that has no steps in the selected phase, for example `17` in the
  public phase or `11` in the worker phase;
- a probe that shares a step with another probe unless both are excluded:
  `5` and `6` share `p05-06-value` (public) and `p05-06-shipped` (worker);
- an exclusion that would remove a prerequisite of a remaining step. The plan
  declares these explicitly, and the dry run prints them as
  `prerequisites (run earlier)`. `p17-recased` needs its control
  `p02-shipped`, so the worker phase refuses `--exclude-probe 2` unless
  `--exclude-probe 17` is also given;
- an exclusion that leaves no probe in the phase.

Excluding a probe removes only its probe, variant, and check steps, for example
`p11` and `c11`. Guard and fixture setup steps always run, including fixtures
that only the excluded probe uses, so the remaining probes see the same SA
state as in the full plan.

The dry-run output, the JSON record, and the markdown record each state the
exclusion, for example
`Excluded by operator: #11 — reason not recorded by harness`. Excluded steps
keep their place in the records with the classification `excluded` (markdown:
"Excluded by operator; not sent."), which is distinct from `not-run`, a planned
step that a stop or refusal prevented. A licensed run completes, with exit code
`0`, when every step that was not excluded is observed.

The harness does not record why a probe was excluded. **Every exclusion must be
justified in the committed evidence**: name the earlier record that stopped (or
the other reason), the step, and its stop reason, and state that the excluded
probe remains unanswered by this record.

## Licensed session (only after the session-time go-ahead)

Use the package and harness built from the same reviewed commit. Pass
`--harness-revision <sha>` so the record names that commit. Choose two short,
non-sensitive attestation references, as for `Test-LicensedSpatialAnalyzer.ps1`.

### Phase 1: public API

1. Close every SpatialAnalyzer, Briosa, and `SpatialAnalyzerSDK` process. Start
   exactly one SA `2024.1.0508.5` instance normally, not hidden, and open the
   fixture job.
2. Run `./eng/Test-LicensedRunnerState.ps1 -Phase Preflight`.
3. Extract the package. Start `Briosa.Server.exe` with
   `--Briosa:Desktop:Mode=Disabled`, the loopback port, and the four
   `--Briosa:SpatialAnalyzer:Identity:...:OperatorAttestation:...` values,
   exactly as `eng/Test-LicensedSpatialAnalyzer.ps1` does. Add the
   `Server admission` arguments that the public-phase dry run prints. The
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
   & $probe --phase public-api --confirm-licensed-sa2024 `
     --fixtures C:\briosa-277\fixtures.json `
     --output-directory artifacts\probe-277\public-api `
     --address http://127.0.0.1:50051 --harness-revision <sha> `
     --step-timeout-seconds 150 --restore-sa-interaction-mode <manual|automatic>
   ```

   Answer each [operator checkpoint](#operator-checkpoints) as it is printed.
   During `p22`, SA prompts for a vector group: press **F2**, select `B277::VG1`
   in the selection tree, click **OK**, then press **Enter**, within
   `--operator-timeout-seconds` (default 300).
6. Run `Briosa.LifecycleClient ... --scenario stop-sdk`, stop the server, and run
   `Test-LicensedRunnerState.ps1 -Phase Postflight`.

### Phase 2: worker

1. Close SA completely, without saving. Start one clean SA `2024.1.0508.5`
   instance and reopen the fixture job. Do not start a Briosa server.
2. Run `Test-LicensedRunnerState.ps1 -Phase Preflight`.
3. Run the worker phase with the worker from the same extracted package:

   ```powershell
   & $probe --phase worker --confirm-licensed-sa2024 `
     --fixtures C:\briosa-277\fixtures.json `
     --output-directory artifacts\probe-277\worker `
     --worker-path <extracted package>\Briosa.Worker.exe `
     --connected-sa-attested-version 2024.1.0508.5 `
     --connected-sa-attestation-reference <reference> `
     --harness-revision <sha> --restore-sa-interaction-mode <manual|automatic>
   ```

   Answer each [operator checkpoint](#operator-checkpoints) as it is printed. The
   worker's step timeout (`--step-timeout-seconds`, default 120 s) is its only
   watchdog, so dismiss a blocking dialog well within it.

4. Run `Test-LicensedRunnerState.ps1 -Phase Postflight`. Close SA without saving.

Exit code `0` means every step that was not excluded was observed as planned. `1` means the session
stopped or was refused, and the record says where and why. `2` means a usage or
manifest error, and nothing started.

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

## Recording

Each phase writes `probe-277-<phase>-2024.1.0508.5.json` (structured, schema 3;
schema 2 added the `exclusions` object and the `excluded` step classification;
schema 3 adds `interaction_mode` and, per step, the planned checkpoints, the
operator's checkpoint answers, the client deadline, and the elapsed time)
and a markdown draft in the style of
[`string-setters-2026-09-28.md`](evidence/string-setters-2026-09-28.md). Review
the draft, add follow-up tasks, and commit it under `docs/testing/evidence/`
as observations, not vendor guarantees. A record made with `--exclude-probe`
must carry the justification for each exclusion before it is committed. Never
transfer one target's observations to the other target.

## Recorded sessions

| Date | Record |
| --- | --- |
| 2026-10-07 | [Licensed probe session on SA `2024.1.0508.5`](evidence/probe-277-2024.1.0508.5-2026-10-07.md). Both phases completed, with probes excluded through `--exclude-probe`. The record lists the open questions and follow-up issues. |
| 2026-10-25 | [Licensed probe session 2 on SA `2024.1.0508.5`](evidence/probe-session-2-2024.1.0508.5-2026-10-25.md). Both phases completed; probe 11 stalled public run 1 and was excluded from run 2. The record answers probes 2, 11, 17, 22, 24, and 25 and lists the follow-ups. |
