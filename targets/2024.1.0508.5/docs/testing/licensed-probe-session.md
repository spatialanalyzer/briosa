# Bounded licensed probe session (#277)

`tools/Briosa.LicensedProbes` prepares the bounded licensed session from
[#277](https://github.com/spatialanalyzer/briosa/issues/277) for SpatialAnalyzer
`2024.1.0508.5`. It settles specific binding questions that portable tests cannot
answer. It is not broad validation of the operation surface.

The maintainer approved the probe list on 2026-10-03. **Attaching to a running
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
| 7 | Callout view list getter substitute | public | `p07` |
| 8 | Callout view list setter substitute | public | `p08` |
| 9 | Circle Line Mode via `SetStringArg` | public | `p09-circle`, `p09-line` |
| 10 | Mirror frame plane via `SetStringArg` | public | `p10` |
| 11 | Mesh Orientation via `SetStringArg` | public | `p11`, `c11` |
| 12 to 14 | Getters without evidence | public | `p12`, `p13`, `p14` |
| 15, 16 | Restored `DeleteCloudPointsByXYZRange` bounds | public, disposable clouds only | `c15-*`, `p15`, `c16-*`, `p16` (after `p15`, because #16 asks whether bounds are retained from #15) |
| 17, 18 | Label case sensitivity | worker (the API cannot recase a label) | `p17-recased` (control `p02-shipped`), `p18-shipped`, `p18-recased` |
| 19 to 21 | SDK step text (#246) | worker (both texts on both targets) | `p19-*`, `p20-*`, `p21-*` |
| 22 | Vector group runtime-select getter (0.9.1) | public; the operator selects `B277::VG1` | `p22` |
| 23 | Vector group fractional percentages (0.9.1) | public | `p23` |

Step texts for #19 to #21 are the SDK View Code forms, which are authoritative
per #246: `Compute Group to Group Orientation (Rx,Ry,Rz)`,
`Angle Between Two Planes' normals` (straight apostrophe), and
`Show / Hide Points`. They are paired with the SA 2026 installed-documentation
forms (`(Rx, Ry, Rz)`, a curly apostrophe, and `Show/Hide Points`). Each record
states which text this target ships.

The optional SDK-ownership probes from #252 (worker crash, watchdog kill, parent
death) are not included. They need their own authorization.

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
- **Destructive scope.** Only `p15` and `p16` destroy anything. Each deletes
  points from one disposable cloud that this session created in `B277`. Plan
  validation proves that the request names only that cloud and that an earlier
  setup step creates it. The session refuses the step unless that creation
  succeeded in the same session. The cloud must hold exactly its eight fixture
  points just before deletion. The worker phase never runs destructive
  operations.
- **Stop on the first unexpected disposition.** Guards, setup steps, and worker
  controls accept only success. Wildcard checks also accept an MP failure,
  because an empty match may be reported that way. Probes and variants accept
  any determinate SDK outcome, because that outcome is the answer:

  | Outcome | Execution disposition | Accepted by probes and variants |
  | --- | --- | --- |
  | `Succeeded`: MP code `2` and every output retrieved | Completed | Yes |
  | `MpFailed`: `GetMPStepResult` returned true with another MP code | Completed | Yes |
  | `OutputRetrievalFailed`: an MP code, but an output was not retrieved | Completed | Yes |
  | `ArgumentRejected`: a setter returned false; `ExecuteStep` was not called | Not started | Yes |
  | `ExecuteStepRejected`: setters returned true; `ExecuteStep` returned false | Started, outcome unknown | Never |
  | `MpResultUnavailable`: `ExecuteStep` returned true; `GetMPStepResult` returned false | Started, outcome unknown | Never |
  | `Indeterminate`: timeout, cancellation, lost channel, or untyped failure | Started, outcome unknown | Never |

  An SDK call that throws is classified by its proven phase, as the shipped
  server reports `SdkCallFaulted`: a fault before `ExecuteStep` is `NotStarted`
  and is not accepted; a fault in `ExecuteStep` or `GetMPStepResult` is
  `Indeterminate`; a getter fault after MP code `2` is `OutputRetrievalFailed`.
  The record keeps the value-free `sdk-*-faulted` diagnostic code.

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
| `G1` with points P1 to P6: four corners of a 100-unit square at z = 0, plus two points at z = ±20 | Fits, bounding planes, ensure-unique, mirror, show/hide |
| `G2`: `G1` rotated 1° about Z | Group-to-group orientation (#19) |
| `PA` (normal +Z) and `PB` (45° to `PA`) | Seed geometry, reference plane, angle (#20) |
| `G3`: measured P1 to P3, one within ±1 and two 5 units out | Relationship `R1` and vector group `VG1` (one third in tolerance) |
| `GDEL` and the disposable clouds `CDEL15` and `CDEL16` (eight points each) | #15 and #16. The deleted count distinguishes unbounded, retained, zero, and ignored bounds. |
| `GRID` (5 × 5 planar) and cloud `CPOLY` | Polygonize (#11) |
| `F1` (identity frame) | Mirror (#10) |
| `CY1` and surface `SCYL` | Circle/line extraction (#9), unless the manifest names a surface |
| Callout view `V1`, created through a text callout | #7 and #8, unless the manifest names a callout view |

The worker phase creates only `G1`, `G2`, `PA`, `PB`, `G3`, `R1`, and `VG1`.

### Prepared manually

These cannot be created reliably through the public API. Prepare them once per
target in a disposable job that contains a collection named, for example,
`B277M`, and never a collection named `B277`. Save the job and reopen it at the
start of each phase. **Never save the job after a phase.**

1. **USMN instruments (#1, #14).** Two instruments in `B277M`, each with
   observations to the same four or more targets. Simulated observations are
   acceptable. Also a nominal point group, for example `Nominals`, that has the
   same target names. A manual USMN on this data must solve.
2. **Template chart (#3).** One existing chart. Its name goes in
   `template_chart_name`.
3. **User-interface profile file (#4).** Export a UI profile to a local file.
   Record its full path and profile name. This probe changes the active UI
   profile, so restore your own profile after the session.
4. **Optional circle/line surface (#9).** A surface with at least one cylindrical
   feature, imported from a non-proprietary test part. Without it, the
   programmatic `SCYL` is used.
5. **Optional callout view (#7, #8).** Needed only if the `s-callout-V1` setup
   step fails.

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
  "ui_profile_file": "C:\\briosa-277\\profile.xml",
  "circle_line_surface": null,
  "callout_view": null
}
```

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
  refused), a number outside the approved list, or the same number twice;
- a probe that has no steps in the selected phase, for example `17` in the
  public phase or `11` in the worker phase;
- a probe that shares a step with another probe unless both are excluded:
  `5` and `6` share `p05-06-value` (public) and `p05-06-shipped` (worker);
- an exclusion that would remove a prerequisite of a remaining step. The plan
  declares these explicitly, and the dry run prints them as
  `prerequisites (run earlier)`. `p17-recased` needs its control
  `p02-shipped`, so the worker phase refuses `--exclude-probe 2` unless
  `--exclude-probe 17` is also given. `p16` needs `p15`, so the public phase
  refuses `--exclude-probe 15` unless `16` is also excluded;
- an exclusion that leaves no probe in the phase.

Excluding a probe removes only its probe, variant, and check steps, for example
`p11` and `c11`. Guard and fixture setup steps always run, including fixtures
that only the excluded probe uses, so the remaining probes see the same SA
state as in the full plan. The destructive `p15` and `p16` still require their
disposable clouds and before-counts.

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
   plus a per-operation `allow` override for each operator-guided selector. The
   harness refuses a plan that needs an exclusive workflow, which no setting can
   admit.
4. Run `Briosa.LifecycleClient --address http://127.0.0.1:50051 --scenario external-connect --timeout-seconds 90`.
   It must report `ready_for_mp`.
5. Run the public phase:

   ```powershell
   & $probe --phase public-api --confirm-licensed-sa2024 `
     --fixtures C:\briosa-277\fixtures.json `
     --output-directory artifacts\probe-277\public-api `
     --address http://127.0.0.1:50051 --harness-revision <sha>
   ```

   During `p22`, SA prompts for a vector group. Select `B277::VG1` and confirm
   within `--operator-timeout-seconds` (default 300).
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
     --harness-revision <sha>
   ```

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

## Recording

Each phase writes `probe-277-<phase>-2024.1.0508.5.json` (structured, schema 2;
schema 2 adds the `exclusions` object and the `excluded` step classification)
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
